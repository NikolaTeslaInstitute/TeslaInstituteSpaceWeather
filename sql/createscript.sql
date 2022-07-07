-- Database: TeslaInstituteSpaceWeather

-- DROP DATABASE IF EXISTS "TeslaInstituteSpaceWeather";

CREATE DATABASE "TeslaInstituteSpaceWeather"
    WITH
    OWNER = boris
    ENCODING = 'UTF8'
    LC_COLLATE = 'en_US.UTF-8'
    LC_CTYPE = 'en_US.UTF-8'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Table: public.ace_swepam_1h

-- DROP TABLE IF EXISTS public.ace_swepam_1h;

CREATE TABLE IF NOT EXISTS public.ace_swepam_1h
(
    time_tag timestamp without time zone NOT NULL,
    dsflag integer,
    dens real,
    speed real,
    temperature real,
    CONSTRAINT ace_swepam_1h_pkey PRIMARY KEY (time_tag)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.ace_swepam_1h
    OWNER to postgres;

-- Table: public.est_kp_7

-- DROP TABLE IF EXISTS public.est_kp_7;

CREATE TABLE IF NOT EXISTS public.est_kp_7
(
    model_prediction_time timestamp without time zone NOT NULL,
    k real,
    CONSTRAINT est_kp_7_pkey PRIMARY KEY (model_prediction_time)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.est_kp_7
    OWNER to postgres;

-- Table: public.geospace_dst_7

-- DROP TABLE IF EXISTS public.geospace_dst_7;

CREATE TABLE IF NOT EXISTS public.geospace_dst_7
(
    time_tag timestamp without time zone NOT NULL,
    dst real,
    CONSTRAINT geospace_dst_7_pkey PRIMARY KEY (time_tag)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.geospace_dst_7
    OWNER to postgres;

-- Table: public.hemispheric

-- DROP TABLE IF EXISTS public.hemispheric;

CREATE TABLE IF NOT EXISTS public.hemispheric
(
    "Observation_Time" timestamp without time zone NOT NULL,
    "Forecast_Time" timestamp without time zone,
    "NorthHemisphericPower" integer,
    "SouthHemisphericPower" integer,
    CONSTRAINT hemispheric_pkey PRIMARY KEY ("Observation_Time")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.hemispheric
    OWNER to postgres;

-- Table: public.magnetometers_7

-- DROP TABLE IF EXISTS public.magnetometers_7;

CREATE TABLE IF NOT EXISTS public.magnetometers_7
(
    time_tag timestamp without time zone NOT NULL,
    satellite integer,
    "He" real,
    "Hp" real,
    "Hn" real,
    total real,
    arcjet_flag boolean,
    CONSTRAINT "magnetometers-7_pkey" PRIMARY KEY (time_tag)
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public.magnetometers_7
    OWNER to postgres;

