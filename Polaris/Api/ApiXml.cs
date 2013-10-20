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

        private Context _context = null;
        public bool Initialize(Context context)
        {
            _context = context;
            return true;
        }

        [ApiCall(false)]
        public string getJsonXmlDictionary(string path)
        {
            string filepath = Helper.TranslateFilePath( path, null);
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);

            object root = GetObjectFromElement ( doc.DocumentElement);

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

        private bool IsArray(XmlElement xel)
        {
            if ( xel.GetAttribute("_array")=="1")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private object GetObjectFromElement ( XmlElement xel)
        {
            if (IsArray (xel)==true)
            {
                Dictionary<string, object> dicArr = new Dictionary<string, object>();
                List<object> list = new List<object>();
                foreach (XmlElement xelChild in xel.ChildNodes)
                {
                    object obj = GetObjectFromElement (xelChild);
                    if (obj is Dictionary<string, object>)
                    {
                        Dictionary<string, object> dic = obj as Dictionary<string, object>;
                        dic["_type"] = xelChild.Name;
                    }
                    list.Add(obj);
                }
                foreach (XmlAttribute attr in xel.Attributes)
                {
                    if (!attr.Name.StartsWith("_"))
                    {
                        dicArr[attr.Name] = attr.Value;
                    }
                }

                dicArr.Add("items", list.ToArray());
                return dicArr;
            }            
            else if (IsDictionary(xel)==true)
            {
                Dictionary<string, object> dicReturn =  new Dictionary<string, object>();
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
                        Dictionary<string, object> dicChild = GetObjectFromElement(xelChild) as Dictionary<string, object>;
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
                            dicReturn[xelChild.Name] = GetObjectFromElement(xelChild);
                        }
                        else
                        {
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
