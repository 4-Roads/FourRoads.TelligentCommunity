using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using FourRoads.Common.TelligentCommunity.Components;
using FourRoads.TelligentCommunity.PowerBI.Analytics.Language.Models;
using Newtonsoft.Json;

namespace FourRoads.TelligentCommunity.PowerBI
{
    public class WatsonLanguage
    {
        private static string AuthToken;
        private static string LanguageUrl;

        public WatsonLanguage(string apikey, string url)
        {
            LanguageUrl = url;
            AuthToken = Base64Encode("apikey:" + apikey);
        }

        #region Anaylse a piece of text using watson nlp to csv 

        public string KeyPhrasesCSV(string text)
        {
            return String.Join(",", KeyPhrases(text).Select(x => x.ToString()).ToArray());
        }
        #endregion


        #region Anaylse a piece of text using watson nlp to list 
        public List<string> KeyPhrases(string text)
        {

            List<string> keywords = new List<string>();

            WatsonRequest request = new WatsonRequest()
            {
                text = text,
                features = new Features()
                {
                    keywords = new Keywords()
                    {
                        limit = 20
                    }
                }
            };

            //POST web request
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }));

            //POST web request
            int attempt = 1;
            var success = false;
            string responseContent = string.Empty;

            while (!success && attempt <= 3)
            {
                attempt++;
                success = SendRequest(LanguageUrl, "POST", byteArray, ref responseContent);
            }

            if (success)
            {
                WatsonResponse response = JsonConvert.DeserializeObject<WatsonResponse>(responseContent);
                if (response != null && response.keywords != null && response.keywords.Any())
                {
                    foreach (var keyword in response.keywords)
                    {
                        keywords.Add(keyword.text);
                    }
                }
            }

            return keywords;
        }
        #endregion

        private bool SendRequest(string url, string method, byte[] content, ref string response)
        {
            bool status = false;
            response = string.Empty;

            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;

                request.Method = method;
                request.KeepAlive = true;
                request.ContentLength = 0;
                request.ContentType = "application/json";

                //Add token to the request header
                request.Headers.Add("Authorization", $"Basic {AuthToken}");

                if (content != null)
                {
                    request.ContentLength = content.Length;

                    //Write JSON byte[] into a Stream
                    using (Stream writer = request.GetRequestStream())
                    {
                        writer.Write(content, 0, content.Length);
                    }
                }

                using (HttpWebResponse httpResponse = (HttpWebResponse)request.GetResponse())
                {
                    //Get StreamReader that holds the response stream  
                    using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        response = reader.ReadToEnd();
                        status = true;
                    }
                }
            }
            catch (WebException e)
            {
                using (WebResponse errorResponse = e.Response)
                {
                    HttpWebResponse httpResponse = (HttpWebResponse)errorResponse;

                    // too many requests
                    if ((int)httpResponse.StatusCode == 429)
                    {
                        System.Threading.Thread.Sleep(1000);
                    }

                    using (Stream data = errorResponse.GetResponseStream())
                    using (var reader = new StreamReader(data))
                    {
                        response = reader.ReadToEnd();
                        new TCException($"Power BI Client - Watson Request failed - Status {httpResponse.StatusCode} - Response {response}").Log();
                    }

                }
            }
            return status;
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

    }
}

