$keySecurityKey = "security_key"
$keyDBServer = "db_server"
$keyDefaultDB = "default_db"
$keyDBAdminUser = "admin_db_user"
$keyDBAdminPwd = "admin_db_pwd"
$keyWizUserName = "wiz_username"
$keyWizPassword = "wiz_password"
$keyDBUser = "db_user"
$keyDBPassword = "db_pwd"
$keyEncryptedSecurityKey = "encrypted_security_key"
$keyEncryptedDBUser = "encrypted_db_user"
$keyEncryptedDBPassword = "encrypted_db_pwd"
$keyAdminConnectionString = "encrypted_admin_connection_string"
$keyClientConnectionString = "encrypted_client_connection_string"
$keyOtherTypeTemplateLar = "other_type_template_lar"
$keyOtherTypeReportServerUrl = "other_type_report_server_url"
$keyOtherTypeEmailFrom = "other_type_email_from"
$keyMenuReportServerUrl = "menu_report_server_url"
$keyBuildLocation = "build_location"
$keyCRAWizLinkedServer = "crawiz_linked_server"
$keyDirWebAdmin = "dir_web_admin"
$keyAdminConnectionString_Update = "encrypted_admin_connection_string_update"

$bbeEncryptionDllPath = ".\lib\PCiServer.DEM.Cryptography.dll"
$crawizEncryptionDllPath = ".\lib\PCISecurity.dll"
$bbeEncryptionDllPath = Resolve-Path $bbeEncryptionDllPath
$crawizEncryptionDllPath = Resolve-Path $crawizEncryptionDllPath

$bbeMenuReportUtlTemplate = "http://REPORT_SERVER_URL"
$defaultDatabase = "DEMAdmin"
$defaultBBESitePassword = "wizuser"
$defaultCRAWizLinkedServer = "(local)"

$bbeConfig = $null
$bbeConfig = @{}

# Encryption
function CRAWizEncrypt
{
    param
    (
        [string]$inputText,
        [string]$encryptKey
    )
    [System.Reflection.Assembly]::LoadFile($crawizEncryptionDllPath) | Out-Null
    $crypto = New-Object PCiSecurity.clsEncryption
    
    Write-Host "encryption init"
    $crypto.Init()        
    Write-Host "encrypt string"
    $outputText = $crypto.EncryptString($inputText, $encryptKey)
    Write-Host "release instance"
    $crypto.Release()
    return $outputText
}

function CRAWizDecrypt
{
    param
    (
        [string]$inputText,
        [string]$decryptKey
    )
    
    [System.Reflection.Assembly]::LoadFile($crawizEncryptionDllPath) | Out-Null
    $crypto = New-Object PCiSecurity.clsEncryption
    
    Write-Host "encryption init"
    $crypto.Init()        
    Write-Host "decrypt string"
    $outputText = $crypto.DecryptString($inputText, $decryptKey)
    Write-Host "release instance"
    $crypto.Release()
    return $outputText
}

function BBEEncrypt
{
    param
    (
        [string]$key,
        [string]$inputText
    )
    [System.Reflection.Assembly]::LoadFile($bbeEncryptionDllPath) | Out-Null
    $encryptedText = [PCiServer.DEM.Cryptography.CryptoFacade]::EncryptString($key, $inputText)
    return $encryptedText
}

function BBEDecrypt
{
    param
    (
        [string]$key,
        [string]$inputText
    )
    [System.Reflection.Assembly]::LoadFile($bbeEncryptionDllPath) | Out-Null
    $decryptedText = [PCiServer.DEM.Cryptography.CryptoFacade]::DecryptString($key, $inputText)
    return $decryptedText
}
# End

function GetBBEConfigValues
{
    param
    (
        [string]$configPath
    )

    $lines = Get-Content -Path $configPath 
    foreach($line in $lines)
    {
        if($line.Trim().Length -gt 0)
        {
            if($line.StartsWith("#"))
            {
                continue
            }
            $splitIndex = $line.IndexOf("=")
            $key = $line.Substring(0,$splitIndex).Trim()
            $value = $line.Substring($splitIndex+1).Trim()
            $bbeConfig.Add($key,$value)
        }
    }
}

