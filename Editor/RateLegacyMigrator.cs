using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Automatic migrator from legacy Green Sauce Games / in-house RateControl implementations
    /// to the modern open-source <c>com.wagenheimer.ratecontrol</c> package.
    ///
    /// <list type="bullet">
    ///   <item>Upgrades <c>formRate.cs</c> to inherit from <see cref="RateDialog"/>, keeping animations and localized text.</item>
    ///   <item>Moves or creates <c>RateConfig.asset</c> into a <c>Resources/</c> directory.</item>
    ///   <item>Assigns <c>formRate.prefab</c> to <see cref="RateConfig.DialogPrefab"/>.</item>
    ///   <item>Synchronizes store IDs and platform channels from <c>GameConfig</c> or <c>PlayerSettings</c>.</item>
    ///   <item>Deletes obsolete legacy scripts (<c>RateControl.cs</c>, <c>RateControlBootstrap.cs</c>).</item>
    ///   <item>Cleans missing/legacy MonoBehaviour components from <c>Main.prefab</c>.</item>
    /// </list>
    /// </summary>
    public static class RateLegacyMigrator
    {
        public const string LegacyRateControlGuid = "ec7f92651e6cdf94db56da858f2a4da2";

        public sealed class DetectionResult
        {
            public bool IsLegacyDetected =>
                HasLegacyRateControlScript ||
                HasLegacyFormRate ||
                IsRateConfigOutsideResources ||
                HasMissingDialogPrefab ||
                HasLegacyBootstrap;

            public bool HasLegacyRateControlScript;
            public string LegacyRateControlScriptPath;

            public bool HasLegacyFormRate;
            public string FormRateScriptPath;

            public bool IsRateConfigOutsideResources;
            public string RateConfigPath;

            public bool HasMissingDialogPrefab;
            public string FormRatePrefabPath;

            public bool HasLegacyBootstrap;
            public string LegacyBootstrapPath;

            public bool HasLegacyMainComponent;
            public string MainPrefabPath;

            public List<string> Details = new();
        }

        [MenuItem("Tools/Wagenheimer/Rate Control/Migrate Legacy RateControl...", priority = 135)]
        public static void MenuMigrate()
        {
            var det = Detect();
            if (!det.IsLegacyDetected)
            {
                EditorUtility.DisplayDialog(
                    "RateControl Migration",
                    "No legacy RateControl setup was detected in this project.\n" +
                    "Your project is already using the modern RateControl architecture.",
                    "OK");
                return;
            }

            Migrate(true);
        }

        public static DetectionResult Detect()
        {
            var res = new DetectionResult();

            // 1. Scan project scripts for legacy RateControl.cs, RateControlBootstrap.cs, and formRate.cs
            var scripts = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            foreach (var guid in scripts)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var fileName = Path.GetFileName(path);
                if (fileName.Equals("RateControl.cs", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var text = File.ReadAllText(path);
                        if (text.Contains("class RateControl : MonoBehaviour") ||
                            text.Contains("RateControl.instance") ||
                            text.Contains("kUniRate"))
                        {
                            res.HasLegacyRateControlScript = true;
                            res.LegacyRateControlScriptPath = path;
                            res.Details.Add($"Legacy MonoBehaviour script found at '{path}'");
                        }
                    }
                    catch { }
                }
                else if (fileName.Equals("RateControlBootstrap.cs", StringComparison.OrdinalIgnoreCase))
                {
                    res.HasLegacyBootstrap = true;
                    res.LegacyBootstrapPath = path;
                    res.Details.Add($"Obsolete RateControlBootstrap found at '{path}'");
                }
                else if (fileName.Equals("formRate.cs", StringComparison.OrdinalIgnoreCase))
                {
                    res.FormRateScriptPath = path;
                    try
                    {
                        var text = File.ReadAllText(path);
                        if (!text.Contains(": RateDialog") && !text.Contains(": Wagenheimer.RateControl.RateDialog"))
                        {
                            res.HasLegacyFormRate = true;
                            res.Details.Add($"'formRate.cs' does not inherit from RateDialog (found at '{path}')");
                        }
                        else if (text.Contains("RateControl.instance"))
                        {
                            res.HasLegacyFormRate = true;
                            res.Details.Add($"'formRate.cs' still references obsolete 'RateControl.instance'");
                        }
                    }
                    catch { }
                }
            }

            // 2. Scan for RateConfig.asset
            var configs = AssetDatabase.FindAssets("t:RateConfig");
            if (configs.Length > 0)
            {
                res.RateConfigPath = AssetDatabase.GUIDToAssetPath(configs[0]);
                if (!res.RateConfigPath.Contains("/Resources/"))
                {
                    res.IsRateConfigOutsideResources = true;
                    res.Details.Add($"RateConfig.asset is outside Resources folder: '{res.RateConfigPath}'");
                }

                var cfg = AssetDatabase.LoadAssetAtPath<RateConfig>(res.RateConfigPath);
                if (cfg != null && cfg.DialogPrefab == null)
                {
                    res.HasMissingDialogPrefab = true;
                    res.Details.Add("RateConfig.DialogPrefab is unassigned");
                }
            }
            else
            {
                res.IsRateConfigOutsideResources = true;
                res.Details.Add("No RateConfig.asset found in project");
            }

            // 3. Scan for formRate.prefab
            var prefabs = AssetDatabase.FindAssets("formRate t:Prefab");
            foreach (var pGuid in prefabs)
            {
                var pPath = AssetDatabase.GUIDToAssetPath(pGuid);
                if (pPath.StartsWith("Assets/"))
                {
                    res.FormRatePrefabPath = pPath;
                    break;
                }
            }

            // 4. Scan for Main.prefab
            var mainPrefabs = AssetDatabase.FindAssets("Main t:Prefab");
            foreach (var mGuid in mainPrefabs)
            {
                var mPath = AssetDatabase.GUIDToAssetPath(mGuid);
                if (mPath.StartsWith("Assets/") && mPath.EndsWith("Main.prefab", StringComparison.OrdinalIgnoreCase))
                {
                    res.MainPrefabPath = mPath;
                    break;
                }
            }

            return res;
        }

        public static bool Migrate(bool interactive = true)
        {
            var det = Detect();

            if (interactive)
            {
                var details = det.Details.Count > 0 ? string.Join("\n• ", det.Details) : "Legacy setup detected.";
                var message = "RateControl Automatic Migration:\n\n" +
                              $"Detected items:\n• {details}\n\n" +
                              "The following actions will be performed:\n" +
                              "1. Upgrade 'formRate.cs' to inherit from RateDialog.\n" +
                              "2. Move or create 'RateConfig.asset' in a Resources/ folder.\n" +
                              "3. Assign 'formRate.prefab' to RateConfig.DialogPrefab.\n" +
                              "4. Synchronize store IDs and distribution channels from GameConfig.\n" +
                              "5. Delete obsolete legacy scripts (RateControl.cs, RateControlBootstrap.cs).\n" +
                              "6. Remove obsolete legacy components from Main.prefab.\n\n" +
                              "Proceed with migration?";

                if (!EditorUtility.DisplayDialog("Migrate to Modern RateControl", message, "Migrate Now", "Cancel"))
                    return false;
            }

            try
            {
                AssetDatabase.StartAssetEditing();

                // 1. Upgrade formRate.cs
                UpgradeFormRateScript(det.FormRateScriptPath);

                // 2. Relocate or create RateConfig.asset in Resources
                var config = EnsureRateConfigInResources(det.RateConfigPath);

                // 3. Assign DialogPrefab
                if (config != null)
                {
                    AssignDialogPrefab(config, det.FormRatePrefabPath);
                    RateBuildPreprocessor.SyncConfig(config, EditorUserBuildSettings.activeBuildTarget);

                    if (string.IsNullOrEmpty(config.StorageKeyPrefix) || config.StorageKeyPrefix == "RateControl")
                    {
                        var cleanName = Regex.Replace(Application.productName, @"[^a-zA-Z0-9]", "");
                        config.StorageKeyPrefix = $"{cleanName}.Rate";
                    }

                    config.AutoSyncOnBuild = true;
                    EditorUtility.SetDirty(config);
                }

                // 4. Delete obsolete scripts
                if (!string.IsNullOrEmpty(det.LegacyRateControlScriptPath) && File.Exists(det.LegacyRateControlScriptPath))
                {
                    AssetDatabase.DeleteAsset(det.LegacyRateControlScriptPath);
                    Debug.Log($"[RateControl Migrator] Deleted legacy '{det.LegacyRateControlScriptPath}'.");
                }

                if (!string.IsNullOrEmpty(det.LegacyBootstrapPath) && File.Exists(det.LegacyBootstrapPath))
                {
                    AssetDatabase.DeleteAsset(det.LegacyBootstrapPath);
                    Debug.Log($"[RateControl Migrator] Deleted legacy '{det.LegacyBootstrapPath}'.");
                }

                // 5. Clean up Main.prefab
                CleanMainPrefab(det.MainPrefabPath);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RateControl Migrator] Migration error: {ex}");
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Migration Error", $"An error occurred during migration:\n\n{ex.Message}", "OK");
                }
                return false;
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (interactive)
                {
                    EditorUtility.DisplayDialog(
                        "Migration Complete",
                        "Project successfully migrated to modern RateControl!\n\n" +
                        "• formRate.cs upgraded to inherit from RateDialog\n" +
                        "• RateConfig.asset located in Resources/ and wired with formRate.prefab\n" +
                        "• Store IDs and distribution channels synchronized from GameConfig\n" +
                        "• Obsolete scripts and components cleaned up\n\n" +
                        "Review the Setup & Checklist window to confirm readiness.",
                        "OK");

                    SetupChecklistWindow.Open();
                }
            }
        }

        private static void UpgradeFormRateScript(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                var guids = AssetDatabase.FindAssets("formRate t:MonoScript", new[] { "Assets" });
                if (guids.Length > 0)
                {
                    path = AssetDatabase.GUIDToAssetPath(guids[0]);
                }
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            var code = @"using System;
using DarkTonic.MasterAudio;
using DG.Tweening;
using I2.Loc;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Wagenheimer.RateControl;

/// <summary>
/// Native localized rate popup dialog.
/// Upgraded to inherit from <see cref=""RateDialog""/> for com.wagenheimer.ratecontrol integration.
/// </summary>
public class formRate : RateDialog
{
    public Button btRateNow;
    public TextMeshProUGUI labelRate;
    public TextMeshProUGUI labelText;

    private void Start()
    {
        UpdateLocalizedText();
    }

    private void UpdateLocalizedText()
    {
        var targetLabel = labelRate != null ? labelRate : labelText;
        if (targetLabel != null)
        {
            var format = LocalizationManager.GetTranslation(""rategametext"");
            if (string.IsNullOrEmpty(format))
            {
                format = ""If you enjoy playing {0}, please take a moment to rate it. Thanks for your support!"";
            }
            targetLabel.text = string.Format(format, ""<color=white>"" + Application.productName + ""</color>"");
        }
    }

    public void OnEnable()
    {
        UpdateLocalizedText();

        if (btRateNow != null)
        {
            btRateNow.transform.DOScale(Vector3.one * 0.15f, 1f)
                .SetRelative()
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
    }

    public void OnDisable()
    {
        if (btRateNow != null)
        {
            btRateNow.transform.DOKill(true);
        }
    }

    public override void Show()
    {
        if (transform.parent != null)
        {
            transform.SetSiblingIndex(transform.parent.childCount - 2);
        }
        gameObject.SetActive(true);
    }

    public override void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Close() => Hide();

    // ── Button Callbacks wired in formRate.prefab ───────────────────────────────

    public void RateNow()
    {
        MasterAudio.PlaySound(""sfx-click"");
        OnRateNow();
        Hide();
    }

    public void RemindLater()
    {
        MasterAudio.PlaySound(""sfx-click"");
        OnRemindLater();
        Hide();
    }

    public void NoThanks()
    {
        MasterAudio.PlaySound(""sfx-click"");
        OnNoThanks();
        Hide();
    }
}
";
            File.WriteAllText(path, code);
            Debug.Log($"[RateControl Migrator] Successfully upgraded '{path}' to inherit from RateDialog.");
        }

        private static RateConfig EnsureRateConfigInResources(string existingPath)
        {
            if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath))
            {
                if (existingPath.Contains("/Resources/"))
                {
                    return AssetDatabase.LoadAssetAtPath<RateConfig>(existingPath);
                }

                // Relocate to a Resources folder alongside existing location
                var dir = Path.GetDirectoryName(existingPath)?.Replace('\\', '/');
                var resourcesDir = $"{dir}/Resources";
                if (!AssetDatabase.IsValidFolder(resourcesDir))
                {
                    AssetDatabase.CreateFolder(dir, "Resources");
                }

                var targetPath = $"{resourcesDir}/RateConfig.asset";
                var err = AssetDatabase.MoveAsset(existingPath, targetPath);
                if (string.IsNullOrEmpty(err))
                {
                    Debug.Log($"[RateControl Migrator] Moved RateConfig to '{targetPath}'.");
                    return AssetDatabase.LoadAssetAtPath<RateConfig>(targetPath);
                }
                else
                {
                    Debug.LogWarning($"[RateControl Migrator] Could not move RateConfig: {err}");
                    return AssetDatabase.LoadAssetAtPath<RateConfig>(existingPath);
                }
            }

            // Create fresh RateConfig in Resources
            string targetFolder = "Assets/Resources";
            if (AssetDatabase.IsValidFolder("Assets/_Game"))
            {
                targetFolder = "Assets/_Game/Resources";
                if (!AssetDatabase.IsValidFolder(targetFolder))
                    AssetDatabase.CreateFolder("Assets/_Game", "Resources");
            }
            else if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var newPath = $"{targetFolder}/RateConfig.asset";
            var created = ScriptableObject.CreateInstance<RateConfig>();
            AssetDatabase.CreateAsset(created, newPath);
            Debug.Log($"[RateControl Migrator] Created RateConfig at '{newPath}'.");
            return created;
        }

        private static void AssignDialogPrefab(RateConfig config, string formRatePrefabPath)
        {
            if (string.IsNullOrEmpty(formRatePrefabPath) || !File.Exists(formRatePrefabPath))
            {
                var prefabs = AssetDatabase.FindAssets("formRate t:Prefab", new[] { "Assets" });
                if (prefabs.Length > 0)
                {
                    formRatePrefabPath = AssetDatabase.GUIDToAssetPath(prefabs[0]);
                }
            }

            if (string.IsNullOrEmpty(formRatePrefabPath)) return;

            var prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(formRatePrefabPath);
            if (prefabGo == null) return;

            var dialog = prefabGo.GetComponent<RateDialog>();
            if (dialog != null)
            {
                config.DialogPrefab = dialog;
                config.DialogResourcePath = "formRate";
                Debug.Log($"[RateControl Migrator] Assigned '{formRatePrefabPath}' to RateConfig.DialogPrefab.");
            }
            else
            {
                // Component may need domain reload after formRate.cs edit
                config.DialogResourcePath = "formRate";
            }
        }

        private static void CleanMainPrefab(string mainPrefabPath)
        {
            if (string.IsNullOrEmpty(mainPrefabPath) || !File.Exists(mainPrefabPath)) return;

            try
            {
                var contents = PrefabUtility.LoadPrefabContents(mainPrefabPath);
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(contents);
                PrefabUtility.SaveAsPrefabAsset(contents, mainPrefabPath);
                PrefabUtility.UnloadPrefabContents(contents);

                if (removed > 0)
                {
                    Debug.Log($"[RateControl Migrator] Cleaned {removed} missing/obsolete MonoBehaviour(s) from '{mainPrefabPath}'.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RateControl Migrator] CleanMainPrefab warning: {ex.Message}");
            }
        }
    }
}

