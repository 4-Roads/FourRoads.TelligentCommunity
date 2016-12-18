using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Optimization;
using FourRoads.TelligentCommunity.Performance.Interfaces;
using FourRoads.TelligentCommunity.Performance.Storage;
using Microsoft.Ajax.Utilities;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;

namespace FourRoads.TelligentCommunity.Performance
{

    public class CustomStyleBuilder : IBundleBuilder
    {
        public virtual string BuildBundleContent(Bundle bundle, BundleContext context, IEnumerable<BundleFile> files)
        {
            CssSettings settings = new CssSettings();
            settings.IgnoreAllErrors = true;
            settings.CommentMode = CssComment.None;
            var minifier = new Minifier();
            var content = new StringBuilder(100000);


            foreach (var file in files)
            {
                FileInfo f = new FileInfo(HttpContext.Current.Server.MapPath(file.VirtualFile.VirtualPath));

                string readFile = Read(f);

                content.Append(minifier.MinifyStyleSheet(readFile, settings));
                content.AppendLine();
            }

            return content.ToString();
        }

        public static string Read(FileInfo file)
        {
            using (var r = file.OpenText())
            {
                return r.ReadToEnd();
            }
        }
    }
    public class StandardStyleBundle : DynamicBundleBase , IDynamicBundle
    {
        private readonly StyleBundle _styleBundle;
        private BundledFileFactory _bundledFileFactory = new BundledFileFactory();
        private static string _selector = "link[type='text/css']";

        public StandardStyleBundle(string basePath)
        {
            _styleBundle = new StyleBundle(basePath + BundlePath);
            _styleBundle.Orderer = new AsIsBundleOrderer();
            _styleBundle.Builder = new CustomStyleBuilder();
            _styleBundle.Transforms.Clear();
        }

        public override Bundle Bundle
        {
            get { return _styleBundle; }
        }

        public void BuildBundleData(ContentFragmentPageControl contentFragmentPage, IHtmlDocument parsedContent)
        {
            if (Configuration.OptomizeGlobalCss)
            {
                //Get the themes CSS files
                var elements = parsedContent.QuerySelectorAll(BuildSelector);

                foreach (var element in elements)
                {
                    string mediaType = element.GetAttribute("media") ?? "screen";

                    if (mediaType.IndexOf("screen", StringComparison.OrdinalIgnoreCase) > -1)
                    {
                        string src = element.GetAttribute("href");

                        if (!string.IsNullOrEmpty(src) && mediaType.IndexOf("dynamic-style.aspx", StringComparison.OrdinalIgnoreCase) < 0 && src.EndsWith(".css"))
                        {
                            IBundledFile file = _bundledFileFactory.GetBundleFile("css", src, contentFragmentPage, "");

                            if (file != null)
                            {
                                HandleLayoutCssPath(file);
                                
                                Include(file);
                            }
                        }
                    }
                }


            }
        }

        private void HandleLayoutCssPath(IBundledFile file)
        {
            using (FileStream fs = new FileStream(file.LocalPath, FileMode.Open, FileAccess.ReadWrite))
            {
                using (StreamReader sr = new StreamReader(fs))
                {
                    string fileData = sr.ReadToEnd();

                    fileData = fileData.Replace("../images/", "../../../../Themes/generic/images/");

                    fs.Seek(0, SeekOrigin.Begin);

                    byte[] bytes = ASCIIEncoding.UTF8.GetBytes(fileData);

                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }

        public void ProcessDisplayElement(IHtmlDocument parsedContent)
        {
            var elements = parsedContent.QuerySelectorAll(ReplaceSelector);
            bool foundFirst = false;

            ////WE can minify the inline javascript, however this costs more than the time it saves from the bytes saved not sent over the network
            foreach (var element in elements)
            {
                string path = element.GetAttribute("href") ?? string.Empty;

                if (!path.EndsWith(BundlePath))
                {
                    if (PartOfBundle(path))
                    {
                        foundFirst = UpdateCssPath(foundFirst, element);
                    }
                } 
            }
        }


        private bool UpdateCssPath(bool foundFirstCss, IElement element)
        {
            if (foundFirstCss == false)
            {
                element.SetAttribute("href", Globals.FullPath(_styleBundle.Path));
            }
            else
            {
                element.Remove();
            }
            return true;
        }

        private string BundlePath {
            get { return "/bundled/css.css"; }
        }

        private string BuildSelector
        {
            get { return "link[type='text/css']"; }
        }

        //private Selector ReplaceSelector  //this will improve performance but requires csquery 1.3.5-beta and above
        private string ReplaceSelector
        {
            get { return _selector; }
        }
    }
}