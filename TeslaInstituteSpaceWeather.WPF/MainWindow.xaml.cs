using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.ComponentModel;
using System.Threading;
using System.Globalization;

using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Portal;

using HemisphericPower = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.HemisphericPower;
using Alerts = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.Alerts;
using KP1hEst = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.KP1hEst;
using KP7day = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.KP7day;
using PlanetaryKIndex = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.PlanetaryKIndex;
using GeospaceDst = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.GeospaceDst;
using Magnetometers7day = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.Magnetometers7day;
using Enlil = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.Enlil;
using AceSwepam = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.AceSwepam;
using OvationAurora = TeslaInstituteSpaceWeather.WPF.TeslaInstituteSpaceWeatherData.OvationAurora;

using GeoPoint = GeoAPI.Geometries.Coordinate;
using GeoAPI.Geometries;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI;

using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Wpf;

using NpgsqlTypes;
using Npgsql;

using Newtonsoft.Json;

using NASABot.Api.Nasa;
using NASABot.Api.Model;
using Path = System.IO.Path;

namespace TeslaInstituteSpaceWeather.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // ------------------------------------------------------------------------------------------------------

        private readonly Esri.ArcGISRuntime.Geometry.Envelope _usEnvelope =
            new Esri.ArcGISRuntime.Geometry.Envelope(-144.619561355187, 18.0328662832097, -66.0903762761083, 67.6390975806745, SpatialReferences.Wgs84);
        public string _esriApiKey = "YOUR ESRI DEVELOPER API KEY HERE";

        Esri.ArcGISRuntime.Data.FeatureCollection features = new Esri.ArcGISRuntime.Data.FeatureCollection();

        BackgroundWorker backgroundWorker = new BackgroundWorker();

        bool refreshAtStart = false;
        bool downloadJSON = true;
        bool downloadAlertJSON = false;

        // ------------------------------------------------------------------------------------------------------
        
        string jsonpath = @"c:\TeslaInstituteSimulation\TeslaInstituteSpaceWeather\json\";
        string graphPath = @"c:\TeslaInstituteSimulation\TeslaInstituteSpaceWeather\graph\";
        string csvPath = @"c:\TeslaInstituteSimulation\TeslaInstituteSpaceWeather\csv\";

        // ------------------------------------------------------------------------------------------------------

        private Donki _donkiApi = new Donki();
        private string NasaAPIKey = "YOUR NASA API KEY HERE";
        public List<CoronalMassEjection> cmeResults;
        public List<GeomagneticStorm> stormResults;
        public ObservableCollection<GridData> GridDataCollection = new ObservableCollection<GridData>();
        Thread loadDonkiThread;

        private DateTime _startDate;
        public DateTime startDate
        {
            get { return _startDate; }
            set { _startDate = value; }
        }

        private DateTime _endDate;
        public DateTime endDate
        {
            get { return _endDate; }
            set { _endDate = value; }
        }

        public class GridData
        {
            public string ActivityId { get; set; }
            public string StartTime { get; set; }
            public float Longitude { get; set; }
            public float Latitude { get; set; }
            public float Speed { get; set; }
            public string Type { get; set; }
            //public string Link { get; set; }

            public float KpIndex { get; set; }
            public string ObservedTime { get; set; }
            public string Source { get; set; }

        } // end class GridData

        // ------------------------------------------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            Esri.ArcGISRuntime.ArcGISRuntimeEnvironment.ApiKey = _esriApiKey;

            // Set up the basemap.
            MySceneView.Scene = new Scene(BasemapStyle.ArcGISImagery);

            initPlotModelSwepam();
            initPlotModelMag();
            initPlotModelDst();
            initPlotModelKPEst();
            initPlotModelHp();

            if (refreshAtStart) refreshFeed();

            backgroundWorker.DoWork += backgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            backgroundWorker.RunWorkerAsync("");

            _endDate = DateTime.Now;
            _startDate = DateTime.Today.AddMonths(-1);

            string applicationDirectory = Directory.GetCurrentDirectory();
            string infoFile = Path.Combine(applicationDirectory, "SWPC.html");
            WebInfo.Navigate(new Uri("file:///"+ infoFile));

        } // end MainWindow

        // ------------------------------------------------------------------------------------------------------

        DataGridTextColumn dgc = new DataGridTextColumn();

        public async void loadDonki()
        {

            log("DONKI API : Start Date " + _startDate);
            log("DONKI API : End Date " + _endDate);

            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                GridDataCollection.Clear()
            )); // end invoke

            int selectedDonkiService = 1;

            DonkiService.Dispatcher.Invoke(() =>
            {
                selectedDonkiService = DonkiService.SelectedIndex;
                if (selectedDonkiService == 0) { 
                    DonkiData.Dispatcher.Invoke(() =>
                    {
                        DonkiData.Columns.Clear();
                        dgc = new DataGridTextColumn();
                        dgc.Header = "ID";
                        dgc.Binding = new Binding("ActivityId");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "StartTime";
                        dgc.Binding = new Binding("StartTime");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Longitude";
                        dgc.Binding = new Binding("Longitude");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Latitude";
                        dgc.Binding = new Binding("Latitude");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Speed";
                        dgc.Binding = new Binding("Speed");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Type";
                        dgc.Binding = new Binding("Type");
                        DonkiData.Columns.Add(dgc);
                    }); // end invoke
                } // end if

                if (selectedDonkiService == 1)
                {
                    DonkiData.Dispatcher.Invoke(() =>
                    {
                        DonkiData.Columns.Clear();
                        dgc = new DataGridTextColumn();
                        dgc.Header = "ID";
                        dgc.Binding = new Binding("ActivityId");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "StartTime";
                        dgc.Binding = new Binding("StartTime");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Kp Index";
                        dgc.Binding = new Binding("KpIndex");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Observed Time";
                        dgc.Binding = new Binding("ObservedTime");
                        DonkiData.Columns.Add(dgc);
                        dgc = new DataGridTextColumn();
                        dgc.Header = "Source";
                        dgc.Binding = new Binding("Source");
                        DonkiData.Columns.Add(dgc);
                    }); // end invoke
                } // end if

            }); // end invoke

            if (selectedDonkiService == 0)
            try
            {
                cmeResults = await _donkiApi.GetCoronalMassEjection(NasaAPIKey, startDate, endDate);
                if (cmeResults != null)
                    log("DONKI API (CME) Number of results : " + cmeResults.Count);
                else
                    log("DONKI API (CME) Number of results : 0");

                    if (cmeResults != null)
				{
					foreach (var item in cmeResults)
					{
                        GridData gd = new GridData();
                        BotApiBox.Dispatcher.Invoke(() =>
                        {
                            BotApiBox.Text += ($"- Activity.ID: {item.ActivityId} ") + "\r\n";
                            gd.ActivityId = item.ActivityId;
                            BotApiBox.Text += ($"- Activity was at: {item.StartTime} ") + "\r\n";
                            gd.StartTime = item.StartTime;
                            if (item.CmeAnalyses != null)
                            {
                                foreach (var cmeanalysis in item.CmeAnalyses)
                                {
                                    if (cmeanalysis != null)
                                    {
                                        if (cmeanalysis.Longitude != null)
                                        {
                                            BotApiBox.Text += ($"- CmeAnalysis.Longitude: {cmeanalysis.Longitude} ") + "\r\n";
                                            gd.Longitude = (float)cmeanalysis.Longitude;
                                        }
                                        else gd.Longitude = 0;
                                        if (cmeanalysis.Longitude != null)
                                        {
                                            BotApiBox.Text += ($"- CmeAnalysis.Latitude: {cmeanalysis.Latitude} ") + "\r\n";
                                            gd.Latitude = (float)cmeanalysis.Longitude;
                                        }
                                        else gd.Latitude = 0;
                                        if (cmeanalysis.Longitude != null)
                                        {
                                            BotApiBox.Text += ($"- CmeAnalysis.Speed: {cmeanalysis.Speed} ") + "\r\n";
                                            gd.Speed = (float)cmeanalysis.Speed;
                                        }
                                        else gd.Speed = 0;
                                        if (cmeanalysis.Type != null)
                                        {
                                            BotApiBox.Text += ($"- CmeAnalysis.Type: {cmeanalysis.Type} ") + "\r\n";
                                            gd.Type = cmeanalysis.Type;
                                        }
                                        gd.Type = "";
                                    } // end if
                                } // end foreach
                            } // end if
                            BotApiBox.Text += ($"- Note: {item.Note} ") + "\r\n";
                            BotApiBox.Text += "----------------------------------------------------\r\n";

                            GridDataCollection.Add(gd);
                        }); // end invoke
                    } // end foreach
                } // end if

                DonkiData.Dispatcher.Invoke(() =>
                {
                    DonkiData.ItemsSource = GridDataCollection;
                });

            } catch (Exception err)
			{
                Console.WriteLine("Error : " + err.StackTrace);
			} // end try

            // ----------------------------------------------------------------------------------------------------------------

            if (selectedDonkiService == 1)
            try
			{

                stormResults = await _donkiApi.GetGeomagneticStorm(NasaAPIKey, startDate, endDate);
                if (stormResults != null)
                    log("DONKI API (GEOMAG) Number of results : " + stormResults.Count);
                else
                    log("DONKI API (CME) Number of results : 0");

                if (stormResults != null)
                {
                    foreach (var item in stormResults)
                    {
                        GridData gd = new GridData();
                        BotApiBox.Dispatcher.Invoke(() =>
                        {
                            BotApiBox.Text += ($"- Activity.ID: {item.GstId} ") + "\r\n";
                            gd.ActivityId = item.GstId;
                            BotApiBox.Text += ($"- Activity was at: {item.StartTime} ") + "\r\n";
                            gd.StartTime = item.StartTime;
                            if (item.AllKpIndex != null)
                            {
                                foreach (var kpi in item.AllKpIndex)
                                {
                                    BotApiBox.Text += ($"- KpIndex: {kpi.KpIndex} ") + "\r\n";
                                    gd.KpIndex = kpi.KpIndex;
                                    BotApiBox.Text += ($"- ObservedTime: {kpi.ObservedTime} ") + "\r\n";
                                    gd.ObservedTime = kpi.ObservedTime;
                                    BotApiBox.Text += ($"- Source: {kpi.Source} ") + "\r\n";
                                    gd.Source = kpi.Source;
                                }
                            } // end if
                            if (item.LinkedEvents != null)
                            {
                                foreach (var lev in item.LinkedEvents)
                                {
                                    BotApiBox.Text += "- LinkedEvents : ";
                                    BotApiBox.Text += ($"{lev.ActivityId} ");
                                    BotApiBox.Text += "\r\n";
                                } // end foreach
                            } // end if
                            BotApiBox.Text += "----------------------------------------------------\r\n";

                            GridDataCollection.Add(gd);
                        }); // end invoke
                    } // end foreach
                } // end if

                DonkiData.Dispatcher.Invoke(() =>
                {
                    DonkiData.ItemsSource = GridDataCollection;
                });

            } catch (Exception err)
			{
                Console.WriteLine("Error : " + err.StackTrace);
            } // end try

        } // end initNasaBot

        // ------------------------------------------------------------------------------------------------------

        public void alert(string line)
        {
            AlertBox.Dispatcher.Invoke(() =>
            {
                AlertBox.Text += line + "\r\n";
                //LogBox.CaretIndex = LogBox.Text.Length;
                //LogBox.ScrollToEnd();
            });
        } // end alert

        public void alertClear()
        {
            AlertBox.Dispatcher.Invoke(() =>
            {
                AlertBox.Clear();
            });
        } // end alertClear


        public void log(string line)
        {
            LogBox.Dispatcher.Invoke(() =>
            {
                LogBox.Text += line + "\r\n";
                //LogBox.CaretIndex = LogBox.Text.Length;
                //LogBox.ScrollToEnd();
            });
        } // end log

        public void logClear()
        {
            LogBox.Dispatcher.Invoke(() =>
            {
                LogBox.Clear();
            });
        } // end logClear

        public void refreshFeed()
        {
            try
            {
                jsonProcessMagnetometers7day("https://services.swpc.noaa.gov/json/goes/primary/magnetometers-7-day.json");
                jsonProcessAceSwepam("https://services.swpc.noaa.gov/json/ace/swepam/ace_swepam_1h.json");
                jsonProcessGeospaceDst("https://services.swpc.noaa.gov/json/geospace/geospace_dst_7_day.json");
                jsonProcessKP7day("https://services.swpc.noaa.gov/json/geospace/geospce_pred_est_kp_7_day.json");
                jsonProcessOvationAurora("https://services.swpc.noaa.gov/json/ovation_aurora_latest.json");
                jsonProcessEnlil("https://services.swpc.noaa.gov/json/enlil_time_series.json");
                processHemisphericPower("https://services.swpc.noaa.gov/text/aurora-nowcast-hemi-power.txt");

                log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Refresh feed.");
            }
            catch (Exception err)
            {
                Console.WriteLine("JSON ERROR : " + err.Message);
            }
        } // end refreshFeed

        // ------------------------------------------------------------------------------------------------------

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

                alertClear();

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

                foreach (Alerts _alert in AlertsOutput)
                {
                    //int result = DateTime.Compare(alert.issue_datetime, DateTime.Now);
                    //Console.WriteLine("TIME: " + dstindex.time_tag + " KP: " + dstindex.dst);
                    if ((DateTime.Now - _alert.issue_datetime).TotalDays < 3)
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
                        int codeindex = _alert.message.IndexOf("Space Weather Message Code:");
                        int searchlength = "Space Weather Message Code:".Length;
                        string code = _alert.message.Substring(
                            codeindex + searchlength + 1, 
                            TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - searchlength - 2);
                        alert("{" + _alert.issue_datetime + "} " + code);

                        try
                        {
                            try
                            {
                                codeindex = _alert.message.IndexOf("ALERT:");
                                code = _alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                                alert("{" + _alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try
                            try
                            {
                                codeindex = _alert.message.IndexOf("WATCH:");
                                int prevIndex = TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex);
                                code = _alert.message.Substring(
                                    codeindex,
                                    prevIndex - codeindex - 1);
                                alert("{" + _alert.issue_datetime + "} " + code);
                                code = _alert.message.Substring(
                                    prevIndex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 2, codeindex));
                                alert("{" + _alert.issue_datetime + "} " + code);

                            }
                            catch (Exception err)
                            {
                            } // end try

                            try
                            {
                                codeindex = _alert.message.IndexOf("SUMMARY:");
                                code = _alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                                alert("{" + _alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try

                            //Begin Time: 2022 May 24 2243 UTC
                            codeindex = _alert.message.IndexOf("Begin Time:");
                            code = _alert.message.Substring(
                                codeindex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                            alert("{" + _alert.issue_datetime + "} " + code);

                            try
                            {
                                codeindex = _alert.message.IndexOf("Threshold Reached:");
                                code = _alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                                alert("{" + _alert.issue_datetime + "} " + code);
                            } catch (Exception err)
                            {
                            } // end try

                            try
                            {
                                codeindex = _alert.message.IndexOf("Estimated Velocity:");
                                code = _alert.message.Substring(
                                    codeindex,
                                    TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                                alert("{" + _alert.issue_datetime + "} " + code);
                            }
                            catch (Exception err)
                            {
                            } // end try

                            alert("------------------------------------------------");

                        }
                        catch (Exception err)
                        {
                        } // end try

                        try
                        {
                            codeindex = _alert.message.IndexOf("WARNING:");
                            code = _alert.message.Substring(
                                codeindex, 
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                            alert("{" + _alert.issue_datetime + "} " + code);
                            codeindex = _alert.message.IndexOf("Valid From:");
                            code = _alert.message.Substring(
                                codeindex, 
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(_alert.message, "\n", 1, codeindex) - codeindex - 1);
                            alert("{" + _alert.issue_datetime + "} " + code);
                            alert("------------------------------------------------");
                        } catch(Exception err)
                        {
                        } // end try

                    } // end if
                } // end foreach

                //Console.WriteLine("------------------------------------------------");

                float KPPMAX = 0;
                DateTime KPPMAXTIME = DateTime.Now;

                foreach (PlanetaryKIndex pkindex in PlanetaryKIndexOutput)
                {
                    //Console.WriteLine("TIME: " + pkindex.time_tag + " KP: " + pkindex.kp_index);
                    if (pkindex.kp_index > 2)
                    {
                        //log("Datetime = {" + pkindex.time_tag + "} KP=" + pkindex.kp_index);
                        if (pkindex.kp_index > KPPMAX)
                        {
                            KPPMAX = pkindex.kp_index;
                            KPPMAXTIME = pkindex.time_tag;
                        } // END IF
                    } // end if
                } // end foreach

                if (KPPMAX > 0)
                    alert("Datetime = {" + KPPMAXTIME + "} KP1h.EST.MAX=" + KPPMAX);

                //Console.WriteLine("------------------------------------------------");

                float DSTMIN = 600;
                DateTime DSTMINTIME = DateTime.Now;

                foreach (GeospaceDst dstindex in Dst1hOutput)
                {
                    //Console.WriteLine("TIME: " + dstindex.time_tag + " KP: " + dstindex.dst);
                    if (dstindex.dst < -25)
                    {
                        //log("Datetime = {" + dstindex.time_tag + "} DST=" + dstindex.dst);
                        //log("Datetime = {" + pkindex.time_tag + "} KP=" + pkindex.kp_index);
                        if (dstindex.dst < DSTMIN)
                        {
                            DSTMIN = dstindex.dst;
                            DSTMINTIME = dstindex.time_tag;
                        } // END IF
                    } // end if
                } // end foreach

                if (DSTMIN != 600)
                    alert("Datetime = {" + DSTMINTIME + "} MIN.DST=" + DSTMIN);

                //Console.WriteLine("------------------------------------------------");

                float KPMAX = 0;
                DateTime KPMAXTIME = DateTime.Now;
                foreach (KP1hEst kpestindex in PredEstKp1hOutput)
                {
                    //Console.WriteLine("TIME: " + dstindex.time_tag + " KP: " + dstindex.dst);
                    if (kpestindex.k > 4)
                    {
                        if (kpestindex.k > KPMAX)
                        {
                            KPMAX = kpestindex.k;
                            KPMAXTIME = kpestindex.model_prediction_time;
                        } // END IF
                    } // end if
                } // end foreach

                if (KPMAX > 0)
                    alert("Datetime = {" + KPMAXTIME + "} KP.EST.MAX=" + KPMAX);

                //e.Result = 1000;
            } // end while
        } // end backgroundWorker_DoWork

        public void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        public void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

        // -------------------------------------------------------------------------
        public class Data_HP
        {
            public DateTime Observation_Time { get; set; }
            public DateTime Forecast_Time { get; set; }
            public int NorthHemisphericPower { get; set; }
            public int SouthHemisphericPower { get; set; }
        }
        public List<PlotModel> PlotModelsHP { get; set; }
        List<HemisphericPower> HemisphericPowerOutput;

        private async void processHemisphericPower(string url)
        {
            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "INSERT INTO hemispheric (\"Observation_Time\", \"Forecast_Time\", \"NorthHemisphericPower\", \"SouthHemisphericPower\") VALUES (@Observation_Time, @Forecast_Time, @NorthHemisphericPower, @SouthHemisphericPower) ON CONFLICT DO NOTHING";

            HemisphericPowerOutput = new List<HemisphericPower>();

            PlotModelsHP = new List<PlotModel>();
            var plotModelNorth = new PlotModel
            {
                Title = "North Hemisphere Power (GW)"
            };
            var plotModelSouth = new PlotModel
            {
                Title = "South Hemisphere Power (GW)"
            };

            var lineSeriesNorth = new OxyPlot.Series.LineSeries();
            var lineSeriesSouth = new OxyPlot.Series.LineSeries();
            var xaNorth = new OxyPlot.Axes.DateTimeAxis();
            xaNorth.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaSouth = new OxyPlot.Axes.DateTimeAxis();
            xaSouth.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            plotModelNorth.Axes.Add(xaNorth);
            plotModelNorth.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelSouth.Axes.Add(xaSouth);
            plotModelSouth.Axes.Add(new OxyPlot.Axes.LinearAxis());

            string result = await DownloadPage(url, downloadAlertJSON);
            if (result != "")
            {
                using (var stream = TeslaInstituteSpaceWeatherUtils.GenerateStreamFromString(result))
                {
                    StreamReader reader = new StreamReader(stream);
                    string line = reader.ReadLine();
                    while (line != string.Empty && line != null)
                    {
                        if (line[0] != '#')
                        {
                            string[] data = line.Split(' ');
                            HemisphericPower hemisphericPower = new HemisphericPower();
                            CultureInfo provider = CultureInfo.InvariantCulture;
                            hemisphericPower.Observation_Time = DateTime.ParseExact(data[0], "yyyy-MM-dd_HH:mm", provider);
                            hemisphericPower.Forecast_Time = DateTime.ParseExact(data[4], "yyyy-MM-dd_HH:mm", provider);
                            try
                            {
                                hemisphericPower.NorthHemisphericPower = Int32.Parse(data[9]);
                            }
                            catch (Exception err)
                            {

                            }
                            try
                            {
                                hemisphericPower.NorthHemisphericPower = Int32.Parse(data[10]);
                            }
                            catch (Exception err)
                            {

                            }
                            try
                            {
                                hemisphericPower.SouthHemisphericPower = Int32.Parse(data[15]);
                            }
                            catch (Exception err)
                            {

                            }
                            try
                            {
                                hemisphericPower.SouthHemisphericPower = Int32.Parse(data[16]);
                            }
                            catch (Exception err)
                            {

                            }

                            HemisphericPowerOutput.Add(hemisphericPower);
                            /*Console.WriteLine(
                                "TIME : " + hemisphericPower.Observation_Time + 
                                " NHPI: " + hemisphericPower.NorthHemisphericPower + 
                                " SHPI: " + hemisphericPower.SouthHemisphericPower);
                            */

                            lineSeriesNorth.Points.Add(
                                new OxyPlot.DataPoint(
                                    OxyPlot.Axes.DateTimeAxis.ToDouble(hemisphericPower.Observation_Time), hemisphericPower.NorthHemisphericPower));
                            lineSeriesSouth.Points.Add(
                                new OxyPlot.DataPoint(
                                    OxyPlot.Axes.DateTimeAxis.ToDouble(hemisphericPower.Observation_Time), hemisphericPower.SouthHemisphericPower));

                            using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                            {
                                command.Parameters.AddWithValue("@Observation_Time", hemisphericPower.Observation_Time);
                                command.Parameters.AddWithValue("@Forecast_Time", hemisphericPower.Forecast_Time);
                                command.Parameters.AddWithValue("@NorthHemisphericPower", hemisphericPower.NorthHemisphericPower);
                                command.Parameters.AddWithValue("@SouthHemisphericPower", hemisphericPower.SouthHemisphericPower);
                                command.ExecuteNonQuery();
                            } // end using
                        } // end if
                        line = reader.ReadLine();
                    } // end while
                } // end using

                plotModelNorth.Series.Add(lineSeriesNorth);
                plotModelSouth.Series.Add(lineSeriesSouth);
                PlotModelsHP.Add(plotModelNorth);
                PlotModelsHP.Add(plotModelSouth);

                this.DataContext = this;

                HP.InvalidateVisual();
            } // end if

            conn.Close();

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved HP to DB.");

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end processHemisphericPower

        // ------------------------------------------------------------------------------------------------------
        List<Alerts> AlertsOutput;

        private async void jsonProcessAlerts(string url)
        {
            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            AlertsOutput = JsonConvert.DeserializeObject<List<Alerts>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessAlerts

        // ------------------------------------------------------------------------------------------------------
        List<KP1hEst> PredEstKp1hOutput;

        private async void jsonProcessPredEstKp1h(string url)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new KP1EstConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            PredEstKp1hOutput = JsonConvert.DeserializeObject<List<KP1hEst>>(jsonresult, settings);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPredEstKp1h

        // ------------------------------------------------------------------------------------------------------
        List<PlanetaryKIndex> PlanetaryKIndexOutput;

        private async void jsonProcessPlanetaryKIndex(string url)
        {
            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            PlanetaryKIndexOutput = JsonConvert.DeserializeObject<List<PlanetaryKIndex>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex
        
        // ------------------------------------------------------------------------------------------------------
        List<GeospaceDst> Dst1hOutput;

        private async void jsonProcessDst1h(string url)
        {
            string jsonresult = await DownloadPage(url, downloadAlertJSON);
            Dst1hOutput = JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult);

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex
        // ------------------------------------------------------------------------------------------------------

        private async void jsonProcessEnlil(string url)
        {
            string jsonresult = await DownloadPage(url, downloadJSON);
            List<Enlil> doc =
                //JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult, new JsonExponentialConverter(typeof(List<Magnetometers7day>)));
                JsonConvert.DeserializeObject<List<Enlil>>(jsonresult);
        } // end jsonProcessEnlil

        // ------------------------------------------------------------------------------------------------------
        string Magnetometers7dayOutput = "";

        public class Data_he
        {
            public DateTime timetag { get; set; }
            public double he { get; set; }
        }
        public List<Data_he> HePoints { get; set; }

        public class Data_hn
        {
            public DateTime timetag { get; set; }
            public double hn { get; set; }
        }
        public List<Data_hn> HnPoints { get; set; }

        public class Data_hp
        {
            public DateTime timetag { get; set; }
            public double hp { get; set; }
        }
        public List<Data_hp> HpPoints { get; set; }

        public class Data_total
        {
            public DateTime timetag { get; set; }
            public double total { get; set; }
        }

        public List<PlotModel> PlotModelsMag { get; set; }

        private async void jsonProcessMagnetometers7day(string url)
        {

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "INSERT INTO magnetometers_7 (time_tag, satellite, \"He\", \"Hp\", \"Hn\", total, arcjet_flag) VALUES (@time_tag, @satellite, @He, @Hp, @Hn, @total, @arcjet_flag) ON CONFLICT DO NOTHING";
            
            // ------------------------------------------------------------------------------------------------------

            string jsonresult = await DownloadPage(url, downloadJSON);
            List<Magnetometers7day> doc =
                //JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult, new JsonExponentialConverter(typeof(List<Magnetometers7day>)));
                JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult);

            HePoints = new List<Data_he>();
            HnPoints = new List<Data_hn>();
            HpPoints = new List<Data_hp>();

            PlotModelsMag = new List<PlotModel>();
            var plotModelHe = new PlotModel
            {
                Title = "He"
            };
            var plotModelHn = new PlotModel
            {
                Title = "Hn"
            };
            var plotModelHp = new PlotModel
            {
                Title = "Hp"
            };
            var lineSeriesHe = new OxyPlot.Series.LineSeries();
            var lineSeriesHn = new OxyPlot.Series.LineSeries();
            var lineSeriesHp = new OxyPlot.Series.LineSeries();
            var xaHe = new OxyPlot.Axes.DateTimeAxis();
            xaHe.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaHn = new OxyPlot.Axes.DateTimeAxis();
            xaHn.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaHp = new OxyPlot.Axes.DateTimeAxis();
            xaHp.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            plotModelHe.Axes.Add(xaHe);
            plotModelHe.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelHn.Axes.Add(xaHn);
            plotModelHn.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelHp.Axes.Add(xaHp);
            plotModelHp.Axes.Add(new OxyPlot.Axes.LinearAxis());

            foreach (Magnetometers7day magindex in doc)
            {
                string line = " TIME: " + magindex.time_tag + " He: " + magindex.He + " Hn: " + magindex.Hn + " Hp: " + magindex.Hp;
                //Console.WriteLine(line);
                //Magnetometers7dayOutput += line + "\r\n";
                //he.Points.AddXY(magindex.time_tag, magindex.He);
                //hn.Points.AddXY(magindex.time_tag, magindex.Hn);
                //hp.Points.AddXY(magindex.time_tag, magindex.Hp);
                HePoints.Add(new Data_he() { timetag = magindex.time_tag, he = magindex.He });
                HnPoints.Add(new Data_hn() { timetag = magindex.time_tag, hn = magindex.Hn });
                HpPoints.Add(new Data_hp() { timetag = magindex.time_tag, hp = magindex.Hp });

                lineSeriesHe.Points.Add(
                    new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(magindex.time_tag), magindex.He));
                lineSeriesHn.Points.Add(
                    new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(magindex.time_tag), magindex.Hn));
                lineSeriesHp.Points.Add(
                    new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(magindex.time_tag), magindex.Hp));

                using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                {
                    command.Parameters.AddWithValue("@time_tag", magindex.time_tag);
                    command.Parameters.AddWithValue("@satellite", magindex.satellite);
                    command.Parameters.AddWithValue("@He", magindex.He);
                    command.Parameters.AddWithValue("@Hp", magindex.Hp);
                    command.Parameters.AddWithValue("@Hn", magindex.Hn);
                    command.Parameters.AddWithValue("@total", magindex.total);
                    command.Parameters.AddWithValue("@arcjet_flag", magindex.arcjet_flag);
                    command.ExecuteNonQuery();
                } // end using

            } // end foreach

            plotModelHe.Series.Add(lineSeriesHe);
            plotModelHn.Series.Add(lineSeriesHn);
            plotModelHp.Series.Add(lineSeriesHp);
            PlotModelsMag.Add(plotModelHe);
            PlotModelsMag.Add(plotModelHn);
            PlotModelsMag.Add(plotModelHp);

            this.DataContext = this;

            Mag.InvalidateVisual();

            conn.Close();

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved Mag to DB.");

        } // end jsonProcessMagnetometers7day

        // -------------------------------------------------------------------------
        string AceSwepamOutput = "";

        public class Data_dens
        {
            public DateTime timetag { get; set; }
            public float dens { get; set; }
        }
        public List<Data_dens> DensPoints { get; set; }

        public class Data_speed
        {
            public DateTime timetag { get; set; }
            public float speed { get; set; }
        }
        public List<Data_speed> SpeedPoints { get; set; }

        public class Data_temperature
        {
            public DateTime timetag { get; set; }
            public float temperature { get; set; }
        }
        public List<Data_temperature> TemperaturePoints { get; set; }

        public List<PlotModel> PlotModelsAce { get; set; }

        private async void jsonProcessAceSwepam(string url)
        {
            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "INSERT INTO ace_swepam_1h (time_tag, dsflag, dens, speed, temperature) VALUES (@time_tag, @dsflag, @dens, @speed, @temperature) ON CONFLICT DO NOTHING";
            // ------------------------------------------------------------------------------

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new JsonExponentialConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url, downloadJSON);
            List<AceSwepam> doc =
                //JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, new JsonExponentialConverter(typeof(List<AceSwepam>)));
                JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, settings);

            DensPoints = new List<Data_dens>();
            SpeedPoints = new List<Data_speed>();
            TemperaturePoints = new List<Data_temperature>();

            PlotModelsAce = new List<PlotModel>();
            var plotModelDens = new PlotModel
            {
                Title = "Density"
            };
            var plotModelSpeed = new PlotModel
            {
                Title = "Speed"
            };
            var plotModelTemp = new PlotModel
            {
                Title = "Temp"
            };
            var lineSeriesDens = new OxyPlot.Series.LineSeries();
            var lineSeriesSpeed = new OxyPlot.Series.LineSeries();
            var lineSeriesTemp = new OxyPlot.Series.LineSeries();
            var xaDens = new OxyPlot.Axes.DateTimeAxis();
            xaDens.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaSpeed = new OxyPlot.Axes.DateTimeAxis();
            xaSpeed.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaTemp = new OxyPlot.Axes.DateTimeAxis();
            xaTemp.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            plotModelDens.Axes.Add(xaDens);
            plotModelDens.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelSpeed.Axes.Add(xaSpeed);
            plotModelSpeed.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelTemp.Axes.Add(xaTemp);
            plotModelTemp.Axes.Add(new OxyPlot.Axes.LinearAxis());

            foreach (AceSwepam acindex in doc)
            {
                string line = 
                    " TIME: " + acindex.time_tag + 
                    " DENSITY: " + acindex.dens + 
                    " SPEED: " + acindex.speed + 
                    " TEMP: " + acindex.temperature;
                //Console.WriteLine(line);
                //AceSwepamOutput += line + "\r\n";

                DensPoints.Add(new Data_dens() { timetag = acindex.time_tag, dens = acindex.dens });
                SpeedPoints.Add(new Data_speed() { timetag = acindex.time_tag, speed = acindex.speed });
                TemperaturePoints.Add(new Data_temperature() { timetag = acindex.time_tag, temperature = acindex.temperature });

                if ( (acindex.dsflag != 3) && (acindex.speed != -500000) )
                {
                    lineSeriesDens.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(acindex.time_tag), acindex.dens));
                    lineSeriesSpeed.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(acindex.time_tag), acindex.speed));
                    lineSeriesTemp.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(acindex.time_tag), acindex.temperature));
                } // end if

                using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                {
                    command.Parameters.AddWithValue("@time_tag", acindex.time_tag);
                    command.Parameters.AddWithValue("@dsflag", acindex.dsflag);
                    command.Parameters.AddWithValue("@dens", acindex.dens);
                    command.Parameters.AddWithValue("@speed", acindex.speed);
                    command.Parameters.AddWithValue("@temperature", acindex.temperature);
                    command.ExecuteNonQuery();
                } // end using

            } // end foreach

            plotModelDens.Series.Add(lineSeriesDens);
            plotModelSpeed.Series.Add(lineSeriesSpeed);
            plotModelTemp.Series.Add(lineSeriesTemp);
            PlotModelsAce.Add(plotModelDens);
            PlotModelsAce.Add(plotModelSpeed);
            PlotModelsAce.Add(plotModelTemp);

            this.DataContext = this;

            Ace.InvalidateVisual();

            conn.Close();

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved Swepam to DB.");

        } // end jsonProcessAceSwepam

        // -------------------------------------------------------------------------
        string GeospaceDstOutput = "";

        public class Data_Dst
        {
            public DateTime timetag { get; set; }
            public float dst { get; set; }
        }

        public List<PlotModel> PlotModelsDst { get; set; }

        private async void jsonProcessGeospaceDst(string url)
        {

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "INSERT INTO geospace_dst_7 (time_tag, dst) VALUES (@time_tag, @dst) ON CONFLICT DO NOTHING";

            // ------------------------------------------------------------------------------

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new JsonGeospaceDstConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url, downloadJSON);
            List<GeospaceDst> doc =
                //JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, new JsonExponentialConverter(typeof(List<AceSwepam>)));
                JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult, settings);

            PlotModelsDst = new List<PlotModel>();
            var plotModel = new PlotModel
            {
                Title = "DST"
            };
            var lineSeries = new OxyPlot.Series.LineSeries();
            var xa = new OxyPlot.Axes.DateTimeAxis();
            xa.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            plotModel.Axes.Add(xa);
            plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis());

            //DstPoints = new List<Data_Dst>();
            foreach (GeospaceDst dstindex in doc)
            {
                string line = " TIME: " + dstindex.time_tag + " DST: " + dstindex.dst;
                //Console.WriteLine(line);
                GeospaceDstOutput += line + "\r\n";
                //DstPoints.Add(new Data_Dst() { timetag = dstindex.time_tag, dst = dstindex.dst });
                //dst.Points.AddXY(dstindex.time_tag, dstindex.dst);
                lineSeries.Points.Add(
                    new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dstindex.time_tag), dstindex.dst));

                /*
                 * INSERT INTO "MQTeslaSolarWeather".public.geospace_dst_7(time_tag, dst) 
                 * VALUES ('2022-05-14 00:00:00', -53.405899::REAL) ON CONFLICT DO NOTHING;
                 */
                using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                {
                    command.Parameters.AddWithValue("@time_tag", dstindex.time_tag);
                    command.Parameters.AddWithValue("@dst", dstindex.dst);
                    command.ExecuteNonQuery();
                } // end using

            } // end foreach

            plotModel.Series.Add(lineSeries);
            PlotModelsDst.Add(plotModel);

            this.DataContext = this;

            Dst.InvalidateVisual();

            conn.Close();

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved Dst to DB.");

            //textBox5.Text = GeospaceDstOutput;
        } // end jsonProcessGeospaceDstOutput

        // -------------------------------------------------------------------------
        string KP7dayOutput = "";

        public class Data_kp7day
        {
            public DateTime timetag { get; set; }
            public float kp7day { get; set; }
        }
        //public List<Data_kp7day> Kp7dayPoints { get; set; }

        public List<PlotModel> PlotModelsKp { get; set; }

        private async void jsonProcessKP7day(string url)
        {
            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "INSERT INTO est_kp_7 (model_prediction_time, k) VALUES (@model_prediction_time, @k) ON CONFLICT DO NOTHING";

            // ------------------------------------------------------------------------------

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(new KP7DatConverter());
            settings.Formatting = Formatting.Indented;

            string jsonresult = await DownloadPage(url, downloadJSON);
            List<KP7day> doc = JsonConvert.DeserializeObject<List<KP7day>>(jsonresult, settings);

            //Kp7dayPoints = new List<Data_kp7day>();

            PlotModelsKp = new List<PlotModel>();
            var plotModel = new PlotModel
            {
                Title = "KP7DAY"
            };
            var lineSeries = new OxyPlot.Series.LineSeries();
            var xa = new OxyPlot.Axes.DateTimeAxis();
            xa.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            plotModel.Axes.Add(xa);
            plotModel.Axes.Add(new OxyPlot.Axes.LinearAxis());

            foreach (KP7day pkindex in doc)
            {
                string line = " TIME: " + pkindex.model_prediction_time + " KP7: " + pkindex.k;
                
                lineSeries.Points.Add(
                    new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(pkindex.model_prediction_time), pkindex.k));

                using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                {
                    command.Parameters.AddWithValue("@model_prediction_time", pkindex.model_prediction_time);
                    command.Parameters.AddWithValue("@k", pkindex.k);
                    command.ExecuteNonQuery();
                } // end using

            } // end foreach

            plotModel.Series.Add(lineSeries);
            PlotModelsKp.Add(plotModel);

            this.DataContext = this;

            Kp.InvalidateVisual();

            conn.Close();

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved Kp7 to DB.");

            //chart5.DataContext = Kp7dayPoints;

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessKP7day

        // -------------------------------------------------------------------------
        string OvationAuroraOutput = "";

        private async void jsonProcessOvationAurora(string url)
        {
            string jsonresult = await DownloadPage(url, downloadJSON);
            OvationAurora doc = JsonConvert.DeserializeObject<OvationAurora>(jsonresult);
            string time = " OBSERVATION TIME: " + doc.Observation_Time + " FORECAST TIME: " + doc.Forecast_Time;
            //logBox.Text += time + "\r\n";

            List<int[]> coordinates = new List<int[]>();
            List<GeoPoint> geoPoints = new List<GeoPoint>();

            List<Field> pointFields = new List<Field>();
            Field placeField = new Field(Esri.ArcGISRuntime.Data.FieldType.Text, "Place", "Place Name", 50);
            pointFields.Add(placeField);
            FeatureCollectionTable pointsTable =
                new FeatureCollectionTable(pointFields, Esri.ArcGISRuntime.Geometry.GeometryType.Point, SpatialReferences.Wgs84);
            pointsTable.Renderer = CreateRenderer(Esri.ArcGISRuntime.Geometry.GeometryType.Point);

            // Create a new renderer for the States Feature Layer.
            SimpleLineSymbol lineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.Black, 1);
            SimpleFillSymbol fillSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, System.Drawing.Color.Transparent, lineSymbol);

            // Set States feature layer renderer.
            //featuresLayer.Renderer = new SimpleRenderer(fillSymbol);


            // Create a feature collection and add the feature collection tables
            features = new Esri.ArcGISRuntime.Data.FeatureCollection();
            features.Tables.Add(pointsTable);

            FeatureCollectionLayer collectionLayer = new FeatureCollectionLayer(features);
            if (MySceneView.Scene.OperationalLayers.Count == 0)
            {
                MySceneView.Scene.OperationalLayers.Add(collectionLayer);
            }
            else
            {
                MySceneView.Scene.OperationalLayers.Clear();
                MySceneView.Scene.OperationalLayers.Add(collectionLayer);
            } // end if

            GraphicsOverlay _overlay = new GraphicsOverlay();
            if (MySceneView.GraphicsOverlays.Count == 0)
            {
                MySceneView.GraphicsOverlays.Add(_overlay);
            }
            else
            {
                MySceneView.GraphicsOverlays.Clear();
                MySceneView.GraphicsOverlays.Add(_overlay);
            } // end if

            foreach (int[] oa in doc.coordinates)
            {
                string line = "LONG: " + oa[0] + " LAT: " + oa[1] + " AURORA: " + oa[2];
                //logBox.Text += line + "\r\n";
                //Console.WriteLine(line);

                GeoPoint globePoint = new GeoPoint(oa[0], oa[1]);
                geoPoints.Add(globePoint);

                Esri.ArcGISRuntime.Data.Feature feature = (Esri.ArcGISRuntime.Data.Feature)pointsTable.CreateFeature();
                MapPoint point1 = new MapPoint(oa[0], oa[1], SpatialReferences.Wgs84);
                feature.Geometry = point1;
                SimpleMarkerSymbol simpleMarker = new SimpleMarkerSymbol(
                    SimpleMarkerSymbolStyle.Circle, System.Drawing.Color.FromArgb(128, 0, 255, 255), oa[2]);

                // Create graphics and add them to graphics overlay
                Graphic graphic = new Graphic(point1, simpleMarker);
                _overlay.Graphics.Add(graphic);


            } // end foreach

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessOvationAurora

        // -------------------------------------------------------------------------


        public async Task<string> DownloadPage(string url, bool save)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //client.DefaultRequestHeaders.Add(htmlheader, APIKEY);
                    Uri uri = new Uri(url);
                    using (var r = await client.GetAsync(uri))
                    {
                        string result = await r.Content.ReadAsStringAsync();
                        string jsonfile = "";
                        string jsonuuid = Guid.NewGuid().ToString();
                        jsonfile = System.IO.Path.GetFileName(uri.AbsolutePath.Substring(0, uri.AbsolutePath.Length - 5));
                        if (save)
                            File.WriteAllText(jsonpath + jsonfile + "-" + jsonuuid + ".json", result);
                        return result;
                    } // end using
                } // end using
            } catch (Exception err)
			{
                log("ERROR (DownloadPage) : " + err.Message);
                return null;
			} // end try
        } // end DownloadPage

        // -------------------------------------------------------------------------

        public void listFiles(string url, string extension)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string html = reader.ReadToEnd();
                    Regex regex = new Regex(TeslaInstituteSpaceWeatherUtils.GetDirectoryListingRegexForUrl(url));
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
                            } // end if
                        } // end foreach
                    } // end if
                } // end using
            } // end using
        } // end listFiles

        // -------------------------------------------------------------------------

        private Renderer CreateRenderer(Esri.ArcGISRuntime.Geometry.GeometryType rendererType)
        {
            // Return a simple renderer to match the geometry type provided
            Symbol sym = null;

            switch (rendererType)
            {
                case Esri.ArcGISRuntime.Geometry.GeometryType.Point:
                case Esri.ArcGISRuntime.Geometry.GeometryType.Multipoint:
                    // Create a marker symbol
                    sym = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Triangle, System.Drawing.Color.Red, 18);
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polyline:
                    // Create a line symbol
                    sym = new SimpleLineSymbol(SimpleLineSymbolStyle.Dash, System.Drawing.Color.Green, 3);
                    break;
                case Esri.ArcGISRuntime.Geometry.GeometryType.Polygon:
                    // Create a fill symbol
                    SimpleLineSymbol lineSym = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Drawing.Color.DarkBlue, 2);
                    sym = new SimpleFillSymbol(SimpleFillSymbolStyle.DiagonalCross, System.Drawing.Color.Cyan, lineSym);
                    break;
                default:
                    break;
            }

            // Return a new renderer that uses the symbol created above
            return new SimpleRenderer(sym);
        } // end CreateRenderer

        private void RefreshButtonClick(object sender, RoutedEventArgs e)
        {
            refreshFeed();
        } // end RefreshButtonClick

        // --------------------------------------------------------------------------------

        OxyPlot.Series.LineSeries lineSeriesDens = new OxyPlot.Series.LineSeries();
        OxyPlot.Series.LineSeries lineSeriesSpeed = new OxyPlot.Series.LineSeries();
        OxyPlot.Series.LineSeries lineSeriesTemp = new OxyPlot.Series.LineSeries();
        PlotModel plotModelDens;
        PlotModel plotModelSpeed;
        PlotModel plotModelTemp;

        // -------------------------------------------------------------------------

        public void initPlotModelSwepam()
		{
            PlotModelsAce = new List<PlotModel>();
            plotModelDens = new PlotModel
            {
                Title = "Density"
            };
            plotModelSpeed = new PlotModel
            {
                Title = "Speed"
            };
            plotModelTemp = new PlotModel
            {
                Title = "Temp"
            };
            var xaDens = new OxyPlot.Axes.DateTimeAxis();
            xaDens.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaSpeed = new OxyPlot.Axes.DateTimeAxis();
            xaSpeed.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaTemp = new OxyPlot.Axes.DateTimeAxis();
            xaTemp.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            plotModelDens.Axes.Add(xaDens);
            plotModelDens.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelSpeed.Axes.Add(xaSpeed);
            plotModelSpeed.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelTemp.Axes.Add(xaTemp);
            plotModelTemp.Axes.Add(new OxyPlot.Axes.LinearAxis());

            plotModelDens.Series.Add(lineSeriesDens);
            plotModelSpeed.Series.Add(lineSeriesSpeed);
            plotModelTemp.Series.Add(lineSeriesTemp);
            PlotModelsAce.Add(plotModelDens);
            PlotModelsAce.Add(plotModelSpeed);
            PlotModelsAce.Add(plotModelTemp);

            Ace.DataContext = this;
            Ace.InvalidateVisual();
        } // end initPlotModelSwepam

        // -------------------------------------------------------------------------

        private void SwepamButtonClick(object sender, RoutedEventArgs e)
        {
            //HePoints = new List<Data_he>();
            //HnPoints = new List<Data_hn>();
            //HpPoints = new List<Data_hp>();

            lineSeriesDens.Points.Clear();
            lineSeriesSpeed.Points.Clear();
            lineSeriesTemp.Points.Clear();

            // ---------------------------------------------------------------------------------------------------------

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "select * from ace_swepam_1h";
            //INSERT INTO "MQTeslaSolarWeather".public.ace_swepam_1h(time_tag, dsflag, dens, speed, temperature) VALUES ('2022-05-22 14:00:00', 1, 1.2928715::REAL, 489.76825::REAL, 187539.41::REAL);
            NpgsqlCommand command = new NpgsqlCommand(sql, conn);
            using (var dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    if (((int)dr[1] != 3) && ((float)dr[3] != -500000))
                    {
                        lineSeriesDens.Points.Add(
                            new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[2]));
                        lineSeriesSpeed.Points.Add(
                            new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[3]));
                        lineSeriesTemp.Points.Add(
                            new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[4]));
                    } // end if
                } // end while
            } // end using

            plotModelDens.InvalidatePlot(true);
            plotModelSpeed.InvalidatePlot(true);
            plotModelTemp.InvalidatePlot(true);

            conn.Close();
        } // end SwepamButtonClick

        // -------------------------------------------------------------------------

        OxyPlot.Series.LineSeries lineSeriesKPEst = new OxyPlot.Series.LineSeries();
        PlotModel plotModelKPEst;

        public void initPlotModelKPEst()
		{
            PlotModelsKp = new List<PlotModel>();
            plotModelKPEst = new PlotModel
            {
                Title = "KP7DAY"
            };
            var xa = new OxyPlot.Axes.DateTimeAxis();
            xa.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            plotModelKPEst.Axes.Add(xa);
            plotModelKPEst.Axes.Add(new OxyPlot.Axes.LinearAxis());

            plotModelKPEst.Series.Add(lineSeriesKPEst);
            PlotModelsKp.Add(plotModelKPEst);

            this.DataContext = this;

            Kp.InvalidateVisual();
        } // end 

        private void KPEstButtonClick(object sender, RoutedEventArgs e)
        {
            lineSeriesKPEst.Points.Clear();

            // ---------------------------------------------------------------------------------------------------------

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "select * from est_kp_7";
            NpgsqlCommand command = new NpgsqlCommand(sql, conn);
            using (var dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    lineSeriesKPEst.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[1]));
                } // end while
            } // end using

            plotModelKPEst.InvalidatePlot(true);

            conn.Close();
        } // end 

        // -------------------------------------------------------------------------

        OxyPlot.Series.LineSeries lineSeriesDst = new OxyPlot.Series.LineSeries();
        PlotModel plotModelDst;

        public void initPlotModelDst()
		{
            PlotModelsDst = new List<PlotModel>();
            plotModelDst = new PlotModel
            {
                Title = "DST"
            };
            var xa = new OxyPlot.Axes.DateTimeAxis();
            xa.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            plotModelDst.Axes.Add(xa);
            plotModelDst.Axes.Add(new OxyPlot.Axes.LinearAxis());

            plotModelDst.Series.Add(lineSeriesDst);
            PlotModelsDst.Add(plotModelDst);

            Dst.DataContext = this;
            Dst.InvalidateVisual();

        } // end initPlotModelDst

        // -------------------------------------------------------------------------

        private void DstButtonClick(object sender, RoutedEventArgs e)
        {

            // ---------------------------------------------------------------------------------------------------------

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "select * from geospace_dst_7";
            //(time_tag, satellite, \"He\", \"Hp\", \"Hn\", total, arcjet_flag) VALUES (@time_tag, @satellite, @He, @Hp, @Hn, @total, @arcjet_flag) ON CONFLICT DO NOTHING"
            NpgsqlCommand command = new NpgsqlCommand(sql, conn);
            using (var dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    lineSeriesDst.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[1]));
                } // end while
            } // end using

            plotModelDst.InvalidatePlot(true);

            conn.Close();
        } // end click

        // -------------------------------------------------------------------------

        OxyPlot.Series.LineSeries lineSeriesHe = new OxyPlot.Series.LineSeries();
        OxyPlot.Series.LineSeries lineSeriesHn = new OxyPlot.Series.LineSeries();
        OxyPlot.Series.LineSeries lineSeriesHp = new OxyPlot.Series.LineSeries();
        PlotModel plotModelHe;
        PlotModel plotModelHn;
        PlotModel plotModelHp;

        // -------------------------------------------------------------------------

        public void initPlotModelMag()
		{
            PlotModelsMag = new List<PlotModel>();
            plotModelHe = new PlotModel
            {
                Title = "He"
            };
            plotModelHn = new PlotModel
            {
                Title = "Hn"
            };
            plotModelHp = new PlotModel
            {
                Title = "Hp"
            };
            lineSeriesHe = new OxyPlot.Series.LineSeries();
            lineSeriesHn = new OxyPlot.Series.LineSeries();
            lineSeriesHp = new OxyPlot.Series.LineSeries();
            var xaHe = new OxyPlot.Axes.DateTimeAxis();
            xaHe.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaHn = new OxyPlot.Axes.DateTimeAxis();
            xaHn.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaHp = new OxyPlot.Axes.DateTimeAxis();
            xaHp.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            plotModelHe.Axes.Add(xaHe);
            plotModelHe.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelHn.Axes.Add(xaHn);
            plotModelHn.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelHp.Axes.Add(xaHp);
            plotModelHp.Axes.Add(new OxyPlot.Axes.LinearAxis());

            plotModelHe.Series.Add(lineSeriesHe);
            plotModelHn.Series.Add(lineSeriesHn);
            plotModelHp.Series.Add(lineSeriesHp);
            PlotModelsMag.Add(plotModelHe);
            PlotModelsMag.Add(plotModelHn);
            PlotModelsMag.Add(plotModelHp);

            Mag.DataContext = this;
        } // end initPlotModel

        private void MagButtonClick(object sender, RoutedEventArgs e)
        {
            //HePoints = new List<Data_he>();
            //HnPoints = new List<Data_hn>();
            //HpPoints = new List<Data_hp>();
            lineSeriesHe.Points.Clear();
            lineSeriesHn.Points.Clear();
            lineSeriesHp.Points.Clear();

            // ---------------------------------------------------------------------------------------------------------

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "select * from magnetometers_7";
            //(time_tag, satellite, \"He\", \"Hp\", \"Hn\", total, arcjet_flag) VALUES (@time_tag, @satellite, @He, @Hp, @Hn, @total, @arcjet_flag) ON CONFLICT DO NOTHING"
            NpgsqlCommand command = new NpgsqlCommand(sql, conn);
            using (var dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    lineSeriesHe.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[2]));
                    lineSeriesHn.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[4]));
                    lineSeriesHp.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (float)dr[3]));
                } // end while
            } // end using

            plotModelHe.InvalidatePlot(true);
            plotModelHn.InvalidatePlot(true);
            plotModelHp.InvalidatePlot(true);

            conn.Close();
        } // end click

        // -------------------------------------------------------------------------

        OxyPlot.Series.LineSeries lineSeriesNorth = new OxyPlot.Series.LineSeries();
        OxyPlot.Series.LineSeries lineSeriesSouth = new OxyPlot.Series.LineSeries();
        PlotModel plotModelNorth;
        PlotModel plotModelSouth;

        // -------------------------------------------------------------------------

        public void initPlotModelHp()
		{
            PlotModelsHP = new List<PlotModel>();
            plotModelNorth = new PlotModel
            {
                Title = "North Hemisphere Power (GW)"
            };
            plotModelSouth = new PlotModel
            {
                Title = "South Hemisphere Power (GW)"
            };

            lineSeriesNorth = new OxyPlot.Series.LineSeries();
            lineSeriesSouth = new OxyPlot.Series.LineSeries();
            var xaNorth = new OxyPlot.Axes.DateTimeAxis();
            xaNorth.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            var xaSouth = new OxyPlot.Axes.DateTimeAxis();
            xaSouth.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");

            plotModelNorth.Axes.Add(xaNorth);
            plotModelNorth.Axes.Add(new OxyPlot.Axes.LinearAxis());
            plotModelSouth.Axes.Add(xaSouth);
            plotModelSouth.Axes.Add(new OxyPlot.Axes.LinearAxis());

            plotModelNorth.Series.Add(lineSeriesNorth);
            plotModelSouth.Series.Add(lineSeriesSouth);
            PlotModelsHP.Add(plotModelNorth);
            PlotModelsHP.Add(plotModelSouth);

            HP.DataContext = this;
        } // end initPlotModelHp

        private void HPButtonClick(object sender, RoutedEventArgs e)
        {
            lineSeriesNorth.Points.Clear();
            lineSeriesSouth.Points.Clear();

            // ---------------------------------------------------------------------------------------------------------

            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "select * from hemispheric";
            NpgsqlCommand command = new NpgsqlCommand(sql, conn);
            using (var dr = command.ExecuteReader())
            {
                while (dr.Read())
                {
                    lineSeriesNorth.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (int)dr[2]));
                    lineSeriesSouth.Points.Add(
                        new OxyPlot.DataPoint(OxyPlot.Axes.DateTimeAxis.ToDouble(dr[0]), (int)dr[3]));
                } // end while
            } // end using

            plotModelNorth.InvalidatePlot(true);
            plotModelSouth.InvalidatePlot(true);

            HP.InvalidateVisual();
        } // end HPButtonClick

        private void SaveDstGraph_Click(object sender, RoutedEventArgs e)
        {
            //PngExporter.Export(this.PlotModelsDst, stream, 800, 600);
            string filename = "DST_" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
            PngExporter.Export(
                PlotModelsDst.ToArray()[0],
                graphPath + filename + ".png", 
                8000, 
                2000, 
                600);

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved Dst graph to " + graphPath);
        }

        private void SaveMagGraph_Click(object sender, RoutedEventArgs e)
        {
            string[] magcode = new string[] { "He", "Hn", "Hp" };
            for (int i = 0; i < 3; i++)
            {
                string filename = "MAG_" + magcode[i] + "_" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
                PngExporter.Export(
                    PlotModelsMag.ToArray()[i],
                    graphPath + filename + ".png",
                    8000,
                    2000,
                    600);
            } // end for

            log(DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + " Saved Magnetometer graphs to " + graphPath);

        } // end SaveMagGraph_Click

		private void SaveDstCSV_Click(object sender, RoutedEventArgs e)
		{
            string filename = "DST_" + DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss");
            string pathname = csvPath + filename + ".csv";
            List<DataPoint> datapoints = lineSeriesDst.Points;
            string csv = "";
            foreach (DataPoint dp in datapoints)
			{
                csv += dp.X + ", " + dp.Y + "\r\n";
			} // end foreach
            File.WriteAllText(pathname, csv);
        } // end SaveDstCSV_Click

		private void loadDonkiButtonClick(object sender, RoutedEventArgs e)
		{
            loadDonkiThread = new Thread(new ThreadStart(loadDonki));
            loadDonkiThread.Start();
        }
	} // end MainWindow

} // end class