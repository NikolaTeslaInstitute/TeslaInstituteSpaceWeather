using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Web;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Collections;
using Newtonsoft.Json;
using System.Windows.Forms.DataVisualization.Charting;
using TeslaInstituteSpaceWeather;

namespace TeslaInstituteSpaceWeather
{
	public partial class Form1 : Form
    {

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


        // --------------------------------------
        // https://services.swpc.noaa.gov/images/
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/ovation_aurora_latest.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/ace/swepam/ace_swepam_1h.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/ace/mag/ace_mag_1h.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/dscovr/dscovr_mag_1s.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/stereo/stereo_a_1m.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/goes/primary/magnetometers-7-day.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/geospace/geospace_dst_7_day.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/products/geospace/propagated-solar-wind.json
        // --------------------------------------
        // https://services.swpc.noaa.gov/json/geospace/geospce_pred_est_kp_7_day.json

        public Form1()
        {
            InitializeComponent();

            using (WebClient client = new WebClient())
            {
                string s = client.DownloadString("https://services.swpc.noaa.gov/text/27-day-outlook.txt");
                textBox1.Text = s.Replace("\n", "\r\n");
            }

            using (WebClient client = new WebClient())
            {
                string s = client.DownloadString("https://services.swpc.noaa.gov/text/daily-geomagnetic-indices.txt");
                textBox2.Text = s.Replace("\n", "\r\n");
            }

            using (WebClient client = new WebClient())
            {
                string s = client.DownloadString("https://services.swpc.noaa.gov/text/3-day-forecast.txt");
                textBox3.Text = s.Replace("\n", "\r\n");
            }

            pictureBox1.Image = LoadPicture("https://services.swpc.noaa.gov/images/planetary-k-index.gif");
            pictureBox2.Image = LoadPicture("https://services.swpc.noaa.gov/images/aurora-forecast-northern-hemisphere.jpg");
            pictureBox3.Image = LoadPicture("https://services.swpc.noaa.gov/images/geospace/geospace_7_day.png");
    
            //listFiles("https://services.swpc.noaa.gov/images/animations/lasco-c2/lasco/", ".jpg");

            //listDirectory("https://services.swpc.noaa.gov/images/animations/enlil/enlil/spi_data/model_runs/", ".jpg");

            //listDirectory("https://services.swpc.noaa.gov/images/animations/ovation/north/", ".jpg");

            try
            {
                jsonProcessPlanetaryKIndex("https://services.swpc.noaa.gov/json/planetary_k_index_1m.json");
                jsonProcessMagnetometers7day("https://services.swpc.noaa.gov/json/goes/primary/magnetometers-7-day.json");
                jsonProcessAceSwepam("https://services.swpc.noaa.gov/json/ace/swepam/ace_swepam_1h.json");
                jsonProcessGeospaceDst("https://services.swpc.noaa.gov/json/geospace/geospace_dst_7_day.json");
                jsonProcessKP7day("https://services.swpc.noaa.gov/json/geospace/geospce_pred_est_kp_7_day.json");
                jsonProcessOvationAurora("https://services.swpc.noaa.gov/json/ovation_aurora_latest.json");

            } catch (Exception err)
            {
                Console.WriteLine("JSON ERROR : " + err.Message);
            }

        }

        // -------------------------------------------------------------------------
        string Magnetometers7dayOutput = "";

        private async void jsonProcessMagnetometers7day(string url)
        {
            string jsonresult = await DownloadPage(url);
            List<Magnetometers7day> doc =
                //JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult, new JsonExponentialConverter(typeof(List<Magnetometers7day>)));
                JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult);

            Series he = this.chart2.Series.Add("HE");
            he.ChartType = SeriesChartType.Spline;
            Series hn = this.chart2.Series.Add("HN");
            hn.ChartType = SeriesChartType.Spline;
            Series hp = this.chart2.Series.Add("HP");
            hp.ChartType = SeriesChartType.Spline;

            foreach (Magnetometers7day magindex in doc)
            {
                string line = " TIME: " + magindex.time_tag + " He: " + magindex.He + " Hn: " + magindex.Hn + " Hp: " + magindex.Hp;
                //Console.WriteLine(line);
                Magnetometers7dayOutput += line + "\r\n";
                he.Points.AddXY(magindex.time_tag, magindex.He);
                hn.Points.AddXY(magindex.time_tag, magindex.Hn);
                hp.Points.AddXY(magindex.time_tag, magindex.Hp);
            } // end foreach
        } // end jsonProcessMagnetometers7day


        // -------------------------------------------------------------------------
        string AceSwepamOutput = "";

