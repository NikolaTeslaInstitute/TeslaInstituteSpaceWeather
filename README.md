HAMSCI

NIKOLA TESLA INSTITUTE
SPACE WEATHER STATION SYSTEM
 
1) WINDOWS FORMS - DESKTOP APPLICATION
2) WINDOW WPF - DESKTOP APPLICATION
3) WINDOWS FORMS ALERT - MONO SUITED VERSION OF DESKTOP APPLICATION
4) CONSOLE PROGRAM IS INTENDED TO BE RUN ON RASPBERRY PI TERMINAL 24/7 WITH THE USE OF MONO

PREREQUISITES INSTALLATION : </br>
	sudo apt install mono-complete</br>
INSTALLATION FAQ FOR MONO : https://pimylifeup.com/raspberry-pi-mono-framework/</br>
	sudo apt install postgresql</br>
INSTALLATION FAQ FOR POSTGRESQL : https://pimylifeup.com/raspberry-pi-postgresql/</br>

TWO SIMULTANEOUS TASKS FOR RETREIVING NOAA DATA AND ALERTING USERS
1) LOOPING EVERY 60 SECONDS WITH DATA RETREIVAL
	DATA INPUT IS SWPC NOAA TEXT AND JSON PRODUCTS
	https://services.swpc.noaa.gov/products/alerts.json
	https://services.swpc.noaa.gov/json/planetary_k_index_1m.json
	ttps://services.swpc.noaa.gov/json/geospace/geospace_pred_est_kp_1_hour.json
	https://services.swpc.noaa.gov/json/geospace/geospace_dst_1_hour.json
2) LOOPING EVERY 90 MINUTES WITH GEOSPACE DATA RETREIVAL, CHECK, DATABASE STORAGE AND MULTICHANNEL ALERTING
	https://services.swpc.noaa.gov/json/goes/primary/magnetometers-7-day.json
	https://services.swpc.noaa.gov/json/ace/swepam/ace_swepam_1h.json
	https://services.swpc.noaa.gov/json/geospace/geospace_dst_7_day.json
	https://services.swpc.noaa.gov/json/geospace/geospce_pred_est_kp_7_day.json
	https://services.swpc.noaa.gov/json/ovation_aurora_latest.json
	https://services.swpc.noaa.gov/json/enlil_time_series.json
	https://services.swpc.noaa.gov/text/aurora-nowcast-hemi-power.txt
 
CREATION OF THE SPACE WEATHER DATABASE

DATA IS COLLECTED FROM THE NATIONAL OCEANIC AND ATMOSPHERIC ADMINISTRATION SPACE WEATHER PREDICTION CENTER (NOAA-SWPC)

ACE/SWEPAM (1h)

<img src="screen/Screen-4.jpg"><br/>

ESTIMATED KP INDEX (7 days)

DST INDEX (7 days)

<img src="screen/Screen-3.jpg"><br/>

HEMISPHERIC POWER

<img src="screen/Screen-1.jpg"><br/>

GOES MAGNETOMETERS (1-minute data, 7 days timespan)</br>
https://www.swpc.noaa.gov/products/goes-magnetometer</br>
Historically, the data have been presented in the E (earthward), P (parallel) and N (normal) coordinate system where:
Hp:  magnetic field vector component, points northward, perpendicular to the orbit plane which for a zero degree inclination orbit is parallel to Earth's spin axis.
He:  magnetic field vector component, perpendicular to Hp and Hn and points earthward.
Hn:  magnetic field vector component, perpendicular to Hp and He and points eastward.

<img src="screen/Screen-5.jpg"><br/>

WSA-ENLIL SOLAR WIND PREDICTION</br>
https://www.swpc.noaa.gov/products/wsa-enlil-solar-wind-prediction</br>

AURORA OVATION</br>
https://services.swpc.noaa.gov/json/ovation_aurora_latest.json</br>

HOW-TO SETUP OF ESRI ARCGIS DEVELOPER ACCOUNT TO USE WITH AURORA OVAL GIS</br>
https://developers.arcgis.com/net/</br>

NASA DONKI API is utilized to display data table of CMEs from start to end date.</br>
Head over to https://api.NASA.gov to applying for an API key sent you your email</br>

<img src="screen/Screen-8.png"><br/>

The Space Weather Database Of Notifications, Knowledge, Information (DONKI), developed at the Community Coordinated Modeling Center (CCMC), is a comprehensive on-line tool for space weather forecasters, scientists, and the general space science community.</br>
DONKI provides:</br>
Chronicles the daily interpretations of space weather observations, analysis, models output, and notifications provided by the Moon to Mars Space Weather Operations Office as a courtesy to the community.</br>
Comprehensive knowledge-base search functionality to support anomaly resolution and space science research.</br>
Intelligent linkages, relationships, cause-and-effects between space weather activities.</br>
Comprehensive webservice API access to information stored in DONKI</br>
DONKI Goals:</br>
One-stop on-line tool for space weather researchers and forecasters.</br>
Gathers and organizes space weather scientists interpretations and daily activities with correlations and direct links between relevant space weather observations.</br>
Enables remote participation by students, world-wide partners, model and forecasting technique developers.</br>

<img src="screen/Screen-6.jpg"><br/>
<img src="screen/Screen-7.jpg"><br/>

POSTGRESSQL IS USED FOR RASPBERRY PI </br>
SQL CREATION SCRIPTS ARE PORVIDED OR</br>
BACKUP RESTORE OF ONE OF OUR DATABASE BACKUPS</br>

CREATION OF TELEGRAM GROUP AND TELEGRAM BOT FOR DST INDEX ALERT AND GRAPHIC DISPLAY</br>
TELEGRAM BOT API IS USED https://github.com/TelegramBots/Telegram.Bot</br>
TELGRAM BOT SETUP FAQ https://creativeminds.helpscoutdocs.com/article/2829-telegram-bot-use-case-how-to-create-a-bot-on-telegram-that-responds-to-group-messages</br>

<img src="telegram/Screenshot_20220708-105439_Telegram.jpg" width="250">&nbsp;<img src="telegram/Screenshot_20220708-055020_Telegram.jpg" width="250">&nbsp;<img src="telegram/Screenshot_20220707-210101_Telegram.jpg" width="250"><br/>