function GetAdminWebConfigSecurityKey
{
    param
    (
        $configFile    
    )
    $admin_web_securityKey = ""
    $adminWebConfig = [xml](Get-Content -Path $configFile)
    $query = "configuration/appSettings/add[@key='security_key']"
    $found = $adminWebConfig.SelectNodes($query)
    if ($found.Count -gt 0)
    {
        $admin_web_securityKey = $found.Item(0).value
    }
    return $admin_web_securityKey
}

function GetAdminConnectionString
{
    param
    (
        $wizCrediential,
        $securityKey
    )
    $clearConnectionString = "Data Source=" + $bbeConfig[$keyDBServer] + ";Initial Catalog=" + $bbeConfig[$keyDefaultDB] + ";User ID=" + $wizCrediential[$keyWizUserName] + ";Password=" + $wizCrediential[$keyWizPassword]
    $encryptedPassword =  BBEEncrypt -key $securityKey -inputText $clearConnectionString
    return $encryptedPassword
}

function GetClientConnectionString
{
    param
    (
        $securityKey
    )
    $clearConnectionString = "Data Source=" + $bbeConfig[$keyDBServer] + ";Initial Catalog=" + $bbeConfig[$keyDefaultDB] + ";User ID=" + $bbeConfig[$keyDBUser] + ";Password=" + $bbeConfig[$keyDBPassword]
    $encryptedPassword =  BBEEncrypt -key $securityKey -inputText $clearConnectionString
    return $encryptedPassword
}
## BUG 36789 Removing OSQL dependency
<#
function GetWizCrediential
{
    $wizCrediential = $null
    $wizCrediential = @{}
    if($bbeConfig.ContainsKey($keyDBServer))
    {
        if($bbeConfig.ContainsKey($keyDBAdminUser) -and $bbeConfig.ContainsKey($keyDBAdminPwd))
        {
            $SQL = "DECLARE @WizUserName VARCHAR(300)
                    DECLARE @WizUserPassword VARCHAR(300)
                    SELECT @WizUserName = [VALUE] FROM ObjectStore.dbo.PROPERTIES WHERE [PROPERTY] = 'username'
                    SELECT @WizUserPassword = [VALUE] FROM ObjectStore.dbo.PROPERTIES WHERE [PROPERTY] = 'server'
                    SELECT @WizUserName + '|' + @WizUserPassword"
            $result = osql -S $bbeConfig[$keyDBServer] -U $bbeConfig[$keyDBAdminUser] -P $bbeConfig[$keyDBAdminPwd] -d "master" -Q $SQL
            if($result.GetType().Name.ToLower() -eq "object[]")
            {
                $dbResponse = $null
                $dbResponse = @{}
                $counter = 0
                foreach($dbvalue in $result)
                {
                    if($dbvalue.Trim().Length -eq 0)
                    {
                        continue
                    }
                    if($dbvalue.Trim().StartsWith('-'))
                    {
                        continue
                    }
                    $dbResponse.Add($counter,$dbvalue.Trim())
                    $counter++
                }
                if($dbResponse.Count -eq 2)
                {
                    if($dbResponse[1].Trim().Contains("row affected"))
                    {
                        $wizCredientialValues = $dbResponse[0].Split("|")
                        $wizCrediential.Add($keyWizUserName, $wizCredientialValues[0])
                        $decryptedWizPassword = '' 
                        try
                        {
                            $decryptedWizPassword = CRAWizDecrypt -inputText $wizCredientialValues[1]
                        }
                        catch
                        {
                            Write-Host "CRAWiz security component is not installed properly." -ForegroundColor Red
                            # throw
                        }
                        $wizCrediential.Add($keyWizPassword, $decryptedWizPassword)
                    }
                }
            }
            else
            {
                throw $result
            }
        }
        else
        {
            throw "Database administrator crediential not provided"
        }
    }
    else
    {
        throw "Database server name not provided"
    }
    return $wizCrediential
}
#>
function UpdateBBEConfigXML
{
    param
    (
        $nodesToSearch
    )
    $configValueKey = ""
    foreach($node in $nodesToSearch.ChildNodes)
    {
        foreach($attribute in $node.Attributes)
        {
            if($attribute.value.Length -gt 0 -and $attribute.value.StartsWith("@"))
            {
                $configValueKey = ""
                $attrValue = ""
                $attrValue = $attribute.value
                $configValueKey = $attrValue.Substring(1, $attrValue.Length - 2)
                if($bbeConfig.ContainsKey($configValueKey))
                {
                    $node.SetAttribute($attribute.name,$bbeConfig[$configValueKey])
                }
            }
        }
        if($node.ChildNodes.Count -eq 1 -and $node.ChildNodes.Item(0).GetType().ToString() -eq "System.Xml.XmlText")
        {
            if($node.InnerText.StartsWith("@"))
            {
                $configValueKey = ""
                $nodeText = ""
                $nodeText = $node.InnerText
                $configValueKey = $nodeText.Substring(1, $nodeText.Length - 2)
                if($bbeConfig.ContainsKey($configValueKey))
                {
                    $node.InnerText = $bbeConfig[$configValueKey]
                }
            }   
        }
        else
        {                
            UpdateBBEConfigXML -nodesToSearch $node
		}
    }
}

