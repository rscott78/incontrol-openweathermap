//using MLS.Common;
using MLS.HA.DeviceController.Common;
using MLS.HA.DeviceController.Common.Device;
using MLS.HA.DeviceController.Common.HaControllerInterface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Configuration;


namespace OWMDevice
{
    public class OWMDevicePlugin : HaController, IHaController
    {

        public enum OWMDataTypes
        {
            Temp, 
            Clouds, 
            PrecipNow, 
            Humidity,
            Pressure,
            WindSpeed,
            WindDirDegree,
            WeatherCon, 
            MinTemp, 
            MaxTemp, 
            Last3HRain, 
            Last3HSnow, 
            Visibility,

            F3HTemp, 
            F3HClouds, 
            F3HPrecip, 
            F3HWindSpeed, 
            F3HWindDirDegree, 
            F3HWeatherCon, 
            F3HMinTemp, 
            F3HMaxTemp, 
            F3HRain, 
            F3HSnow, 
            
            PrecipIn24H,
            PrecipIn48H,
            ExtremeWeather24H

        }

        List<HaDevice> localDevices;
        Thread pollingThread;
        bool isRunning;

        string apiUrl = "http://api.openweathermap.org/data/2.5/{type}?lat={lat}&lon={long}&units={units}&APPID={apikey}";
        private Configuration config = null;

        string apiKey ;

        string unitsDisplay;

        string latitude;
      
        string longitude;

        public string controllerName
        {
            get { return getControllerName(); }
            set { }
        }

        /// <summary>
        /// Constructor sets up the local device list and starts the polling thread.
        /// </summary>
        public OWMDevicePlugin()
        {
            localDevices = new List<HaDevice>();
            isRunning = true;

            pollingThread = new Thread(() => { pollWeather(); });
            pollingThread.IsBackground = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="haDevice"></param>
        /// <returns></returns>
        public HaDevice trackDevice(HaDeviceDto haDevice)
        {

            // Nofies the controller of a new device to be tracked. Adds the new device to the local list.
            var newDevice = new HaDevice();

            // Our devices always start at level 0. 
            newDevice.level = 0;

            // Grab some of the other values and set them in our local device
            newDevice.providerDeviceId = haDevice.uniqueName;
            newDevice.deviceId = haDevice.deviceId;
            newDevice.name = haDevice.deviceName;

            // Add the device to our local list
            localDevices.Add(newDevice);

            return newDevice;
        }

        /// <summary>
        /// If this is called and there are no devices, add one
        /// </summary>
        public void finishedTracking()
        {
            try
            {

                try
                {
                    string exeConfigPath = this.GetType().Assembly.Location;
                    config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);

                    if (config != null)
                    {

                        loadSettings();
                        
                        pollingThread.Start();

                        if (localDevices.Count == 0)
                        {
                            base.raiseDiscoveredDevice(DeviceProviderTypes.PluginDevice, controllerName, DeviceType.LevelDisplayer, OWMDevicePlugin.getDeviceProviderId(latitude, longitude), "Weather");
                        }

                        base.writeLog(string.Format("Weather plugin using coordiates {0}, {1}", latitude.Replace(',', '.'), longitude.Replace(',', '.')));
                    }


                }
                catch (Exception ex)
                {
                    base.writeLog("There is no OpenWeatherConfigFile", ex);
                }

            }
            catch (Exception ex)
            {
                base.writeLog("Error in finishTracking", ex);
            }
        }

        private void loadSettings() {
            latitude = GetAppSetting(config, WeatherOption.SETTING_LAT);
            longitude = GetAppSetting(config, WeatherOption.SETTING_LONG);
            unitsDisplay = GetAppSetting(config, WeatherOption.SETTING_SIGNS) == "metric" ? "metric" : "imperial";
            apiKey = GetAppSetting(config, WeatherOption.SETTING_APIKEY);
        }


        DateTime lastPollTimeWeather = DateTime.Now;
        long lastWeatherUnixTimeStamp = 0;
        DateTime lastPollTimeForcast = DateTime.Now;
        dynamic weather = null;
        dynamic weatherForcast = null;


