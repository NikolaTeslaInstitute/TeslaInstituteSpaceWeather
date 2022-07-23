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
using System.Net.Http;
//using System.Web.Script.Serialization;
using System.Collections;
using System.Threading;
using System.Diagnostics;
using System.Globalization;

using HemisphericPower = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.HemisphericPower;
using Alerts = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.Alerts;
using KP1hEst = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.KP1hEst;
using KP7day = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.KP7day;
using PlanetaryKIndex = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.PlanetaryKIndex;
using GeospaceDst = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.GeospaceDst;
using Magnetometers7day = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.Magnetometers7day;
using Enlil = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.Enlil;
using AceSwepam = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.AceSwepam;
using OvationAurora = TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherData.OvationAurora;

using Npgsql;
using Newtonsoft.Json;
using Telegram.Bot;

/*
 * HAMSCI
 * 
 * NIKOLA TESLA INSTITUTE
 * SPACE WEATHER STATION SYSTEM
 * 
 * 1) WINDOWS FORMS - DESKTOP APPLICATION
 * 2) WINDOW WPF - DESKTOP APPLICATION
 * 3) WINDOWS FORMS ALERT - MONO SUITED VERSION OF DESKTOP APPLICATION
 * 4) CONSOLE PROGRAM IS INTENDED TO BE RUN ON RASPBERRY PI TERMINAL 24/7 WITH THE USE OF MONO
 * 
 * PREREQUISITES INSTALLATION : 
 * sudo apt install mono-complete
 * INSTALLATION FAQ FOR MONO : https://pimylifeup.com/raspberry-pi-mono-framework/
 * sudo apt install postgresql
 * INSTALLATION FAQ FOR POSTGRESQL : https://pimylifeup.com/raspberry-pi-postgresql/
 * 
 * TWO SIMULTANEOUS TASKS FOR RETREIVING NOAA DATA AND ALERTING USERS
 * 1) LOOPING EVERY 60 SECONDS WITH DATA RETREIVAL
 *    DATA INPUT IS SWPC NOAA TEXT AND JSON PRODUCTS
 *    https://services.swpc.noaa.gov/products/alerts.json
 *    https://services.swpc.noaa.gov/json/planetary_k_index_1m.json
 *    https://services.swpc.noaa.gov/json/geospace/geospace_pred_est_kp_1_hour.json
 *    https://services.swpc.noaa.gov/json/geospace/geospace_dst_1_hour.json
 * 2) LOOPING EVERY 90 MINUTES WITH GEOSPACE DATA RETREIVAL, CHECK, DATABASE STORAGE AND MULTICHANNEL ALERTING
 *    https://services.swpc.noaa.gov/json/goes/primary/magnetometers-7-day.json
 *    https://services.swpc.noaa.gov/json/ace/swepam/ace_swepam_1h.json
 *    https://services.swpc.noaa.gov/json/geospace/geospace_dst_7_day.json
 *    https://services.swpc.noaa.gov/json/geospace/geospce_pred_est_kp_7_day.json
 *    https://services.swpc.noaa.gov/json/ovation_aurora_latest.json
 *    https://services.swpc.noaa.gov/json/enlil_time_series.json
 *    https://services.swpc.noaa.gov/text/aurora-nowcast-hemi-power.txt
 *    
 * CREATION OF THE SPACE WEATHER DATABASE
 * 
 * DATA IS COLLECTED FROM THE NATIONAL OCEANIC AND ATMOSPHERIC ADMINISTRATION SPACE WEATHER PREDICTION CENTER (NOAA-SWPC)
 * 
 * ACE/SWEPAM (1h)
 * 
 * ESTIMATED KP INDEX (7 days)
 * 
 * DST INDEX (7 days)
 * 
 * HEMISPHERIC POWER
 * 
 * GOES MAGNETOMETERS (1-minute data, 7 days timespan)
 * https://www.swpc.noaa.gov/products/goes-magnetometer
 * Historically, the data have been presented in the E (earthward), P (parallel) and N (normal) coordinate system where:
 * Hp:  magnetic field vector component, points northward, perpendicular to the orbit plane which for a zero degree inclination orbit is parallel to Earth's spin axis.
 * He:  magnetic field vector component, perpendicular to Hp and Hn and points earthward.
 * Hn:  magnetic field vector component, perpendicular to Hp and He and points eastward.
 * 
 * WSA-ENLIL SOLAR WIND PREDICTION
 * https://www.swpc.noaa.gov/products/wsa-enlil-solar-wind-prediction
 * 
 * AURORA OVATION
 * https://services.swpc.noaa.gov/json/ovation_aurora_latest.json
 * 
 * POSTGRESSQL IS USED FOR RASPBERRY PI 
 * SQL CREATION SCRIPTS ARE PORVIDED OR
 * BACKUP RESTORE OF ONE OF OUR DATABASE BACKUPS
 * 
 * CREATION OF TELEGRAM GROUP AND TELEGRAM BOT FOR DST INDEX ALERT AND GRAPHIC DISPLAY
 * TELEGRAM BOT API IS USED https://github.com/TelegramBots/Telegram.Bot
 * TELGRAM BOT SETUP FAQ https://creativeminds.helpscoutdocs.com/article/2829-telegram-bot-use-case-how-to-create-a-bot-on-telegram-that-responds-to-group-messages
 * 
 * HOW-TO SETUP OF ESRI ARCGIS DEVELOPER ACCOUNT TO USE WITH AURORA OVAL GIS
 * https://developers.arcgis.com/net/
 * 
 */

