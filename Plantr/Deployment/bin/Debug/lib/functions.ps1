# Steps Start
#	Working
function iis_action 
{
	param
	(
		[string]$parm_host,
		[string]$parm_action
	)
	iisreset $parm_host /$parm_action
}

function UnEscape ($expr)
{
    return $expr.Replace("{@}","@")
}


#	Working
function unregister_complus_components 
{
	param
	(
		[string]$parm_host,
		[string]$parm_com_app
	)	
	
	Execute-DeployTool -action com_app_uninstall -paramters @{"host" = $parm_host; "app" = $parm_com_app}
}

function update_appsettings
{
    param
    (
        [string]$parm_section_name,
        [string]$parm_web_config
    )

    $xmlsection = $func_context.config[$parm_section_name]
    $webconfig = [xml] (Get-Content $parm_web_config)
    
    foreach($item in $xmlsection.ChildNodes)
    {
        $query = "configuration/appSettings/add[@key='" + $item.key +"']"
        $found = $webconfig.SelectNodes($query)
        if ($found.Count -gt 0)
        {
            $found.Item(0).SetAttribute("value",$item.value);
        }
        else
        {
            $appset = $webconfig.SelectNodes("configuration/appSettings").Item(0)
            $elm = $webconfig.createElement("add")
            $elm.SetAttribute("key",$item.key);
            $elm.SetAttribute("value",$item.value);
            $appset.AppendChild($elm);
        }
    }
    $webconfig.Save($parm_web_config)
}
    
function ensure_xml_nonunique_nodes
{
    param
    (
        [string]$parm_section_name,
        [string]$parm_web_config,
        [string]$parm_parent_element_xpath,
        [string]$parm_element_check_expression
    )
    $xmlsection = $func_context.config[$parm_section_name]
    $webconfig = [xml] (Get-Content $parm_web_config)
    
    $ndDestParent = $webconfig.SelectSingleNode($parm_parent_element_xpath);
    foreach ($ndSrc in $xmlsection.ChildNodes)
    {
        $found = $false
        foreach ($ndDest in $ndDestParent.ChildNodes)
        {
            $srcVal = $ndSrc.SelectSingleNode($parm_element_check_expression);
            $destVal = $ndDest.SelectSingleNode($parm_element_check_expression);
            if ( $srcVal.InnerText -eq $destVal.InnerText)
            {
                $found=$true
            }
        }
        if ($found -eq $true)
        {
            "Found existing, not updating: " + $ndSrc.InnerText
        }
        else
        {
            # Copy
            Copy-XmlNodeTree $webconfig $ndSrc $ndDestParent
        }
    }
    $webconfig.Save($parm_web_config)
}

function ensure_xml_nonunique_nodes_extended
{
    param
    (
        [string]$parm_section_name,
        [string]$parm_web_config,
        [string]$parm_parent_element_xpath,
        [string]$parm_element_check_expression
    )
    $parm_element_check_expression = UnEscape $parm_element_check_expression
    $parm_parent_element_xpath = UnEscape $parm_parent_element_xpath
    $xmlsection = $func_context.config[$parm_section_name]
    $srcNodes = $xmlsection.SelectNodes($parm_element_check_expression)
    $webconfig = [xml] (Get-Content $parm_web_config)

    $ndDestParent = $webconfig.SelectNodes($parm_parent_element_xpath + $parm_element_check_expression.substring($parm_element_check_expression.lastindexof("/")));
    foreach ($ndSrc in $srcNodes)
    {
        $found = $false
        foreach ($ndDest in $ndDestParent)
        {
            $attheratepos = $parm_element_check_expression.LastIndexOf("@")
            if ( $attheratepos -gt 0)
            {
                $attrName =  $parm_element_check_expression.SubString($attheratepos + 1)
                $attrName = $attrName.SubString(0,$attrName.Length-1);
                             
                $srcValFound  = ""
                $destValFound = ""
                try{
                    $srcValFound = $ndSrc.GetAttribute($attrName)
                    $destValFound = $ndDest.GetAttribute($attrName)
                    
                } 
                catch 
                {}
                finally 
                {}

            } 
            else
            {
                $srcValFound = $ndSrc.InnerText 
                $destValFound = $ndDest.InnerText
            }
            if($srcValFound -eq $destValFound -and $srcValFound -ne $null)
            {
               $found = $true 
            }            
        }
        if ($found -eq $true)
        {
            "Found existing, not updating: " + $ndSrc.InnerText
        }
        else
        {
            # Copy
            $destination = $webconfig.SelectSingleNode($parm_parent_element_xpath)            
            Copy-XmlNodeTree $webconfig $ndSrc $destination            
        }   
    }    
    
    $webconfig.Save($parm_web_config)
}


