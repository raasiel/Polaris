using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace Polaris.Api
{
    public class ApiXml : IApiModule
    {

        public string ModuleName
        {
            get { return "xml"; }
        }

        [ApiCall(false)]
        public string getJsonXmlDictionary(string path)
        {
            string filepath = Helper.TranslateFilePath( path, null);
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            object root = FillDictionary ( doc.DocumentElement);

            return JsonConvert.SerializeObject(root);
        }

        private bool IsDictionary(XmlElement xel)
        {
            if (xel.GetAttribute("_key").Length > 0)
            {
                return true;
            }

            if ( xel.Attributes.Count>0)
            {
                return true;
            }

            if (xel.ChildNodes.Count > 1)
            {
                return true;
            }

            if (xel.ChildNodes.Count == 1)
            {
                if (xel.ChildNodes[0] is XmlText)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        private object FillDictionary ( XmlElement xel)
        {
            Dictionary<string, object> dicReturn = new Dictionary<string, object>();

            bool isDictionary = IsDictionary(xel);
            if (isDictionary)
            {
                bool isExplicit = false;
                string explicitKey = xel.GetAttribute("_key");
                if (explicitKey.Length > 0)
                {
                    isExplicit = true;
                }
                if (isExplicit)
                {
                    foreach (XmlElement xelChild in xel.ChildNodes)
                    {
                        Dictionary<string, object> dicChild = FillDictionary(xelChild) as Dictionary<string, object>;
                        string propName = dicChild[explicitKey] as string;
                        dicReturn[propName] = dicChild;
                    }
                    foreach (XmlAttribute attr in xel.Attributes)
                    {
                        if (!attr.Name.StartsWith("_"))
                        {
                            dicReturn[attr.Name] = attr.Value;
                        }
                    }

                }
                else
                {
                    foreach (XmlAttribute attr in xel.Attributes)
                    {
                        dicReturn[attr.Name] = attr.Value;
                    }
                    foreach (XmlNode xndChild in xel.ChildNodes)
                    {
                        XmlElement xelChild = xndChild as XmlElement;
                        if (xelChild != null)
                        {
                            dicReturn[xelChild.Name] = FillDictionary(xelChild);
                        }
                        else
                        {
                            var a = 1;
                            if ( xndChild.GetType().Equals ( typeof(XmlText)))
                            {
                                dicReturn[xndChild.Name] = xndChild.InnerText;
                            }
                            
                        }
                    }
                }
                return dicReturn;
            }
            return xel.InnerText;
        }

    }
}