        public string GetAppSetting(Configuration config, string key) {            
            return getDbSetting(key);
        }

        /// <summary>
        /// Talks to the API and gathers data for each of the devices.
        /// </summary>
        public void pollWeather()
        {           

            while (isRunning)
            {
                try
                {                   
                    
                    // Do this each time in case the user changes settings
                    // This also fixes it so the user doesn't have to restart InControl after the initial setup                 
                    loadSettings();

                    // 10 min between fetching data
                    FetchWeatherData();
                    FetchWeatherForcastData();

                    foreach (var d in localDevices)
                    {
                        // 5 min between polls for each device
                        if ((DateTime.Now - d.lastPollTime).Minutes >= 5)
                        {
                            try
                            {

                                double _weatherCon = getWeatherConData();
                                double _forcastWeatherCon3H = getForcastWeatherCon3HData();

                                d.level = getTempData();

                                setReading(d, OWMDataTypes.Temp, getTempData(), getTempSign());
                                setReading(d, OWMDataTypes.Clouds, getCloudsData(), "%");
                                setReading(d, OWMDataTypes.PrecipNow, getPrecipNowData(_weatherCon), "Y/N");
                                setReading(d, OWMDataTypes.Humidity, getHumidityData(), "%");
                                setReading(d, OWMDataTypes.Pressure, getPressureData(), "P");
                                setReading(d, OWMDataTypes.WindSpeed, getWindSpeedData(), getSpeedSign());
                                setReading(d, OWMDataTypes.WindDirDegree, getWinDegData(), "°");
                                setReading(d, OWMDataTypes.WeatherCon, _weatherCon, "Id");
                                setReading(d, OWMDataTypes.MinTemp, getMinTempData(), getTempSign());
                                setReading(d, OWMDataTypes.MaxTemp, getMaxTempData(), getTempSign());
                                setReading(d, OWMDataTypes.Last3HRain, getRainLast3hData(), "mm");
                                setReading(d, OWMDataTypes.Last3HSnow, getSnowLast3hData(), "mm");
                                setReading(d, OWMDataTypes.Visibility, getVisibilityData(), "m");

                                setReading(d, OWMDataTypes.F3HTemp, getForcast3hTempData(), getTempSign());
                                setReading(d, OWMDataTypes.F3HClouds, getForcast3hCloudsData(), "%");
                                setReading(d, OWMDataTypes.F3HPrecip, getForcastPrecip3HData(_forcastWeatherCon3H), "Y/N");
                                setReading(d, OWMDataTypes.F3HWindSpeed, getForcast3hWindSpeedData(), getTempSign());
                                setReading(d, OWMDataTypes.F3HWindDirDegree, getForcast3hWinDegData(), "°");
                                setReading(d, OWMDataTypes.F3HWeatherCon, _forcastWeatherCon3H, "Id");
                                setReading(d, OWMDataTypes.F3HMinTemp, getForcast3hMinTempData(), getTempSign());
                                setReading(d, OWMDataTypes.F3HMaxTemp, getForcast3hMaxTempData(), getTempSign());
                                setReading(d, OWMDataTypes.F3HRain, getForcastRain3HData(), "mm");
                                setReading(d, OWMDataTypes.F3HSnow, getForcastSnow3HData(), "mm");

                                setReading(d, OWMDataTypes.PrecipIn24H, getPrecipIn24HData(), "mm");
                                setReading(d, OWMDataTypes.PrecipIn48H, getPrecipIn48HData(), "mm");

                                setReading(d, OWMDataTypes.ExtremeWeather24H, getExtremeWeather24HData(), "Y/N");

                            }
                            catch (Exception ex)
                            {
                                writeLog("Weather: Error polling", ex);
                            }

                            d.lastPollTime = DateTime.Now;
                        }
                    }
                }
                catch
                {

                }

                // Sleep                
                Thread.Sleep(10000);
            }
        }

        private string getSpeedSign()
        {
            return unitsDisplay == "metric" ? "km/h" : "m/h";
        }

