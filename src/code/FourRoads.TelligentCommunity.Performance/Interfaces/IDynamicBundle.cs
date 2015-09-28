// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System.Web.Optimization;
using CsQuery;
using Telligent.Evolution.Components;
using Telligent.Evolution.Controls;

namespace FourRoads.TelligentCommunity.Performance.Interfaces
{
    public interface IDynamicBundle
    {
        void BuildBundleData(ContentFragmentPageControl contentFragmentPage, CQ parsedContent);
        void ProcessDisplayElement(CQ parsedContent);
        Bundle Bundle { get; }
    }
}