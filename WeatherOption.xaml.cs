using MLS.HA.DeviceController.Common.Gui;
using System;
using System.Windows;
using System.Configuration;


namespace OWMDevice
{
 
    public partial class WeatherOption : PluginGuiWindow
    {
        private Configuration config = null;

        public WeatherOption()
        {
            try
            {
                string exeConfigPath = this.GetType().Assembly.Location;
                config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);

            }
            catch (Exception ex)
            {
                //handle errror here.. means DLL has no sattelite configuration file.
            }

            if (config != null)
            {
                InitializeComponent();
                txtApiKey.Text = GetAppSetting(config, "ApiKey");
                txtLat.Text = GetAppSetting(config, "Lat");
                txtLng.Text  = GetAppSetting(config, "Lng");

                if (GetAppSetting(config, "Signs") == "metric")
                    radioMetric.IsChecked = true;
                else
                    radioImperial.IsChecked = true;
 
            }
        }


        private void btnCancel_click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

            SetAppSetting(config, "ApiKey", txtApiKey.Text.ToString());
            SetAppSetting(config, "Lat", txtLat.Text.ToString());
            SetAppSetting(config, "Lng", txtLng.Text.ToString());

            if (radioMetric.IsChecked == true)
                SetAppSetting(config, "Signs", "metric");
            else
                SetAppSetting(config, "Signs", "imperial");


            Close();
        }

        string GetAppSetting(Configuration config, string key)
        {
            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element != null)
            {
                string value = element.Value;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

        private void SetAppSetting(Configuration config, string key, string value)
        {

            config.AppSettings.Settings[key].Value = value;
            config.Save();


        }
    }
}