function Copy-XmlNodeTree ($doc, $nodeToCreate, $destParent)
{
    if ($nodeToCreate.LocalName.StartsWith("#comment"))
    {
        return;
    }    

    if ($nodeToCreate.LocalName.StartsWith("#text"))
    {
        if ($nodeToCreate.Value.Length -gt 0)
        {
            $ndTxt = $doc.CreateTextNode($nodeToCreate.Value);
            $destParent.AppendChild($ndTxt);
            return;
        }
    }    

    $new = $doc.CreateElement($nodeToCreate.LocalName)
    $destParent.AppendChild($new)

    foreach($attrToWrite in $nodeToCreate.Attributes)
    {
        $new.SetAttribute($attrToWrite.name,$attrToWrite.value)
    }
    
    foreach($nodeChild in $nodeToCreate.ChildNodes)
    {
        Copy-XmlNodeTree $doc $nodeChild $new
    }
}

function ensure_xml_in_webconfig 
{
    param
    (
        [string]$parm_section_name,
        [string]$parm_web_config
    )
    
    $xmlsection = $func_context.config[$parm_section_name]
    $webconfig = [xml] (Get-Content $parm_web_config)
    
    Ensure-Xml-Recursive $webconfig $xmlsection $webconfig.DocumentElement 
    $webconfig.Save($parm_web_config)
}

function Ensure-Xml-Recursive ($ddoc,$srcParent,$destParent)
{
    foreach ($nodeToWrite in $srcParent.ChildNodes)
    {
        $found = $false
        # search in web config 
        foreach ($nodeToExist in $destParent.ChildNodes)
        {
            if ($nodeToWrite.LocalName -eq $nodeToExist.LocalName)
            {
                $found = $true
                # azad
                # Set attributes
                foreach($attrToWrite in $nodeToWrite.Attributes)
                {
                    $nodeToExist.SetAttribute($attrToWrite.name,$attrToWrite.value)
                }
				# check if the node contains innertext instead of nested-node/element
                if($nodeToWrite.ChildNodes.Count -eq 1 -and $nodeToWrite.ChildNodes.Item(0).GetType().ToString() -eq "System.Xml.XmlText")
                {
                    $nodeToExist.InnerText = $nodeToWrite.ChildNodes.Item(0).InnerText   
                }
                else
                {                
					foreach ($nodeToWriteChild in $nodeToWrite.ChildNodes)
					{
						Ensure-Xml-Recursive $ddoc $nodeToWrite $nodeToExist $nodeToWriteChild
					}
				}
			}
        }
    
        if ($found -eq $false)
        {
            $elm = $ddoc.CreateElement($nodeToWrite.LocalName)
            $destParent.AppendChild($elm)
            foreach($attrToWrite in $nodeToWrite.Attributes)
            {
                $elm.SetAttribute($attrToWrite.name,$attrToWrite.value)
            }
            Ensure-Xml-Recursive $ddoc $nodeToWrite $elm
        }
    }
}

function clean_directory_except
{
	param 
	(
		[string]$parm_path, 
		$parm_exception_dirs, 
		$parm_exception_files
	)
	
	$rootItems = Get-ChildItem "$parm_path"

	# removing directories
	foreach ($item in $rootItems)
	{
		Remove-DirectoriesExcept -item $item -parm_exception_dirs $parm_exception_dirs -parm_path $parm_path
	}
	
	# remove files form root only
	Remove-FilesExcept $parm_exception_files $parm_path
}

function overwrite_file
{
	param
	(
		[string]$parm_src,
		[string]$parm_dest
	)
	Copy-Item $parm_src -Destination $parm_dest -Force
}

function overwrite_files
{
	param
	(
		[string]$parm_src,
		[string]$parm_dest
	)
	Copy-DirectoryInternal $parm_src $parm_src $parm_dest $parm_dest
}

function Copy-DirectoryInternal($rootPath,$srcPath,$destPathRoot,$destPath)
{
	$dir_src = new-Object System.IO.DirectoryInfo $srcPath
	$dirs = $dir_src.GetDirectories();
	$files = $dir_src.GetFiles();
	foreach ( $srcfile in $files)
	{ 
		Copy-Item $srcfile.FullName -Destination $destPathRoot -Recurse -Force
	}
	foreach ( $dir in $dirs) 
	{ 		
		Copy-Item $dir.FullName -Destination $destPathRoot -Recurse -Force
	}

	
}

