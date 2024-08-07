using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.Utils
{
    public interface IDynamicWinExtension
    {
        public string GetExtensionName();
        public void LoadExtension();
    }
}
