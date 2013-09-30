using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Polaris
{
    public static class EntryPoint
    {
        public static void Start(string[] args)
        {
            Configuration config = new Configuration();

            // TODO: In the future change make this configurable 
            config.AppBase = AppDomain.CurrentDomain.BaseDirectory;

            Context context = new Context();
            context.Config = config;

            // Load the configuration 
            LoadConfiguration(config);

            IApplicationController controller = Activator.CreateInstance(config.AppControllerType) as IApplicationController;
            controller.Initialize(context);
            controller.Run();
            
        }

        private const string XPATH_APP_CONTROLLER = "//boot/app_controller";
        private const string XPATH_APP_HOST = "//boot/app_host";
        private const string XPATH_API_PROVIDER = "//boot/api_provider";
        private const string XPATH_START_PAGE= "//boot/start_page";
        private const string XPATH_API_MODULES = "//modules/module";

        private static void LoadConfiguration( Configuration config)
        {
            string configPath = Helper.TranslateFilePath( "{APPPATH}polaris.config", config);
            Helper.VerifyFileExists(configPath, "Polaris configuration file was not found. It was expected at {0} location");
            XmlDocument doc = new XmlDocument();
            doc.Load(configPath);
            
            config.AppControllerType    = Helper.GetType( doc.SelectSingleNode(XPATH_APP_CONTROLLER).InnerText);
            config.ApiProviderType      = Helper.GetType(doc.SelectSingleNode(XPATH_API_PROVIDER).InnerText);
            config.HostType             = Helper.GetType(doc.SelectSingleNode(XPATH_APP_HOST).InnerText);
            config.StartPage            = doc.SelectSingleNode(XPATH_START_PAGE).InnerText;

            XmlNodeList xnlModuleNames = doc.SelectNodes(XPATH_API_MODULES);
            List<Type> modules = new List<Type>();
            foreach (XmlNode xndModule in xnlModuleNames)
            {
                string moduleTypename = xndModule.InnerText;
                Type typModule = Helper.GetType(moduleTypename);
                modules.Add(typModule);
            }
            config.ApiModules = modules.ToArray();

        }

        
    }
}
