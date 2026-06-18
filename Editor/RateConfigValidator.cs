#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Wagenheimer.RateControl.Editor
{
    internal sealed class RateConfigValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var guids = AssetDatabase.FindAssets("t:RateConfig");
            if (guids.Length == 0) return;

            foreach (var guid in guids)
            {
                var path   = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<RateConfig>(path);
                if (config != null)
                    Validate(config, report.summary.platform, path);
            }
        }

        private static void Validate(RateConfig c, BuildTarget target, string assetPath)
        {
            var label = $"[RateControl] ({assetPath})";

            switch (target)
            {
                case BuildTarget.StandaloneOSX:
                    if (c.MacOs == MacOsChannel.None)
                        Debug.LogWarning($"{label} Building for macOS but MacOs channel is None — rate will be skipped. Set MacOs in RateConfig.");
                    else if (c.MacOs == MacOsChannel.MacAppStore && string.IsNullOrEmpty(c.MacAppStoreId))
                        Debug.LogWarning($"{label} macOS channel is MacAppStore but MacAppStoreId is empty — rate URL will fail.");
                    else if (c.MacOs == MacOsChannel.Steam && string.IsNullOrEmpty(c.SteamAppId))
                        Debug.LogWarning($"{label} macOS channel is Steam but SteamAppId is empty — rate URL will fail.");
                    break;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    if (c.Windows == StandaloneChannel.None)
                        Debug.LogWarning($"{label} Building for Windows but Windows channel is None — rate will be skipped. Set Windows in RateConfig.");
                    else if (c.Windows == StandaloneChannel.Steam && string.IsNullOrEmpty(c.SteamAppId))
                        Debug.LogWarning($"{label} Windows channel is Steam but SteamAppId is empty — rate URL will fail.");
                    break;

                case BuildTarget.StandaloneLinux64:
                    if (c.Linux == StandaloneChannel.None)
                        Debug.LogWarning($"{label} Building for Linux but Linux channel is None — rate will be skipped. Set Linux in RateConfig.");
                    else if (c.Linux == StandaloneChannel.Steam && string.IsNullOrEmpty(c.SteamAppId))
                        Debug.LogWarning($"{label} Linux channel is Steam but SteamAppId is empty — rate URL will fail.");
                    break;

                case BuildTarget.iOS:
                    if (string.IsNullOrEmpty(c.iOSAppId))
                        Debug.LogWarning($"{label} Building for iOS but iOSAppId is empty — SKStoreReviewManager fallback URL will be broken.");
                    break;

                case BuildTarget.Android:
                    // AndroidPackageId is optional — Application.identifier used as fallback.
                    break;
            }
        }
    }
}
#endif