function copy_files
{
	param
	(
		[string]$parm_src, 
		[string]$parm_dest, 
		$parm_no_overwrite,
		$parm_files_exception_list
	)
	
	Copy-Item $srcPath -Destination $destPathRoot -Recurse -Force
}

function update_web_config_file
{
	param
	(
		[string]$parm_web_root, 
		[string]$parm_web_config_file_path, 
		$parm_wiz_sentinel_settings,
		$parm_app_settings_updates
	)	
	
	$fileWebConfigUpdates = [System.String]::Join("", ($parm_web_root.Trim(), $parm_web_config_file_path.Trim()))	
	$webConfigUpdates =[XML](Get-Content $fileWebConfigUpdates)
	$webConfigUpdatesRoot = $webConfigUpdates.get_DocumentElement()	
	
	$applicationSettings = $webConfigUpdatesRoot.applicationSettings	
	$wizSentinelSettings = $applicationSettings.FirstChild
		
	#foreach( $newSetting in $newWizSentinelSettings.setting )
	foreach( $newSetting in $parm_wiz_sentinel_settings )	# need to check if it works
	{	
		foreach( $setting in $wizSentinelSettings.setting )
		{		
			if($newSetting.name -eq $setting.name)
			{
				$setting.value = $newSetting.value
				break;
			}
		}
	}
	
	foreach( $newSetting in $parm_app_settings_updates.add )
	{	
		foreach( $setting in $webConfigUpdatesRoot.appSettings.add )
		{		
			if($newSetting.key -eq $setting.key)
			{
				$setting.value = $newSetting.value
				break;
			}
		}
	}	
	$webConfigUpdates.Save($fileWebConfigUpdates);
}

function register_com_plus_app
{   
	param
	(
		[string]$parm_host,
		[string]$parm_com_app,
		[string]$parm_dll_path
	)
	
	Execute-DeployTool -action com_app_install -paramters @{"host" = $parm_host; "app" = $parm_com_app; "file" = $parm_dll_path}
}

function set_com_dll_intrensics_property
{
	param
	(
		[string]$parm_host,
		[string]$parm_com_app,
		[string]$parm_dll_path
	)
	.\tools\DeploymentUtility.exe set_com_app_value /host:$parm_host /app:$parm_com_app /prop:IISIntrinsics /value:yes /path:$parm_dll_path
	#	$temp_prop = "IISIntrinsics";
	#	$temp_value = "yes";
	#	Execute-DeployTool -action set_com_app_value -paramters @{"host" = $parm_host; "app" = $parm_com_app; "prop" = $temp_prop; "value" = $temp_value; "path" = $parm_dll_path}
}

function update_pciamsvr_file
{
	param
	(
		[string]$parm_data_path,
		[string]$parm_web_root
	)
	
	$newContentForPCiAMSvrini = "[GeoData]
;Path for GeoStan
DataPath=$parm_data_path

"

	$PCiAMSvrini = $parm_web_root+"\Bin\PCiAMSvr.ini"
	Set-Content -path $PCiAMSvrini $newContentForPCiAMSvrini	
}

function copy_metadata_xml
{
	param
	(
		[string]$parm_web_root
	)
	
	$PropMetaDataxml = $parm_web_root + "\Bin\PropMetaData.xml"  
	Copy-Item -Path $PropMetaDataxml -Destination "C:\Windows\SysWOW64\inetsrv\" -Force	
}

function shutdown_com_plus_application 
{
	param
	(
		[string]$parm_com_app
	)	
	
	$comAdmin = New-Object -com ("COMAdmin.COMAdminCatalog.1")
	$comAdmin.ShutdownApplication($parm_com_app)	
}

function execute_sql_script_in_order
{
	param
	(
		[string]$parm_listfile,
		[string]$parm_connection_key
	)
	
	$file = New-Object System.IO.FileInfo $parm_listfile

	$lines = [System.IO.File]::ReadAllLines($file.FullName)
	#$lines | %	{	$file.Directory.FullName + $_ }
	
	$conn = $func_context.config[$parm_connection_key]
	foreach ($line in $lines)
	{
        # ignore any empty line from script file
        if($line.Length -gt 0)
        {
    		$fileToRun = $file.Directory.FullName + $line
    		osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -i $fileToRun -n 
        }
	}
}