namespace TeslaInstituteSpaceWeatherConsole
{
	class Program
	{

        bool downloadAlertJSON = false;
        bool downloadJSON = false;

        static void Main(string[] args)
		{
            int cyclesleep = 60000; // 1.0 min (in millisec)
            //int cyclesize = 360; // 360 cycles of 0.5 min (180 minutes)
            //int cyclesize = 180; // 180 cycles of 0.5 min (90 minutes)
            int cyclesize = 90; // 90 cycles of 1.0 min (90 minutes)

            string AlertsUrl = @"https://services.swpc.noaa.gov/products/alerts.json";
            string PlanetaryKIndexUrl = @"https://services.swpc.noaa.gov/json/planetary_k_index_1m.json";
            string EstKPindexUrl = @"https://services.swpc.noaa.gov/json/geospace/geospace_pred_est_kp_1_hour.json";
            string DstIndexUrl = @"https://services.swpc.noaa.gov/json/geospace/geospace_dst_1_hour.json";

            // -----------------------------------------------------------------------------------------

            Stopwatch sw = Stopwatch.StartNew();
            int cycles = 0;

            log("Tesla Petrovic Foundation - Space Weather Console");
            Program prog = new Program();

            logOpen();

            //bool refreshAtStart = true;
            //if (refreshAtStart)
            //    ThreadPool.QueueUserWorkItem(o => prog.RefreshDatabase(null));

            // http://t.me/NikolaTeslaInstituteBot
            string telegramToken = "<YOUR TELEGRAM TOKEN HERE>";
            string telegramChatID = "<YOUR TELEGRAM CHAT ID HERE>";

            while (true)
			{
                // -----------------------------------------------------------------------
                // Download/Refresh Data + Display console notifications
                ThreadPool.QueueUserWorkItem(o => prog.BackgroundTask(null));
                Thread.Sleep(cyclesleep); // sleep 30 secs

                log("------------------------------------------------");
                log("TIME UP " + sw.Elapsed);
                if ( (cycles > cyclesize) || (cycles == 0) ) {

                    // -----------------------------------------------------------------------
                    log("NOTIFY TELEGRAM");

                    JsonSerializerSettings settings = new JsonSerializerSettings();
                    settings.Converters.Add(new JsonGeospaceDstConverter());
                    settings.Formatting = Formatting.Indented;

                    string jsonresult = DownloadPage("https://services.swpc.noaa.gov/json/geospace/geospace_dst_1_hour.json");
                    List<GeospaceDst> Dst1hOutput = new List<GeospaceDst>();
                    bool dstAlert = false;
                    if (jsonresult != "")
                    {
                        Dst1hOutput = JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult, settings);
                    } // end if
                    foreach (GeospaceDst dstindex in Dst1hOutput)
                    {
                        if (dstindex.dst < -35)
                        {
                            dstAlert = true;
                            //log("TELEGRAM TIME=" + dstindex.time_tag + ", DST=" + dstindex.dst);
                            //TelegramSendMessage(
                            //    telegramToken, 
                            //    telegramChatID,
                            //    "TIME = " + dstindex.time_tag + "\r\nDST =" + dstindex.dst);
                        } // end if 
                    } // end foreach

                    // -----------------------------------------------------------------------
                    if (dstAlert)
                    {
                        log("TELEGRAM SEND GRAPH");
                        TelegramSendPhoto(
                            telegramToken,
                            telegramChatID,
                            GraphPlot(Dst1hOutput)
                        );
                    } // end if

                    // -----------------------------------------------------------------------
                    log("REFRESH DATABASE");
                    ThreadPool.QueueUserWorkItem(o => prog.RefreshDatabase(null));
                    
                    // reset cycles counter 
                    cycles = 0;
                } // end if
                //if (dt.Minute ==)

                cycles++;

            } // end while

