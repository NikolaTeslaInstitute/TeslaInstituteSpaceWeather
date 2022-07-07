using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TeslaInstituteSpaceWeather
{
    public class JsonGeospaceDstConverter : JsonConverter
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
            dst : 0
         */
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            List<Form1.GeospaceDst> list = new List<Form1.GeospaceDst>();
            Form1.GeospaceDst item = new Form1.GeospaceDst();
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
                    }
                    else
                        float.TryParse(reader.Value.ToString(), out amount);

                    if (i % 5 == 0) item.time_tag = DateTime.Parse(val);
                    if (i % 5 == 1) item.dst = amount;
                } // end if
                //if (i % 5 == 0) list.Add(item);
                if (reader.TokenType == JsonToken.EndObject)
                {
                    list.Add(item);
                    item = new Form1.GeospaceDst();
                    i = -2;
                }
                i++;
            } // end while

            return list;
        }

    }
}
