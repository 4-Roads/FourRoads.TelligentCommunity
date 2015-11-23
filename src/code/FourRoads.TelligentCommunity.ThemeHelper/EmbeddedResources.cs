using System.IO;
using System.Reflection;
using FourRoads.Common.TelligentCommunity.Components;

namespace FourRoads.TelligentCommunity.ThemeHelper
{
	internal sealed class EmbeddedResources: EmbeddedResourcesBase
	{
		private static readonly Assembly Assembly = typeof(EmbeddedResources).Assembly;

		private EmbeddedResources()
		{
		}

		/// <summary>
		/// Gets the string contents of an embedded resource at the provided path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		new internal static string GetString(string path)
		{
			return GetResourceString(Assembly, path);
		}

		/// <summary>
		/// Gets the stream for an embedded resource at the provided path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		new internal static Stream GetStream(string path)
		{
			return GetResourceStream(Assembly, path);
		}

		/// <summary>
		/// Gets the base path for embedded resources.
		/// </summary>
		internal static string BasePath
		{
			get { return string.Format("{0}.Resources.", typeof(EmbeddedResources).Namespace); }
		}
	}
}