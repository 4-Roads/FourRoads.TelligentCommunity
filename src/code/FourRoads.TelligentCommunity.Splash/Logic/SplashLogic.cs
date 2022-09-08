using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using FourRoads.TelligentCommunity.Splash.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using FourRoads.Common.TelligentCommunity.Routing;
using AngleSharp.Dom.Html;

namespace FourRoads.TelligentCommunity.Splash.Logic
{
    public class SplashLogic : ISplashLogic
    {
        private SplashConfigurationDetails ?_configuration = null;
        private static bool _initialized =false;
        private static object _syncLock = new object();
        private static readonly string _headerFileName = "header.txt";
        private static readonly string _responsesFolder = "responses";
        private static readonly string _pageName = "splash";

        public SplashLogic()
        {
        
        }

        protected void EventsAfterIdentify(UserAfterIdentifyEventArgs e)
        {
            if (_configuration.HasValue)
            {
                if (Apis.Get<IUsers>().AccessingUser.Id == Apis.Get<IUsers>().Get(new UsersGetOptions() {Username = "anonymous"}).Id)
                {
                    //if (HttpContext.Current.Request.Url.LocalPath != "/splash")
                    string urlRequest = HttpContext.Current.Request.Url.LocalPath;

                    var pageContext = Apis.Get<IUrl>().ParsePageContext(HttpContext.Current.Request.Url.OriginalString);

                    var whitelisted = _configuration.Value.WhitelistedPages.Split(',');

                    if (whitelisted.Length > 0 && whitelisted.Any(w => w.Trim().Equals(urlRequest.Trim(), StringComparison.InvariantCultureIgnoreCase)))
                    {
                        return;
                    }

                    if (pageContext != null)
                    {
                        if(whitelisted.Length > 0 &&
                            (!string.IsNullOrWhiteSpace(pageContext.PageName) && whitelisted.Any(w => w.Trim().Equals(pageContext.PageName.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            || !string.IsNullOrWhiteSpace(pageContext.UrlName) && whitelisted.Any(w => w.Trim().Equals(pageContext.UrlName.Trim(), StringComparison.InvariantCultureIgnoreCase)))
                            )
                        {
                            return;
                        }
                        
                        if (pageContext.PageName != _pageName 
                            && !CentralizedFileStorage.IsCentralizedFileUrl(urlRequest) 
                            && !(urlRequest.EndsWith(".js") 
                                || urlRequest.EndsWith(".axd") 
                                || urlRequest.EndsWith(".ashx") 
                                || urlRequest.IndexOf("socket.ashx") >= 0 
                                || urlRequest.StartsWith("/resized-image/__size/")))
                        {
                            HttpCookie cookie = HttpContext.Current.Request.Cookies["Splash"];

                            if (cookie == null || cookie["hash"] != GetPasswordHash())
                            {
                                HttpContext.Current.Response.Redirect("/splash" + "?ReturnUrl=" + Apis.Get<ICoreUrls>().Home(false), true);
                            }
                        }
                    }

                    
                }
            }
        }

        private void Initialize()
        {
            //Just in case
            lock (_syncLock)
            {
                if (!_initialized)
                {
                    Apis.Get<IUsers>().Events.AfterIdentify += EventsAfterIdentify;
                    _initialized = true;
                }
            }
        }

        private string GetPasswordHash(string value)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA1.Create();

            Byte[] hashBytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));

            return GetHashString(hashBytes);
        }

        private string GetPasswordHash()
        {
            return GetPasswordHash(_configuration.Value.Password);
        }

        private string GetHashString(byte[] input)
        {
            StringBuilder sb = new StringBuilder();

            foreach (byte b in input)
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }

        public void UpdateConfiguration(SplashConfigurationDetails configuration)
        {
            Initialize();

            _configuration = configuration;
        }

        public void RegisterUrls(IUrlController controller)
        {
            controller.AddPage(_pageName, _pageName, new SiteRootRouteConstraint(), null, _pageName, new PageDefinitionOptions
            {
                HasApplicationContext = false,
                SetCustomPageOutput = (context, outputController) =>
                {
                    
                 

                } ,
                ParseContext = context =>
                {

                },
                Validate = (context, accessController) =>
                {
                    if (Apis.Get<IUsers>().AccessingUser != null)
                    {
                        User anon = Apis.Get<IUsers>().Get(new UsersGetOptions { Username = Apis.Get<IUsers>().AnonymousUserName });
                        if (anon.Id != Apis.Get<IUsers>().AccessingUser.Id)
                        {
                            //If the user is a system administrator then grant access to the splash page so they can download the splash csv file, else redirect
                            if (!Apis.Get<IRoleUsers>().IsUserInRoles(Apis.Get<IUsers>().AccessingUser.Username , new []{"Administrators"}))
                                accessController.Redirect(Apis.Get<ICoreUrls>().Home(false));
                        }
                    }
                }
            });
        }