        private string getTempSign()
        {
            return unitsDisplay == "metric" ? "°C" : "°F";
        }

        private double getExtremeWeather24HData()
        {
            double _extremeWeather24H = 0;
            try
            {

                for (int i = 0; i <= 8; i++)
                {
                    bool extremeAlert = false;

                    try { extremeAlert = OWMHelper.extremeWeatherCheck(Math.Round((double)weatherForcast.list[i].weather.id, 0)); } catch { extremeAlert = false; }

                    if (extremeAlert)
                    {
                        _extremeWeather24H = 1;
                        break;
                    }
                }
            }
            catch { _extremeWeather24H = 0; }

            return _extremeWeather24H;
        }

        private double getPrecipIn48HData()
        {
            double _precipIn48H = 0;
            try
            {
                double _verPercip = 0;

                for (int i = 1; i <= 16; i++)
                {

                    try { _verPercip = Math.Round((double)weatherForcast.list[i].rain.H3h, 2); } catch { _verPercip = 0; }

                    _precipIn48H = _precipIn48H + _verPercip;

                    try { _verPercip = Math.Round((double)weatherForcast.list[i].snow.H3h, 2); } catch { _verPercip = 0; }

                    _precipIn48H = _precipIn48H + _verPercip;
                }
            }
            catch { _precipIn48H = 0; }

            return _precipIn48H;
        }

        private double getPrecipIn24HData()
        {
            double _precipIn24H = 0;
            try
            {
                double _verPercip = 0;

                for (int i = 1; i <= 8; i++)
                {

                    try { _verPercip = Math.Round((double)weatherForcast.list[i].rain.H3h, 2); } catch { _verPercip = 0; }

                    _precipIn24H = _precipIn24H + _verPercip;


                    try { _verPercip = Math.Round((double)weatherForcast.list[i].snow.H3h, 2); } catch { _verPercip = 0; }

                    _precipIn24H = _precipIn24H + _verPercip;
                }
            }
            catch { _precipIn24H = 0; }

            return _precipIn24H;
        }

        private static double getForcastPrecip3HData(double _forcastWeatherCon3H)
        {
            double _forcastPrecip3H;
            try
            {
                _forcastPrecip3H = 0;

                if (_forcastWeatherCon3H != 0)
                {
                    if (OWMHelper.perceptionCheck(_forcastWeatherCon3H))
                        _forcastPrecip3H = 1;
                }

            }
            catch { _forcastPrecip3H = 0; }

            return _forcastPrecip3H;
        }

        private double getForcastWeatherCon3HData()
        {
            double _forcastWeatherCon3H;
            try { _forcastWeatherCon3H = Math.Round((double)weatherForcast.list[1].weather[0].id, 0); } catch { _forcastWeatherCon3H = 0; }

            return _forcastWeatherCon3H;
        }

        private double getForcastSnow3HData()
        {
            double _forcastSnow3H;
            try { _forcastSnow3H = Math.Round((double)weatherForcast.list[1].snow.H3h, 2); } catch { _forcastSnow3H = 0; }

            return _forcastSnow3H;
        }

        private double getForcastRain3HData()
        {
            double _forcastRain3H;
            try { _forcastRain3H = Math.Round((double)weatherForcast.list[1].rain.H3h, 2); } catch { _forcastRain3H = 0; }

            return _forcastRain3H;
        }

        private double getForcast3hWinDegData()
        {
            double _forcastWindDeg;
            try { _forcastWindDeg = Math.Round((double)weatherForcast.list[1].wind.deg, 0); } catch { _forcastWindDeg = 0; }

            return _forcastWindDeg;
        }

        private double getForcast3hWindSpeedData()
        {
            double _forcastWindSpeed;
            try { _forcastWindSpeed = Math.Round((double)weatherForcast.list[1].wind.speed, 1); } catch { _forcastWindSpeed = 0; }

            return _forcastWindSpeed;
        }

        private double getForcast3hCloudsData()
        {
            double _forcast3hClouds;
            try { _forcast3hClouds = Math.Round((double)weatherForcast.list[1].clouds.all, 0); } catch { _forcast3hClouds = 0; }

            return _forcast3hClouds;
        }