function execute_sql_script 
{
	param
	(
		[string]$parm_build_files_location,
		[string]$parm_db_conn_datasource,
		[string]$parm_db_conn_user_name,
		[string]$parm_db_conn_password,
		[string]$parm_db_conn_database_name,
		[string]$parm_db_script_location,
		#$parm_db_scripts
		[string]$parm_db_scripts
	)
	
	# func_context is a special variable;
	$a =$func_context.config[$parm_db_scripts]
	foreach($b in $a)
	{
		$b
	}
	return;
	$scriptLocation = [System.String]::Join("", ($parm_build_files_location, $parm_db_script_location))
	
	foreach ($script in $parm_db_scripts.script)
	{
		if( $script.type -eq "folder")
		{
			$scriptPath = [System.String]::Join("", ($scriptLocation, $script.InnerText))
			
			$dirScripts = Get-ChildItem -Path "$scriptPath" -Filter "*.sql"
			
			foreach ($dirScript in $dirScripts)
			{
				Write-Host $dirScript.FullName
				
				Execute-Script($dirScript.FullName)
				osql -S $dataSource -U $dbUserName -P $dbPassword -d $databaseName -i $dirScript.FullName -n 
			}			
		}
		elseif( $script.type -eq "file")
		{
			Write-Host $scriptPath.FullName
			
			$scriptPath = [System.String]::Join("", ($scriptLocation, $script.InnerText))
			
			osql -S $dataSource -U $dbUserName -P $dbPassword -d $databaseName -i $scriptPath.FullName -n 
		}
	}
}

# Azad
# BBE Deployment specific Function
# Only used for new depmoyment

#Start: Creating Vitual Directory in IIS

function CreateUNCVirtualDirectory
{
    param
	(
		[string]$parm_siteName,
        [string]$parm_vDirName,
        [string]$parm_uncPath,
        [string]$parm_uncUserName,
        [string]$parm_uncPassword
	)
    
    if(!$parm_siteName)
    {
        throw "Must provide a Site Name"
    }
    
    if(!$parm_vDirName)
    {
        throw "Must provide a Virtual Directory Name"
    }
    
    if(!$parm_uncPath)
    {
        throw "Must provide a UNC path"
    }
    
    if(!$parm_uncUserName)
    {
        throw "Must provide a UserName"
    }
    
    if(!$parm_uncPassword)
    {
        throw "Must provide a password"
    }
    
    #check to see if uncpath is missing or not
    if(!(test-path $parm_uncPath))
    {
        # Azad: Create new uncPath if not exists instead of throw error
        New-Item -type directory -Path $parm_uncPath
        #throw "The uncPath does not exist."
    }
    
    $iisWebSite = Get-WmiObject -Namespace 'root\MicrosoftIISv2' -Class IISWebServerSetting -Filter "ServerComment = '$parm_siteName'"
    
    $iisVD = "IIS://localhost/$($iisWebSite.Name)/ROOT/$parm_vDirName"
    
    if ([System.DirectoryServices.DirectoryEntry]::Exists($iisVD)) 
    {
        throw "Virtual Directory Already Exists."
    }
    
	# create virtual directory
	"Creating IIS Virtual Directory '$parm_vDirName' under web site '$parm_siteName'..."
    $objIIS = new-object System.DirectoryServices.DirectoryEntry("IIS://localhost/" + $iisWebSite.Name + "/Root")
    $children = $objIIS.psbase.children
    $vDir = $children.add($parm_vDirName,$objIIS.psbase.SchemaClassName)
    $vDir.psbase.CommitChanges()
	
	"Setting Virtual Directory path & credential..."
    $vDir.Path = $parm_uncPath
    $vDir.UNCUserName = $parm_uncUserName
    $vDir.UNCPassword = $parm_uncPassword
		
    $vDir.psbase.CommitChanges()
	#"IIS Virtual Directory created."
	
	# convert virtual directory to web application
	"Converting IIS Virtual Directory '$parm_vDirName' under web site '$parm_siteName' to a Web Application..."
	$newWebApp = Get-WmiObject -Namespace 'root\MicrosoftIISv2' -Class IISWebVirtualDir | Where-Object {$_.Name -eq  $iisWebSite.Name + '/Root/' + $vDir.Name}	
	$newWebApp.AppCreate2(2)
	"Web Application convertion done."
    
    # Sandipan 20130211
    # Following is needed only on Windows 2003
    ##
    $osVer = [Environment]::OSVersion.Version.Major
    if($osVer -eq 5)
    {
        "Windows 2003 detected. Flushing IIS configuration changes to disk.."
        CScript $env:windir\system32\IisCnfg.vbs /save
    }
}

