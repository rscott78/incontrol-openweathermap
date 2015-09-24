using MLS.HA.DeviceController.Common.Gui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OWMDevice
{
    public class MenuItem : IPluginMenuItem
    {

        public string mainMenuName()
        {
            return "Weather";
        }

        /// <summary>
        /// Gets all sub-menu items.
        /// </summary>
        /// <returns></returns>
        public List<PluginSubMenuItem> subMenus()
        {
            var subs = new List<PluginSubMenuItem>();

            var menuItem = new PluginSubMenuItem();
            menuItem.menuName = "Options";
            menuItem.onMenuItemClicked += showWeatherOptions;
            subs.Add(menuItem);

            return subs;
        }


        void showWeatherOptions(System.Windows.Window windowOwner)
        {
            //MessageBox.Show("You clicked the menu!");
            var frm = new WeatherOption();
            frm.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            frm.Owner = windowOwner;
            frm.ShowDialog();
        }
    }
}
