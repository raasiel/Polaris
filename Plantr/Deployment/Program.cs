using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.ServiceProcess;
using System.IO;

namespace Deployment
{
    class Program
    {
        static void Main(string[] args)
        {
            //IISFunctions IisFunctions = new IISFunctions();
            //IisFunctions.CreateSite("IIS://Localhost/W3SVC", "555", "dstest", "D:\\ds_test");
            //IisFunctions.SetSingleProperty("IIS://Localhost/W3SVC/555", "ServerBindings", ":8080:");
            //IisFunctions.CreateVDir("IIS://Localhost/W3SVC/1/Root", "dstest", "D:\\ds_test");

            Core core = new Core();
            BBEFunctions BBEFunctions = new BBEFunctions();

            if (args.Length > 1)
            {
                Console.Out.WriteLine("Found argument. Running with " + args[0] + " configuration with steps in " + args[1] + ".");
                Console.Out.WriteLine("Preparing config for deployment. Please wait...");
                //BBEFunctions.BBEConfigXMLUpdate(args[0], "bbe");
                Console.Out.WriteLine("Config preparation complete.");
                if (args[1] == "db")
                {
                    core.RunSteps("bbe", args[0], args[1], 1, 1);
                    //return;
                    /*
                    Console.Out.WriteLine("Running database deployment...");
                    if (args.Length > 2)
                    {
                        if (args[2].ToLower() == "new")
                        {
                            Console.Out.WriteLine("Running database deployment for new installation...");
                            core.RunSteps("bbe", args[0], args[1], 1, 3);
                            Console.Out.WriteLine("Database deployment for new installation complete.");
                            return;
                        }
                    }
                    core.RunSteps("bbe", args[0], args[1], 4, 5);
                    Console.Out.WriteLine("Database deployment complete.");
                    */
                }
                else if (args[1] == "web")
                {
                    Console.Out.WriteLine("Running web deployment...");
                    if (args.Length > 2)
                    {
                        if (args[2].ToLower() == "new")
                        {
                            Console.Out.WriteLine("Running web deployment for new installation...");
                            core.RunSteps("bbe", args[0], args[1], 1, 6);
                            core.RunSteps("bbe", args[0], args[1], 8, 20);
                            Console.Out.WriteLine("Web deployment for new installation complete.");
                            return;
                        }
                    }
                    core.RunSteps("bbe", args[0], args[1], 3, 11);
                    core.RunSteps("bbe", args[0], args[1], 14, 20);
                    Console.Out.WriteLine("Web deployment complete.");
                }
            }
            else
            {
                Console.Out.WriteLine("No argument is supplied.");
                Console.Out.WriteLine("Please specify a configuraton file & a steps file. e.g. \"main.ps1 dev web\" or \"main.ps1 dev db\"");
                // Write-Host 'No argument is supplied. Running with default "dev" configuration.'
                // Run-Steps -product "bbe" -config "dev" -steps "bbeng" -start_step 1 -end_step 9
            }

            Console.Out.WriteLine("Press enter to exit...");
            Console.In.ReadLine();
        }


    }
}
