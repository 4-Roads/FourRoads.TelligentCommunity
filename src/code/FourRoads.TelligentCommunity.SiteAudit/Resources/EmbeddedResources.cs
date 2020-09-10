using FourRoads.Common.TelligentCommunity.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FourRoads.TelligentCommunity.SiteAudit
{
	internal class EmbeddedResources : EmbeddedResourcesBase
    {
		public void EnumerateResources(string resourcePath, string extension, Action<string> action)
		{
			IEnumerable<string> scripts = GetType().Assembly.GetManifestResourceNames().Where(r => r.StartsWith(resourcePath) && (string.IsNullOrEmpty(extension) || r.EndsWith(extension)));

			foreach (string resourceName in scripts)
			{
				action(resourceName);
			}
		}
	}
}