function GetBBEConfigDic
{
    param
    (
        $configValueFilePath
    )
    
    # get configuration values from a text file
    GetBBEConfigValues -configPath $configValueFilePath
    # get the Build Location
    $buildLocation = Get-Location
    $buildLocation = Split-Path $buildLocation
    # Store Build Location
    if($bbeConfig.ContainsKey($keyBuildLocation))
    {
        $bbeConfig[$keyBuildLocation] = $buildLocation
    }
    else
    {
        $bbeConfig.Add($keyBuildLocation, $buildLocation)
    }
    
    # Store CRAWiz Linked Server Name
    if($bbeConfig.ContainsKey($keyCRAWizLinkedServer))
    {
        $bbeConfig[$keyCRAWizLinkedServer] = $defaultCRAWizLinkedServer
    }
    else
    {
        $bbeConfig.Add($keyCRAWizLinkedServer, $defaultCRAWizLinkedServer)
    }
    
    # Set Default database AS "DemAdmin"
    if($bbeConfig.ContainsKey($keyDefaultDB))
    {
        $bbeConfig[$keyDefaultDB] = $defaultDatabase
    }
    else
    {
        $bbeConfig.Add($keyDefaultDB, $defaultDatabase)
    }
    
    # encrypt securitykey value without a key
    $encryptedKey = BBEEncrypt -key "" -inputText $bbeConfig[$keySecurityKey] 
    # hold the encrypted security-key value into dictionary with key "encrypted_security_key"
    if($bbeConfig.ContainsKey($keyEncryptedSecurityKey))
    {
        $bbeConfig[$keyEncryptedSecurityKey] = $encryptedKey
    }
    else
    {
        $bbeConfig.Add($keyEncryptedSecurityKey,$encryptedKey)
    }
    
    # Encrypt DBUsername
    $encryptedDBUserName = BBEEncrypt -key $bbeConfig[$keySecurityKey] -inputText $bbeConfig[$keyDBUser]
    # Store encrypted DBUsername
    if($bbeConfig.ContainsKey($keyEncryptedDBUser))
    {
        $bbeConfig[$keyEncryptedDBUser] = $encryptedDBUserName
    }
    else
    {
        $bbeConfig.Add($keyEncryptedDBUser,$encryptedDBUserName)
    }
    
    # Encrypt DBPassword
    $encryptedDBPassword = BBEEncrypt -key $bbeConfig[$keySecurityKey] -inputText $bbeConfig[$keyDBPassword]
    # Store encrypted DBPassword
    if($bbeConfig.ContainsKey($keyEncryptedDBPassword))
    {
        $bbeConfig[$keyEncryptedDBPassword] = $encryptedDBPassword
    }
    else
    {
        $bbeConfig.Add($keyEncryptedDBPassword,$encryptedDBPassword)
    }
    
    ## get wizuser crediential from Objectstore database
    ## BUG 36789 Removing OSQL dependency
	#$wizCrediential = GetWizCrediential

    $wizCrediential = @{}
    $wizCrediential.add($keyWizUserName, $bbeConfig[$keyDBAdminUser]);
    $wizCrediential.add($keyWizPassword, $bbeConfig[$keyDBAdminPwd]);
    
    ## get encrypted connection string for BBE Admin site
    $encryptedAdminConnectionString = GetAdminConnectionString -wizCrediential $wizCrediential -securityKey $bbeConfig[$keySecurityKey]
    # Store encrypted connectionstring for admin site
    if($bbeConfig.ContainsKey($keyAdminConnectionString))
    {
        $bbeConfig[$keyAdminConnectionString] = $encryptedAdminConnectionString
    }
    else
    {
        $bbeConfig.Add($keyAdminConnectionString, $encryptedAdminConnectionString)
    }
    
    # get encrypted connection string for BBE Client site
    $encryptedClientConnectionString = GetClientConnectionString -securityKey $bbeConfig[$keySecurityKey]
    # Store encrypted connectionstring for client site    
    if($bbeConfig.ContainsKey($keyClientConnectionString))
    {
        $bbeConfig[$keyClientConnectionString] = $encryptedClientConnectionString
    }
    else
    {
        $bbeConfig.Add($keyClientConnectionString, $encryptedClientConnectionString)
    }
    # For Update installation
    # get the security key from admin web.config
    # and change the admin connectionstring with wizuser crediential
    
    # At first, check if the admin web.config is exist
    # then we're running with update deployemnt
    $adminWebConfigFile = Join-Path -Path $bbeConfig[$keyDirWebAdmin] -ChildPath "web.config"
    $admin_security_key = ""
    $encryptedAdminConnectionString_update = ""
    if(Test-Path -Path $adminWebConfigFile)
    {
        $admin_encrypted_security_key = GetAdminWebConfigSecurityKey -configFile $adminWebConfigFile
        $admin_security_key = BBEDecrypt -key "" -inputText $admin_encrypted_security_key
    }
    if($admin_security_key.Length -gt 0)
    {
        $encryptedAdminConnectionString_update = GetAdminConnectionString -wizCrediential $wizCrediential -securityKey $admin_security_key
    }
    if($bbeConfig.ContainsKey($keyAdminConnectionString_Update))
    {
        $bbeConfig[$keyAdminConnectionString_Update] = $encryptedAdminConnectionString_update
    }
    else
    {
        $bbeConfig.Add($keyAdminConnectionString_Update, $encryptedAdminConnectionString_update)
    }
}

