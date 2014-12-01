using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using PCiSecurity;
using PCiServer.DEM.Cryptography;
using System.Data.SqlClient;
using System.Collections;

namespace Deployment
{
    class BBEFunctions
    {
        #region BBEFuntion
        String keySecurityKey = "security_key";
        String keyDBServer = "db_server";
        String keyDefaultDB = "default_db";
        String keyDBAdminUser = "admin_db_user";
        String keyDBAdminPwd = "admin_db_pwd";
        String keyWizUserName = "wiz_username";
        String keyWizPassword = "wiz_password";
        String keyDBUser = "db_user";
        String keyDBPassword = "db_pwd";
        String keyEncryptedSecurityKey = "encrypted_security_key";
        String keyEncryptedDBUser = "encrypted_db_user";
        String keyEncryptedDBPassword = "encrypted_db_pwd";
        String keyAdminConnectionString = "encrypted_admin_connection_string";
        String keyClientConnectionString = "encrypted_client_connection_string";
        String keyOtherTypeTemplateLar = "other_type_template_lar";
        String keyOtherTypeReportServerUrl = "other_type_report_server_url";
        String keyOtherTypeEmailFrom = "other_type_email_from";
        String keyMenuReportServerUrl = "menu_report_server_url";
        String keyBuildLocation = "build_location";
        String keyCRAWizLinkedServer = "crawiz_linked_server";
        String keyDirWebAdmin = "dir_web_admin";
        String keyAdminConnectionString_Update = "encrypted_admin_connection_string_update";

        String bbeMenuReportUtlTemplate = "http://REPORT_SERVER_URL";
        String defaultDatabase = "DEMAdmin";
        String defaultBBESitePassword = "wizuser";
        String defaultCRAWizLinkedServer = "(local)";

        Dictionary<string, string> bbeConfig = new Dictionary<string, string>();
        Dictionary<string, string> func_context_config = new Dictionary<string, string>();
        //Dictionary<string,string> wizCrediential = new Dictionary<string,string>();

