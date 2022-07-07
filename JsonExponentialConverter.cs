using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaInstituteSpaceWeather
{
    public class JsonExponentialConverter : JsonConverter
    {
        //private readonly Type[] _types;

        //public JsonExponentialConverter(params Type[] types)
        //{
        //    _types = types;
        //}

        public override bool CanRead { get { return true; } }
        public override bool CanConvert(Type objectType)
        {
            return true;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }

        /*
         *  time_tag : 20.5.2022. 23:00:00
            dsflag : 0
            dens : 0,95125556
            speed : 551,70007
            temperature : 120035,93
         */
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<Form1.AceSwepam> list = new List<Form1.AceSwepam>();
            Form1.AceSwepam item = new Form1.AceSwepam();
            int i = -1;
            //while (reader.TokenType != JsonToken.EndObject)
            while (reader.Read())
            {
                //reader.Read();
                if (reader.Value != null)
                {
                    string key = reader.Value.ToString();
                    //Console.Write(key);
                    // ------------------
                    //Console.Write(" : ");
                    reader.Read();
                    float amount = 0;
                    string val = "";
                    if (reader.Value == null)
                    {
                        amount = 0;
                    }
                    else
                    {
                        val = reader.Value.ToString();
                        //Console.WriteLine(val);
                    }
                    //if (float.TryParse(reader.Value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
                    {
                    }
                    if (reader.Value == null)
                    {
                        amount = 0;
                    } else 
                        float.TryParse(reader.Value.ToString(), out amount);

                    if (i % 5 == 0) item.time_tag = DateTime.Parse(val);
                    if (i % 5 == 1) item.dsflag = Int32.Parse(val);
                    if (i % 5 == 2) item.dens = amount;
                    if (i % 5 == 3) item.speed = amount;
                    if (i % 5 == 4) item.temperature = amount;
                } // end if
                //if (i % 5 == 0) list.Add(item);
                if (reader.TokenType == JsonToken.EndObject)
                {
                    list.Add(item);
                    item = new Form1.AceSwepam();
                    i = -2;
                }
                i++;
            } // end while

            return list;
        }

    }
}