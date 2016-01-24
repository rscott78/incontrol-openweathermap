using MLS.HA.DeviceController.Common.Gui;
using System;
using System.Windows;
using System.Configuration;


namespace OWMDevice {

    public partial class WeatherOption : PluginGuiWindow {
        private Configuration config = null;

        public const string SETTING_LAT = "Latitude";
        public const string SETTING_LONG = "Longitude";
        public const string SETTING_SIGNS = "omw_signs";
        public const string SETTING_APIKEY = "omw_apikey";

        public WeatherOption() {
            try {
                string exeConfigPath = this.GetType().Assembly.Location;
                config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);

            } catch (Exception ex) {
                //handle errror here.. means DLL has no sattelite configuration file.
            }

            if (config != null) {
                InitializeComponent();
                txtApiKey.Text = GetAppSetting(config, SETTING_APIKEY);
                txtLat.Text = GetAppSetting(config, SETTING_LAT);
                txtLng.Text = GetAppSetting(config, SETTING_LONG);

                // If the lat/lng are not set, default it to InControl's lat/long values
                if (txtLat.Text == "yourlat") {
                    txtLat.Text = base.getSetting(SETTING_LAT);
                }

                if (txtLng.Text == "yourlong") {
                    txtLng.Text = base.getSetting(SETTING_LONG);
                }

                if (GetAppSetting(config, SETTING_SIGNS) == "metric") {
                    radioMetric.IsChecked = true;
                } else {
                    radioImperial.IsChecked = true;
                }

            }
        }


        private void btnCancel_click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e) {

            SetAppSetting(config, SETTING_APIKEY, txtApiKey.Text.ToString());
            SetAppSetting(config, SETTING_LAT, txtLat.Text.ToString());
            SetAppSetting(config, SETTING_LONG, txtLng.Text.ToString());

            if (radioMetric.IsChecked == true) {
                SetAppSetting(config, SETTING_SIGNS, "metric");
            } else {
                SetAppSetting(config, SETTING_SIGNS, "imperial");
            }
            
            Close();
        }

        public string GetAppSetting(Configuration config, string key) {

            return getSetting(key);

            //KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            //if (element != null) {
            //    string value = element.Value;
            //    if (!string.IsNullOrEmpty(value))
            //        return value;
            //}
            //return string.Empty;
        }

        private void SetAppSetting(Configuration config, string key, string value) {
            setSetting(key, value);

            //config.AppSettings.Settings[key].Value = value;
            //config.Save();
        }

        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            launchUrl("http://home.openweathermap.org/users/sign_up");
        }

        private void launchUrl(string url) {
            try {
                System.Diagnostics.Process.Start("explorer.exe", url);
            } catch {
                MessageBox.Show("Please open your browser to: " + url);
            }
        }

        private void lblApiKey_down(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            launchUrl("http://home.openweathermap.org/");
        }
    }
}
