using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FourRoads.TelligentCommunity.Installer.Components.Utility
{
    public abstract class EmbeddedResourcesBase
	{
		protected static string GetResourceString(Assembly assembly, string path)
		{
			using (Stream stream = GetResourceStream(assembly, path))
			{
				byte[] data = new byte[stream.Length];

				stream.Read(data, 0, data.Length);

				var text = Encoding.UTF8.GetString(data);

				return text[0] > 255 ? text.Substring(1) : text;
			}
		}

		protected static Stream GetResourceStream(Assembly assembly, string path)
		{
			return assembly.GetManifestResourceStream(path);
		}

	    public Stream GetStream(string resourceName )
	    {
	        return GetResourceStream(GetType().Assembly, resourceName);

	    }

        public string GetString(string resourceName)
        {
            return GetResourceString(GetType().Assembly, resourceName);
        }

	    public void EnumerateResources(string resourcePath , string extension, Action<string> action)
	    {
            IEnumerable<string> scripts = GetType().Assembly.GetManifestResourceNames().Where(r => r.StartsWith(resourcePath) && (string.IsNullOrEmpty(extension) || r.EndsWith(extension)));

	        foreach (string resourceName in scripts)
	        {
	            action(resourceName);
	        }
	    }
	}
}