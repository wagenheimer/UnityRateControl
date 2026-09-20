using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Automatically preprocesses and synchronizes RateConfig before any build
    /// (via UnityBuildPipeline, CLI, or the Editor Build Settings window).
    ///
    /// When <see cref="RateConfig.AutoSyncOnBuild"/> is enabled (default), it reads
    /// publisher and store identifiers from BuildPipeline's GameConfig or PlayerSettings,
    /// guaranteeing the correct channel (MacAppStore, MacGameStore, Steam) and store IDs
    /// are active for the build without requiring manual Editor inspector adjustments.
    /// </summary>
    public sealed class RateBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => -50;

        public void OnPreprocessBuild(BuildReport report)
        {
            var config = FindActiveConfig();
            if (config == null || !config.AutoSyncOnBuild) return;

            SyncConfig(config, report.summary.platform);
        }

        public static RateConfig FindActiveConfig()
        {
            var config = Resources.Load<RateConfig>("RateConfig");
            if (config != null) return config;

            var guids = AssetDatabase.FindAssets("t:RateConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<RateConfig>(path);
            }

            return null;
        }

        public static bool SyncConfig(RateConfig config, BuildTarget target)
        {
            if (config == null) return false;

            bool changed = false;

            // 1. Try syncing from BuildPipeline's GameConfig if available in the project
            var gameConfig = FindGameConfig();
            if (gameConfig != null)
            {
                changed |= SyncFromGameConfig(config, gameConfig, target);
            }

            // 2. Fallbacks from PlayerSettings
            if (string.IsNullOrEmpty(config.AndroidPackageId))
            {
                var id = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
                if (!string.IsNullOrEmpty(id))
                {
                    config.AndroidPackageId = id;
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                Debug.Log($"[RateControl] Auto-synchronized RateConfig for build target {target} (macOS: {config.MacOs}, MacAppStoreId: {config.MacAppStoreId}, SteamAppId: {config.SteamAppId}).");
            }

            return changed;
        }

        public static ScriptableObject FindGameConfig()
        {
            var guids = AssetDatabase.FindAssets("t:GameConfig");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            }
            return null;
        }

        public static bool SyncFromGameConfig(RateConfig rateConfig, ScriptableObject gameConfig, BuildTarget target)
        {
            bool changed = false;
            var type = gameConfig.GetType();

            var publisherProp = type.GetField("Publisher");
            var macAppStoreIdField = type.GetField("MacAppStoreID");
            var iosAppIdFreeField = type.GetField("iOSAppIDFree");
            var iosAppIdFullField = type.GetField("iOSAppIDFull");
            var androidFreeField = type.GetField("AndroidFree");
            var androidFullField = type.GetField("AndroidFull");
            var fullGameField = type.GetField("FullGame");

            bool isFull = fullGameField != null && (bool)fullGameField.GetValue(gameConfig);

            if (macAppStoreIdField != null)
            {
                var macId = macAppStoreIdField.GetValue(gameConfig) as string;
                if (!string.IsNullOrEmpty(macId) && rateConfig.MacAppStoreId != macId)
                {
                    rateConfig.MacAppStoreId = macId;
                    changed = true;
                }
            }

            if (iosAppIdFreeField != null || iosAppIdFullField != null)
            {
                var freeId = iosAppIdFreeField?.GetValue(gameConfig) as string;
                var fullId = iosAppIdFullField?.GetValue(gameConfig) as string;
                var chosen = isFull && !string.IsNullOrEmpty(fullId) ? fullId : (!string.IsNullOrEmpty(freeId) ? freeId : fullId);
                if (!string.IsNullOrEmpty(chosen) && rateConfig.iOSAppId != chosen)
                {
                    rateConfig.iOSAppId = chosen;
                    changed = true;
                }
            }

            if (androidFreeField != null || androidFullField != null)
            {
                var freePkg = androidFreeField?.GetValue(gameConfig) as string;
                var fullPkg = androidFullField?.GetValue(gameConfig) as string;
                var chosen = isFull && !string.IsNullOrEmpty(fullPkg) ? fullPkg : (!string.IsNullOrEmpty(freePkg) ? freePkg : fullPkg);
                if (!string.IsNullOrEmpty(chosen) && rateConfig.AndroidPackageId != chosen)
                {
                    rateConfig.AndroidPackageId = chosen;
                    changed = true;
                }
            }

            // Sync distribution channels based on Publisher enum name
            if (publisherProp != null)
            {
                var pubVal = publisherProp.GetValue(gameConfig);
                var pubName = pubVal != null ? pubVal.ToString() : "";

                if (pubName.Contains("MacAppStore"))
                {
                    if (rateConfig.MacOs != MacOsChannel.MacAppStore)
                    {
                        rateConfig.MacOs = MacOsChannel.MacAppStore;
                        changed = true;
                    }
                }
                else if (pubName == "MacGameStore")
                {
                    if (rateConfig.MacOs != MacOsChannel.MacGameStore)
                    {
                        rateConfig.MacOs = MacOsChannel.MacGameStore;
                        changed = true;
                    }
                }
                else if (pubName == "Steam")
                {
                    if (rateConfig.MacOs != MacOsChannel.Steam)
                    {
                        rateConfig.MacOs = MacOsChannel.Steam;
                        changed = true;
                    }
                    if (rateConfig.Windows != StandaloneChannel.Steam)
                    {
                        rateConfig.Windows = StandaloneChannel.Steam;
                        changed = true;
                    }
                    if (rateConfig.Linux != StandaloneChannel.Steam)
                    {
                        rateConfig.Linux = StandaloneChannel.Steam;
                        changed = true;
                    }
                }
            }

            return changed;
        }
    }
}