        private async void jsonProcessAceSwepam(string url)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new JsonExponentialConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url);
            List<AceSwepam> doc =
                //JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, new JsonExponentialConverter(typeof(List<AceSwepam>)));
                JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, settings);

            Series dens = this.chart1.Series.Add("DENSITY");
            dens.ChartType = SeriesChartType.Spline;
            Series speed = this.chart6.Series.Add("SPEED");
            speed.ChartType = SeriesChartType.Spline;
            Series temperature = this.chart7.Series.Add("TEMPERATURE");
            temperature.ChartType = SeriesChartType.Spline;

            foreach (AceSwepam acindex in doc)
            {
                string line = " TIME: " + acindex.time_tag + " DENSITY: " + acindex.dens + " SPEED: " + acindex.speed + " SPEED: " + acindex.temperature;
                //Console.WriteLine(line);
                AceSwepamOutput += line + "\r\n";
                dens.Points.AddXY(acindex.time_tag, acindex.dens);
                speed.Points.AddXY(acindex.time_tag, acindex.speed);
                temperature.Points.AddXY(acindex.time_tag, acindex.temperature);
            } // end foreach

            //textBox5.Text = AceSwepamOutput;
        } // end jsonProcessAceSwepam

        // -------------------------------------------------------------------------
        string GeospaceDstOutput = "";

        private async void jsonProcessGeospaceDst(string url)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new JsonGeospaceDstConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url);
            List<GeospaceDst> doc =
                //JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, new JsonExponentialConverter(typeof(List<AceSwepam>)));
                JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult, settings);

            Series dst = this.chart3.Series.Add("DST");
            dst.ChartType = SeriesChartType.Spline;

            foreach (GeospaceDst dstindex in doc)
            {
                string line = " TIME: " + dstindex.time_tag + " DST: " + dstindex.dst;
                //Console.WriteLine(line);
                GeospaceDstOutput += line + "\r\n";
                dst.Points.AddXY(dstindex.time_tag, dstindex.dst);
            } // end foreach

            //textBox5.Text = GeospaceDstOutput;
        } // end jsonProcessGeospaceDstOutput

        // -------------------------------------------------------------------------
        string PlanetaryKIndexOutput = "";

        private async void jsonProcessPlanetaryKIndex(string url)
        {
            string jsonresult = await DownloadPage(url);
            List<PlanetaryKIndex> doc = JsonConvert.DeserializeObject<List<PlanetaryKIndex>>(jsonresult);

            Series kp = this.chart4.Series.Add("KP INDEX");
            kp.ChartType = SeriesChartType.Column;

            foreach (PlanetaryKIndex pkindex in doc)
            {
                string line = " TIME: " + pkindex.time_tag + " KP: " + pkindex.kp_index;
                //Console.WriteLine(line);
                PlanetaryKIndexOutput += line + "\r\n";
                kp.Points.AddXY(pkindex.time_tag, pkindex.kp_index);
            } // end foreach

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex

        // -------------------------------------------------------------------------
        string KP7dayOutput = "";

        private async void jsonProcessKP7day(string url)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new KP7DatConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url);
            List<KP7day> doc = JsonConvert.DeserializeObject<List<KP7day>>(jsonresult, settings);

            Series kp = this.chart5.Series.Add("KP 7 DAY INDEX");
            kp.ChartType = SeriesChartType.Column;

            foreach (KP7day pkindex in doc)
            {
                string line = " TIME: " + pkindex.model_prediction_time + " KP7: " + pkindex.k;
                //Console.WriteLine(line);
                PlanetaryKIndexOutput += line + "\r\n";
                kp.Points.AddXY(pkindex.model_prediction_time, pkindex.k);
            } // end foreach

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessKP7day

        // -------------------------------------------------------------------------
        string OvationAuroraOutput = "";

        private async void jsonProcessOvationAurora(string url)
        {
            string jsonresult = await DownloadPage(url);
            OvationAurora doc = JsonConvert.DeserializeObject<OvationAurora> (jsonresult);
            string time = " OBSERVATION TIME: " + doc.Observation_Time + " FORECAST TIME: " + doc.Forecast_Time;
            //logBox.Text += time + "\r\n";

            List<int[]> coordinates = new List<int[]>();

            foreach (int[] oa in doc.coordinates)
            {
                string line = "LONG: " + oa[0] + " LAT: " + oa[1] + " AURORA: " + oa[2];
                //logBox.Text += line + "\r\n";
                //Console.WriteLine(line);
            } // end foreach

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex

        // -------------------------------------------------------------------------

        static async Task<string> DownloadPage(string url)
        {
            using (var client = new HttpClient())
            {
                //client.DefaultRequestHeaders.Add(htmlheader, APIKEY);
                using (var r = await client.GetAsync(new Uri(url)))
                {
                    string result = await r.Content.ReadAsStringAsync();
                    return result;
                }
            } // end using
        } // end DownloadPage


        public void listFiles(string url, string extension)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(GetDirectoryListingRegexForUrl(url));
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                string m = match.Groups["name"].Value;
                                if (m.Contains(extension))
                                {
                                    Console.WriteLine("FILE : " + url + m);
                                } else if ( (m.Contains("/")) && (!m.Contains("Last modified")) )
                                {
                                    Console.WriteLine("DIR : " + m);
                                } // end if
                            }
                        }
                    }
                }
            }
        }

        public void listDirectory(string url, string extension)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(GetDirectoryListingRegexForUrl(url));
                    MatchCollection matches = regex.Matches(html);
                    if (matches.Count > 0)
                    {
                        foreach (Match match in matches)
                        {
                            if (match.Success)
                            {
                                string m = match.Groups["name"].Value;
                                if (m.Contains(extension))
                                {
                                    Console.WriteLine("FILE : " + url + m);
                                }
                                else if ((m.Contains("/")) && (!m.Contains("Last modified")))
                                {
                                    Console.WriteLine("DIR : " + m);
                                } // end if
                            }
                        }
                    }
                }
            }
        }

        public static string GetDirectoryListingRegexForUrl(string url)
        {
            //return "<a href=\".*\">(?<name>.*)</a>";
            return "<a href=\"(?<name>.*)\">";
        }

        public static Bitmap LoadPicture(string url)
        {
            System.Net.HttpWebRequest wreq;
            System.Net.HttpWebResponse wresp;
            Stream mystream;
            Bitmap bmp;

            bmp = null;
            mystream = null;
            wresp = null;
            try
            {
                wreq = (System.Net.HttpWebRequest)System.Net.HttpWebRequest.Create(url);
                wreq.AllowWriteStreamBuffering = true;

                wresp = (System.Net.HttpWebResponse)wreq.GetResponse();

                if ((mystream = wresp.GetResponseStream()) != null)
                    bmp = new Bitmap(mystream);
            }
            catch
            {
                // Do nothing... 
            }
            finally
            {
                if (mystream != null)
                    mystream.Close();

                if (wresp != null)
                    wresp.Close();
            }

            return (bmp);
        }

        /// <summary>
        /// Takes in an image, scales it maintaining the proper aspect ratio of the image such it fits in the PictureBox's canvas size and loads the image into picture box.
        /// Has an optional param to center the image in the picture box if it's smaller then canvas size.
        /// </summary>
        /// <param name="image">The Image you want to load, see LoadPicture</param>
        /// <param name="canvas">The canvas you want the picture to load into</param>
        /// <param name="centerImage"></param>
        /// <returns></returns>

        public static Image ResizeImage(Image image, PictureBox canvas, bool centerImage)
        {
            if (image == null || canvas == null)
            {
                return null;
            }

            int canvasWidth = canvas.Size.Width;
            int canvasHeight = canvas.Size.Height;
            int originalWidth = image.Size.Width;
            int originalHeight = image.Size.Height;

            System.Drawing.Image thumbnail =
                new Bitmap(canvasWidth, canvasHeight); // changed parm names
            System.Drawing.Graphics graphic =
                         System.Drawing.Graphics.FromImage(thumbnail);

            graphic.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphic.SmoothingMode = SmoothingMode.HighQuality;
            graphic.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphic.CompositingQuality = CompositingQuality.HighQuality;

            /* ------------------ new code --------------- */

            // Figure out the ratio
            double ratioX = (double)canvasWidth / (double)originalWidth;
            double ratioY = (double)canvasHeight / (double)originalHeight;
            double ratio = ratioX < ratioY ? ratioX : ratioY; // use whichever multiplier is smaller

            // now we can get the new height and width
            int newHeight = Convert.ToInt32(originalHeight * ratio);
            int newWidth = Convert.ToInt32(originalWidth * ratio);

            // Now calculate the X,Y position of the upper-left corner 
            // (one of these will always be zero)
            int posX = Convert.ToInt32((canvasWidth - (image.Width * ratio)) / 2);
            int posY = Convert.ToInt32((canvasHeight - (image.Height * ratio)) / 2);

            if (!centerImage)
            {
                posX = 0;
                posY = 0;
            }
            graphic.Clear(Color.White); // white padding
            graphic.DrawImage(image, posX, posY, newWidth, newHeight);

            /* ------------- end new code ---------------- */

            System.Drawing.Imaging.ImageCodecInfo[] info =
                             ImageCodecInfo.GetImageEncoders();
            EncoderParameters encoderParameters;
            encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality,
                             100L);

            Stream s = new System.IO.MemoryStream();
            thumbnail.Save(s, info[1],
                              encoderParameters);

            return Image.FromStream(s);
        }

    }
}
