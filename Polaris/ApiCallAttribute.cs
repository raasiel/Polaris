using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Polaris
{
    public class ApiCallAttribute : Attribute
    {
        private bool _isSynchronous = false;
        public bool IsSynchronous
        {
            get
            {
                return _isSynchronous;
            }
        }

        public ApiCallAttribute(bool isSynchronous)
        {
            _isSynchronous = isSynchronous;
        }

    }
}