function BBEConfigXMLUpdate
{
    param
    (
        [string]$config,
		[string]$product
    )
    try
    {
        $wrapperConfigFilePath = ".\products\$product\config\$config.txt"
        $configTemplateFilePath = ".\products\$product\config\config.template.xml"
        $pathPrefix = Get-Location
        $configFilePath = Join-Path -Path $pathPrefix -ChildPath "products\$product\config\$config.xml"
        # populate bbecofig dictionary to hold values to replace into xml config
        GetBBEConfigDic -configValueFilePath $wrapperConfigFilePath
        # get absoulate path for bbe-configuration xml file
        $configTemplateFilePath = Resolve-Path $configTemplateFilePath
        # if config xml is already exists
        # Then remove the config xml
        if(Test-Path -Path $configFilePath)
        {
            Remove-Item -Path $configFilePath -Force
        }
        $xmlConfig = [xml](Get-Content -Path $configTemplateFilePath)
        UpdateBBEConfigXML -nodesToSearch $xmlConfig
        $xmlConfig.Save($configFilePath)
    }
    catch
    {
        Write-Host $error[0] -ForegroundColor Red
    }
}

function execute_db_post_action_new
{
    param
    (
        [string]$parm_connection_key
    )
    Write-Host "Update DemAdmin.dbo.OtherType table"
    UpdateOtherTypeTableValues -parm_connection_key $parm_connection_key
    Write-Host "Update DemAdmin.dbo.Menu table"
    UpdateMenuTableValues -parm_connection_key $parm_connection_key
    Write-Host "Update DemAdmin.dbo.User table and DemAdmin.dbo.DefaultAccountSettings table"
    UpdateAdminPassword -parm_connection_key $parm_connection_key
}

