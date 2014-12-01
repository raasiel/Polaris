﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.DirectoryServices;

namespace Deployment
{
    class IISFunctions
    {
        /**************************************************************/
        //CreateSite("IIS://Localhost/W3SVC", "555", "MySite", "D:\\Inetpub\\Wwwroot");
        //SetSingleProperty("IIS://Localhost/W3SVC/555", "ServerBindings", ":8080:");
        //CreateVDir("IIS://Localhost/W3SVC/1/Root", "MyVDir", "D:\\Inetpub\\Wwwroot");
        /**************************************************************/
        
        public void CreateSite(string metabasePath, string siteID, string siteName, string physicalPath)
        {
            if (IsSiteExists(metabasePath, siteName))
            {
                throw new System.Exception("site " + siteName + " already exists");
            }
            //  metabasePath is of the form "IIS://<servername>/<service>"
            //    for example "IIS://localhost/W3SVC" 
            //  siteID is of the form "<number>", for example "555"
            //  siteName is of the form "<name>", for example, "My New Site"
            //  physicalPath is of the form "<drive>:\<path>", for example, "C:\Inetpub\Wwwroot"
            Console.WriteLine("\nCreating site {0}/{1}, mapping the Root application to {2}:",
                metabasePath, siteID, physicalPath);

            try
            {
                DirectoryEntry service = new DirectoryEntry(metabasePath);
                string className = service.SchemaClassName.ToString();
                if (className.EndsWith("Service"))
                {
                    DirectoryEntries sites = service.Children;
                    DirectoryEntry newSite = sites.Add(siteID, (className.Replace("Service", "Server")));
                    newSite.Properties["ServerComment"][0] = siteName;
                    newSite.Properties["Serverbindings"].Add(":8080:");
                    newSite.CommitChanges();

                    DirectoryEntry newRoot;
                    newRoot = newSite.Children.Add("Root", "IIsWebVirtualDir");
                    newRoot.Properties["Path"][0] = physicalPath;
                    newRoot.Properties["AccessScript"][0] = true;
                    newRoot.CommitChanges();

                    Console.WriteLine(" Done. Your site will not start until you set the ServerBindings or SecureBindings property.");
                }
                else
                    Console.WriteLine(" Failed. A site can only be created in a service node.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed in CreateSite with the following exception: \n{0}", ex.Message);
            }
        }
        public void SetSingleProperty(string metabasePath, string propertyName, object newValue)
        {
            //  metabasePath is of the form "IIS://<servername>/<path>"
            //    for example "IIS://localhost/W3SVC/1" 
            //  propertyName is of the form "<propertyName>", for example "ServerBindings"
            //  value is of the form "<intStringOrBool>", for example, ":80:"
            Console.WriteLine("\nSetting single property at {0}/{1} to {2} ({3}):",
                metabasePath, propertyName, newValue, newValue.GetType().ToString());

            try
            {
                DirectoryEntry path = new DirectoryEntry(metabasePath);
                PropertyValueCollection propValues = path.Properties[propertyName];
                string oldType = propValues.Value.GetType().ToString();
                string newType = newValue.GetType().ToString();
                Console.WriteLine(" Old value of {0} is {1} ({2})", propertyName, propValues.Value, oldType);
                if (newType == oldType)
                {
                    path.Properties[propertyName][0] = newValue;
                    path.CommitChanges();
                    Console.WriteLine("Done");
                }
                else
                    Console.WriteLine(" Failed in SetSingleProperty; type of new value does not match property");
            }
            catch (Exception ex)
            {
                if ("HRESULT 0x80005006" == ex.Message)
                    Console.WriteLine(" Property {0} does not exist at {1}", propertyName, metabasePath);
                else
                    Console.WriteLine("Failed in SetSingleProperty with the following exception: \n{0}", ex.Message);
            }
        }

        public void CreateVDir(string metabasePath, string vDirName, string physicalPath)
        {
            if (IsVDirExists(metabasePath, vDirName))
            {
                throw new System.Exception("Virtual Directory " + vDirName + " already exists");
            }
            
            //  metabasePath is of the form "IIS://<servername>/<service>/<siteID>/Root[/<vdir>]"
            //    for example "IIS://localhost/W3SVC/1/Root" 
            //  vDirName is of the form "<name>", for example, "MyNewVDir"
            //  physicalPath is of the form "<drive>:\<path>", for example, "C:\Inetpub\Wwwroot"
            Console.WriteLine("\nCreating virtual directory {0}/{1}, mapping the Root application to {2}:",
                metabasePath, vDirName, physicalPath);

            try
            {
                DirectoryEntry site = new DirectoryEntry(metabasePath);
                string className = site.SchemaClassName.ToString();
                if ((className.EndsWith("Server")) || (className.EndsWith("VirtualDir")))
                {
                    DirectoryEntries vdirs = site.Children;
                    DirectoryEntry newVDir = vdirs.Add(vDirName, (className.Replace("Service", "VirtualDir")));
                    newVDir.Properties["Path"][0] = physicalPath;
                    newVDir.Properties["AccessScript"][0] = true;
                    // These properties are necessary for an application to be created.
                    newVDir.Properties["AppFriendlyName"][0] = vDirName;
                    newVDir.Properties["AppIsolated"][0] = "1";
                    newVDir.Properties["AppRoot"][0] = "/LM" + metabasePath.Substring(metabasePath.IndexOf("/", ("IIS://".Length)));

                    newVDir.CommitChanges();

                    Console.WriteLine(" Done.");
                }
                else
                    Console.WriteLine(" Failed. A virtual directory can only be created in a site or virtual directory node.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed in CreateVDir with the following exception: \n{0}", ex.Message);
            }
        }
        public bool IsSiteExists(string metabasePath, string siteName)
        {
            bool IsSiteExists=false;

            DirectoryEntry Services = new DirectoryEntry(metabasePath);

            try
            {
                foreach (DirectoryEntry server in Services.Children)
                {
                    if (server.SchemaClassName == "IIsWebServer")
                    {
                        if (server.Properties["ServerComment"][0].ToString() == siteName)
                        {
                            IsSiteExists = true;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                Console.Out.WriteLine(Ex.Message);
            }

            return IsSiteExists;
        }

        public bool IsVDirExists(string metabasePath, string vDirName)
        {
            bool IsVDirExists = false;

            try
            {
                DirectoryEntry site = new DirectoryEntry(metabasePath);
                string className = site.SchemaClassName.ToString();
                if ((className.EndsWith("Server")) || (className.EndsWith("VirtualDir")))
                {
                    DirectoryEntries vdirs = site.Children;
                    Console.Out.WriteLine(vdirs.ToString());
                    foreach (DirectoryEntry vdir in vdirs)
                    {
                        if (vdir.Properties["AppFriendlyName"][0].ToString() == vDirName)
                        {
                            IsVDirExists = true;
                        }
                    }
                }
            }
            catch(Exception Ex)
            {
                Console.Out.WriteLine(Ex.Message);
            }

            return IsVDirExists;
        }

        
    
    
    }
}
