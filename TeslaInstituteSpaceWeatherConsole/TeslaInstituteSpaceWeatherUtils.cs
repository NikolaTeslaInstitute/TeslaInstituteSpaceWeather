using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TeslaInstituteSpaceWeatherConsole
{
    class TeslaInstituteSpaceWeatherUtils
    {

        public static int IndexOfNth(string str, string c, int nth, int startPosition)
        {
            int index = str.IndexOf(c, startPosition);
            if (index >= 0 && nth > 1)
            {
                return IndexOfNth(str, c, nth - 1, index + 1);
            }

            return index;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static string GetDirectoryListingRegexForUrl(string url)
        {
            //return "<a href=\".*\">(?<name>.*)</a>";
            return "<a href=\"(?<name>.*)\">";
        }


        public static List<string> listDirectory(string url, string extension)
        {
            List<string> output = new List<string>();

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
                                    Console.WriteLine("FILE=" + url + m);
                                    output.Add("FILE=" + url + m);
                                }
                                else if ((m.Contains("/")) && (!m.Contains("Last modified")))
                                {
                                    Console.WriteLine("DIR=" + m);
                                    output.Add("DIR=" + m);
                                } // end if
                            }
                        }
                    }
                }
            }
            return output;
        }

    }
}