        // Encryption
        // CRAWiz uses PCISecurity.dll
        // PCiServer.DEM.Cryptography.dll
        //System.DirectoryServices
        public String CRAWizEncrypt(String inputText, String encryptKey)
        {
            PCiSecurity.clsEncryption crypto = new PCiSecurity.clsEncryption();

            Console.Out.WriteLine("encryption init");
            crypto.Init();
            Console.Out.WriteLine("encrypt string");
            String outputText = crypto.EncryptString(inputText, encryptKey);
            Console.Out.WriteLine("release instance");
            crypto.Release();
            return outputText;
        }
        public String CRAWizDecrypt(String inputText, String decryptKey)
        {
            PCiSecurity.clsEncryption crypto = new PCiSecurity.clsEncryption();

            Console.Out.WriteLine("encryption init");
            crypto.Init();
            Console.Out.WriteLine("decrypt string");
            String outputText = crypto.DecryptString(inputText, decryptKey);
            Console.Out.WriteLine("release instance");
            crypto.Release();
            return outputText;
        }
        public String BBEEncrypt(String key, String inputText)
        {
            String encryptedText = PCiServer.DEM.Cryptography.CryptoFacade.EncryptString(key, inputText);
            return encryptedText;
        }
        public String BBEDecrypt(String key, String inputText)
        {
            String decryptedText = PCiServer.DEM.Cryptography.CryptoFacade.DecryptString(key, inputText);
            return decryptedText;
        }
        // End
        public void GetBBEConfigValues(String configPath)
        {
            var lines = File.ReadLines(configPath);
            foreach (String line in lines)
            {
                if (line.Trim().Length > 0)
                {
                    if (line.StartsWith("#"))
                    {
                        continue;
                    }
                    int splitIndex = line.IndexOf("=");
                    String key = line.Substring(0, splitIndex).Trim();
                    String value = line.Substring(splitIndex + 1).Trim();
                    bbeConfig.Add(key, value);
                }
            }
        }
        public String GetAdminWebConfigSecurityKey(String configFile)
        {
            String admin_web_securityKey = "";
            XmlDocument adminWebConfig = new XmlDocument();
            adminWebConfig.Load(configFile);
            String query = "configuration/appSettings/add[@key='security_key']";
            XmlNodeList found = adminWebConfig.SelectNodes(query);
            if (found.Count > 0)
            {
                admin_web_securityKey = found.Item(0).Value;
            }
            return admin_web_securityKey;
        }
        public String GetAdminConnectionString(String securityKey)
        {
            String clearConnectionString = "Data Source=" + bbeConfig[keyDBServer] + ";Initial Catalog=" + bbeConfig[keyDefaultDB] + ";User ID=" + bbeConfig[keyDBAdminUser] + ";Password=" + bbeConfig[keyDBAdminPwd];
            String encryptedPassword = BBEEncrypt(securityKey, clearConnectionString);
            return encryptedPassword;
        }
        public String GetClientConnectionString(String securityKey)
        {
            String clearConnectionString = "Data Source=" + bbeConfig[keyDBServer] + ";Initial Catalog=" + bbeConfig[keyDefaultDB] + ";User ID=" + bbeConfig[keyDBUser] + ";Password=" + bbeConfig[keyDBPassword];
            String encryptedPassword = BBEEncrypt(securityKey, clearConnectionString);
            return encryptedPassword;
        }
        public void UpdateBBEConfigXML(XmlNode nodesToSearch)
        {
            String configValueKey = "";
            foreach (XmlNode node in nodesToSearch.ChildNodes)//foreach($node in $nodesToSearch.ChildNodes)
            {
                if (node.Attributes != null && node.Attributes.Count > 0)
                {
                    foreach (XmlAttribute attribute in node.Attributes)
                    {
                        if (attribute.Value.Length > 0 && attribute.Value.StartsWith("@"))
                        {
                            //String configValueKey = "";
                            String attrValue = "";
                            attrValue = attribute.Value;
                            configValueKey = attrValue.Substring(1, attrValue.Length - 2);
                            if (bbeConfig.ContainsKey(configValueKey))
                            {
                                node.Attributes[attribute.Name].Value = bbeConfig[configValueKey];
                            }
                        }
                    }
                }
                if (node.ChildNodes.Count == 1 && node.ChildNodes.Item(0).GetType().ToString() == "System.Xml.XmlText")
                {
                    if (node.InnerText.StartsWith("@"))
                    {
                        configValueKey = "";
                        String nodeText = "";
                        nodeText = node.InnerText;
                        configValueKey = nodeText.Substring(1, nodeText.Length - 2);
                        if (bbeConfig.ContainsKey(configValueKey))
                        {
                            node.InnerText = bbeConfig[configValueKey];
                        }
                    }
                }
                else
                {
                    UpdateBBEConfigXML(node);
                }
            }
        }
        public void GetBBEConfigDic(String configValueFilePath)
        {
            //get configuration values from a text file
            GetBBEConfigValues(configValueFilePath);
            //get the Build Location
            //$buildLocation = Get-Location
            //$buildLocation = Split-Path $buildLocation
            String buildLocation = Directory.GetCurrentDirectory();
            // Store Build Location
            if (bbeConfig.ContainsKey(keyBuildLocation))
            {
                bbeConfig[keyBuildLocation] = buildLocation;
            }
            else
            {
                bbeConfig.Add(keyBuildLocation, buildLocation);
            }

            // Store CRAWiz Linked Server Name
            if (bbeConfig.ContainsKey(keyCRAWizLinkedServer))
            {
                bbeConfig[keyCRAWizLinkedServer] = defaultCRAWizLinkedServer;
            }
            else
            {
                bbeConfig.Add(keyCRAWizLinkedServer, defaultCRAWizLinkedServer);
            }

            // Set Default database AS "DemAdmin"
            if (bbeConfig.ContainsKey(keyDefaultDB))
            {
                bbeConfig[keyDefaultDB] = defaultDatabase;
            }
            else
            {
                bbeConfig.Add(keyDefaultDB, defaultDatabase);
            }

            // encrypt securitykey value without a key
            String encryptedKey = BBEEncrypt("", bbeConfig[keySecurityKey]);
            // hold the encrypted security-key value into dictionary with key "encrypted_security_key"
            if (bbeConfig.ContainsKey(keyEncryptedSecurityKey))
            {
                bbeConfig[keyEncryptedSecurityKey] = encryptedKey;
            }
            else
            {
                bbeConfig.Add(keyEncryptedSecurityKey, encryptedKey);
            }

            // Encrypt DBUsername
            String encryptedDBUserName = BBEEncrypt(bbeConfig[keySecurityKey], bbeConfig[keyDBUser]);
            // Store encrypted DBUsername
            if (bbeConfig.ContainsKey(keyEncryptedDBUser))
            {
                bbeConfig[keyEncryptedDBUser] = encryptedDBUserName;
            }
            else
            {
                bbeConfig.Add(keyEncryptedDBUser, encryptedDBUserName);
            }

            // Encrypt DBPassword
            String encryptedDBPassword = BBEEncrypt(bbeConfig[keySecurityKey], bbeConfig[keyDBPassword]);
            // Store encrypted DBPassword
            if (bbeConfig.ContainsKey(keyEncryptedDBPassword))
            {
                bbeConfig[keyEncryptedDBPassword] = encryptedDBPassword;
            }
            else
            {
                bbeConfig.Add(keyEncryptedDBPassword, encryptedDBPassword);
            }

            // get wizuser crediential from Objectstore database
            // BUG 36789 Removing OSQL dependency
            // get encrypted connection string for BBE Admin site
            String encryptedAdminConnectionString = GetAdminConnectionString(bbeConfig[keySecurityKey]);
            // Store encrypted connectionstring for admin site
            if (bbeConfig.ContainsKey(keyAdminConnectionString))
            {
                bbeConfig[keyAdminConnectionString] = encryptedAdminConnectionString;
            }
            else
            {
                bbeConfig.Add(keyAdminConnectionString, encryptedAdminConnectionString);
            }

            // get encrypted connection string for BBE Client site
            String encryptedClientConnectionString = GetClientConnectionString(bbeConfig[keySecurityKey]);
            // Store encrypted connectionstring for client site    
            if (bbeConfig.ContainsKey(keyClientConnectionString))
            {
                bbeConfig[keyClientConnectionString] = encryptedClientConnectionString;
            }
            else
            {
                bbeConfig.Add(keyClientConnectionString, encryptedClientConnectionString);
            }
        }
        public void BBEConfigXMLUpdate(String config, String product)
        {
            try
            {
                String wrapperConfigFilePath = "products\\" + product + "\\config\\" + config + ".txt";
                String configTemplateFilePath = "products\\" + product + "\\config\\config.template.xml";
                String pathPrefix = Directory.GetCurrentDirectory();
                String configFilePath = Path.Combine(pathPrefix, "products\\" + product + "\\config\\" + config + ".xml");
                // populate bbecofig dictionary to hold values to replace into xml config
                GetBBEConfigDic(wrapperConfigFilePath);
                // get absoulate path for bbe-configuration xml file
                configTemplateFilePath = Path.GetFullPath(configTemplateFilePath);
                // if config xml is already exists
                // Then remove the config xml
                if (File.Exists(configFilePath))
                {
                    File.Delete(configFilePath);
                }
                XmlDocument xmlConfig = new XmlDocument();
                xmlConfig.Load(configTemplateFilePath);
                UpdateBBEConfigXML(xmlConfig);
                xmlConfig.Save(configFilePath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
        // Database value updates
        // start
        public void UpdateOtherTypeTableValues(String parm_connection_key, Boolean parm_overright_template_lar = true, Boolean parm_overright_report_server_url = true, Boolean parm_overright_email_from = true)
        {
            SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);

            String SQL = "";
            if (parm_overright_template_lar)
            {
                SQL = SQL + "TemplateLAR = '" + bbeConfig[keyOtherTypeTemplateLar] + "', ";
            }
            if (parm_overright_report_server_url)
            {
                SQL = SQL + "ReportServerUrl = '" + bbeConfig[keyOtherTypeReportServerUrl] + "', ";
            }
            if (parm_overright_email_from)
            {
                SQL = SQL + "EmailFrom = '" + bbeConfig[keyOtherTypeEmailFrom] + "' ";
            }

            SQL = SQL.Trim();

            if (SQL.Length > 0)
            {
                if (SQL.EndsWith(","))
                {
                    SQL = SQL.Substring(0, SQL.Length - 1).Trim();
                }
                SQL = @"UPDATE DemAdmin.dbo.OtherType
                        SET " + SQL;
                SqlCommand myCommand = new SqlCommand(SQL, myConnection);

                try
                {
                    myConnection.Open();
                    try
                    {
                        myCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Out.WriteLine(ex.ToString());
                        Console.ResetColor();
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(e.ToString());
                    Console.ResetColor();
                }
                finally
                {
                    myCommand.Dispose();
                    myConnection.Close();
                }

            }
        }
        public void UpdateMenuTableValues(String parm_connection_key)
        {
            SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);

            String SQL_CreateSP = @"CREATE PROCEDURE ReplaceMenuTargetUrl
                    @TargetUrl VARCHAR(2000)
                    AS
                    UPDATE DemAdmin.dbo.Menu
                    SET TargetUrl = REPLACE(TargetUrl,'" + bbeMenuReportUtlTemplate + "',@TargetUrl)";

            String SQL_ExecuteSP = "EXECUTE dbo.ReplaceMenuTargetUrl '" + bbeConfig[keyMenuReportServerUrl] + "'";

            String SQL_DropSP = @"IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ReplaceMenuTargetUrl]') AND TYPE IN (N'P', N'PC'))
                    DROP PROCEDURE [dbo].[ReplaceMenuTargetUrl]";
            SqlCommand Cmd_SQL_CreateSP = new SqlCommand(SQL_CreateSP, myConnection);
            SqlCommand Cmd_SQL_ExecuteSP = new SqlCommand(SQL_ExecuteSP, myConnection);
            SqlCommand Cmd_SQL_DropSP = new SqlCommand(SQL_DropSP, myConnection);

            try
            {
                myConnection.Open();
                try
                {
                    Cmd_SQL_CreateSP.ExecuteNonQuery();
                    Cmd_SQL_ExecuteSP.ExecuteNonQuery();
                    Cmd_SQL_DropSP.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(ex.ToString());
                    Console.ResetColor();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine(e.ToString());
                Console.ResetColor();
            }
            finally
            {
                Cmd_SQL_CreateSP.Dispose();
                Cmd_SQL_ExecuteSP.Dispose();
                Cmd_SQL_DropSP.Dispose();
                myConnection.Close();
            }
        }
        public void UpdateAdminPassword(String parm_connection_key)
        {
            SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);
            String encryptedAdminPassword = BBEEncrypt(bbeConfig[keySecurityKey], defaultBBESitePassword);

            String Query_DemAdmin_UsrTblUpdate = @"UPDATE DemAdmin.dbo.[User] SET EncryptedPassword = '" + encryptedAdminPassword + "' WHERE Login = 'admin'";
            String Query_DemAdmin_DefAccSettingsTblUpdate = "UPDATE DemAdmin.dbo.[DefaultAccountSettings] SET [EncryptedPassword] = '" + encryptedAdminPassword + "' WHERE [ID] = 3";

            SqlCommand SQL_DemAdmin_UsrTblUpdate = new SqlCommand(Query_DemAdmin_UsrTblUpdate, myConnection);
            SqlCommand SQL_DemAdmin_DefAccSettingsTblUpdate = new SqlCommand(Query_DemAdmin_DefAccSettingsTblUpdate, myConnection);

            try
            {
                myConnection.Open();
                try
                {
                    SQL_DemAdmin_UsrTblUpdate.ExecuteNonQuery();
                    SQL_DemAdmin_DefAccSettingsTblUpdate.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(ex.ToString());
                    Console.ResetColor();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine(e.ToString());
                Console.ResetColor();
            }
            finally
            {
                SQL_DemAdmin_UsrTblUpdate.Dispose();
                SQL_DemAdmin_DefAccSettingsTblUpdate.Dispose();
                myConnection.Close();
            }
        }
        public void execute_db_post_action_new(String parm_connection_key)
        {
            Console.Out.WriteLine("Update DemAdmin.dbo.OtherType table");
            UpdateOtherTypeTableValues(parm_connection_key);
            Console.Out.WriteLine("Update DemAdmin.dbo.Menu table");
            UpdateMenuTableValues(parm_connection_key);
            Console.Out.WriteLine("Update DemAdmin.dbo.User table and DemAdmin.dbo.DefaultAccountSettings table");
            UpdateAdminPassword(parm_connection_key);
        }
        public void execute_db_post_action_update(String parm_connection_key)
        {
            // As by sending all parm_overright- parameter(s) with false, no update script will run in database
            // so, instead of sending all parm_overright- with false,
            // preferred to not calling the function for upgrade installation
            /*
                Console.Out.WriteLine ("Update DemAdmin.dbo.OtherType table" );
                UpdateOtherTypeTableValues (parm_connection_key,false,false,false);
            */
            Console.Out.WriteLine("Update DemAdmin.dbo.Menu table");
            UpdateMenuTableValues(parm_connection_key);
        }
        // End
        /* Moved from function.ps1: Start */
        public Tuple<bool, string> AnyDBExists(String commaSeperatedDBNames, String parm_connection_key)
        {
            Boolean isAnyBBEDBExists = false;
            String errorMessage = "";
            String[] dbNames = commaSeperatedDBNames.Split(',');
            SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);
            foreach (String dbName in dbNames)
            {
                String SQL_String = "IF EXISTS(SELECT name FROM MASTER.SYS.DATABASES WHERE name = '" + dbName + "') SELECT 1 ELSE SELECT 0";
                SqlCommand SQL = new SqlCommand(SQL_String, myConnection);
                try
                {
                    myConnection.Open();
                    SqlDataReader myReader = null;
                    try
                    {
                        myReader = SQL.ExecuteReader();
                        while (myReader.Read())
                        {
                            if (myReader.GetInt32(0) == 1)
                            {
                                isAnyBBEDBExists = true;
                                break;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Out.WriteLine(ex.ToString());
                        Console.ResetColor();
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(e.ToString());
                    Console.ResetColor();
                }
                finally
                {
                    myConnection.Close();
                }

            }
            //"Database : $dbName exists into Server. Please remove BBE specific database(s) [ex. DEMAdmin,LarService,LicenseInfo,WizEnterpriseUtils] from server"
            return Tuple.Create(isAnyBBEDBExists, errorMessage);
        }
        public Tuple<bool, string> BBEAllDBExists(String commaSeperatedDBNames, String parm_connection_key)
        {
            Boolean isAllBBEDBExists = false;
            String errorMessage = "";
            String[] dbNames = commaSeperatedDBNames.Split(',');
            int ExistingDBCountFlag = 0;
            SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);
            foreach (String dbName in dbNames)
            {
                String SQL_String = "IF EXISTS(SELECT name FROM MASTER.SYS.DATABASES WHERE name = '" + dbName + "') SELECT 1 ELSE SELECT 0";
                SqlCommand SQL = new SqlCommand(SQL_String, myConnection);
                try
                {
                    myConnection.Open();
                    SqlDataReader myReader = null;
                    try
                    {
                        myReader = SQL.ExecuteReader();
                        while (myReader.Read())
                        {
                            if (myReader.GetInt32(0) == 1)
                            {
                                ExistingDBCountFlag += 1;
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Out.WriteLine(ex.ToString());
                        Console.ResetColor();
                    }
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(e.ToString());
                    Console.ResetColor();
                }
                finally
                {
                    myConnection.Close();
                }
                if (ExistingDBCountFlag == dbNames.Count())
                {
                    isAllBBEDBExists = true;
                }

            }
            return Tuple.Create(isAllBBEDBExists, errorMessage);
        }
        public Tuple<bool, string> DBLoginExists(String loginName, String parm_connection_key)
        {
            Boolean isBBELoginExists = false;
            String errorMessage = "";
            SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);
            String SQL_String = "IF EXISTS(SELECT name FROM sys.server_principals WHERE name = N'" + loginName + "') SELECT 1 ELSE SELECT 0";
            SqlCommand SQL = new SqlCommand(SQL_String, myConnection);
            try
            {
                myConnection.Open();
                SqlDataReader myReader = null;
                try
                {
                    myReader = SQL.ExecuteReader();
                    while (myReader.Read())
                    {
                        if (myReader.GetInt32(0) == 1)
                        {
                            isBBELoginExists = true;
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine(loginName + " already exists in the database. Please remove " + loginName + " from the database.");
                            Console.ResetColor();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(ex.ToString());
                    Console.ResetColor();
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine(e.ToString());
                Console.ResetColor();
            }
            finally
            {
                myConnection.Close();
            }
            return Tuple.Create(isBBELoginExists, errorMessage);
        }

        public void execute_sql_script_for_new_installation(String parm_sourceLocation, String parm_connection_key)
        {
            String checkExistanceDBNames = "DEMAdmin,LarService,LicenseInfo,WizEnterpriseUtils,WizEnterpriseServiceUsage";
            //for new installation
            //check if BBE Specific any databae exists
            //if yes, then the deployment will not continue
            //unless all the BBE database manually dropped/ removed from Database Server
            Tuple<bool, string> AnyBBEDBExists = AnyDBExists(checkExistanceDBNames, parm_connection_key);
            Boolean isAnyBBEDBExists = AnyBBEDBExists.Item1;
            if (isAnyBBEDBExists)
            {
                throw new System.Exception(AnyBBEDBExists.Item2);
            }
            Tuple<bool, String> BBELoginExists = DBLoginExists(bbeConfig[keyDBUser], parm_connection_key);
            Boolean isBBELoginExists = BBELoginExists.Item1;
            if (isBBELoginExists)
            {
                throw new System.Exception(BBELoginExists.Item2);
            }
            String[] scripExecuteDBNames = new String[] { "USCensus", "Procedures", "LarService", "DEMAdmin", "LicenseInfo", "WizEnterpriseUtils" };
            foreach (String dbName in scripExecuteDBNames)
            {
                // The script listfile
                String listFile = parm_sourceLocation + "\\" + dbName + "\\" + (dbName + "DBOrder.txt");
                if (File.Exists(listFile))
                {
                    Console.Out.WriteLine("Executing Scripts for Database: $dbName. Please wait...");
                    // execute the scripts as ordered into the listed file
                    Functions functions = new Functions();
                    functions.execute_sql_script_in_order(listFile, parm_connection_key);
                    Console.Out.WriteLine("Executing Scripts for Database: " + dbName + " is complete.");
                }
                else
                {
                    Console.Out.WriteLine("Script order list file not found for Database: " + dbName);
                }
            }
            // Now create a new login and user named "BBEUser"
            // And map the user with BBE Database(s)
            String securityScriptsPath = parm_sourceLocation + "\\Security";

            if (Directory.Exists(securityScriptsPath))
            {
                string[] files = Directory.GetFiles(securityScriptsPath);
                SqlConnection myConnection = new SqlConnection(func_context_config[parm_connection_key]);
                try
                {
                    myConnection.Open();
                    foreach (String file in files)
                    {
                        string script = File.ReadAllText(file);
                        SqlCommand myCommand = new SqlCommand(script, myConnection);
                        myCommand.ExecuteNonQuery();
                    }
                    // Create BBEUser login and access permission to required database(s)
                    Console.Out.WriteLine("Creating SQL Login for BBE. Please wait...");
                    String SQL_String = "EXECUTE CreateSQLLoginForBBE '" + bbeConfig[keyDBUser] + "','" + bbeConfig[keyDBPassword] + "'";
                    SqlCommand SQL = new SqlCommand(SQL_String, myConnection);
                    SQL.ExecuteNonQuery();

                    String DopProcString = @"IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[CreateSQLLoginForBBE]') AND type in (N'P', N'PC'))
                    DROP PROCEDURE [dbo].[CreateSQLLoginForBBE]";
                    SqlCommand DopProcSQL = new SqlCommand(DopProcString, myConnection);
                    DopProcSQL.ExecuteNonQuery();

                    Console.Out.WriteLine("Creation SQL Login for BBE complete.");
                }
                catch (Exception e)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Out.WriteLine(e.ToString());
                    Console.ResetColor();
                }
                finally
                {
                    myConnection.Close();
                }
            }
            else
            {
                Console.Out.WriteLine("Security folder not found into path: $parm_sourceLocation.");
            }
        }
        /* Moved from function.ps1: End */
        public void CreateTaskScheduler(String parm_program_to_run, String parm_start_time)
        {
            String taskName = @"Wolters Kluwer Financial Services\BBE_NG\EmailScheduler";

            bool taskExist = false;
            String task = "";
            try
            {
                task = "SCHTASKS /QUERY /TN " + taskName;
                System.Diagnostics.Process ExistingProcess = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo ExistingProcessStartInfo = new System.Diagnostics.ProcessStartInfo();
                ExistingProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                ExistingProcessStartInfo.Verb = "runas";
                ExistingProcessStartInfo.FileName = "cmd.exe";
                ExistingProcessStartInfo.Arguments = task;
                ExistingProcess.StartInfo = ExistingProcessStartInfo;
                ExistingProcess.Start();
                ExistingProcess.WaitForExit();
            }
            catch (Exception ex)
            {

                task = "";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Out.WriteLine(ex.ToString());
                Console.ResetColor();
            }

            if (task.Length > 0)
            {
                taskExist = true;
            }

            // if task is not already exists
            // then create the task
            if (taskExist == false)
            {
                Console.Out.WriteLine("Creating new schedule... Please wait");
                System.Diagnostics.Process NewProcess = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo StartInfo = new System.Diagnostics.ProcessStartInfo();
                StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                StartInfo.FileName = "cmd.exe";
                StartInfo.Arguments = "SCHTASKS /CREATE /SC DAILY /TN " + taskName + " /ST " + parm_start_time + " /TR \"\'" + parm_program_to_run + "\'\"";
                NewProcess.StartInfo = StartInfo;
                NewProcess.Start();
                NewProcess.WaitForExit();
                Console.Out.WriteLine("New schedule creation complete.");
            }
        }

        /********************************************/
        #endregion
    }
}