function execute_db_post_action_update
{
    param
    (
        [string]$parm_connection_key
    )
    # As by sending all parm_overright- parameter(s) with false, no update script will run in database
    # so, instead of sending all parm_overright- with false,
    # preferred to not calling the function for upgrade installation
    <#
        Write-Host "Update DemAdmin.dbo.OtherType table" 
        UpdateOtherTypeTableValues -parm_connection_key $parm_connection_key -parm_overright_template_lar $false -parm_overright_report_server_url $false -parm_overright_email_from $false 
    #>
    Write-Host "Update DemAdmin.dbo.Menu table"
    UpdateMenuTableValues -parm_connection_key $parm_connection_key
}

# Database value updates
# start
function UpdateOtherTypeTableValues
{
    param
    (
        [string]$parm_connection_key,
        [bool]$parm_overright_template_lar = $true,
        [bool]$parm_overright_report_server_url = $true,
        [bool]$parm_overright_email_from = $true
    )
    $conn = $func_context.config[$parm_connection_key]
    
    $SQL = ""
    if($parm_overright_template_lar)
    {
        $SQL = $SQL + "TemplateLAR = '" + $bbeConfig[$keyOtherTypeTemplateLar] + "', "
    }
    if($parm_overright_report_server_url)
    {
        $SQL = $SQL + "ReportServerUrl = '" + $bbeConfig[$keyOtherTypeReportServerUrl] + "', "
    }
    if($parm_overright_email_from)
    {
        $SQL = $SQL + "EmailFrom = '" + $bbeConfig[$keyOtherTypeEmailFrom] + "' "
    }
    
    $SQL = $SQL.Trim()
    
    if($SQL.Length -gt 0)
    {
        if($SQL.EndsWith(','))
        {
            $SQL = $SQL.Substring(0, $SQL.Length - 1).Trim()
        }
        $SQL = "UPDATE DemAdmin.dbo.OtherType
                SET " + $SQL
        
        osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL 
    }
}

function UpdateMenuTableValues
{
    param
    (
        [string]$parm_connection_key
    )
    $conn = $func_context.config[$parm_connection_key]
    
    $SQL_CreateSP = "CREATE PROCEDURE ReplaceMenuTargetUrl
            @TargetUrl VARCHAR(2000)
            AS
            UPDATE DemAdmin.dbo.Menu
            SET TargetUrl = REPLACE(TargetUrl,'$bbeMenuReportUtlTemplate',@TargetUrl)"
            
     osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL_CreateSP
     
     $SQL_ExecuteSP = "EXECUTE dbo.ReplaceMenuTargetUrl '" + $bbeConfig[$keyMenuReportServerUrl] + "'"
     
     osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL_ExecuteSP
     
     $SQL_DropSP = "IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReplaceMenuTargetUrl]') AND TYPE IN (N'P', N'PC'))
            DROP PROCEDURE [dbo].[ReplaceMenuTargetUrl]"
            
     osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL_DropSP
}