        private double getForcast3hMaxTempData()
        {
            double _forcast3hMaxTemp;
            try { _forcast3hMaxTemp = Math.Round((double)weatherForcast.list[1].main.temp_max, 2); } catch { _forcast3hMaxTemp = 0; }

            return _forcast3hMaxTemp;
        }

        private double getForcast3hMinTempData()
        {
            double _forcast3hMinTemp;
            try { _forcast3hMinTemp = Math.Round((double)weatherForcast.list[1].main.temp_min, 2); } catch { _forcast3hMinTemp = 0; }

            return _forcast3hMinTemp;
        }

        private double getForcast3hTempData()
        {
            double _forcast3hTemp;
            try { _forcast3hTemp = Math.Round((double)weatherForcast.list[1].main.temp, 2); } catch { _forcast3hTemp = 0; }

            return _forcast3hTemp;
        }

        private double getSnowLast3hData()
        {
            double _snowLast3h;
            try { _snowLast3h = Math.Round((double)weather.snow.H3h, 2); } catch { _snowLast3h = 0; }

            return _snowLast3h;
        }

        private double getRainLast3hData()
        {
            double _rainLast3h;
            try { _rainLast3h = Math.Round((double)weather.rain.H3h, 2); } catch { _rainLast3h = 0; }

            return _rainLast3h;
        }

        private double getCloudsData()
        {
            double _clouds;
            try { _clouds = Math.Round((double)weather.clouds.all, 0); } catch { _clouds = 0; }

            return _clouds;
        }

        private double getWinDegData()
        {
            double _windDeg;
            try { _windDeg = Math.Round((double)weather.wind.deg, 0); } catch { _windDeg = 0; }

            return _windDeg;
        }

        private double getWindSpeedData()
        {
            double _windSpeed;
            try { _windSpeed = Math.Round((double)weather.wind.speed, 1); } catch { _windSpeed = 0; }

            return _windSpeed;
        }

        private static double getPrecipNowData(double _weatherCon)
        {
            double _precipNow;
            try
            {
                _precipNow = 0;

                if (_weatherCon != 0)
                {
                    if (OWMHelper.perceptionCheck(_weatherCon))
                        _precipNow = 1;
                }

            }
            catch { _precipNow = 0; }

            return _precipNow;
        }

        private double getWeatherConData()
        {
            double _weatherCon;
            try { _weatherCon = Math.Round((double)weather.weather[0].id, 0); } catch { _weatherCon = 0; }

            return _weatherCon;
        }

        private double getVisibilityData()
        {
            double _visibility;
            try { _visibility = Math.Round((double)weather.visibility, 0); } catch { _visibility = 0; }

            return _visibility;
        }

        private double getPressureData()
        {
            double _pressure;
            try { _pressure = Math.Round((double)weather.main.pressure, 0); } catch { _pressure = 0; }

            return _pressure;
        }

        private double getHumidityData()
        {
            double _humidity;
            try { _humidity = Math.Round((double)weather.main.humidity, 0); } catch { _humidity = 0; }

            return _humidity;
        }

        private double getMaxTempData()
        {
            double _maxTemp;
            try { _maxTemp = Math.Round((double)weather.main.temp_max, 1); } catch { _maxTemp = 0; }

            return _maxTemp;
        }

        private double getMinTempData()
        {
            double _minTemp;
            try { _minTemp = Math.Round((double)weather.main.temp_min, 1); } catch { _minTemp = 0; }

            return _minTemp;
        }

        private double getTempData()
        {
            double _temp;
            try { _temp = Math.Round((double)weather.main.temp, 1); } catch { _temp = 0; }

            return _temp;
        }

