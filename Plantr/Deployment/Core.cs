using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Reflection;

namespace Deployment
{
    public class Core
    {
        // Reads the xml configuration file (eg. sqa, staging)
        // and then stores them in a hashtable.
        public void FillConfig(Hashtable parent, XmlNodeList element)
        {
            string elementType = string.Empty;
            foreach (XmlNode elm in element)
            {
                if (elm.Attributes["type"] != null)
                {
                    elementType = elm.Attributes["type"].Value;
                    if (elementType.Trim() == "xml")
                    {
                        parent[elm.Name] = elm;
                    }
                }
                else if (!HasChildXMLNode(elm))
                {
                    parent[elm.Name] = elm.InnerText;
                }
                else
                {
                    Hashtable child = new Hashtable();
                    parent[elm.Name] = child;
                    FillConfig(child, elm.ChildNodes);
                }
            }
        }

        public bool HasChildXMLNode(XmlNode node)
        {
            return node.FirstChild.GetType() != typeof(XmlText);
        }

        // Retrieves configuration parameter from a string
        // for example [@root]\some folder will return root
        public string GetConfigExpression(string exp)
        {
            if (exp.Length > 0)
            {
                int start = exp.IndexOf("[@");
                if (start > -1)
                {
                    start = start + 2;
                    int end = exp.IndexOf("]");
                    int len = end - start;
                    return exp.Substring(start, len);
                }
            }
            return "";
        }

        // Evalues a configuration parameter by reading the config 
        // For example if root has value c:\build in the config file
        // then [@root]\some folder will return c:\build\somefolder
        public object GetConfigValue(Hashtable config, string exp) // conf
        {
            if (exp.IndexOf("@") == -1)
            {
                return exp;
            }

            string var = GetConfigExpression(exp);
            object val = null;
            if (var.Length > 0)
            {
                val = config[var];
            }
            if (val.GetType() == typeof(ArrayList))
            {
                // $a = 1;
            }
            if (val.GetType() == typeof(string))
            {
                return exp.Replace("[@" + var + "]", (string)val);
            }
            else
            {
                return val;
            }
        }
        public string MakeArrayExpression(List<String> list)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("@(");
            foreach (String v in list)
            {
                sb.Append("\"");
                sb.Append(v);
                sb.Append("\",");
            }
            sb.Remove(sb.Length - 1, 1);
            sb.Append(") ");
            return sb.ToString();
        }

        // Core engine.
        // Runs the steps for specified product for specified configuration
        public void RunSteps(string product, string config_name, string steps_name, int start_step, int end_step)
        {
            // Evaluate location
            String config_location = "products\\" + product + "\\config\\" + config_name + ".xml";
            String steps_location = "products\\" + product + "\\steps\\" + steps_name + ".xml";
            // Load the steps
            XmlDocument xml_steps = new XmlDocument();
            XmlDocument xml_config = new XmlDocument();

            xml_steps.Load(steps_location);
            xml_config.Load(config_location);

            // Build the configuration object
            Hashtable config = new Hashtable();

            // Load configuration object
            FillConfig(config, xml_config.DocumentElement.FirstChild.ChildNodes);
            //Fill-Config $config $xml_config.DocumentElement.FirstChild.ChildNodes

            // Loop through the steps. Check if we can execute the steps
            // then prepare the command string and then make it into a 
            // a scriptblock and then execute the script block
            foreach (XmlNode step in xml_steps.DocumentElement.ChildNodes)
            {
                int current_step = Convert.ToInt32(step.Attributes["id"].Value);
                if (current_step >= start_step)
                {
                    if (current_step <= end_step)
                    {
                        List<string> ArgNamesList = new List<string>();
                        List<object> ArgValuelist = new List<object>();

                        StringBuilder sb = new StringBuilder();
                        String commandName = step.FirstChild.Name.ToString();
                        sb.Append(commandName);
                        foreach (XmlAttribute attr in step.FirstChild.Attributes)
                        {
                            object val = GetConfigValue(config, attr.Value);
                            //sb.Append(" -parm_");
                            //sb.Append(attr.Name);
                            //sb.Append(" ");
                            ArgNamesList.Add(attr.Name.ToString());
                            if (val.GetType() == typeof(Array))
                            {
                                List<string> tempValue = (List<string>)val;
                                String exp = MakeArrayExpression(tempValue);
                                //sb.Append(exp);
                                ArgValuelist.Add(val);
                            }
                            else if (val.GetType() == typeof(XmlElement))
                            {
                                Console.WriteLine("this is xml");
                                //sb.Append(val);
                                ArgValuelist.Add(val);
                            }
                            else
                            {
                                ArgValuelist.Add(val);
                                //sb.Append("\""+val+"\"");
                            }
                        }
                        String[] ArgNames = ArgNamesList.ToArray();
                        Object[] ArgValues = ArgValuelist.ToArray();
                        Console.WriteLine("============================================================");
                        Console.WriteLine("Will execute step " + current_step + " . Command is " + commandName + " .");
                        String commandText = sb.ToString();
                        // Create context
                        //func_context = @{"config" = config; "func_name" = commandName;}

                        //$scr = $scriptBlock = [Scriptblock]::Create($commandText)
                        Console.WriteLine("Executing ' " + commandText + " '");
                        Console.WriteLine("------------------------------------------------------------");
                        Console.WriteLine("");
                        try
                        {
                            // BindingFlags.InvokeMethod 
                            // Call a static method.
                            Type t = typeof(BBEFunctions);
                            ConstructorInfo magicConstructor = t.GetConstructor(Type.EmptyTypes);
                            object magicClassObject = magicConstructor.Invoke(new object[] { });
                            MethodInfo magicMethod = t.GetMethod(commandName);
                            object magicValue = magicMethod.Invoke(magicClassObject, ArgValues);
                            //t.InvokeMember(commandName, BindingFlags.InvokeMethod, null, null, ArgValues, null, null, ArgNames);


                            //Object obj = t.InvokeMember(null,
                            //BindingFlags.DeclaredOnly |
                            //BindingFlags.Public | BindingFlags.NonPublic |
                            //BindingFlags.Instance | BindingFlags.CreateInstance, null, null, null);
                            //// Call a method.
                            //String s = (String)t.InvokeMember(commandName,
                            //BindingFlags.DeclaredOnly |
                            //BindingFlags.Public | BindingFlags.NonPublic |
                            //BindingFlags.Instance | BindingFlags.InvokeMethod, null, obj, null);

                            //Invoke-Command -ScriptBlock $scr 
                            
                            Console.WriteLine("Step " + current_step + " : " + commandName + " is completed.");
                        }
                        catch (Exception ex)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Step " + current_step + " : " + commandName + " has failed.");
                            Console.WriteLine(ex.Message);
                            Console.ResetColor();
                        }
                        finally
                        {
                            //object function_context = null;
                        }

                    }
                }
            }
        }

        public void ExecuteDeployTool(string action, Hashtable paramters)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(action);
            foreach (String key in paramters.Keys)
            {
                sb.Append(" /");
                sb.Append(key);
                sb.Append(":");
                sb.Append(paramters[key]);
            }
            // Use ProcessStartInfo class
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "tools\\DeploymentUtility.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = sb.ToString();

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }
            }
            catch
            {
                // Log error.
            }
        }


        //**********
    }
}