function UpdateAdminPassword
{
    param
    (
        [string]$parm_connection_key
    )
    $conn = $func_context.config[$parm_connection_key]
    $encryptedAdminPassword = BBEEncrypt -key $bbeConfig[$keySecurityKey] -inputText $defaultBBESitePassword
    
    $SQL = "UPDATE DemAdmin.dbo.[User]
            SET EncryptedPassword = '" + $encryptedAdminPassword + "'
	        WHERE Login = 'admin'

            UPDATE DemAdmin.dbo.[DefaultAccountSettings]
            SET [EncryptedPassword] = '" + $encryptedAdminPassword + "'
            WHERE [ID] = 3"
    osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL 
}
# End

<# Moved from function.ps1: Start #>

function execute_sql_script_for_new_installation
{
    param
	(
		[string]$parm_sourceLocation,
		[string]$parm_connection_key
	)
    $checkExistanceDBNames = "DEMAdmin,LarService,LicenseInfo,WizEnterpriseUtils,WizEnterpriseServiceUsage"
    # for new installation
    # check if BBE Specific any databae exists
    # if yes, then the deployment will not continue
    # unless all the BBE database manually dropped/ removed from Database Server
    $bbeExistsDB = ""
    $isAnyBBEDBExists = AnyDBExists -commaSeperatedDBNames $checkExistanceDBNames -parm_connection_key $parm_connection_key -errorMessage ([ref]$bbeExistsDB)
    if($isAnyBBEDBExists)
    {
        throw "$bbeExistsDB"
    }
    
    $bbeExistsLogin = ""
    $isBBELoginExists = DBLoginExists -loginName $bbeConfig[$keyDBUser] -parm_connection_key $parm_connection_key -errorMessage ([ref]$bbeExistsLogin)
    if($isBBELoginExists)
    {
        throw "$bbeExistsLogin"
    }
    $scripExecuteDBNames = ("USCensus","Procedures","LarService","DEMAdmin","LicenseInfo","WizEnterpriseUtils")
    foreach($dbName in $scripExecuteDBNames)
    {
        # The script listfile
        $listFile = $parm_sourceLocation + "\" + $dbName + "\" + ($dbName + "DBOrder.txt")
        if(Test-Path -Path $listFile)
        {
            Write-Host "Executing Scripts for Database: $dbName. Please wait..."
            # execute the scripts as ordered into the listed file
            execute_sql_script_in_order -parm_listfile $listFile -parm_connection_key $parm_connection_key
            Write-Host "Executing Scripts for Database: $dbName complete"
        }
        else
        {
            Write-Host "Script order list file not found for Database: $dbName"
        }
    }
    # Now create a new login and user named "BBEUser"
    # And map the user with BBE Database(s)
    $securityScriptsPath = $parm_sourceLocation + "\Security"

    if(Test-Path -Path $securityScriptsPath -PathType container)
    {
        $files = Get-ChildItem -Path $securityScriptsPath | ?{$_.Attributes -ne 'Directory'}
        $conn = $func_context.config[$parm_connection_key]
        foreach($file in $files)
        {
            osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -i $file.FullName -n         
        }
        # Create BBEUser login and access permission to required database(s)
        Write-Host "Creating SQL Login for BBE. Please wait..."
        $SQL = "EXECUTE CreateSQLLoginForBBE '" + $bbeConfig[$keyDBUser] + "','" +  $bbeConfig[$keyDBPassword] + "'"
        osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL
        
        $SQL = "IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CreateSQLLoginForBBE]') AND type in (N'P', N'PC'))
        DROP PROCEDURE [dbo].[CreateSQLLoginForBBE]"
        
        osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL
        
        Write-Host "Creation SQL Login for BBE complete."
    }
    else
    {
        Write-Host "Security folder not found into path: $parm_sourceLocation."
    }
}