        private void FetchWeatherForcastData()
        {
            if ((DateTime.Now - lastPollTimeForcast).Minutes >= 90 || weatherForcast == null)
            {
                using (var c = new WebClient())
                {
                    try
                    {
                        var addressForcast = apiUrl.Replace("{lat}", latitude.Replace(',', '.')).Replace("{long}", longitude.Replace(',', '.')).Replace("{apikey}", apiKey).Replace("{units}", unitsDisplay).Replace("{type}", "forecast");
                        var jsonRawForcast = c.DownloadString(new Uri(addressForcast));

                        weatherForcast = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonRawForcast.Replace("\"3h\"", "\"H3h\""));

                    }
                    catch (Exception ex)
                    {
                        writeLog("Weather: Error fetching Forcast data", ex);
                    }
                }

                lastPollTimeForcast = DateTime.Now;
            }
        }

        private void FetchWeatherData()
        {
            if ((DateTime.Now - lastPollTimeWeather).Minutes >= 10 || weather == null)
            {
                using (var c = new WebClient())
                {
                    try
                    {

                        for (int i = 0; i <= 5; i++)
                        {
                            var addressWeather = apiUrl.Replace("{lat}", latitude.Replace(',', '.')).Replace("{long}", longitude.Replace(',', '.')).Replace("{apikey}", apiKey).Replace("{units}", unitsDisplay).Replace("{type}", "weather");
                            var jsonRawWeather = c.DownloadString(new Uri(addressWeather));

                            dynamic dummy_weather = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonRawWeather.Replace("\"3h\"", "\"H3h\""));

                            long newTimeStamp = 0;

                            try { newTimeStamp = (long)dummy_weather.dt; } catch { };

                            if (lastWeatherUnixTimeStamp < newTimeStamp)
                            {
                                lastWeatherUnixTimeStamp = newTimeStamp;
                                weather = dummy_weather;
                                break;
                            }

                            Thread.Sleep(10000);
                        }


                    }
                    catch (Exception ex)
                    {
                        writeLog("Weather: Error fetching Weather data", ex);
                    }
                }

                lastPollTimeWeather = DateTime.Now;
            }
        }




        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="value"></param>
        private void setReading(HaDevice device, OWMDataTypes dataType, double value, string label)
        {

            if (device.sensorReadings == null)
            {
                device.sensorReadings = new List<SensorReading>();
            }

            var reading = device.sensorReadings.Where(r => r.name == dataType.ToString()).FirstOrDefault();

            if (reading == null)
            {
                reading = new SensorReading();
                device.sensorReadings.Add(reading);
            }

            reading.value = value;
            reading.name = dataType.ToString();
            reading.label = label;
        }

        /// <summary>
        /// Gets a device from the locally tracked list.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public HaDevice getHaDevice(Guid deviceId)
        {
            return localDevices.Where(d => d.deviceId == deviceId).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="providerId"></param>
        /// <returns></returns>
        public HaDevice getHaDevice(object providerId)
        {
            return localDevices.Where(d => d.providerDeviceId.ToString() == providerId.ToString()).FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public List<HaDevice> getHaDevices()
        {
            return localDevices;
        }

        /// <summary>
        /// Stops the plugin.
        /// </summary>
        public void stop()
        {
            isRunning = false;
        }

        #region Static helper methods
        internal static string getControllerName()
        {
            return "OWMDevicePlugin";
        }



        /// <summary>
        /// Generates a unique name to use for each device and the associated data the device tracks.
        /// </summary>
        /// <returns></returns>
        internal static string getDeviceProviderId(string latitude, string longitude)
        {
            return string.Format("{0}_{1}", latitude.Replace(',','.'), longitude.Replace(',', '.'));
        }

        #endregion

        #region Unused or ignored implementation methods
        public HaDeviceDetails getDeviceDetails(object providerDeviceId)
        {
            return new HaDeviceDetails();
        }

        public void executeSpecialCommand(object providerDeviceId, MLS.HA.DeviceController.Common.SpecialCommand command, object value)
        {
            // Not used
        }

        public void setLevel(object providerDeviceId, int newLevel)
        {
            // Ignored
        }

        public void setPower(object providerDeviceId, bool powered)
        {
            // Ignored
        }

        public MLS.HA.DeviceController.Common.ControllerTestResult testController()
        {
            return new MLS.HA.DeviceController.Common.ControllerTestResult();
        }
        #endregion


    }
}
