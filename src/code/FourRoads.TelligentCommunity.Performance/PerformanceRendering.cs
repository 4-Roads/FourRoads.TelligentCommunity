// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using CsQuery;
using Telligent.Evolution.Controls;
using Telligent.Evolution.Extensibility.Api.Version1;
using Telligent.Evolution.Extensibility.Urls.Version1;
using Semaphore = System.Threading.Semaphore;
using FourRoads.TelligentCommunity.Performance.Storage;
using FourRoads.TelligentCommunity.RenderingHelper;

namespace FourRoads.TelligentCommunity.Performance
{
    /// <summary>
    /// This is the main facade used for bundle rendering
    /// </summary>
    public class PerformanceRendering : ICQProcessor
    {
        private Dictionary<string, PageBundle> _bundleDictionary = new Dictionary<string, PageBundle>(StringComparer.OrdinalIgnoreCase);
        private static Semaphore _initialization = new Semaphore(1,1);

        public Dictionary<string, PageBundle> BundleDictionary
        {
            get { return _bundleDictionary; }
        }

        public void Process(CQ parsedContent)
        {
            ContentFragmentPageControl contentFragmentPage = ContentFragmentPageControl.GetCurrentContentFragmentPage();

            if (contentFragmentPage != null )
            {
                PageContext currentContext = PublicApi.Url.CurrentContext;

                if (currentContext != null && !string.IsNullOrWhiteSpace(currentContext.UrlName))
                {
                    string key = CreateKey(contentFragmentPage.ThemeTypeId, contentFragmentPage.ThemeName, currentContext.UrlName);

                    EnsureBundleIntegrity(key);

                    //Find the right bundle
                    if (BundleDictionary.ContainsKey(key))
                    {
                        PageBundle bundle = BundleDictionary[key];

                        if (!bundle.BundleRegistered)
                        {
                            if (_initialization.WaitOne(1))
                            {
                                try
                                {
                                    bundle.BuildBundleData(contentFragmentPage, parsedContent);
                                }
                                finally
                                {
                                    _initialization.Release();
                                }
                            }
                            else
                            {
                                return;
                            }
                        }

                        bundle.ProcessDisplayElement(parsedContent);
                    }
                } 
            }

        }

        /// <summary>
        /// When plugin is loaded the bundles for the site need to be built
        /// </summary>
        public void EnsureBundleIntegrity(string key)
        {
            if (!BundleDictionary.ContainsKey(key))
            {
                PageBundle pageBundle = new PageBundle(key);
                BundleDictionary[key] = pageBundle;
            }
            else
            {
                //For now replcae the bundle every three hours, minor performance degredatoin expected 
                //when a page is updated
                if (BundleDictionary[key].LastConsistencyCheck.Add(Configuration.BundleTimeout) < DateTime.Now)
                {
                    PageBundle pageBundle = new PageBundle(key);
                    BundleDictionary[key] = pageBundle;
                }
            } 
        }

        private static string CreateKey(Guid themeTypeId, string themeName, string rawURL)
        {
            return string.Format("~/bundling/{0}/{1}/{2}", rawURL.Replace(".", ""), themeName, themeTypeId);
        }

        public void PurgeBundles()
        {
            if (_initialization.WaitOne(1))
            {
                try
                {
                    foreach (PageBundle pageBundle in BundleDictionary.Values)
                    {
                        pageBundle.DeRegister();
                    }
                    BundleDictionary.Clear();

                    FilestoreCache fs = new FilestoreCache();

                    fs.ClearCache();
                }
                finally
                {
                    _initialization.Release();
                }
            }
        }
    }
}