#End: Creating Vitual Directory in IIS

# Steps End

# Helper Functions Start
# Azad

function Execute-OrderingDBScriptTool 
{
	param
	(
		[string] $scriptSource,
        [string] $dbName,
        [string] $commaSeperatedExecLevel,
        [string] $outputFileName
	)

	$sb = new-Object System.Text.StringBuilder	
	$sb.Append(".\products\$product\tools\OrderingBBEScript.exe") | Out-Null
    $sb.Append(" $scriptSource") | Out-Null
    $sb.Append(" $dbName") | Out-Null
    $sb.Append(" $commaSeperatedExecLevel") | Out-Null
    $sb.Append(" $outputFileName") | Out-Null

	$scr = $scriptBlock = [Scriptblock]::Create($sb.ToString())
    Invoke-Command -ScriptBlock $scr 
}

function UpdateGeocodeConfig
{
    param
    (
        [string]$parm_section_name,
        [string]$parm_geo_config
    )
    
    $xmlsection = $func_context.config[$parm_section_name]
    $config = [xml] (Get-Content $parm_geo_config)
    
    foreach($item in $xmlsection.ChildNodes)
    {
        if($item.CensusYear -ne $null)
        {
            $query = "GeoPathSettings/GeoPath[@CensusYear='" + $item.CensusYear +"']";
        }
        else
        {
            $query = "GeoPathSettings/MapPath";
        }
        $found = $config.SelectNodes($query)
        if ($found.Count -gt 0)
        {
            $found.Item(0).SetAttribute("DataPath",$item.DataPath);
        }
        else
        {
            $geoSet = $config.SelectNodes("GeoPathSettings").Item(0)
            if($item.CensusYear -ne $null)
            {
                $elm = $config.createElement("GeoPath");
                $elm.SetAttribute("CensusYear",$item.CensusYear);
                $elm.SetAttribute("DataPath",$item.DataPath);
            }
            else
            {
                $elm = $config.createElement("MapPath");
                $elm.SetAttribute("DataPath",$item.DataPath);
            }
            $geoSet.AppendChild($elm);
        }
    }
    $config.Save($parm_geo_config)
}
# Azad: end

# it works for only one level
function Remove-FilesExcept($parm_exception_dirs, $parm_path)
{
	$rootItems = Get-ChildItem "$parm_path"

	foreach ($item in $rootItems)
	{
		if(-not $item.PsIsContainer)
		{
			if(Can-RemoveFile $item $parm_exception_files $parm_path)
			{
				remove-item $item.FullName -force	
			}
		}
	}
}

function Remove-DirectoriesExcept 
{
	param
	(
		$item, 
		$parm_exception_dirs, 
		$parm_path
	)
	if($item.PsIsContainer)
	{
		$childItems = Get-ChildItem $item.FullName
		
		foreach ($child in $childItems)
		{
			Remove-DirectoriesExcept $child $parm_exception_dirs $parm_path
		}
		
		if(Can-RemoveDirectory $item $parm_exception_dirs $parm_path)
		{
			remove-item $item.FullName -Recurse -force	
		}		
	}
}

function Can-RemoveDirectory($item, $parm_exception_dirs, $parm_path)
{
	$result = $true;

	foreach( $directory in $parm_exception_dirs)
	{
		if( "$parm_path$directory" -eq $item.FullName)
		{
			$result = $false;
			break;
		}
	}	
	
	return $result;
}

function Can-RemoveFile($item, $parm_exception_files, $parm_path)
{
	$result = $true;

	#foreach( $file in $filesNotToRemove.File)
	foreach( $file in $parm_exception_files)	
	{			
		if( "$parm_path$file" -eq $item.FullName)
		{
			$result = $false;
			break;
		}
	}
	return $result;
}

function Can-CopyFile($item, $parm_src, $parm_files_exception_list)
{
	$result = $true;

	foreach( $file in $parm_files_exception_list)
	{			
		if( "$file" -eq $item.FullName.Replace($parm_src, ""))
		{
			$result = $false;
			break;
		}
	}
	return $result;
}

function Can-CopyDirectory($item, $parm_src, $parm_no_overwrite)
{
	$result = $true;

	foreach( $directory in $parm_no_overwrite)
	{
		if( "$directory" -eq $item.FullName.Replace($parm_src, ""))
		{
			$result = $false;
			break;
		}
	}	
	return $result;
}
# Helper Functions End