            logClose();
        } // end main
        
        public static void logOpen()
		{

        } // end logOpen

        public static void logClose()
        {

        } // end logClose

        public static void log(string line)
		{
            string datetime = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            Console.WriteLine("[" + datetime + "] " + line);
        } // end log

        public void BackgroundTask(Object stateInfo)
        {
            //"https://services.swpc.noaa.gov/products/solar-wind/mag-5-minute.json"
            //"https://services.swpc.noaa.gov/products/solar-wind/plasma-5-minute.json"
            //"https://services.swpc.noaa.gov/products/alerts.json"
            //"https://services.swpc.noaa.gov/text/aurora-nowcast-hemi-power.txt"

            log("RETRIEVE NOAA DATA");

            jsonProcessAlerts("https://services.swpc.noaa.gov/products/alerts.json");
            jsonProcessPlanetaryKIndex("https://services.swpc.noaa.gov/json/planetary_k_index_1m.json");
            jsonProcessPredEstKp1h("https://services.swpc.noaa.gov/json/geospace/geospace_pred_est_kp_1_hour.json");
            jsonProcessDst1h("https://services.swpc.noaa.gov/json/geospace/geospace_dst_1_hour.json");

            log("------------------------------------------------");

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
            SUM10R	SUMMARY: 10 cm Radio Burst	WOXX03	Proxy of solar EUV emission, fimportant for satellite drag.
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
                int daysCondition = 3;
                bool alertCondition = (DateTime.Now - alert.issue_datetime).TotalDays < daysCondition;
                //Console.WriteLine("ALERT: " + alert.issue_datetime);
                if (alertCondition)
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
                    log("Datetime = {" + alert.issue_datetime + "} " + code);

                    try
                    {
                        try
                        {
                            codeindex = alert.message.IndexOf("ALERT:");
                            code = alert.message.Substring(
                                codeindex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                            log("Datetime = {" + alert.issue_datetime + "} " + code);
                        }
                        catch (Exception err)
                        {
                        } // end try

                        try
                        {
                            codeindex = alert.message.IndexOf("WATCH:");
                            int prevIndex = TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex);
                            code = alert.message.Substring(
                                codeindex,
                                prevIndex - codeindex - 1);
                            log("Datetime = {" + alert.issue_datetime + "} " + code);
                            code = alert.message.Substring(
                                prevIndex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 2, codeindex));
                            log("Datetime = {" + alert.issue_datetime + "} " + code);

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
                            log("Datetime = {" + alert.issue_datetime + "} " + code);
                        }
                        catch (Exception err)
                        {
                        } // end try

                        //Begin Time: 2022 May 24 2243 UTC
                        codeindex = alert.message.IndexOf("Begin Time:");
                        code = alert.message.Substring(
                            codeindex,
                            TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                        log("Datetime = {" + alert.issue_datetime + "} " + code);

                        try
                        {
                            codeindex = alert.message.IndexOf("Threshold Reached:");
                            code = alert.message.Substring(
                                codeindex,
                                TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                            log("Datetime = {" + alert.issue_datetime + "} " + code);
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
                            log("Datetime = {" + alert.issue_datetime + "} " + code);
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
                        log("Datetime = {" + alert.issue_datetime + "} " + code);
                        codeindex = alert.message.IndexOf("Valid From:");
                        code = alert.message.Substring(
                            codeindex,
                            TeslaInstituteSpaceWeatherUtils.IndexOfNth(alert.message, "\n", 1, codeindex) - codeindex - 1);
                        log("Datetime = {" + alert.issue_datetime + "} " + code);
                        log("------------------------------------------------");
                    }
                    catch (Exception err)
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
                log("Datetime = {" + KPPMAXTIME + "} KP1h.EST.MAX=" + KPPMAX);

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
                log("Datetime = {" + DSTMINTIME + "} MIN.DST=" + DSTMIN);

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
                log("Datetime = {" + KPMAXTIME + "} KP.EST.MAX=" + KPMAX);

            //e.Result = 1000;

        } // end backgroundWorker_DoWork

        // ------------------------------------------------------------------------------------------------------
        List<Alerts> AlertsOutput = new List<Alerts>();

        private async void jsonProcessAlerts(string url)
        {
            log("PROCESS : " + url);

            try
            {
                string jsonresult = await DownloadPage(url, downloadAlertJSON);

                if (jsonresult != "")
                {
                    AlertsOutput = JsonConvert.DeserializeObject<List<Alerts>>(jsonresult);
                }  // end if
            }
            catch (Exception err)
            {
                log("ERROR : " + err.StackTrace);
            }
            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessAlerts

        // ------------------------------------------------------------------------------------------------------
        List<PlanetaryKIndex> PlanetaryKIndexOutput = new List<PlanetaryKIndex>();

        private async void jsonProcessPlanetaryKIndex(string url)
        {
            log("PROCESS : " + url);

            try { 

                string jsonresult = await DownloadPage(url, downloadAlertJSON);

                if (jsonresult != "")
                {
                    PlanetaryKIndexOutput = JsonConvert.DeserializeObject<List<PlanetaryKIndex>>(jsonresult);
                } // end if
                }
            catch (Exception err)
            {
                log("ERROR : " + err.StackTrace);
            }

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex

        // ------------------------------------------------------------------------------------------------------
        List<KP1hEst> PredEstKp1hOutput = new List<KP1hEst>();

        private async void jsonProcessPredEstKp1h(string url)
        {
            log("PROCESS : " + url);


            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Converters.Add(new KP1EstConverter());
                settings.Formatting = Formatting.Indented;

                string jsonresult = await DownloadPage(url, downloadAlertJSON);

                if (jsonresult != "")
                {
                    PredEstKp1hOutput = JsonConvert.DeserializeObject<List<KP1hEst>>(jsonresult, settings);
                } // end if
            }
            catch (Exception err)
            {
                log("ERROR : " + err.StackTrace);
            }
            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPredEstKp1h

        // ------------------------------------------------------------------------------------------------------
        List<GeospaceDst> Dst1hOutput = new List<GeospaceDst>();

        private async void jsonProcessDst1h(string url)
        {
            log("PROCESS : " + url);

            try
            {

                string jsonresult = await DownloadPage(url, downloadAlertJSON);

                if (jsonresult != "")
                {
                    Dst1hOutput = JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult);
                } // end if
            } catch (Exception err)
			{
                log("ERROR : " + err.StackTrace);
			}
            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessPlanetaryKIndex

        // -------------------------------------------------------------------------

        static string DownloadPage(string url)
        {
            string result = "";

            try
            {
                using (var client = new HttpClient())
                {
                    //client.DefaultRequestHeaders.Add(htmlheader, APIKEY);
                    Uri uri = new Uri(url);

                    var response = client.GetAsync(url).GetAwaiter().GetResult();
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = response.Content;
                        result = responseContent.ReadAsStringAsync().GetAwaiter().GetResult();
                    } // end if

                } // end using

                return result;
            }
            catch (Exception err)
            {
                log("ERROR (DownloadPage) : " + err.Message);
                return result;
            } // end try
        } // end DownloadPage

        static async Task<string> DownloadPage(string url, bool save)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //client.DefaultRequestHeaders.Add(htmlheader, APIKEY);
                    Uri uri = new Uri(url);
                    using (var r = await client.GetAsync(uri))
                    {
                        //log("Accessed : " + uri);
                        string result = await r.Content.ReadAsStringAsync();
                        string jsonpath = @"../json/";
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
                return "";
            } // end try
        } // end DownloadPage

        string Magnetometers7dayUrl = "https://services.swpc.noaa.gov/json/goes/primary/magnetometers-7-day.json";
        string AceSwepamUrl = "https://services.swpc.noaa.gov/json/ace/swepam/ace_swepam_1h.json";
        string GeospaceDstUrl = "https://services.swpc.noaa.gov/json/geospace/geospace_dst_7_day.json";
        string KP7dayUrl = "https://services.swpc.noaa.gov/json/geospace/geospce_pred_est_kp_7_day.json";
        string OvationAuroraUrl = "https://services.swpc.noaa.gov/json/ovation_aurora_latest.json";
        string EnlilUrl = "https://services.swpc.noaa.gov/json/enlil_time_series.json";
        string HemisphericPowerUrl = "https://services.swpc.noaa.gov/text/aurora-nowcast-hemi-power.txt";

        public void RefreshDatabase(Object stateInfo)
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
            }
            catch (Exception err)
            {
                Console.WriteLine("JSON ERROR : " + err.Message);
            }
        } // end refreshFeed

