// ------------------------------------------------------------------------------
// <copyright company=" 4 Roads LTD">
//     Copyright (c) 4 Roads LTD - 2013.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Optimization;
using FourRoads.TelligentCommunity.Performance.Interfaces;

namespace FourRoads.TelligentCommunity.Performance
{
    public abstract class DynamicBundleBase
    {
        private readonly Dictionary<string, bool> _includedFiles = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        protected void Include(IBundledFile bundledFile)
        {
            if (!PartOfBundle(bundledFile.OrignalUri))
            {
                Bundle.Include(bundledFile.RelativeUri);

                _includedFiles[bundledFile.OrignalUri] = true;
            }
        }

        protected bool PartOfBundle(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return _includedFiles.ContainsKey(path);
        }

        public abstract Bundle Bundle { get; }
    }
}