        public string ValidateAndHashAccessCode(string password)
        {
            string hash = GetPasswordHash(password);

            if (string.Compare(hash, GetPasswordHash(), StringComparison.OrdinalIgnoreCase) == 0)
            {
                return hash;
            }
            return null;
        }

        public bool SaveDetails(string email, IDictionary additionalFields)
        {
            //To prevent conflicts each repsonse is added to a single file and then aggregated when downloaded
            ICentralizedFileStorageProvider fs = CentralizedFileStorage.GetFileStore(Constants.FILESTOREKEY);
            if (fs != null)
            {
                var header = fs.GetFile("" , _headerFileName);

                if (header == null)
                {
                    StringBuilder headerBuffer = new StringBuilder();
                    //Add in the headers
                    headerBuffer.AppendLine(string.Join(",", EnumerateFieldsIntoList(additionalFields , (dictionary, field) => field).Concat(new []{"email"}).Select(Csv.Escape)));

                    WriteFileToCfs(headerBuffer, "", _headerFileName);
                }

                StringBuilder responseBuffer = new StringBuilder();

                responseBuffer.AppendLine(string.Join(",", EnumerateFieldsIntoList(additionalFields, (dictionary, field) => dictionary[field].ToString()).Concat(new []{email}).Select(Csv.Escape)));

                WriteFileToCfs(responseBuffer, _responsesFolder, Guid.NewGuid().ToString());

                return true;
            }

            return false;
        }

        private List<string> EnumerateFieldsIntoList(IDictionary additionalFields, Func<IDictionary , string, string> action)
        {
            List<string> fields = new List<string>();
            
            foreach (string field in additionalFields.Keys)
            {
                fields.Add(action(additionalFields , field));
            }
            return fields;
        }

        public string GetUserListDownloadUrl()
        {
            //Aggregate the current data into one csv file
            ICentralizedFileStorageProvider fs = CentralizedFileStorage.GetFileStore(Constants.FILESTOREKEY);
            if (fs != null)
            {
                var header = fs.GetFile("" , _headerFileName);

                if (header != null)
                {
                    StringBuilder fileBuffer = new StringBuilder();

                    using (StreamReader sr = new StreamReader(header.OpenReadStream()))
                    {
                        fileBuffer.AppendLine(sr.ReadToEnd());
                    }

                    //Build a new csv file
                    foreach (var response in fs.GetFiles(_responsesFolder, PathSearchOption.AllPaths))
                    {
                        using (StreamReader sr = new StreamReader(response.OpenReadStream()))
                        {
                            fileBuffer.Append(sr.ReadToEnd());
                        }
                    }

                    return WriteFileToCfs(fileBuffer, "", "userlist.csv").GetDownloadUrl();
                }
            }

            return null;
        }

        private ICentralizedFile WriteFileToCfs(StringBuilder buffer , string path , string filename)
        {
            ICentralizedFileStorageProvider fs = CentralizedFileStorage.GetFileStore(Constants.FILESTOREKEY);
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter wr = new StreamWriter(ms))
                {
                    wr.Write(buffer);
                    wr.Flush();

                    ms.Seek(0, SeekOrigin.Begin);

                    return fs.AddUpdateFile(path, filename, ms);
                }
            }
        }

        public void Process(IHtmlDocument document)
        {
            if (_configuration.HasValue)
            {
                var pageContext = Apis.Get<IUrl>().ParsePageContext(HttpContext.Current.Request.Url.PathAndQuery);

                if (pageContext != null && pageContext.PageName == _pageName)
                {
                    if (_configuration.Value.RemoveHeader)
                    {
                        var elems = document.QuerySelectorAll(".header-fragments");

                        foreach(var elem in elems )
                        {
                            elem.Remove();
                        }
                    }

                    if (_configuration.Value.RemoveFooter)
                    {
                        var elems = document.QuerySelectorAll(".footer-fragments");

                        foreach ( var elem in elems )
                        {
                            elem.Remove();
                        }
                    }
                }
            }
        }
    }
}