        // -----------------------------------------------------------------------

        private async void jsonProcessMagnetometers7day(string url)
        {

            var conn = new NpgsqlConnection(
                TeslaInstituteSpaceWeatherConsole.TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();

            var sql = "INSERT INTO magnetometers_7 (time_tag, satellite, \"He\", \"Hp\", \"Hn\", total, arcjet_flag) VALUES (@time_tag, @satellite, @He, @Hp, @Hn, @total, @arcjet_flag) ON CONFLICT DO NOTHING";

            // ------------------------------------------------------------------------------------------------------

            string jsonresult = await DownloadPage(url, downloadJSON);

            if (jsonresult != "")
            {
                List<Magnetometers7day> doc =
                    //JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult, new JsonExponentialConverter(typeof(List<Magnetometers7day>)));
                    JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult);

                foreach (Magnetometers7day magindex in doc)
                {
                    string line = " TIME: " + magindex.time_tag + " He: " + magindex.He + " Hn: " + magindex.Hn + " Hp: " + magindex.Hp;

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

                log("Saved Mag to DB.");

            } // end if

            conn.Close();

        } // end jsonProcessMagnetometers7day

        // -------------------------------------------------------------------

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

            if (jsonresult != "")
            {
                List<AceSwepam> doc =
                //JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, new JsonExponentialConverter(typeof(List<AceSwepam>)));
                JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, settings);

                foreach (AceSwepam acindex in doc)
                {
                    string line =
                        " TIME: " + acindex.time_tag +
                        " DENSITY: " + acindex.dens +
                        " SPEED: " + acindex.speed +
                        " TEMP: " + acindex.temperature;
                    //Console.WriteLine(line);
                    //AceSwepamOutput += line + "\r\n";

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

                log("Saved Swepam to DB.");

            } // end if

            conn.Close();

        } // end jsonProcessAceSwepam

