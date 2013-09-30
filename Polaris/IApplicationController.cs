using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Polaris
{
    public interface IApplicationController
    {
        bool Initialize(Context context);
        void Run();
    }
}
