// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System.Web.Optimization;
using Telligent.Evolution.Controls;
using AngleSharp.Dom.Html;

namespace FourRoads.TelligentCommunity.Performance.Interfaces
{
    public interface IDynamicBundle
    {
        void BuildBundleData(ContentFragmentPageControl contentFragmentPage, IHtmlDocument parsedContent);
        void ProcessDisplayElement(IHtmlDocument parsedContent);
        Bundle Bundle { get; }
    }
}