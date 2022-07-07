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
//using System.Web.Script.Serialization;
using System.Collections;
using System.Threading;

using HemisphericPower = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.HemisphericPower;
using Alerts = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.Alerts;
using KP1hEst = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.KP1hEst;
using KP7day = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.KP7day;
using PlanetaryKIndex = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.PlanetaryKIndex;
using GeospaceDst = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.GeospaceDst;
using Magnetometers7day = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.Magnetometers7day;
using Enlil = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.Enlil;
using AceSwepam = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.AceSwepam;
using OvationAurora = TeslaInstituteSpaceWeatherAlerts.TeslaInstituteSpaceWeatherData.OvationAurora;

using Newtonsoft.Json;

namespace TeslaInstituteSpaceWeatherAlerts
{
	public partial class Form1 : Form
	{
		BackgroundWorker backgroundWorker = new BackgroundWorker();
        bool downloadAlertJSON = true;

        public Form1()
		{
			InitializeComponent();

			backgroundWorker.DoWork += backgroundWorker_DoWork;
			backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
			backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
			backgroundWorker.RunWorkerAsync("");
		} // end form

        public void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (backgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                //"https://services.swpc.noaa.gov/products/solar-wind/mag-5-minute.json"
                //"https://services.swpc.noaa.gov/products/solar-wind/plasma-5-minute.json"
                //"https://services.swpc.noaa.gov/products/alerts.json"
                //"https://services.swpc.noaa.gov/text/aurora-nowcast-hemi-power.txt"

                jsonProcessAlerts("https://services.swpc.noaa.gov/products/alerts.json");
                jsonProcessPlanetaryKIndex("https://services.swpc.noaa.gov/json/planetary_k_index_1m.json");
                jsonProcessPredEstKp1h("https://services.swpc.noaa.gov/json/geospace/geospace_pred_est_kp_1_hour.json");
                jsonProcessDst1h("https://services.swpc.noaa.gov/json/geospace/geospace_dst_1_hour.json");

                //backgroundWorker.ReportProgress(i);
                Thread.Sleep(30000);

                logClear();

                //Console.WriteLine("------------------------------------------------");

                /*
                Code	Product	WMO Code	NOAA Scale	Impacts
                ALTXMF	ALERT: X-ray Flux exceeded M5	WOXX01	R2	Loss of HF and degradation of low frequency navigation signals for tens of minutes.
                SUMXM5	SUMMARY: X-Ray Event exceeded M5	WOXX01	R2	Loss of HF and degradation of low frequency navigation signals for tens of minutes.
                SUMX01	SUMMARY: X-Ray Event exceeded X1	WOXX02	R3	Wide area loss of HF and low-frequency navigation signals for one hour.
                SUMX10	SUMMARY: X-Ray Event exceeded X10	WOXX02	R4	Lost HF and outages of low-frequency navigation signals for one to two hours.
                SUMX20	SUMMARY: X-Ray Event exceeded X20	WOXX02	R5	Complete HF blackouts and outages on low-frequency navigation signals for several hours.
                 */

                /*
                Code	Product	WMO Code	Description
                245MHz	SUMMARY: 245 MHz Radio Emission	 	Daily summary of radio interference which can affect critical search and rescue frequencies.
                ALTTP2	ALERT: Type II Radio Emission	WOXX04	Occur in loose association with major solar flares and are indicative of a shock wave moving through the solar atmosphere.
                ALTTP4	ALERT: Type IV Radio Emission	WOXX04	Associated with some major solar flare events beginning 10 to 20 minutes after the flare maximum, and can last for hours.
                SUM10R	SUMMARY: 10 cm Radio Burst	WOXX03	Proxy of solar EUV emission, important for satellite drag.
                 */

                /*
                Code    Product                                                                     WMO Code    NOAA Scale      Impacts
                ATLK04  ALERT: Geomagnetic K-index of 4                                             WOXX13                      Minor system effects.
                WARK04  WARNING: Geomagnetic K-index of 4 expected                                  WOXX13                      Minor system effects expected.
                ALTK05  ALERT: Geomagnetic K-index of 5                                             WOXX11      G1              Weak power grid fluctuations, minor satellite operations impact.
                WARK05  WARNING: Geomagnetic K-index of 5                                           WOXX11      G1              Weak power grid fluctuations, minor satellite operations impact.
                ALTK06  ALERT: Geomagnetic K-index of 6                                             WOXX12      G2              High latitude power systems affected, satellite drag effect, high - latitude HF radio, high-latitude aurora.
                WARK06  WARNING: Geomagnetic K-index of 6                                           WOXX12      G2              High latitude power systems affected, satellite drag effect, high - latitude HF radio, high-latitude aurora.
                ALTK07  ALERT: Geomagnetic K-index of 7                                             WOXX14      G3              Power system voltage effects, satellite surface charging, HF radio, mid - latitude aurora.
                WARK07  WARNING: Geomagnetic K-index of 7 or greater                                WOXX14      G3              Power system voltage effects, satellite surface charging, HF radio, mid - latitude aurora.
                ALTK08  ALERT: Geomagnetic K-index of 8                                             WOXX15      G4              Voltage problems, satellite surface charging, HF and low-frequency communication degraded, possible aurora near tropics.
                ALTK09  ALERT: Geomagnetic K-index of 9                                             WOXX16      G5              Grid System can collapse, extensive satellite surface charging, extended degraded. HF communication and low-frequency navigation.
                SUMSUD  SUMMARY: Geomagnetic Sudden Impulse                                         WOXX10                      Marks the possible beginning of a geomagnetic storm.
                WARSUD  WARNING: Geomagnetic Sudden Impulse expected                                WOXX10                      Marks the possible beginning of an expected geomagnetic storm.
                WATA20  WATCH: Geomagnetic Storm Category G1 Predicted                              WOXX20      G1              Minor system effects.
                WATA30  WATCH: Geomagnetic Storm Category G2 Predicted                              WOXX21      G2              Weak power grid fluctuations, minor satellite operation impact. Possible high-latitude power systems affected, satellite drag effect, high - latitude HF radio, high-latitude aurora.
                WATA50  WATCH: Geomagnetic Storm Category G3 Predicted                              WOXX22      G3              High-latitude power systems affected, satellite drag effect, high - latitude HF radio, high-latitude aurora.Possible voltage problems, satellite surface charging, HF and low - frequency communication degraded, possible aurora near tropics.
                WATA99  WATCH: Geomagnetic Storm Category G4 or Greater Predicted                   WOXX23      G4 or greater   Grid system can collapse, extensive satellite surface charging, extended degraded.HF communication and low-frequency navigation.
                */

                /*
                Code	Product	                                                                    WMO Code	NOAA Scale	Impacts
                ALTPX1	ALERT: Proton Event 10 MeV Integral Flux exceeded 10 pfu (S1)	            WOXX32	    S1	Minor impacts.
                SUMPX1	SUMMARY: Proton Event 10 MeV Integral Flux exceeded 10 pfu (S1)	            WOXX32	    S1	Minor impacts.
                WARPX1	WARNING: Proton 10 MeV Integral Flux above 10 pfu expected (S1 or Greater)	WOXX32	    S1-S5	Minor impacts.
                ALTPX2	ALERT: Proton Event 10 MeV Integral Flux exceeded 100 pfu (S2)	            WOXX32	    S2	Infrequent effects on HF through polar regions and satellite operations.
                SUMPX2	SUMMARY: Proton Event 10 MeV Integral Flux exceeded 100 pfu (S2)	        WOXX32	    S2	Infrequent effects on HF through polar regions and satellite operations.
                ALTPX3	ALERT: Proton Event 10 MeV Integral Flux exceeded 1,000 pfu (S3)	        WOXX32	    S3	Degraded HF at polar regions and navigation position errors, satellite effects on imaging systems and solar panel currents, significant radiation hazard to astronauts on EVA and high-latitude aircraft passengers.
                SUMPX3	SUMMARY: Proton Event 10 MeV Integral Flux exceeded 1,000 pfu (S3)	        WOXX32	    S3	Degraded HF at polar regions and navigation position errors, satellite effects on imaging systems and solar panel currents, significant radiation hazard to astronauts on EVA and high-latitude aircraft passengers.
                ALTPX4	ALERT: Proton Event 10 MeV Integral Flux exceeded 10,000 pfu (S4)	        WOXX32	    S4	Blackout of HF through the polar regions and navigation position errors over several days, satellite effects degraded imaging systems and memory device problems, high radiation risk to astronauts on EVA and high-latitude aircraft passengers.
                SUMPX4	SUMMARY: Proton Event 10 MeV Integral Flux exceeded 10,000 pfu (S4)	        WOXX32	    S4	Blackout of HF through the polar regions and navigation position errors over several days, satellite effects degraded imaging systems and memory device problems, high radiation risk to astronauts on EVA and high-latitude aircraft passengers.
                ALTPX5	ALERT: Proton Event 10 MeV Integral Flux exceeded 100,000 pfu (S5)	        WOXX32	    S5	No HF in the polar regions and position errors make navigation operations extremely difficult, loss of some satellites and memory impacts cause loss of control, unavoidable high radiation risk for astronauts on EVA and high-latitude aircraft passengers.
                SUMPX5	SUMMARY: Proton Event 10 MeV Integral Flux exceeded 100,000 pfu (S5)	    WOXX32	    S5	No HF in the polar regions and position errors make navigation operations extremely difficult, loss of some satellites and memory impacts cause loss of control, unavoidable high radiation risk for astronauts on EVA and high-latitude aircraft passengers.
                ALTPC0	ALERT: Proton Event 100 MeV Integral Flux exceeded 1 pfu	                WOXX31	 	    Minor impacts on HF through polar regions and satellite operations.
                SUMPC0	SUMMARY: Proton Event 100 MeV Integral Flux exceeded 1 pfu	                WOXX31	 	    Minor impacts on HF through polar regions and satellite operations.
                WARPC0	WARNING: Proton 100 MeV Integral Flux above 1 pfu expected	                WOXX31	 	    Possible minor impacts on HF through polar regions and satellite operations.
                 */

                foreach (Alerts alert in AlertsOutput)
                {
                    //int result = DateTime.Compare(alert.issue_datetime, DateTime.Now);
                    //Console.WriteLine("TIME: " + dstindex.time_tag + " KP: " + dstindex.dst);
                    if ((DateTime.Now - alert.issue_datetime).TotalDays < 3)
                    {
                        //log("{" + alert.issue_datetime + "} " + alert.message);
                        /*  ALTK04
                            ALERT: Geomagnetic K-index of 4
                            Threshold Reached: 2022 May 22 0150 UTC
                            Synoptic Period: 0000 - 0300 UTC
                            WARK04
                            WARNING: Geomagnetic K-index of 4 expected
                            Valid From: 2022 May 20 2240 UTC
                            Valid To: 2022 May 21 0600 UTC
                            Warning Condition: Onset
                        */
                        int codeindex = alert.message.IndexOf("Space Weather Message Code:");
                        int searchlength = "Space Weather Message Code:".Length;
                        string code = alert.message.Substring(
                            codeindex + searchlength + 1,
                            TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - searchlength - 2);
                        log("{" + alert.issue_datetime + "} " + code);

                        try
                        {
                            try
                            {
                                codeindex = alert.message.IndexOf("ALERT:");
                                code = alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                                log("{" + alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try

                            try
                            {
                                codeindex = alert.message.IndexOf("SUMMARY:");
                                code = alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                                log("{" + alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try

                            //Begin Time: 2022 May 24 2243 UTC
                            codeindex = alert.message.IndexOf("Begin Time:");
                            code = alert.message.Substring(
                                codeindex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                            log("{" + alert.issue_datetime + "} " + code);

                            try
                            {
                                codeindex = alert.message.IndexOf("Threshold Reached:");
                                code = alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                                log("{" + alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try

                            try
                            {
                                codeindex = alert.message.IndexOf("Estimated Velocity:");
                                code = alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                                log("{" + alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try

                            log("------------------------------------------------");

                        }
                        catch (Exception err)
                        {
                        } // end try

                        try
                        {
                            codeindex = alert.message.IndexOf("WARNING:");
                            code = alert.message.Substring(
                                codeindex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                            log("{" + alert.issue_datetime + "} " + code);
                            codeindex = alert.message.IndexOf("Valid From:");
                            code = alert.message.Substring(
                                codeindex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                            log("{" + alert.issue_datetime + "} " + code);
                            log("------------------------------------------------");
                        }
                        catch (Exception err)
                        {
                        } // end try

                    } // end if
                } // end foreach

                //Console.WriteLine("------------------------------------------------");

                foreach (PlanetaryKIndex pkindex in PlanetaryKIndexOutput)
                {
                    //Console.WriteLine("TIME: " + pkindex.time_tag + " KP: " + pkindex.kp_index);
                    if (pkindex.kp_index > 2)
                        log("{" + pkindex.time_tag + "} KP=" + pkindex.kp_index);
                } // end foreach

                //Console.WriteLine("------------------------------------------------");

                foreach (GeospaceDst dstindex in Dst1hOutput)
                {
                    //Console.WriteLine("TIME: " + dstindex.time_tag + " KP: " + dstindex.dst);
                    if (dstindex.dst < -25)
                        log("{" + dstindex.time_tag + "} DST=" + dstindex.dst);
                } // end foreach

                //Console.WriteLine("------------------------------------------------");

                foreach (KP1hEst kpestindex in PredEstKp1hOutput)
                {
                    //Console.WriteLine("TIME: " + dstindex.time_tag + " KP: " + dstindex.dst);
                    if (kpestindex.k > 2)
                        log("{" + kpestindex.model_prediction_time + "} KP.EST=" + kpestindex.k);
                } // end foreach

                //e.Result = 1000;
            } // end while
        } // end backgroundWorker_DoWork

        public void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        public void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        public void log(string line)
        {
            LogBox.Invoke((MethodInvoker)(() =>
            {
                LogBox.Text += line + "\r\n";
                //LogBox.CaretIndex = LogBox.Text.Length;
                //LogBox.ScrollToEnd();
            }));
        } // end log

        public void logClear()
        {
            LogBox.Invoke((MethodInvoker)(() =>
            {
                LogBox.Clear();
            }));
        } // end logClear

        // ------------------------------------------------------------------------------------------------------
        List<Alerts> AlertsOutput;

        private async void jsonProcessAlerts(string url)
        {
            Console.WriteLine("PROCESS : " + url);
            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            AlertsOutput = JsonConvert.DeserializeObject<List<Alerts>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessAlerts

        // ------------------------------------------------------------------------------------------------------
        List<PlanetaryKIndex> PlanetaryKIndexOutput;

        private async void jsonProcessPlanetaryKIndex(string url)
        {
            Console.WriteLine("PROCESS : " + url);

            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            PlanetaryKIndexOutput = JsonConvert.DeserializeObject<List<PlanetaryKIndex>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex

        // ------------------------------------------------------------------------------------------------------
        List<KP1hEst> PredEstKp1hOutput;

        private async void jsonProcessPredEstKp1h(string url)
        {
            Console.WriteLine("PROCESS : " + url);

            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            PredEstKp1hOutput = JsonConvert.DeserializeObject<List<KP1hEst>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPredEstKp1h

        // ------------------------------------------------------------------------------------------------------
        List<GeospaceDst> Dst1hOutput;

        private async void jsonProcessDst1h(string url)
        {
            Console.WriteLine("PROCESS : " + url);

            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            Dst1hOutput = JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex

        // -------------------------------------------------------------------------

        static async Task<string> DownloadPage(string url, bool save)
        {
            using (var client = new HttpClient())
            {
                //client.DefaultRequestHeaders.Add(htmlheader, APIKEY);
                Uri uri = new Uri(url);
                using (var r = await client.GetAsync(uri))
                {
                    string result = await r.Content.ReadAsStringAsync();
                    string jsonpath = @"..\json\";
                    string jsonfile = "";
                    string jsonuuid = Guid.NewGuid().ToString();
                    jsonfile = System.IO.Path.GetFileName(uri.AbsolutePath.Substring(0, uri.AbsolutePath.Length - 5));
                    if (save)
                        File.WriteAllText(jsonpath + jsonfile + "-" + jsonuuid + ".json", result);
                    return result;
                } // end using
            } // end using
        } // end DownloadPage

    } // end class
}
