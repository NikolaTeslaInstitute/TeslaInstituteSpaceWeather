using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaInstituteSpaceWeatherConsole
{
    public class TeslaInstituteSpaceWeatherData
    {
        public class Alerts
        {
            // Space Weather Message Code:
            // Serial Number:
            // ALERT:
            // EXTENDED WARNING:
            // Warning Condition:
            public string product_id { get; set; }
            public DateTime issue_datetime { get; set; }
            public string message { get; set; }
        }

        public class HemisphericPower
        {
            public DateTime Observation_Time { get; set; }
            public DateTime Forecast_Time { get; set; }
            public int NorthHemisphericPower { get; set; }
            public int SouthHemisphericPower { get; set; }
        }

        public class OvationAurora
        {
            /* {"Observation Time": "2022-05-21T06:41:00Z", 
            "Forecast Time": "2022-05-21T07:34:00Z", 
            "Data Format": "[Longitude, Latitude, Aurora]", 
            "coordinates": */
            public DateTime Observation_Time { get; set; }
            public DateTime Forecast_Time { get; set; }
            public string Data_Format { get; set; }
            public List<int[]> coordinates { get; set; }
        }

        public class Enlil
        {
            public DateTime time_tag { get; set; }
            public double earth_particles_per_cm3 { get; set; }
            public double temperature { get; set; }
            public double v_r { get; set; }
            public double v_theta { get; set; }
            public double v_phi { get; set; }
            public double b_r { get; set; }
            public double b_theta { get; set; }
            public double b_phi { get; set; }
            public double polarity { get; set; }
            public object cloud { get; set; }
        }

        public class Magnetometers7day
        {
            public DateTime time_tag { get; set; }
            public int satellite { get; set; }
            public double He { get; set; }
            public double Hp { get; set; }
            public double Hn { get; set; }
            public double total { get; set; }
            public bool arcjet_flag { get; set; }
        }

        public class PlanetaryKIndex
        {
            public DateTime time_tag { get; set; }
            public int kp_index { get; set; }
        }

        public class KP1hEst
        {
            public DateTime model_prediction_time { get; set; }
            public float k { get; set; }
        }

        public class KP7day
        {
            public DateTime model_prediction_time { get; set; }
            public float k { get; set; }
        }

        public class GeospaceDst
        {
            public DateTime time_tag { get; set; }
            public float dst { get; set; }
        }

        public class AceSwepam
        {
            public DateTime time_tag { get; set; }
            public int dsflag { get; set; }

            public float dens { get; set; }
            public float speed { get; set; }
            public float temperature { get; set; }
        }
    }
}