function AnyDBExists
{
    Param
    (
        [string]$commaSeperatedDBNames,
        [string]$parm_connection_key,
        [ref]$errorMessage
    )
    $return = $false
    $dbNames = $commaSeperatedDBNames.split(',')
    $conn = $func_context.config[$parm_connection_key]
    foreach($dbName in $dbNames)
    {
        $SQL = "IF EXISTS(SELECT name FROM MASTER.SYS.DATABASES WHERE name = '$dbName') SELECT 1 ELSE SELECT 0"
        $result = osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL
        # exception error
        if($result.GetType().Name.ToLower() -eq "object[]")
        {
            $dbResponse = ""
            foreach($resultItem in $result)
            {
                $dbResponse = $dbResponse + " " + $resultItem.Trim()
            }
            if($dbResponse.Contains("row affected"))
            {
                $dbValue = $result[2].Trim()
                if($dbValue -eq "1")
                {
                    $errorMessage.Value = "Database : $dbName exists into Server. Please remove BBE specific database(s) [ex. DEMAdmin,LarService,LicenseInfo,WizEnterpriseUtils] from server"   
				    $return = $true
				    break
                }
            }
            else
            {
                $errorMessage.Value = $dbResponse
                $return = $true
                break                
            }
		}
        else
        {
			$errorMessage.Value = $result
            $return = $true
            break
		}
        $result = $null
    }
    return $return
}

function DBLoginExists
{
    Param
    (
        [string]$loginName,
        [string]$parm_connection_key,
        [ref]$errorMessage
    )
    $return = $flase
    $conn = $func_context.config[$parm_connection_key]
    $SQL = "IF EXISTS(SELECT name FROM sys.server_principals WHERE name = N'$loginName') SELECT 1 ELSE SELECT 0"
    $result = osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL
    
    # exception error
    if($result.GetType().Name.ToLower() -eq "object[]")
	{
        $dbResponse = ""
        foreach($resultItem in $result)
        {
            $dbResponse = $dbResponse + " " + $resultItem.Trim()
        }
        if($dbResponse.Contains("row affected"))
        {
            $dbValue = $result[2].Trim()
            if($dbValue -eq "1")
            {
                $errorMessage.Value = "Database Login : $loginName exists into Server. Please remove $loginName from database server"
				$return = $true
            }
        }
        else
        {
            $errorMessage.Value = $dbResponse
            $return = $true
        }
	}
    else
    {
        $errorMessage.Value = $result
        $return = $true   
    }  
    return $return
}

function BBE-AllDBExists
{
    Param
    (
        [string]$commaSeperatedDBNames,
        [string]$parm_connection_key
    )
    $return = $true
    $dbNames = $commaSeperatedDBNames.split(',')
    $conn = $func_context.config[$parm_connection_key]
    foreach($dbName in $dbNames)
    {
        $SQL = "IF EXISTS(SELECT name FROM MASTER.SYS.DATABASES WHERE name = '$dbName') SELECT 1"
        $result = osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -Q $SQL
        if($result -eq $null)
        {
            $return = $false
        }
        if(!$return)
        {
            break
        }
    }
    return $return
}

<# Moved from function.ps1: End #>

function CreateTaskScheduler
{
    param
    (
        [string]$parm_program_to_run,
        [string]$parm_start_time
    )
    $taskName = "Wolters Kluwer Financial Services\BBE_NG\EmailScheduler"
    
    $taskExist = $false
    $task = ""
    Try
    {
        $task = SCHTASKS /QUERY /TN $taskName   
    }
    Catch [System.Exception]
    {
        $task = ""
    }

    if($task.length -gt 0)
    {
        $taskExist = $true
    }
    
    # if task is not already exists
    # then create the task
    if ($taskExist -eq $false)
    {
        Write-Host("Creating new schedule... Please wait")
		SCHTASKS /CREATE /SC DAILY /TN $taskName /ST $parm_start_time /TR `"`'$parm_program_to_run`'`"
        Write-Host("New schedule creation complete.")
    }
}