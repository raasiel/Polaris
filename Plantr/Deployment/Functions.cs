using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Data.SqlClient;


namespace Deployment
{
    class Functions
    {
        Dictionary<string, string> func_context_config = new Dictionary<string, string>();
        // Steps Start
        //	Working
        public void iis_action(string parm_host, string parm_action)
        {
            //iisreset $parm_host /$parm_action
            System.Diagnostics.Process ProcToStart = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo StartInfo = new System.Diagnostics.ProcessStartInfo();
            StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            StartInfo.FileName = "iisreset.exe";
            StartInfo.Arguments = parm_host + " /" + parm_action;
            StartInfo.RedirectStandardOutput = true;
            StartInfo.RedirectStandardError = true;
            StartInfo.UseShellExecute = false;
            StartInfo.Verb = "runas";
            StartInfo.WorkingDirectory = "";
            ProcToStart.StartInfo = StartInfo;
            ProcToStart.Start();
            Console.WriteLine(ProcToStart.StandardOutput.ReadToEnd());
            Console.WriteLine(ProcToStart.StandardError.ReadToEnd());
            ProcToStart.WaitForExit();
        }
        public String UnEscape(string expr)
        {
            return expr.Replace("{@}", "@");
        }
        //	Working
        public void unregister_complus_components(string parm_host, string parm_com_app)
        {
            Hashtable parameters = new Hashtable();
            parameters.Add("host", parm_host);
            parameters.Add("app", parm_com_app);
            Core core = new Core();
            core.ExecuteDeployTool("com_app_uninstall", parameters);
        }
        public void update_appsettings(string parm_section_name, string parm_web_config)
        {

            XmlDocument xmlContent = new XmlDocument();
            xmlContent.Load(func_context_config[parm_section_name]);
            XmlNode xmlsection = xmlContent.DocumentElement;
            XmlDocument webconfig = new XmlDocument();
            webconfig.Load(parm_web_config);

            foreach (XmlNode item in xmlsection.ChildNodes)
            {
                String query = "configuration/appSettings/add[@key='" + item.Attributes["key"].Value + "']";
                XmlNodeList found = webconfig.SelectNodes(query);
                if (found.Count > 0)
                {
                    found.Item(0).Attributes["value"].Value = item.Attributes["value"].Value;
                }
                else
                {
                    XmlNode appset = webconfig.SelectNodes("configuration/appSettings").Item(0);
                    XmlElement elm = webconfig.CreateElement("add");
                    elm.SetAttribute("key", item.Attributes["key"].Value);
                    elm.SetAttribute("value", item.Attributes["value"].Value);
                    appset.AppendChild(elm);
                }
            }
            webconfig.Save(parm_web_config);
        }
        public void CopyXmlNodeTree(XmlDocument doc, XmlNode nodeToCreate, XmlNode destParent)
        {
            if (nodeToCreate.LocalName.StartsWith("#comment"))
            {
                return;
            }

            if (nodeToCreate.LocalName.StartsWith("#text"))
            {
                if (nodeToCreate.Value.Length > 0)
                {
                    XmlText ndTxt = doc.CreateTextNode(nodeToCreate.Value);
                    destParent.AppendChild(ndTxt);
                    return;
                }
            }

            XmlNode newElement = doc.CreateElement(nodeToCreate.LocalName);
            destParent.AppendChild(newElement);

            foreach (XmlAttribute attrToWrite in nodeToCreate.Attributes)
            {
                newElement.Attributes["name"].Value = attrToWrite.Attributes["name"].Value;
                newElement.Attributes["value"].Value = attrToWrite.Attributes["value"].Value;
            }

            foreach (XmlNode nodeChild in nodeToCreate.ChildNodes)
            {
                CopyXmlNodeTree(doc, nodeChild, newElement);
            }
        }
        public void ensure_xml_nonunique_nodes(string parm_section_name, string parm_web_config, string parm_parent_element_xpath, string parm_element_check_expression)
        {
            XmlDocument xmlContent = new XmlDocument();
            xmlContent.Load(func_context_config[parm_section_name]);
            XmlNode xmlsection = xmlContent.DocumentElement;
            XmlDocument webconfig = new XmlDocument();
            webconfig.Load(parm_web_config);

            XmlNode ndDestParent = webconfig.SelectSingleNode(parm_parent_element_xpath);
            foreach (XmlNode ndSrc in xmlsection.ChildNodes)
            {
                Boolean found = false;
                foreach (XmlNode ndDest in ndDestParent.ChildNodes)
                {
                    XmlNode srcVal = ndSrc.SelectSingleNode(parm_element_check_expression);
                    XmlNode destVal = ndDest.SelectSingleNode(parm_element_check_expression);
                    if (srcVal.InnerText == destVal.InnerText)
                    {
                        found = true;
                    }
                }
                if (found == true)
                {
                    Console.Out.WriteLine("Found existing, not updating: " + ndSrc.InnerText);
                }
                else
                {
                    // Copy
                    CopyXmlNodeTree(webconfig, ndSrc, ndDestParent);
                }
            }
            webconfig.Save(parm_web_config);
        }
        public void ensure_xml_nonunique_nodes_extended(string parm_section_name, string parm_web_config, string parm_parent_element_xpath, string parm_element_check_expression)
        {
            parm_element_check_expression = UnEscape(parm_element_check_expression);
            parm_parent_element_xpath = UnEscape(parm_parent_element_xpath);
            XmlDocument xmlContent = new XmlDocument();
            xmlContent.Load(func_context_config[parm_section_name]);
            XmlNode xmlsection = xmlContent.DocumentElement;
            XmlNodeList srcNodes = xmlsection.SelectNodes(parm_element_check_expression);
            XmlDocument webconfig = new XmlDocument();
            webconfig.Load(parm_web_config);


            var ndDestParent = webconfig.SelectNodes(parm_parent_element_xpath + parm_element_check_expression.Substring(parm_element_check_expression.LastIndexOf("/")));
            foreach (XmlNode ndSrc in srcNodes)
            {
                Boolean found = false;
                foreach (XmlNode ndDest in ndDestParent)
                {
                    int attheratepos = parm_element_check_expression.LastIndexOf("@");
                    String srcValFound = "";
                    String destValFound = "";
                    if (attheratepos > 0)
                    {
                        String attrName = parm_element_check_expression.Substring(attheratepos + 1);
                        attrName = attrName.Substring(0, attrName.Length - 1);

                        try
                        {
                            srcValFound = ndSrc.Attributes[attrName].Value;
                            destValFound = ndDest.Attributes[attrName].Value;

                        }
                        catch
                        { }
                        finally
                        { }

                    }
                    else
                    {
                        srcValFound = ndSrc.InnerText;
                        destValFound = ndDest.InnerText;
                    }
                    if (srcValFound == destValFound && srcValFound != null)
                    {
                        found = true;
                    }
                }
                if (found == true)
                {
                    Console.Out.WriteLine("Found existing, not updating: " + ndSrc.InnerText);
                }
                else
                {
                    // Copy
                    XmlNode destination = webconfig.SelectSingleNode(parm_parent_element_xpath);
                    CopyXmlNodeTree(webconfig, ndSrc, destination);
                }
            }

            webconfig.Save(parm_web_config);
        }
        public void EnsureXmlRecursive(XmlDocument ddoc, XmlNode srcParent, XmlNode destParent)
        {
            foreach (XmlNode nodeToWrite in srcParent.ChildNodes)
            {
                Boolean found = false;
                // search in web config 
                foreach (XmlNode nodeToExist in destParent.ChildNodes)
                {
                    if (nodeToWrite.LocalName == nodeToExist.LocalName)
                    {
                        found = true;
                        // azad
                        // Set attributes
                        foreach (XmlAttribute attrToWrite in nodeToWrite.Attributes)
                        {
                            nodeToExist.Attributes["name"].Value = attrToWrite.Attributes["name"].Value;
                            nodeToExist.Attributes["value"].Value = attrToWrite.Attributes["value"].Value;
                        }
                        // check if the node contains innertext instead of nested-node/element
                        if (nodeToWrite.ChildNodes.Count == 1 && nodeToWrite.ChildNodes.Item(0).GetType().ToString() == "System.Xml.XmlText")
                        {
                            nodeToExist.InnerText = nodeToWrite.ChildNodes.Item(0).InnerText;
                        }
                        else
                        {
                            EnsureXmlRecursive(ddoc, nodeToWrite, nodeToExist);
                            //foreach (XmlNode nodeToWriteChild in nodeToWrite.ChildNodes)
                            //{
                            //    EnsureXml-Recursive $ddoc $nodeToWrite $nodeToExist $nodeToWriteChild
                            //}
                        }
                    }
                }

                if (found == false)
                {
                    XmlNode elm = ddoc.CreateElement(nodeToWrite.LocalName);
                    destParent.AppendChild(elm);
                    foreach (XmlAttribute attrToWrite in nodeToWrite.Attributes)
                    {
                        elm.Attributes["name"].Value = attrToWrite.Attributes["name"].Value;
                        elm.Attributes["value"].Value = attrToWrite.Attributes["value"].Value;
                    }
                    EnsureXmlRecursive(ddoc, nodeToWrite, elm);
                }
            }
        }
        public void ensure_xml_in_webconfig(String parm_section_name, String parm_web_config)
        {
            XmlDocument xmlContent = new XmlDocument();
            xmlContent.Load(func_context_config[parm_section_name]);
            XmlNode xmlsection = xmlContent.DocumentElement;
            XmlDocument webconfig = new XmlDocument();
            webconfig.Load(parm_web_config);

            EnsureXmlRecursive(webconfig, xmlsection, webconfig.DocumentElement);
            webconfig.Save(parm_web_config);
        }
        // Helper Functions Start
        // Azad

