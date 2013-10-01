using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Polaris
{
    public class Dispatch
    {
        public string Module { get; set; }
        public string Method { get; set; }
        public object[] Parameters { get; set; }
        public int CallId { get; set; }
        public object Result { get; set; }
    }
}
