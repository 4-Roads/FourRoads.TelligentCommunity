using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Web;

namespace FourRoads.TelligentCommunity.Nexus2.Common
{
    public static class OAuthFunctions
    {
        public static string RemoveVerificationCodeFromUri(HttpContextBase context)
        {
            if (context == null)
                throw new ArgumentException("Context null");

            if (context.Request == null)
                throw new ArgumentException("Context.Request null");

            if (context.Request.Url == null)
                throw new ArgumentException("Context.Request.Url null");

            return RemoveVerificationCodeFromUri(context.Request.Url);
        }

        public static string RemoveVerificationCodeFromUri(Uri uri)
        {
            NameValueCollection nameValues = HttpUtility.ParseQueryString(uri.Query);

            if (nameValues["code"] != null)
                nameValues.Remove("code");

            string updatedQueryString = "?" + nameValues;

            return uri.AbsolutePath + updatedQueryString;
        }

        public static string WebRequest(string method, string url, string postData, NameValueCollection headers = null)
        {
            HttpWebRequest webRequest = (HttpWebRequest)System.Net.WebRequest.Create(url);

            webRequest.Method = method;
            webRequest.ServicePoint.Expect100Continue = false;
          
            if (headers != null)
                webRequest.Headers.Add(headers);

            if (string.Compare(method , "POST" , StringComparison.OrdinalIgnoreCase) == 0)
            {
                webRequest.ContentType = "application/x-www-form-urlencoded";
             
                using (StreamWriter streamWriter = new StreamWriter(webRequest.GetRequestStream()))
                {
                    streamWriter.Write(postData);
                }
            }

            return WebResponseGet(webRequest);
        }

        public static string WebResponseGet(HttpWebRequest webRequest)
        {
            if (webRequest == null)
                throw new ArgumentException("webRequest null");

            using (WebResponse response = webRequest.GetResponse())
            {
                using (Stream repsonseStream = response.GetResponseStream())
                {
                    if (repsonseStream == null)
                        throw new ArgumentException("repsonseStream is null");

                    using (StreamReader streamReader = new StreamReader(repsonseStream))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }

        public static NameValueCollection GetVerificationParameters(HttpContextBase context)
        {
            if (context == null)
                throw new ArgumentException("Context null");

            if (context.Request == null)
                throw new ArgumentException("Context.Request null");

            if (context.Request.QueryString["code"] == null)
                return null;

            NameValueCollection nameValueCollection = new NameValueCollection();
            
            nameValueCollection.Add("verificationCode", context.Request.QueryString["code"]);

            return nameValueCollection;
        }
    }
}