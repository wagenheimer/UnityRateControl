using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Wagenheimer.PackageHub.Editor;
using Wagenheimer.RateControl.UI;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Master UI Toolkit Hub window for Rate Control: aggregates Diagnostics & Setup Checklist,
    /// Live QA & Testing, Configuration overview, Documentation guide, and Package updates.
    /// </summary>
    public sealed class RateControlHubWindow : EditorWindow
    {
        public enum Tab
        {
            Diagnostics = 0,
            LiveQA = 1,
            Config = 2,
            Guide = 3,
            About = 4
        }

        private const string PackageJsonPath = "Packages/com.wagenheimer.ratecontrol/package.json";
        private const string RepoUrl = "https://github.com/wagenheimer/UnityRateControl";
        private const string IssuesUrl = "https://github.com/wagenheimer/UnityRateControl/issues";

        private string _version = "1.16.0";
        private Tab _currentTab = Tab.Diagnostics;
        private VisualElement _contentContainer;
        private Button[] _tabButtons;

        // Diagnostics Cache
        private RateConfig _activeConfig;
        private string _activeConfigPath;
        private List<SetupChecklistWindow.Section> _sections = new();
        private DateTime _lastScanTime;

        [MenuItem("Tools/Wagenheimer/Rate Control/Dashboard...", priority = 100)]
        public static void OpenDashboard()
        {
            Open(Tab.Diagnostics);
        }

        [MenuItem("Tools/Wagenheimer/Rate Control/Live QA & Testing...", priority = 121)]
        public static void OpenLiveQA()
        {
            Open(Tab.LiveQA);
        }

        [MenuItem("Tools/Wagenheimer/Rate Control/Configuration...", priority = 122)]
        public static void OpenConfig()
        {
            Open(Tab.Config);
        }

        public static RateControlHubWindow Open(Tab tab = Tab.Diagnostics)
        {
            var window = GetWindow<RateControlHubWindow>("Rate Control");
            window.minSize = new Vector2(700, 560);
            window.SwitchTab(tab);
            window.Show();
            window.Focus();
            return window;
        }

        private void OnEnable()
        {
            LoadPackageVersion();
            RunDiagnostics();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            RateControlUIStyle.Apply(rootVisualElement);

            var root = new VisualElement();
            root.AddToClassList("rc-root");

            // Header Banner
            var header = RateControlUIStyle.CreateHeader(
                "Rate Control",
                "Cross-Platform Smart Rate Prompt, Analytics & Store Manager",
                _version);
            root.Add(header);

            // Tab Bar
            var tabToolbar = new VisualElement();
            tabToolbar.AddToClassList("rc-tab-toolbar");

            var tabNames = new[] { "Setup & Diagnostics", "Live QA & Tester", "Configuration", "Documentation", "About & Updates" };
            _tabButtons = new Button[tabNames.Length];

            for (var i = 0; i < tabNames.Length; i++)
            {
                var tabIndex = (Tab)i;
                var btn = new Button(() => SwitchTab(tabIndex))
                {
                    text = tabNames[i]
                };
                btn.AddToClassList("rc-tab-btn");
                _tabButtons[i] = btn;
                tabToolbar.Add(btn);
            }
            root.Add(tabToolbar);

            // Content Area
            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1;
            root.Add(_contentContainer);

            rootVisualElement.Add(root);

            RenderActiveTab();
        }

        public void SwitchTab(Tab tab)
        {
            _currentTab = tab;
            RenderActiveTab();
        }

        private void RenderActiveTab()
        {
            if (_contentContainer == null) return;
            _contentContainer.Clear();

            if (_tabButtons != null)
            {
                for (var i = 0; i < _tabButtons.Length; i++)
                {
                    if (i == (int)_currentTab)
                        _tabButtons[i].AddToClassList("rc-tab-btn-active");
                    else
                        _tabButtons[i].RemoveFromClassList("rc-tab-btn-active");
                }
            }

            switch (_currentTab)
            {
                case Tab.Diagnostics:
                    _contentContainer.Add(BuildDiagnosticsView());
                    break;
                case Tab.LiveQA:
                    _contentContainer.Add(BuildLiveQAView());
                    break;
                case Tab.Config:
                    _contentContainer.Add(BuildConfigView());
                    break;
                case Tab.Guide:
                    _contentContainer.Add(BuildGuideView());
                    break;
                case Tab.About:
                    _contentContainer.Add(BuildAboutView());
                    break;
            }
        }

        #region Tab 0: Diagnostics & Setup Checker

        private VisualElement BuildDiagnosticsView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            int autoTotal = _sections.Sum(s => s.AutomatedTotal);
            int autoPass = _sections.Sum(s => s.AutomatedPassed);
            int fails = _sections.SelectMany(s => s.Items).Count(i => i.Status == SetupChecklistWindow.CheckStatus.Fail);
            int warns = _sections.SelectMany(s => s.Items).Count(i => i.Status == SetupChecklistWindow.CheckStatus.Warning);

            // 1. Metric Counters
            var metricsRow = new VisualElement();
            metricsRow.AddToClassList("rc-metrics-row");

            metricsRow.Add(RateControlUIStyle.CreateMetricCard("Passed Checks", $"{autoPass}/{autoTotal}", out var passVal));
            passVal.style.color = new StyleColor(new Color(0.24f, 0.78f, 0.44f));

            metricsRow.Add(RateControlUIStyle.CreateMetricCard("Warnings", warns.ToString(), out var warnVal));
            if (warns > 0) warnVal.style.color = new StyleColor(new Color(0.95f, 0.65f, 0.15f));

            metricsRow.Add(RateControlUIStyle.CreateMetricCard("Failures", fails.ToString(), out var failVal));
            if (fails > 0) failVal.style.color = new StyleColor(new Color(0.95f, 0.30f, 0.30f));

            scroll.Add(metricsRow);

            // 2. Legacy Migration Warning Banner (if detected)
            var legacy = RateLegacyMigrator.Detect();
            if (legacy.IsLegacyDetected)
            {
                var legacyCard = RateControlUIStyle.CreateCard("Legacy RateControl Detected", "Found obsolete in-house scripts or unmigrated formRate prefabs.");
                legacyCard.style.backgroundColor = new StyleColor(new Color(0.24f, 0.14f, 0.08f));
                legacyCard.Add(RateControlUIStyle.CreateCallout(
                    "This project contains legacy RateControl files. Upgrade them automatically to the modern package architecture with one click.",
                    "warn"));

                var migrateBtn = RateControlUIStyle.CreateButton("Migrate to Modern RateControl Now", "rc-btn-primary", () =>
                {
                    RateLegacyMigrator.MenuMigrate();
                    RunDiagnostics();
                    RenderActiveTab();
                });
                migrateBtn.style.alignSelf = Align.FlexStart;
                legacyCard.Add(migrateBtn);
                scroll.Add(legacyCard);
            }

            // 3. Actions Toolbar Card
            var toolCard = RateControlUIStyle.CreateCard("Diagnostic Controls", $"Target: {EditorUserBuildSettings.activeBuildTarget} | Scanned: {_lastScanTime:HH:mm:ss}");
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;

            toolbar.Add(RateControlUIStyle.CreateButton("Re-Run Diagnostic Scan", "rc-btn-primary", () =>
            {
                RunDiagnostics();
                RenderActiveTab();
            }));

            var copyPromptBtn = RateControlUIStyle.CreateButton("Copy Issue Report / AI Prompt", "rc-btn-secondary", () =>
            {
                CopyIssuePrompt();
            });
            toolbar.Add(copyPromptBtn);

            var createConfigBtn = RateControlUIStyle.CreateButton("Create RateConfig Asset", "rc-btn-secondary", () =>
            {
                RateEditorMenuItems.CreateRateConfigAsset();
                RunDiagnostics();
                RenderActiveTab();
            });
            toolbar.Add(createConfigBtn);

            var createPrefabBtn = RateControlUIStyle.CreateButton("Create Default Dialog Prefab", "rc-btn-secondary", () =>
            {
                RateEditorMenuItems.CreateDefaultPrefab();
                RunDiagnostics();
                RenderActiveTab();
            });
            toolbar.Add(createPrefabBtn);

            toolCard.Add(toolbar);
            scroll.Add(toolCard);

            // 4. Detailed Sections
            foreach (var section in _sections)
            {
                var card = RateControlUIStyle.CreateCard(section.Title, section.Subtitle);

                foreach (var item in section.Items)
                {
                    var row = new VisualElement();
                    row.AddToClassList("rc-check-row");

                    var infoCol = new VisualElement();
                    infoCol.AddToClassList("rc-check-info");

                    var titleLbl = new Label(item.Title);
                    titleLbl.AddToClassList("rc-check-title");
                    infoCol.Add(titleLbl);

                    if (!string.IsNullOrEmpty(item.Detail))
                    {
                        var descLbl = new Label(item.Detail);
                        descLbl.AddToClassList("rc-check-desc");
                        infoCol.Add(descLbl);
                    }

                    if (item.Facts.Count > 0)
                    {
                        foreach (var fact in item.Facts)
                        {
                            var factLbl = new Label($"• {fact}");
                            factLbl.style.fontSize = 10;
                            factLbl.style.color = new StyleColor(new Color(0.6f, 0.6f, 0.65f));
                            factLbl.style.marginLeft = 6;
                            infoCol.Add(factLbl);
                        }
                    }

                    var actionCol = new VisualElement();
                    actionCol.AddToClassList("rc-check-actions");

                    var badgeSeverity = item.Status switch
                    {
                        SetupChecklistWindow.CheckStatus.Pass => "pass",
                        SetupChecklistWindow.CheckStatus.Warning => "warn",
                        SetupChecklistWindow.CheckStatus.Fail => "fail",
                        _ => "info"
                    };

                    var badge = RateControlUIStyle.CreateBadge(item.Status.ToString().ToUpperInvariant(), badgeSeverity);
                    actionCol.Add(badge);

                    if (item.Action != null && !string.IsNullOrEmpty(item.ActionLabel))
                    {
                        var actBtn = RateControlUIStyle.CreateButton(item.ActionLabel, "rc-btn-secondary", () =>
                        {
                            item.Action();
                            RunDiagnostics();
                            RenderActiveTab();
                        });
                        actBtn.style.marginLeft = 6;
                        actionCol.Add(actBtn);
                    }

                    row.Add(infoCol);
                    row.Add(actionCol);
                    card.Add(row);
                }

                scroll.Add(card);
            }

            return scroll;
        }

        private void CopyIssuePrompt()
        {
            var pending = _sections.SelectMany(s => s.Items)
                .Where(i => i.Status == SetupChecklistWindow.CheckStatus.Fail || i.Status == SetupChecklistWindow.CheckStatus.Warning)
                .ToList();

            if (pending.Count == 0)
            {
                EditorUtility.DisplayDialog("Rate Control", "All automated checks have passed! No issues detected.", "OK");
                return;
            }

            var lines = new List<string> { "Please fix the following issues in the Unity RateControl integration:" };
            int count = 1;
            foreach (var item in pending)
            {
                lines.Add($"\n{count++}. {item.Title}");
                if (!string.IsNullOrEmpty(item.Detail)) lines.Add($"   Detail: {item.Detail}");
                if (item.Facts.Count > 0)
                {
                    lines.Add("   Facts:");
                    foreach (var f in item.Facts) lines.Add($"   - {f}");
                }
                if (!string.IsNullOrEmpty(item.Prompt)) lines.Add($"   Recommended Fix: {item.Prompt}");
            }

            EditorGUIUtility.systemCopyBuffer = string.Join("\n", lines);
            Debug.Log("[RateControl] Issues copied to clipboard as an actionable report.");
            EditorUtility.DisplayDialog("Rate Control", "Issues copied to clipboard as an actionable report.", "OK");
        }

        #endregion

        #region Tab 1: Live QA & Interactive Tester

        private VisualElement BuildLiveQAView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            var prefix = _activeConfig != null && !string.IsNullOrEmpty(_activeConfig.StorageKeyPrefix)
                ? _activeConfig.StorageKeyPrefix
                : "RateControl";

            int eventCount = PlayerPrefs.GetInt($"{prefix}.EventCount", 0);
            int startCount = PlayerPrefs.GetInt($"{prefix}.StartCount", 0);
            bool dontAsk = PlayerPrefs.GetInt($"{prefix}.DontAsk", 0) == 1;
            int showCount = PlayerPrefs.GetInt($"{prefix}.ShowCount", 0);
            string lastVer = PlayerPrefs.GetString($"{prefix}.LastVersion", "(none)");
            string remindUntil = PlayerPrefs.GetString($"{prefix}.RemindLaterUntil", "(none)");

            int reqStarts = _activeConfig != null ? _activeConfig.StartsBeforeFirstPrompt : 3;
            int reqEvents = _activeConfig != null ? _activeConfig.EventsPerPrompt : 10;

            // 1. Current State Card
            var stateCard = RateControlUIStyle.CreateCard("Live PlayerPrefs State", $"Storage prefix: \"{prefix}\"");

            AddStateRow(stateCard, "App Starts Count", $"{startCount} / {reqStarts}");
            AddStateRow(stateCard, "Milestone Events Count", $"{eventCount} / {reqEvents}");
            AddStateRow(stateCard, "Total Times Prompt Shown", showCount.ToString());
            AddStateRow(stateCard, "Don't Ask Again (Rated / Declined)", dontAsk ? "TRUE (Blocked)" : "FALSE (Eligible)");
            AddStateRow(stateCard, "Remind Later Cooldown Until", remindUntil);
            AddStateRow(stateCard, "Last Rated App Version", lastVer);

            scroll.Add(stateCard);

            // 2. Readiness Progress Card
            var progressCard = RateControlUIStyle.CreateCard("Threshold Progress & Readiness");

            var startRatio = reqStarts > 0 ? Mathf.Clamp01((float)startCount / reqStarts) : 1f;
            var eventRatio = reqEvents > 0 ? Mathf.Clamp01((float)eventCount / reqEvents) : 1f;

            AddProgressBar(progressCard, "App Starts", startCount, reqStarts, startRatio);
            AddProgressBar(progressCard, "Milestone Events", eventCount, reqEvents, eventRatio);

            bool isReady = startCount >= reqStarts && eventCount >= reqEvents && !dontAsk;
            string readinessText = isReady ? "ELIGIBLE FOR PROMPT" : (dontAsk ? "BLOCKED (USER DECLINED)" : "ACCUMULATING THRESHOLDS");
            string readinessSeverity = isReady ? "pass" : (dontAsk ? "fail" : "info");

            var readyRow = new VisualElement();
            readyRow.style.flexDirection = FlexDirection.Row;
            readyRow.style.alignItems = Align.Center;
            readyRow.style.marginTop = 8;

            var readyLbl = new Label("Current Trigger Status: ");
            readyLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            readyRow.Add(readyLbl);
            readyRow.Add(RateControlUIStyle.CreateBadge(readinessText, readinessSeverity));
            progressCard.Add(readyRow);

            scroll.Add(progressCard);

            // 3. QA Simulation Tools Card
            var simCard = RateControlUIStyle.CreateCard("Simulation & Testing Actions", "Simulate game events or force the rating dialog flow without waiting days.");
            var simToolbar = new VisualElement();
            simToolbar.style.flexDirection = FlexDirection.Row;
            simToolbar.style.flexWrap = Wrap.Wrap;

            simToolbar.Add(RateControlUIStyle.CreateButton("⚡ Force Show Prompt", "rc-btn-primary", () =>
            {
                if (!Application.isPlaying)
                {
                    EditorUtility.DisplayDialog("Rate Control", "Enter Play Mode to display the active UI prompt on screen.", "OK");
                    return;
                }
                var rc = UnityEngine.Object.FindObjectOfType<RateControl>();
                if (rc != null) rc.ForceShowPrompt();
                else if (RateControl.Instance != null) RateControl.Instance.ForceShowPrompt();
                else Debug.LogWarning("[RateControl] RateControl instance is not active in the scene.");
            }));

            simToolbar.Add(RateControlUIStyle.CreateButton("📈 +1 Milestone Event", "rc-btn-secondary", () =>
            {
                if (Application.isPlaying) RateControl.LogEvent();
                else
                {
                    PlayerPrefs.SetInt($"{prefix}.EventCount", eventCount + 1);
                    PlayerPrefs.Save();
                }
                RenderActiveTab();
            }));

            simToolbar.Add(RateControlUIStyle.CreateButton("🚀 +1 App Start", "rc-btn-secondary", () =>
            {
                PlayerPrefs.SetInt($"{prefix}.StartCount", startCount + 1);
                PlayerPrefs.Save();
                RenderActiveTab();
            }));

            simToolbar.Add(RateControlUIStyle.CreateButton("⏳ Clear Remind Cooldown", "rc-btn-secondary", () =>
            {
                PlayerPrefs.DeleteKey($"{prefix}.RemindLaterUntil");
                PlayerPrefs.Save();
                RenderActiveTab();
            }));

            simToolbar.Add(RateControlUIStyle.CreateButton("🗑️ Reset All Saved State", "rc-btn-danger", () =>
            {
                RateEditorMenuItems.ResetSavedState();
                RenderActiveTab();
            }));

            simCard.Add(simToolbar);
            scroll.Add(simCard);

            // 4. In-Game Debug Overlay Card
            var overlayCard = RateControlUIStyle.CreateCard("In-Game Debug Overlay", "Floating UI in Editor & Development Builds showing live counters.");
            bool overlayEnabled = _activeConfig != null && _activeConfig.EnableDebugOverlay;

            var ovRow = new VisualElement();
            ovRow.style.flexDirection = FlexDirection.Row;
            ovRow.style.alignItems = Align.Center;
            ovRow.style.marginBottom = 6;

            var ovStatus = new Label($"Debug Overlay: {(overlayEnabled ? "ENABLED" : "DISABLED")}");
            ovStatus.style.flexGrow = 1;
            ovRow.Add(ovStatus);
            ovRow.Add(RateControlUIStyle.CreateBadge(overlayEnabled ? "ACTIVE" : "OFF", overlayEnabled ? "pass" : "info"));
            overlayCard.Add(ovRow);

            var toggleOvBtn = RateControlUIStyle.CreateButton(overlayEnabled ? "Disable Debug Overlay" : "Enable Debug Overlay", "rc-btn-secondary", () =>
            {
                if (_activeConfig != null)
                {
                    Undo.RecordObject(_activeConfig, "Toggle Debug Overlay");
                    _activeConfig.EnableDebugOverlay = !_activeConfig.EnableDebugOverlay;
                    EditorUtility.SetDirty(_activeConfig);
                    AssetDatabase.SaveAssets();
                    RenderActiveTab();
                }
            });
            toggleOvBtn.style.alignSelf = Align.FlexStart;
            overlayCard.Add(toggleOvBtn);

            scroll.Add(overlayCard);

            return scroll;
        }

        private static void AddStateRow(VisualElement card, string label, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingTop = 3;
            row.style.paddingBottom = 3;

            var lbl = new Label(label);
            lbl.style.color = new StyleColor(new Color(0.65f, 0.65f, 0.7f));
            lbl.style.fontSize = 11;

            var val = new Label(value);
            val.style.fontSize = 11;
            val.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(lbl);
            row.Add(val);
            card.Add(row);
        }

        private static void AddProgressBar(VisualElement card, string label, int current, int target, float ratio)
        {
            var box = new VisualElement();
            box.AddToClassList("rc-progress-container");

            var row = new VisualElement();
            row.AddToClassList("rc-progress-label-row");

            var titleLbl = new Label(label);
            titleLbl.style.fontSize = 11;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;

            var countLbl = new Label($"{current} / {target} ({(int)(ratio * 100)}%)");
            countLbl.style.fontSize = 11;
            countLbl.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.75f));

            row.Add(titleLbl);
            row.Add(countLbl);
            box.Add(row);

            var bar = new VisualElement();
            bar.AddToClassList("rc-progress-bar");

            var fill = new VisualElement();
            fill.AddToClassList("rc-progress-fill");
            fill.style.width = Length.Percent(ratio * 100f);
            if (ratio >= 1f) fill.style.backgroundColor = new StyleColor(new Color(0.24f, 0.78f, 0.44f));

            bar.Add(fill);
            box.Add(bar);

            card.Add(box);
        }

        #endregion

        #region Tab 2: Configuration & Platforms

        private VisualElement BuildConfigView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            if (_activeConfig == null)
            {
                var emptyCard = RateControlUIStyle.CreateCard("No RateConfig Asset Found");
                emptyCard.Add(RateControlUIStyle.CreateCallout("A RateConfig ScriptableObject is required to manage settings.", "fail"));
                var createBtn = RateControlUIStyle.CreateButton("Create RateConfig Asset", "rc-btn-primary", () =>
                {
                    RateEditorMenuItems.CreateRateConfigAsset();
                    RunDiagnostics();
                    RenderActiveTab();
                });
                createBtn.style.alignSelf = Align.FlexStart;
                emptyCard.Add(createBtn);
                scroll.Add(emptyCard);
                return scroll;
            }

            // Asset Summary Card
            var summaryCard = RateControlUIStyle.CreateCard("Active RateConfig Asset", _activeConfigPath);
            AddStateRow(summaryCard, "Storage Key Prefix", _activeConfig.StorageKeyPrefix);
            AddStateRow(summaryCard, "Auto Initialize on Boot", _activeConfig.AutoInitialize ? "Yes" : "No");
            AddStateRow(summaryCard, "Dialog Resource Path", _activeConfig.DialogResourcePath ?? "(none)");
            AddStateRow(summaryCard, "Auto Sync on Build", _activeConfig.AutoSyncOnBuild ? "Yes" : "No");
            AddStateRow(summaryCard, "Enable Debug Overlay", _activeConfig.EnableDebugOverlay ? "Yes" : "No");

            var pingBtn = RateControlUIStyle.CreateButton("Select / Ping Asset in Project", "rc-btn-secondary", () =>
            {
                Selection.activeObject = _activeConfig;
                EditorGUIUtility.PingObject(_activeConfig);
            });
            pingBtn.style.marginTop = 6;
            pingBtn.style.alignSelf = Align.FlexStart;
            summaryCard.Add(pingBtn);
            scroll.Add(summaryCard);

            // Thresholds Card
            var threshCard = RateControlUIStyle.CreateCard("Configured Trigger Thresholds");
            AddStateRow(threshCard, "App Starts Before First Prompt", _activeConfig.StartsBeforeFirstPrompt.ToString());
            AddStateRow(threshCard, "App Starts Before Subsequent Prompts", _activeConfig.StartsBeforeSubsequentPrompts.ToString());
            AddStateRow(threshCard, "Milestone Events Per Prompt", _activeConfig.EventsPerPrompt.ToString());
            AddStateRow(threshCard, "Remind Later Cooldown Days", $"{_activeConfig.RemindLaterCooldownDays} day(s)");
            scroll.Add(threshCard);

            // Distribution Channels & Store IDs Card
            var storesCard = RateControlUIStyle.CreateCard("Distribution Channels & Store IDs");
            AddStateRow(storesCard, "Android Package ID", string.IsNullOrEmpty(_activeConfig.AndroidPackageId) ? $"(default: {Application.identifier})" : _activeConfig.AndroidPackageId);
            AddStateRow(storesCard, "Apple App Store ID", string.IsNullOrEmpty(_activeConfig.iOSAppId) ? "(unassigned)" : _activeConfig.iOSAppId);
            AddStateRow(storesCard, "Mac App Store ID", string.IsNullOrEmpty(_activeConfig.MacAppStoreId) ? "(unassigned)" : _activeConfig.MacAppStoreId);
            AddStateRow(storesCard, "macOS Channel", _activeConfig.MacOs.ToString());
            AddStateRow(storesCard, "Windows Channel", _activeConfig.Windows.ToString());
            AddStateRow(storesCard, "Linux Channel", _activeConfig.Linux.ToString());
            AddStateRow(storesCard, "Steam App ID", string.IsNullOrEmpty(_activeConfig.SteamAppId) ? "(unassigned)" : _activeConfig.SteamAppId);
            AddStateRow(storesCard, "More Games URL", string.IsNullOrEmpty(_activeConfig.MoreGamesUrl) ? "(none)" : _activeConfig.MoreGamesUrl);
            scroll.Add(storesCard);

            return scroll;
        }

        #endregion

        #region Tab 3: Setup Guide & API Reference

        private VisualElement BuildGuideView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            var quickStartCard = RateControlUIStyle.CreateCard("Quick Start Integration Guide", "Three easy steps to cross-platform rating success.");

            AddGuideStep(quickStartCard, "1. Configure RateConfig",
                "Create a RateConfig asset via 'Create Rate Config Asset' or place it in Assets/Resources/RateConfig.asset. Configure your thresholds and store IDs.");

            AddGuideStep(quickStartCard, "2. Initialize RateControl",
                "Call RateControl.Initialize() during your game's bootstrap phase (or leave AutoInitialize checked on RateConfig).");

            AddGuideStep(quickStartCard, "3. Log Milestone Events",
                "Whenever the player completes a satisfying milestone (e.g. finishes a level, wins a match, solves a puzzle), call RateControl.LogEvent(). When thresholds are reached, the rating prompt is displayed safely!");

            scroll.Add(quickStartCard);

            // Code Snippets Card
            var codeCard = RateControlUIStyle.CreateCard("Runtime API Code Snippets");

            AddCodeSnippet(codeCard, "Log Milestone Event & Prompt Automatically",
                "// Call after meaningful accomplishments\nWagenheimer.RateControl.RateControl.LogEvent();");

            AddCodeSnippet(codeCard, "Manual Review Prompt Check",
                "// Attempts to show the prompt if thresholds and blockers allow\nWagenheimer.RateControl.RateControl.TryPrompt();");

            AddCodeSnippet(codeCard, "Implementing a Rate Blocker (IRateBlocker)",
                "public class GameplayManager : MonoBehaviour, IRateBlocker\n{\n    public bool CanShowRatePrompt => !IsPlayingCutscene && !IsAdShowing;\n}");

            scroll.Add(codeCard);

            return scroll;
        }

        private static void AddGuideStep(VisualElement container, string title, string description)
        {
            var item = new VisualElement();
            item.style.marginBottom = 10;

            var t = new Label(title);
            t.style.fontSize = 12;
            t.style.unityFontStyleAndWeight = FontStyle.Bold;

            var d = new Label(description);
            d.style.fontSize = 11;
            d.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.75f));
            d.style.whiteSpace = WhiteSpace.Normal;
            d.style.marginLeft = 6;
            d.style.marginTop = 2;

            item.Add(t);
            item.Add(d);
            container.Add(item);
        }

        private static void AddCodeSnippet(VisualElement container, string title, string code)
        {
            var item = new VisualElement();
            item.style.marginBottom = 10;

            var t = new Label(title);
            t.style.fontSize = 11.5f;
            t.style.unityFontStyleAndWeight = FontStyle.Bold;
            item.Add(t);

            var codeBox = new VisualElement();
            codeBox.AddToClassList("rc-code-box");

            var codeLbl = new Label(code);
            codeLbl.style.fontSize = 10.5f;
            codeLbl.style.color = new StyleColor(new Color(0.55f, 0.82f, 0.65f));
            codeBox.Add(codeLbl);

            var copyBtn = RateControlUIStyle.CreateButton("Copy Snippet", "rc-btn-secondary", () =>
            {
                EditorGUIUtility.systemCopyBuffer = code;
                Debug.Log($"[RateControl] Copied snippet to clipboard:\n{code}");
            });
            copyBtn.style.marginTop = 4;
            copyBtn.style.alignSelf = Align.FlexEnd;
            codeBox.Add(copyBtn);

            item.Add(codeBox);
            container.Add(item);
        }

        #endregion

        #region Tab 4: About & Updates

        private VisualElement BuildAboutView()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            var infoCard = RateControlUIStyle.CreateCard("Package Information", "Open-source smart rate prompt system by Cezar Wagenheimer.");
            AddStateRow(infoCard, "Package Name", "com.wagenheimer.ratecontrol");
            AddStateRow(infoCard, "Installed Version", _version);
            AddStateRow(infoCard, "Author", "Cezar Wagenheimer");
            AddStateRow(infoCard, "License", "MIT");

            var infoToolbar = new VisualElement();
            infoToolbar.style.flexDirection = FlexDirection.Row;
            infoToolbar.style.marginTop = 10;

            infoToolbar.Add(RateControlUIStyle.CreateButton("Check for Updates", "rc-btn-primary", () =>
            {
                PackageHubWindow.OpenToPackage("com.wagenheimer.ratecontrol");
            }));

            infoToolbar.Add(RateControlUIStyle.CreateButton("GitHub Repository", "rc-btn-secondary", () =>
            {
                Application.OpenURL(RepoUrl);
            }));

            infoToolbar.Add(RateControlUIStyle.CreateButton("Report Issue", "rc-btn-secondary", () =>
            {
                Application.OpenURL(IssuesUrl);
            }));

            infoCard.Add(infoToolbar);
            scroll.Add(infoCard);

            // Capabilities Card
            var capCard = RateControlUIStyle.CreateCard("Key Capabilities");
            AddStateRow(capCard, "Google Play In-App Review", "Automatic Play Core dialog with fallback store URL");
            AddStateRow(capCard, "Apple App Store", "SKStoreReviewManager support with native deep-linking");
            AddStateRow(capCard, "Steam Review Linking", "steam://openurl/ support with web fallback");
            AddStateRow(capCard, "Non-Intrusive Prompt Logic", "Smart thresholds, cooldown periods, and IRateBlocker");
            AddStateRow(capCard, "Drop-in Default Dialog", "Customizable UI Canvas prefab with star ratings");
            scroll.Add(capCard);

            return scroll;
        }

        #endregion

        #region Diagnostics Runner

        private void RunDiagnostics()
        {
            _lastScanTime = DateTime.Now;
            _sections.Clear();

            FindActiveConfig();

            // Run Sections
            _sections.Add(BuildSection1Config());
            _sections.Add(BuildSection2Dialog());
            _sections.Add(BuildSection3Stores());
            _sections.Add(BuildSection4CodeIntegration());
            _sections.Add(BuildSection5ReleaseReadiness());
        }

        private void FindActiveConfig()
        {
            _activeConfig = null;
            _activeConfigPath = null;

            var guids = AssetDatabase.FindAssets("t:RateConfig");
            if (guids.Length > 0)
            {
                _activeConfigPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _activeConfig = AssetDatabase.LoadAssetAtPath<RateConfig>(_activeConfigPath);
            }
        }

        private SetupChecklistWindow.Section BuildSection1Config()
        {
            var sec = new SetupChecklistWindow.Section
            {
                Title = "1. Configuration Asset (RateConfig)",
                Subtitle = "Inspects asset placement, resource path, and storage prefix."
            };

            if (_activeConfig != null)
            {
                var inResources = _activeConfigPath.Contains("/Resources/");
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = inResources ? "RateConfig in Resources folder" : "RateConfig outside Resources folder",
                    Status = inResources ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Warning,
                    Detail = inResources ? $"Located at: {_activeConfigPath}" : $"Located at '{_activeConfigPath}'. Must be inside a Resources/ folder for runtime loading.",
                    ActionLabel = inResources ? "Select Asset" : "Move to Resources",
                    Action = () =>
                    {
                        if (inResources)
                        {
                            Selection.activeObject = _activeConfig;
                            EditorGUIUtility.PingObject(_activeConfig);
                        }
                        else
                        {
                            MoveConfigToResources();
                        }
                    }
                });

                var prefix = _activeConfig.StorageKeyPrefix;
                bool isDefault = string.IsNullOrWhiteSpace(prefix) || prefix == "RateControl";
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = isDefault ? "StorageKeyPrefix is generic ('RateControl')" : $"StorageKeyPrefix is unique ('{prefix}')",
                    Status = isDefault ? SetupChecklistWindow.CheckStatus.Warning : SetupChecklistWindow.CheckStatus.Pass,
                    Detail = isDefault ? "Using a studio-specific prefix (e.g. 'Studio.Game.Rate') prevents key collisions on shared devices." : "Namespace is properly isolated.",
                    ActionLabel = "Select Config",
                    Action = () => { Selection.activeObject = _activeConfig; EditorGUIUtility.PingObject(_activeConfig); }
                });
            }
            else
            {
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = "RateConfig asset missing",
                    Status = SetupChecklistWindow.CheckStatus.Fail,
                    Detail = "No RateConfig asset found in project.",
                    ActionLabel = "Create RateConfig",
                    Action = () => { RateEditorMenuItems.CreateRateConfigAsset(); RunDiagnostics(); }
                });
            }

            return sec;
        }

        private SetupChecklistWindow.Section BuildSection2Dialog()
        {
            var sec = new SetupChecklistWindow.Section
            {
                Title = "2. Rating Dialog Prefab & UI Canvas",
                Subtitle = "Verifies prefab existence, RateDialog component, and UI bindings."
            };

            if (_activeConfig == null)
            {
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = "Cannot verify dialog (RateConfig missing)",
                    Status = SetupChecklistWindow.CheckStatus.Fail
                });
                return sec;
            }

            RateDialog prefabDialog = _activeConfig.DialogPrefab;
            GameObject prefab = prefabDialog != null ? prefabDialog.gameObject : null;
            if (prefab == null && !string.IsNullOrEmpty(_activeConfig.DialogResourcePath))
            {
                var res = Resources.Load<RateDialog>(_activeConfig.DialogResourcePath);
                if (res != null) prefab = res.gameObject;
            }

            if (prefab != null)
            {
                var dialog = prefab.GetComponentInChildren<RateDialog>(true);
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = dialog != null ? $"Dialog component verified ({dialog.GetType().Name})" : "Missing RateDialog component on prefab",
                    Status = dialog != null ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Fail,
                    Detail = $"Prefab: '{prefab.name}' | Component: {(dialog != null ? dialog.GetType().Name : "None")}",
                    ActionLabel = "Select Prefab",
                    Action = () => { Selection.activeObject = prefab; EditorGUIUtility.PingObject(prefab); }
                });
            }
            else
            {
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = "Dialog prefab not found or unassigned",
                    Status = SetupChecklistWindow.CheckStatus.Fail,
                    Detail = "No dialog prefab assigned to DialogPrefab or found via DialogResourcePath.",
                    ActionLabel = "Create Default Prefab",
                    Action = () => { RateEditorMenuItems.CreateDefaultPrefab(); RunDiagnostics(); }
                });
            }

            return sec;
        }

        private SetupChecklistWindow.Section BuildSection3Stores()
        {
            var sec = new SetupChecklistWindow.Section
            {
                Title = "3. Platform Store IDs (Active: " + EditorUserBuildSettings.activeBuildTarget + ")",
                Subtitle = "Validates store review IDs for the active build target."
            };

            if (_activeConfig == null) return sec;

            var target = EditorUserBuildSettings.activeBuildTarget;
            switch (target)
            {
                case BuildTarget.Android:
                    var hasPkg = !string.IsNullOrEmpty(_activeConfig.AndroidPackageId);
                    sec.Items.Add(new SetupChecklistWindow.CheckResult
                    {
                        Title = hasPkg ? $"Android Package ID: {_activeConfig.AndroidPackageId}" : "Android Package empty (uses PlayerSettings)",
                        Status = SetupChecklistWindow.CheckStatus.Pass,
                        Detail = hasPkg ? "Explicit package configured." : $"Falling back to '{PlayerSettings.applicationIdentifier}'.",
                        ActionLabel = "Sync From PlayerSettings",
                        Action = () =>
                        {
                            _activeConfig.AndroidPackageId = PlayerSettings.applicationIdentifier;
                            EditorUtility.SetDirty(_activeConfig);
                            AssetDatabase.SaveAssets();
                            RunDiagnostics();
                        }
                    });
                    break;

                case BuildTarget.iOS:
                    var hasIos = !string.IsNullOrEmpty(_activeConfig.iOSAppId);
                    sec.Items.Add(new SetupChecklistWindow.CheckResult
                    {
                        Title = hasIos ? $"iOS App Store ID: {_activeConfig.iOSAppId}" : "iOS App Store ID is blank",
                        Status = hasIos ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Warning,
                        Detail = hasIos ? "Numeric App Store ID is set." : "Set your numeric Apple App Store ID for store review redirects.",
                        ActionLabel = "Select Config",
                        Action = () => { Selection.activeObject = _activeConfig; EditorGUIUtility.PingObject(_activeConfig); }
                    });
                    break;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                case BuildTarget.StandaloneLinux64:
                    var hasSteam = !string.IsNullOrEmpty(_activeConfig.SteamAppId);
                    sec.Items.Add(new SetupChecklistWindow.CheckResult
                    {
                        Title = hasSteam ? $"Steam App ID: {_activeConfig.SteamAppId}" : "Steam App ID is blank",
                        Status = hasSteam ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Info,
                        Detail = hasSteam ? "Steam review protocol supported." : "Set SteamAppId if distributing on Steam.",
                        ActionLabel = "Select Config",
                        Action = () => { Selection.activeObject = _activeConfig; EditorGUIUtility.PingObject(_activeConfig); }
                    });
                    break;

                default:
                    sec.Items.Add(new SetupChecklistWindow.CheckResult
                    {
                        Title = "More Games / Fallback URL",
                        Status = !string.IsNullOrEmpty(_activeConfig.MoreGamesUrl) ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Info,
                        Detail = _activeConfig.MoreGamesUrl ?? "No fallback / More Games URL set."
                    });
                    break;
            }

            return sec;
        }

        private SetupChecklistWindow.Section BuildSection4CodeIntegration()
        {
            var sec = new SetupChecklistWindow.Section
            {
                Title = "4. Project Code & Runtime Scanner",
                Subtitle = "Audits game scripts for RateControl calls and blockers."
            };

            var initFound = false;
            var eventFound = false;
            var blockerFound = false;

            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" });
            foreach (var guid in scriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;
                if (path.Contains("/Tests/")) continue;

                try
                {
                    var text = File.ReadAllText(path);
                    if (Regex.IsMatch(text, @"RateControl\s*\.\s*(Initialize|Init)\b")) initFound = true;
                    if (Regex.IsMatch(text, @"RateControl\s*\.\s*LogEvent\b")) eventFound = true;
                    if (Regex.IsMatch(text, @":\s*([a-zA-Z0-9_.,\s]*\b)?IRateBlocker\b")) blockerFound = true;
                }
                catch { }
            }

            sec.Items.Add(new SetupChecklistWindow.CheckResult
            {
                Title = initFound ? "RateControl.Initialize() call detected in scripts" : (_activeConfig != null && _activeConfig.AutoInitialize ? "Auto-Initialize on Boot active (RateConfig)" : "No RateControl initialization found"),
                Status = (initFound || (_activeConfig != null && _activeConfig.AutoInitialize)) ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Warning,
                Detail = initFound ? "Game scripts explicitly initialize RateControl." : "RateControl will initialize automatically on scene load via RateConfig."
            });

            sec.Items.Add(new SetupChecklistWindow.CheckResult
            {
                Title = eventFound ? "RateControl.LogEvent() calls detected in scripts" : "No RateControl.LogEvent() calls found in project scripts",
                Status = eventFound ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Warning,
                Detail = eventFound ? "Milestone event tracking is integrated into gameplay." : "Remember to call RateControl.LogEvent() when players accomplish goals!"
            });

            sec.Items.Add(new SetupChecklistWindow.CheckResult
            {
                Title = blockerFound ? "IRateBlocker implementation detected" : "No IRateBlocker detected (Recommended)",
                Status = blockerFound ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Info,
                Detail = blockerFound ? "Custom rate blocker is protecting critical moments." : "Implement IRateBlocker on your gameplay/UI manager to suppress prompts during tense scenes."
            });

            return sec;
        }

        private SetupChecklistWindow.Section BuildSection5ReleaseReadiness()
        {
            var sec = new SetupChecklistWindow.Section
            {
                Title = "5. Release Readiness & Quality Gates",
                Subtitle = "Checks debug flags, build safety, and threshold sanity."
            };

            if (_activeConfig != null)
            {
                if (_activeConfig.EnableDebugOverlay)
                {
                    sec.Items.Add(new SetupChecklistWindow.CheckResult
                    {
                        Title = "Debug Overlay is currently ENABLED",
                        Status = SetupChecklistWindow.CheckStatus.Warning,
                        Detail = "Disable before submitting to public stores (it only shows in Development builds, but recommended off).",
                        ActionLabel = "Disable Overlay",
                        Action = () =>
                        {
                            _activeConfig.EnableDebugOverlay = false;
                            EditorUtility.SetDirty(_activeConfig);
                            AssetDatabase.SaveAssets();
                            RunDiagnostics();
                        }
                    });
                }
                else
                {
                    sec.Items.Add(new SetupChecklistWindow.CheckResult
                    {
                        Title = "Debug Overlay is DISABLED (Production Safe)",
                        Status = SetupChecklistWindow.CheckStatus.Pass,
                        Detail = "Overlay is turned off for clean player presentation."
                    });
                }

                bool thresholdsOk = _activeConfig.StartsBeforeFirstPrompt >= 1 && _activeConfig.EventsPerPrompt >= 1 && _activeConfig.RemindLaterCooldownDays >= 1;
                sec.Items.Add(new SetupChecklistWindow.CheckResult
                {
                    Title = thresholdsOk ? "Prompt threshold values are healthy" : "Aggressive thresholds detected",
                    Status = thresholdsOk ? SetupChecklistWindow.CheckStatus.Pass : SetupChecklistWindow.CheckStatus.Warning,
                    Detail = thresholdsOk ? $"Starts: {_activeConfig.StartsBeforeFirstPrompt}, Events: {_activeConfig.EventsPerPrompt}, Cooldown: {_activeConfig.RemindLaterCooldownDays}d" : "Thresholds < 1 will prompt too early."
                });
            }

            return sec;
        }

        private void MoveConfigToResources()
        {
            if (_activeConfig == null || string.IsNullOrEmpty(_activeConfigPath)) return;

            var targetFolder = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var dest = $"{targetFolder}/RateConfig.asset";
            AssetDatabase.MoveAsset(_activeConfigPath, dest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RunDiagnostics();
            Debug.Log($"[RateControl] Moved RateConfig to '{dest}'.");
        }

        private void LoadPackageVersion()
        {
            try
            {
                if (File.Exists(PackageJsonPath))
                {
                    var json = File.ReadAllText(PackageJsonPath);
                    var match = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"");
                    if (match.Success)
                    {
                        _version = match.Groups[1].Value;
                        return;
                    }
                }
            }
            catch { }
            _version = "1.16.0";
        }

        #endregion
    }
}
