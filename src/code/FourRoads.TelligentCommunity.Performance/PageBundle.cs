// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Optimization;

using FourRoads.TelligentCommunity.Performance.Interfaces;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;
using AngleSharp.Dom.Html;

namespace FourRoads.TelligentCommunity.Performance
{
    public class PageBundle 
    {
        public PageBundle(string path)
        {
            BundleRegistered = false;
            LastConsistencyCheck = DateTime.Now;
            
            _dynamicBundles.Add(new StandardScriptBundle(path));
            _dynamicBundles.Add(new StandardStyleBundle(path));
        }

        private List<IDynamicBundle> _dynamicBundles = new List<IDynamicBundle>(); 

        public DateTime LastConsistencyCheck { get; private set; }

        public bool BundleRegistered { get; protected set; }

        public void RegisterBundles()
        {
            _dynamicBundles.ForEach(b => BundleTable.Bundles.Add(b.Bundle));

            BundleRegistered = true;
        }

        public void DeRegister()
        {
            _dynamicBundles.ForEach(b => { 
                BundleTable.Bundles.Remove(b.Bundle);
            });
        }

        public void BuildBundleData(ContentFragmentPageControl contentFragmentPage, IHtmlDocument parsedContent)
        {
            _dynamicBundles.ForEach(b => b.BuildBundleData(contentFragmentPage, parsedContent));
            RegisterBundles();
        }

        public void ProcessDisplayElement(IHtmlDocument parsedContent)
        {
            _dynamicBundles.ForEach(b => b.ProcessDisplayElement(parsedContent));
        }
    }
}