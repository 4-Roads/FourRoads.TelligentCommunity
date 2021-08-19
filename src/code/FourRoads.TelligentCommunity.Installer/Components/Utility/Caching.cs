using Telligent.Evolution.Caching.Services;
using Telligent.Evolution.Components;
using Telligent.Evolution.Extensibility;
using Telligent.Evolution.Platform.Auditing;
using Telligent.Evolution.Platform.Scripting.Services;
using Telligent.Evolution.ScriptedContentFragments.Services;

namespace FourRoads.TelligentCommunity.Installer.Components.Utility
{
    //based on calls from ExpireCachesAdministrationPanel
    public class Caching
    {
        public static void ExpireUICaches()
        {
            Telligent.Common.Services.Get<IFactoryDefaultScriptedContentFragmentService>().ExpireCache();
            Telligent.Common.Services.Get<IScriptedContentFragmentService>().ExpireCache();
            Telligent.Common.Services.Get<IContentFragmentPageService>().RemoveAllFromCache();
            Telligent.Common.Services.Get<IContentFragmentService>().RefreshContentFragments();
            Telligent.Common.Services.Get<IScriptedExtensionProcessedFileVersioningService>().Update();
            SystemFileStore.RequestHostVersionedThemeFileRegeneration();
        }

        public static void ExpireAllCaches()
        {
            Telligent.Common.Services.Get<ICacheService>().Clear(CacheScope.All);
        }

        public static void ReloadPlugins()
        {
            Telligent.Common.Services.Get<IPluginManager>().Initialize(true);
            Telligent.Common.Services.Get<ICacheService>().Clear(CacheScope.All);
        }

    }
}