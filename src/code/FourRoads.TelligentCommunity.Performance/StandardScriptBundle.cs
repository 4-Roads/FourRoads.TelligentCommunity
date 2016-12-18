using System.IO;
using System.Web.Optimization;
using FourRoads.TelligentCommunity.Performance.Interfaces;
using FourRoads.TelligentCommunity.Performance.Storage;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using System.Collections.Generic;
using AngleSharp.Dom.Html;
using AngleSharp.Dom;

namespace FourRoads.TelligentCommunity.Performance
{
    internal class AsIsBundleOrderer : IBundleOrderer
    {
        public IEnumerable<BundleFile> OrderFiles(BundleContext context, IEnumerable<BundleFile> files)
        {
            return files;
        }
    }


    public class StandardScriptBundle : DynamicBundleBase, IDynamicBundle
    {
        private readonly ScriptBundle _scriptBundle;
        private readonly BundledFileFactory _bundledFileFactory = new BundledFileFactory();
        private static string _selector = "script[type='text/javascript']";
        //private static Selector _selector = new Selector("script[type='text/javascript']"); //this will improve performance but requires csquery 1.3.5-beta and above

        public StandardScriptBundle(string basePath)
        {
            _scriptBundle = new ScriptBundle(basePath + BundlePath);
            _scriptBundle.Orderer = new AsIsBundleOrderer();
        }

        public override Bundle Bundle
        {
            get
            {
                return _scriptBundle;
        }
        }

        public void BuildBundleData(ContentFragmentPageControl contentFragmentPage, IHtmlDocument parsedContent)
        {
            HandleInlineScripts(parsedContent , contentFragmentPage);
        }

        public void ProcessDisplayElement(IHtmlDocument parsedContent)
        {
            var elements = parsedContent.QuerySelectorAll(ReplaceSelector);
            bool foundFirstScript = false;

            ////WE can minify the inline javascript, however this costs more than the time it saves from the bytes saved not sent over the network
            foreach (IElement element in elements)
            {
                string path = element.GetAttribute("src") ?? string.Empty;

                if (!path.EndsWith(BundlePath))
                {
                    if (PartOfBundle(path))
                    {
                        foundFirstScript = UpdateJavascriptPath(foundFirstScript, element);
                    }
                }
            }
        }

        private bool UpdateJavascriptPath(bool foundFirstScript, IElement element)
        {
            if (foundFirstScript == false)
            {
                element.SetAttribute("src", Globals.FullPath(_scriptBundle.Path));
            }
            else
            {
                //Just make this local, cost of removing from dom is expensive
                element.RemoveAttribute("src");
                //element.Remove();
            }
            return true;
        }


        private string BundlePath
        {
            get { return "/bundled/js.js"; }
        }

        private string BuildSelector
        {
            get { return "html head script[type='text/javascript']"; }
        }

        private string BuildAxdSelector
        {
            get { return string.Format("script[type='text/javascript'][src*='WebResource.axd'],script[type='text/javascript'][src*='/utility/jquery/'],script[type='text/javascript'][src*='{0}/utility/jquery/'],script[type='text/javascript'][src*=' /__key/defaultwidgets/'],script[type='text/javascript'][src*=' /__key/widgetfiles/']", PublicApi.Url.Absolute("~")); }
        }

        //private Selector ReplaceSelector  //this will improve performance but requires csquery 1.3.5-beta and above
        private string ReplaceSelector
        {
            get { return _selector; }
        }

        private void HandleInlineScripts(IHtmlDocument parsedContent, ContentFragmentPageControl contentFragmentPage)
        {
            if (Configuration.OptomizeGlobalJs)
            {
                //Get the themes CSS files
                var elements = parsedContent.QuerySelectorAll(BuildSelector);

                foreach (var element in elements)
                {
                    string src = element.GetAttribute("src");

                    if (!string.IsNullOrEmpty(src))
                    {
                        IBundledFile file = _bundledFileFactory.GetBundleFile("js", src, contentFragmentPage, "");

                        if (file != null)
                            Include(file);
                    }
                }
                //Get the themes axd files
                elements = parsedContent.QuerySelectorAll(BuildAxdSelector);
                int i = 0;
                foreach (var element in elements)
                {
                    string src = element.GetAttribute("src");

                    if (!string.IsNullOrEmpty(src))
                    {
                        IBundledFile file = _bundledFileFactory.GetBundleFile("js", src, contentFragmentPage, i.ToString());

                        if (file != null)
                           Include(file);

                        i++;
                    }
                }
            }
        }
     }
}