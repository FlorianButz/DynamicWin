using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicWin.UI.Widgets
{
    public interface IRegisterableSetting
    {
        /// <summary>
        /// The ID the settings get saved at.
        /// </summary>
        public string SettingID { get; }
        public string SettingTitle { get; }

        /// <summary>
        /// Every setting that want to be added have to be returned inside of a List here.
        /// </summary>
        /// <returns></returns>
        public List<UIObject> SettingsObjects();

        /// <summary>
        /// Every setting that the IRegisterableSetting class adds has to be saved. Do it here.
        /// </summary>
        public void SaveSettings();

        /// <summary>
        /// Every setting that the IRegisterableSetting class adds has to be loaded. Do it here.
        /// </summary>
        public void LoadSettings();
    }
}
