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

namespace OWMDevice
{
    public static class OWMHelper
    {

        //between numbers
        private static bool betweenWeatherId(this double num, double lower, double upper, bool inclusive = false)
        {
            return inclusive
                ? lower <= num && num <= upper
                : lower < num && num < upper;
        }

        // check for perception
        public static bool perceptionCheck(double weatherId)
        {
            if (betweenWeatherId(weatherId, 200, 210, true) || betweenWeatherId(weatherId, 230, 232, true) || betweenWeatherId(weatherId, 300, 321, true) ||
                betweenWeatherId(weatherId, 500, 531, true) || betweenWeatherId(weatherId, 600, 622, true) || weatherId == 902 || betweenWeatherId(weatherId, 960, 962, true))
                return true;

            return false;
        }

        // check for perception
        public static bool extremeWeatherCheck(double weatherId)
        {
            if (weatherId == 962 || weatherId == 961 || betweenWeatherId(weatherId, 900, 906, true))
                return true;

            return false;
        }

    }
      
}