        //public void ExecuteOrderingDBScriptTool (String scriptSource,String dbName,String commaSeperatedExecLevel,String outputFileName)
        //{
        //    String ExeculableNameWithRelPath="products\\" + product + "\\tools\\OrderingBBEScript.exe";
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append(" "+scriptSource);
        //    sb.Append(" "+dbName);
        //    sb.Append(" "+commaSeperatedExecLevel);
        //    sb.Append(" "+outputFileName);
        //    String scr = sb.ToString();

        //    System.Diagnostics.Process ProcToStart = new System.Diagnostics.Process();
        //    System.Diagnostics.ProcessStartInfo StartInfo = new System.Diagnostics.ProcessStartInfo();
        //    StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        //    StartInfo.FileName = ExeculableNameWithRelPath;
        //    StartInfo.Arguments = scr;
        //    StartInfo.RedirectStandardOutput = true;
        //    StartInfo.RedirectStandardError = true;
        //    StartInfo.UseShellExecute = false;
        //    StartInfo.Verb = "runas";
        //    StartInfo.WorkingDirectory = "";
        //    ProcToStart.StartInfo = StartInfo;
        //    ProcToStart.Start();
        //    Console.WriteLine(ProcToStart.StandardOutput.ReadToEnd());
        //    Console.WriteLine(ProcToStart.StandardError.ReadToEnd());
        //    ProcToStart.WaitForExit();
        //}

