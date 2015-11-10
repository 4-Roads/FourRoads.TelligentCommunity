using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using CsQuery;
using FourRoads.TelligentCommunity.Splash.Interfaces;
using Telligent.Evolution.Extensibility.Api.Entities.Version1;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Storage.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Telligent.Evolution.Urls.Routing;

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
                if (PublicApi.Users.AccessingUser.Id == PublicApi.Users.Get(new UsersGetOptions() {Username = "anonymous"}).Id)
                {
                    //if (HttpContext.Current.Request.Url.LocalPath != "/splash")
                    string urlRequest = HttpContext.Current.Request.Url.LocalPath;

                    var pageContext = PublicApi.Url.ParsePageContext(HttpContext.Current.Request.Url.OriginalString);

                    if (pageContext != null && pageContext.PageName != _pageName && !CentralizedFileStorage.IsCentralizedFileUrl(urlRequest) && !(urlRequest.EndsWith(".js") || urlRequest.EndsWith(".axd") || urlRequest.EndsWith(".ashx") || urlRequest.IndexOf("socket.ashx") >= 0))
                    {
                        HttpCookie cookie = HttpContext.Current.Request.Cookies["Splash"];

                        if (cookie == null || cookie["hash"] != GetPasswordHash())
                        {
                            HttpContext.Current.Response.Redirect("/splash" + "?ReturnUrl=" + PublicApi.CoreUrls.Home(false), true);
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
                    PublicApi.Users.Events.AfterIdentify += EventsAfterIdentify;
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
                    if (PublicApi.Users.AccessingUser != null)
                    {
                        User anon = PublicApi.Users.Get(new UsersGetOptions { Username = PublicApi.Users.AnonymousUserName });
                        if (anon.Id != PublicApi.Users.AccessingUser.Id)
                        {
                            //If the user is a system administrator then grant access to the splash page so they can download the splash csv file, else redirect
                            if (!PublicApi.RoleUsers.IsUserInRoles(PublicApi.Users.AccessingUser.Username , new []{"Administrators"}))
                                accessController.Redirect(PublicApi.CoreUrls.Home(false));
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

        public void Process(CQ document)
        {
            if (_configuration.HasValue)
            {
                var pageContext = PublicApi.Url.ParsePageContext(HttpContext.Current.Request.Url.OriginalString);

                if (pageContext != null && pageContext.PageName == _pageName)
                {
                    if (_configuration.Value.RemoveHeader)
                    {
                        document.Select(".header-fragments").Remove();
                    }

                    if (_configuration.Value.RemoveFooter)
                    {
                        document.Select(".footer-fragments").Remove();
                    }
                }
            }
        }
    }
}