        // -------------------------------------------------------------------

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

            if (jsonresult != "")
            {
                List<GeospaceDst> doc =
                //JsonConvert.DeserializeObject<List<AceSwepam>>(jsonresult, new JsonExponentialConverter(typeof(List<AceSwepam>)));
                JsonConvert.DeserializeObject<List<GeospaceDst>>(jsonresult, settings);

                foreach (GeospaceDst dstindex in doc)
                {
                    string line = " TIME: " + dstindex.time_tag + " DST: " + dstindex.dst;
                    //Console.WriteLine(line);

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

                log("Saved Dst to DB.");

            } // end if

            conn.Close();

            //textBox5.Text = GeospaceDstOutput;
        } // end jsonProcessGeospaceDstOutput

        // -------------------------------------------------------------------

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

            if (jsonresult != "")
            {
                List<KP7day> doc = JsonConvert.DeserializeObject<List<KP7day>>(jsonresult, settings);

                //Kp7dayPoints = new List<Data_kp7day>();

                foreach (KP7day pkindex in doc)
                {
                    string line = " TIME: " + pkindex.model_prediction_time + " KP7: " + pkindex.k;

                    using (NpgsqlCommand command = new NpgsqlCommand(sql, conn))
                    {
                        command.Parameters.AddWithValue("@model_prediction_time", pkindex.model_prediction_time);
                        command.Parameters.AddWithValue("@k", pkindex.k);
                        command.ExecuteNonQuery();
                    } // end using

                } // end foreach

                log("Saved Kp7 to DB.");

            } // end if