        public void UpdateGeocodeConfig(string parm_section_name, string parm_geo_config)
        {
            XmlDocument xmlContent = new XmlDocument();
            xmlContent.Load(func_context_config[parm_section_name]);
            XmlNode xmlsection = xmlContent.DocumentElement;
            XmlDocument config = new XmlDocument();
            config.Load(parm_geo_config);

            foreach (XmlNode item in xmlsection.ChildNodes)
            {
                String query = "";
                if (item.Attributes["CensusYear"].Value == null)
                {
                    query = "GeoPathSettings/GeoPath[@CensusYear='" + item.Attributes["CensusYear"].Value + "']";
                }
                else
                {
                    query = "GeoPathSettings/MapPath";
                }
                XmlNodeList found = config.SelectNodes(query);
                if (found.Count > 0)
                {
                    found.Item(0).Attributes["DataPath"].Value = item.Attributes["DataPath"].Value;
                }
                else
                {
                    XmlNode geoSet = config.SelectNodes("GeoPathSettings").Item(0);
                    if (item.Attributes["CensusYear"] != null)
                    {
                        XmlNode elm = config.CreateElement("GeoPath");
                        elm.Attributes["CensusYear"].Value = item.Attributes["CensusYear"].Value;
                        elm.Attributes["DataPath"].Value = item.Attributes["DataPath"].Value;
                        geoSet.AppendChild(elm);
                    }
                    else
                    {
                        XmlNode elm = config.CreateElement("MapPath");
                        elm.Attributes["DataPath"].Value = item.Attributes["DataPath"].Value;
                        geoSet.AppendChild(elm);
                    }
                }
            }
            config.Save(parm_geo_config);
        }
        // Azad: end
        public bool CanRemoveFile(string item, string[] parm_exception_files, string parm_path)
        {
            Boolean result = true;

            foreach (String file in parm_exception_files)
            {
                if (parm_path + file == item)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
        // it works for only one level
        public void RemoveFilesExcept(string[] parm_exception_files, string parm_path)
        {
            String[] rootItems = Directory.GetFiles(parm_path);

            foreach (String item in rootItems)
            {
                if (CanRemoveFile(item, parm_exception_files, parm_path))
                {
                    File.Delete(item);
                }
            }
        }
        public bool CanRemoveDirectory(string item, string[] parm_exception_dirs, string parm_path)
        {
            Boolean result = true;

            foreach (String directory in parm_exception_dirs)
            {
                if (parm_path + directory == item)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }

        public void RemoveDirectoriesExcept(string item, string[] parm_exception_dirs, string parm_path)
        {
            String[] childItems = Directory.GetDirectories(item);

            foreach (String child in childItems)
            {
                if (CanRemoveDirectory(item, parm_exception_dirs, parm_path))
                {
                    Directory.Delete(item, true);
                }
            }
        }
        public bool CanCopyFile(string item, string parm_src, string[] parm_files_exception_list)
        {
            Boolean result = true;

            foreach (String file in parm_files_exception_list)
            {
                if (file == item.Replace(parm_src, ""))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public bool CanCopyDirectory(string item, string parm_src, string[] parm_no_overwrite)
        {
            Boolean result = true;

            foreach (String directory in parm_no_overwrite)
            {
                if (directory == item.Replace(parm_src, ""))
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
        // Helper Functions End
        public void clean_directory_except(string parm_path, string[] parm_exception_dirs, string[] parm_exception_files)
        {
            String[] rootItems = Directory.GetDirectories(parm_path);

            // removing directories
            foreach (String item in rootItems)
            {
                RemoveDirectoriesExcept(item, parm_exception_dirs, parm_path);
            }

            // remove files form root only
            RemoveFilesExcept(parm_exception_files, parm_path);
        }
        public void overwrite_file(string parm_src, string parm_dest)
        {
            File.Copy(parm_src, parm_dest, true);
        }

        public void CopyDirectoryInternal(string srcPath, string destPath)
        {
            if (!destPath.EndsWith(@"\"))
            {
                destPath = destPath + @"\";
            }
            DirectoryInfo dir_src = new System.IO.DirectoryInfo(srcPath);
            DirectoryInfo[] dirs = dir_src.GetDirectories();
            FileInfo[] files = dir_src.GetFiles();

            if (!Directory.Exists(destPath))
            {
                Directory.CreateDirectory(destPath);
            }

            foreach (FileInfo file in files)
            {
                string destination_file = string.Format(@"{0}{1}", destPath, file.Name);
                File.Copy(file.FullName, destination_file, true);
            }
            foreach (DirectoryInfo dir in dirs)
            {
                CopyDirectoryInternal(dir.FullName, string.Format("{0}{1}", destPath, dir.Name));
            }
        }
        public void copy_files(string parm_src, string parm_dest, string[] parm_no_overwrite, string[] parm_files_exception_list)
        {
            File.Copy(parm_src, parm_dest, true);
        }
        /*public void update_web_config_file (string parm_web_root,string parm_web_config_file_path,XmlNode parm_wiz_sentinel_settings,XmlNode parm_app_settings_updates)
        {
            //$fileWebConfigUpdates = [System.String]::Join("", ($parm_web_root.Trim(), $parm_web_config_file_path.Trim()))
            String fileWebConfigUpdates = string.Format("{0}{1}", parm_web_root.Trim(), parm_web_config_file_path.Trim());
            XmlDocument webConfigUpdates = new XmlDocument();
            webConfigUpdates.Load(fileWebConfigUpdates);

            XmlElement webConfigUpdatesRoot = webConfigUpdates.DocumentElement;	
	
            XmlNode applicationSettings = webConfigUpdatesRoot.SelectSingleNode("applicationSettings");
            XmlNode wizSentinelSettings = applicationSettings.FirstChild;
		
            //foreach( $newSetting in $newWizSentinelSettings.setting )
            foreach( var newSetting in parm_wiz_sentinel_settings )	// need to check if it works
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
        }*/
        public void register_com_plus_app(string parm_host, string parm_com_app, string parm_dll_path)
        {
            Hashtable parameters = new Hashtable();
            parameters.Add("host", parm_host);
            parameters.Add("app", parm_com_app);
            parameters.Add("file", parm_dll_path);
            Core core = new Core();
            core.ExecuteDeployTool("com_app_install", parameters);
        }
        //public void set_com_dll_intrensics_property(string parm_host,string parm_com_app,string parm_dll_path)
        //{
        //    .\tools\DeploymentUtility.exe set_com_app_value /host:$parm_host /app:$parm_com_app /prop:IISIntrinsics /value:yes /path:$parm_dll_path
        //    //	$temp_prop = "IISIntrinsics";
        //    //	$temp_value = "yes";
        //    //	Execute-DeployTool -action set_com_app_value -paramters @{"host" = $parm_host; "app" = $parm_com_app; "prop" = $temp_prop; "value" = $temp_value; "path" = $parm_dll_path}
        //}
        public void update_pciamsvr_file(string parm_data_path, string parm_web_root)
        {
            string newContentForPCiAMSvrini = @"[GeoData]
            ;Path for GeoStan
            DataPath=$parm_data_path

            ";

            string PCiAMSvrini = parm_web_root + "\\Bin\\PCiAMSvr.ini";
            File.WriteAllText(PCiAMSvrini, newContentForPCiAMSvrini);
        }
        public void copy_metadata_xml(string parm_web_root)
        {
            String PropMetaDataxml = parm_web_root + "\\Bin\\PropMetaData.xml";
            File.Copy(PropMetaDataxml, "C:\\Windows\\SysWOW64\\inetsrv\\", true);
        }
        //public void shutdown_com_plus_application (string parm_com_app)
        //{
        //    comAdmin = New-Object -com ("COMAdmin.COMAdminCatalog.1")
        //    $comAdmin.ShutdownApplication($parm_com_app)	
        //}
        public void execute_sql_script_in_order(string listFile, string parm_connection_key)
        {
            //$file = New-Object System.IO.FileInfo $parm_listfile
            FileInfo file = new FileInfo(listFile);

            String[] lines = File.ReadAllLines(file.FullName);
            //$lines | %	{	$file.Directory.FullName + $_ }

            SqlConnection conn = new SqlConnection(func_context_config[parm_connection_key]);
            try
            {
                conn.Open();
                foreach (string line in lines)
                {
                    // ignore any empty line from script file
                    if (line.Length > 0)
                    {
                        string fileToRun = file.Directory.FullName + line;
                        string script = File.ReadAllText(fileToRun);
                        SqlCommand SQLCommand = new SqlCommand(script, conn);
                        SQLCommand.ExecuteNonQuery();
                        //osql -S $conn.datasource -U $conn.username -P $conn.password -d $conn.database -i $fileToRun -n 
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.Write(e.ToString());
                Console.ResetColor();
            }
            finally
            {
                conn.Close();
            }
        }
        //public void execute_sql_script (string parm_build_files_location,string parm_db_conn_datasource,string parm_db_conn_user_name,string parm_db_conn_password,string parm_db_conn_database_name,string parm_db_script_location,string parm_db_scripts)
        //{
        //    # func_context is a special variable;
        //    $a =$func_context.config[$parm_db_scripts]
        //    foreach($b in $a)
        //    {
        //        $b
        //    }
        //    return;
        //    $scriptLocation = [System.String]::Join("", ($parm_build_files_location, $parm_db_script_location))

        //    foreach ($script in $parm_db_scripts.script)
        //    {
        //        if( $script.type -eq "folder")
        //        {
        //            $scriptPath = [System.String]::Join("", ($scriptLocation, $script.InnerText))

        //            $dirScripts = Get-ChildItem -Path "$scriptPath" -Filter "*.sql"

        //            foreach ($dirScript in $dirScripts)
        //            {
        //                Write-Host $dirScript.FullName

        //                Execute-Script($dirScript.FullName)
        //                osql -S $dataSource -U $dbUserName -P $dbPassword -d $databaseName -i $dirScript.FullName -n 
        //            }			
        //        }
        //        elseif( $script.type -eq "file")
        //        {
        //            Write-Host $scriptPath.FullName

        //            $scriptPath = [System.String]::Join("", ($scriptLocation, $script.InnerText))

        //            osql -S $dataSource -U $dbUserName -P $dbPassword -d $databaseName -i $scriptPath.FullName -n 
        //        }
        //    }
        //}
        // Azad
        // BBE Deployment specific Function
        // Only used for new depmoyment

        //Start: Creating Vitual Directory in IIS

        public void CreateUNCVirtualDirectory(string parm_siteName, string parm_vDirName, string parm_uncPath, string parm_uncUserName, string parm_uncPassword)
        {

            if (String.IsNullOrEmpty(parm_siteName))
            {
                throw new System.Exception("Must provide a Site Name");
            }

            if (String.IsNullOrEmpty(parm_vDirName))
            {
                throw new System.Exception("Must provide a Virtual Directory Name");
            }

            if (String.IsNullOrEmpty(parm_uncPath))
            {
                throw new System.Exception("Must provide a UNC path");
            }

            if (String.IsNullOrEmpty(parm_uncUserName))
            {
                throw new System.Exception("Must provide a UserName");
            }

            if (String.IsNullOrEmpty(parm_uncPassword))
            {
                throw new System.Exception("Must provide a password");
            }

            //check to see if uncpath is missing or not
            if (!(Directory.Exists(parm_uncPath)))
            {
                // Azad: Create new uncPath if not exists instead of throw error
                Directory.CreateDirectory(parm_uncPath);
                //throw "The uncPath does not exist."
            }

            IISFunctions IisFunctions = new IISFunctions();
            IisFunctions.CreateSite("IIS://Localhost/W3SVC", "555", parm_siteName, parm_uncPath);
            //String iisWebSite = Get-WmiObject -Namespace 'root\MicrosoftIISv2' -Class IISWebServerSetting -Filter "ServerComment = '$parm_siteName'"
            IisFunctions.SetSingleProperty("IIS://Localhost/W3SVC/555", "ServerBindings", ":8080:");

            //$iisVD = "IIS://localhost/$($iisWebSite.Name)/ROOT/$parm_vDirName"
            IisFunctions.CreateVDir("IIS://Localhost/W3SVC/1/Root", parm_vDirName, parm_uncPath);


            // Sandipan 20130211
            // Following is needed only on Windows 2003
            //
            int osVer = Environment.OSVersion.Version.Major;
            if (osVer == 5)
            {
                Console.Out.WriteLine("Windows 2003 detected. Flushing IIS configuration changes to disk..");
                //CScript $env:windir\system32\IisCnfg.vbs /save
            }
        }

        //End: Creating Vitual Directory in IIS

        // Steps End
        /***********************/
    }
}