            conn.Close();

            //chart5.DataContext = Kp7dayPoints;

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessKP7day

        // -------------------------------------------------------------------

        private async void jsonProcessOvationAurora(string url)
        {
            string jsonresult = await DownloadPage(url, downloadJSON);

            if (jsonresult != "")
            {
                OvationAurora doc = JsonConvert.DeserializeObject<OvationAurora>(jsonresult);
                string time = " OBSERVATION TIME: " + doc.Observation_Time + " FORECAST TIME: " + doc.Forecast_Time;
                //logBox.Text += time + "\r\n";

                List<int[]> coordinates = new List<int[]>();
                List<NetTopologySuite.Geometries.Point> geoPoints =
                    new List<NetTopologySuite.Geometries.Point>();

                int numcoords = 0;

                foreach (int[] oa in doc.coordinates)
                {
                    string line = "LONG: " + oa[0] + " LAT: " + oa[1] + " AURORA: " + oa[2];
                    //logBox.Text += line + "\r\n";
                    //Console.WriteLine(line);
                    numcoords++;
                } // end foreach

                log("Ovation Aurora coordinates : " + numcoords);
            } // end if

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end jsonProcessOvationAurora

        // -------------------------------------------------------------------

        private async void jsonProcessEnlil(string url)
        {
            string jsonresult = await DownloadPage(url, downloadJSON);

            if (jsonresult != "")
            {
                List<Enlil> doc =
                //JsonConvert.DeserializeObject<List<Magnetometers7day>>(jsonresult, new JsonExponentialConverter(typeof(List<Magnetometers7day>)));
                JsonConvert.DeserializeObject<List<Enlil>>(jsonresult);
            } // end if
        } // end jsonProcessEnlil

        // -------------------------------------------------------------------

        private async void processHemisphericPower(string url)
        {
            var conn = new NpgsqlConnection(TeslaInstituteSpaceWeatherDB.connectionString);
            conn.Open();
            var sql = "INSERT INTO hemispheric (\"Observation_Time\", \"Forecast_Time\", \"NorthHemisphericPower\", \"SouthHemisphericPower\") VALUES (@Observation_Time, @Forecast_Time, @NorthHemisphericPower, @SouthHemisphericPower) ON CONFLICT DO NOTHING";

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

                log("Saved HP to DB.");

            } // end if
            conn.Close();

            //textBox4.Text = PlanetaryKIndexOutput;
        } // end processHemisphericPower

        public static string GraphPlot(List<GeospaceDst> dstData)
		{
            string plotPath = "graphtTemp.png";

            double[] dataX = new double[dstData.Count];
            double[] dataY = new double[dstData.Count];

            int i = 0;
            foreach (GeospaceDst dst in dstData)
            {
                dataX[i] = dst.time_tag.ToOADate();
                dataY[i] = dst.dst;
                i++;
            } // end foreach

            var plt = new ScottPlot.Plot(400, 300);
            plt.XAxis.DateTimeFormat(true); 
            plt.AddScatter(dataX, dataY);

            plt.SaveFig(plotPath);
            return plotPath;
        } // end GraphPlot

        public async static void TelegramSendPhoto(string apilToken, string chatId, string photoPath)
        {
            TelegramBotClient Bot = new TelegramBotClient(apilToken);
            var imageFile = File.Open(photoPath, FileMode.Open);
            await Bot.SendPhotoAsync(chatId, photo: imageFile, caption: "DST GRAPH");
        } // end 

        public static string TelegramSendMessage(string apilToken, string destID, string text)
        {
            string urlString = $"https://api.telegram.org/bot{apilToken}/sendMessage?chat_id={destID}&text={text}";

            WebClient webclient = new WebClient();
            string result = "";
            try
            {
                result = webclient.DownloadString(urlString);
            } catch (Exception err)
            {

            } // end try

            return result;
        } // end TelegramSendMessage

    } // end class
}
