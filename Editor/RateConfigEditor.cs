using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.RateControl.Editor
{
    [CustomEditor(typeof(RateConfig))]
    internal sealed class RateConfigEditor : UnityEditor.Editor
    {
        private static readonly Color kCyan   = new Color(0.30f, 0.62f, 0.82f);
        private static readonly Color kGreen  = new Color(0.20f, 0.68f, 0.32f);
        private static readonly Color kOrange = new Color(0.95f, 0.58f, 0.10f);
        private static readonly Color kSilver = new Color(0.65f, 0.65f, 0.68f);
        private static readonly Color kBlue   = new Color(0.15f, 0.47f, 0.83f);
        private static readonly Color kSteam  = new Color(0.24f, 0.52f, 0.80f);
        private static readonly Color kGray   = new Color(0.48f, 0.48f, 0.48f);
        private static readonly Color kYellow = new Color(0.85f, 0.68f, 0.20f);
        private static readonly Color kRed    = new Color(0.75f, 0.28f, 0.28f);

        public override VisualElement CreateInspectorGUI()
        {
            var so   = serializedObject;
            var root = new VisualElement();
            root.style.paddingBottom = 8;

            // Action buttons row
            var helpRow = new VisualElement();
            helpRow.style.flexDirection  = FlexDirection.Row;
            helpRow.style.justifyContent = Justify.FlexEnd;
            helpRow.style.marginBottom   = 6;
            helpRow.style.paddingRight   = 2;

            var checklistBtn = new Button(OpenChecklistWindow) { text = "📋  Setup & Checklist" };
            checklistBtn.style.fontSize        = 10;
            checklistBtn.style.paddingLeft     = 10;
            checklistBtn.style.paddingRight    = 10;
            checklistBtn.style.paddingTop      = 4;
            checklistBtn.style.paddingBottom   = 4;
            checklistBtn.style.color           = kGreen;
            checklistBtn.style.borderTopColor  = checklistBtn.style.borderBottomColor =
            checklistBtn.style.borderLeftColor = checklistBtn.style.borderRightColor = kGreen;
            checklistBtn.style.borderTopWidth  = checklistBtn.style.borderBottomWidth =
            checklistBtn.style.borderLeftWidth = checklistBtn.style.borderRightWidth = 1;
            checklistBtn.style.borderTopLeftRadius    = checklistBtn.style.borderTopRightRadius =
            checklistBtn.style.borderBottomLeftRadius = checklistBtn.style.borderBottomRightRadius = 3;
            checklistBtn.style.backgroundColor = new Color(0.12f, 0.24f, 0.16f);
            checklistBtn.style.marginRight     = 6;
            helpRow.Add(checklistBtn);

            var helpBtn = new Button(OpenSetupGuide) { text = "?  Setup Guide" };
            helpBtn.style.fontSize        = 10;
            helpBtn.style.paddingLeft     = 10;
            helpBtn.style.paddingRight    = 10;
            helpBtn.style.paddingTop      = 4;
            helpBtn.style.paddingBottom   = 4;
            helpBtn.style.color           = kCyan;
            helpBtn.style.borderTopColor  = helpBtn.style.borderBottomColor =
            helpBtn.style.borderLeftColor = helpBtn.style.borderRightColor = kCyan;
            helpBtn.style.borderTopWidth  = helpBtn.style.borderBottomWidth =
            helpBtn.style.borderLeftWidth = helpBtn.style.borderRightWidth = 1;
            helpBtn.style.borderTopLeftRadius    = helpBtn.style.borderTopRightRadius =
            helpBtn.style.borderBottomLeftRadius = helpBtn.style.borderBottomRightRadius = 3;
            helpBtn.style.backgroundColor = new Color(0.14f, 0.22f, 0.32f);
            helpRow.Add(helpBtn);
            root.Add(helpRow);

            root.Add(BuildQuickSyncCard(so));

            root.Add(Section("Distribution Channels", kOrange, DistributionContent(so)));
            root.Add(Section("Store IDs",             kCyan,   StoreIdsContent(so)));
            root.Add(Section("More Games",            kGreen,  MoreGamesContent(so)));
            root.Add(Section("Trigger Thresholds",    kYellow, ThresholdsContent(so)));
            root.Add(Section("Scene Filter",          kRed,    SceneFilterContent(so)));
            root.Add(Section("Storage",               kGray,   StorageContent(so)));
            root.Add(Section("UI",                    kGray,   UiContent(so)));
            root.Add(Section("Bootstrap & Automation", kCyan,  LifecycleContent(so)));

            return root;
        }

        // ── Section content builders ──────────────────────────────────────────────

        private VisualElement BuildQuickSyncCard(SerializedObject so)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new Color(0.14f, 0.18f, 0.24f);
            card.style.borderLeftColor = kCyan;
            card.style.borderLeftWidth = 3;
            card.style.borderTopColor = card.style.borderBottomColor = card.style.borderRightColor = new Color(0.24f, 0.30f, 0.38f);
            card.style.borderTopWidth = card.style.borderBottomWidth = card.style.borderRightWidth = 1;
            card.style.borderTopLeftRadius = card.style.borderTopRightRadius =
            card.style.borderBottomLeftRadius = card.style.borderBottomRightRadius = 4;
            card.style.paddingLeft = card.style.paddingRight = 10;
            card.style.paddingTop = card.style.paddingBottom = 8;
            card.style.marginBottom = 8;

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 4;

            var titleLbl = new Label("⚡ BUILDPIPELINE & STORE AUTO-SYNC");
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.fontSize = 11;
            titleLbl.style.color = kCyan;
            titleLbl.style.flexGrow = 1;
            titleRow.Add(titleLbl);

            var gameConfig = RateBuildPreprocessor.FindGameConfig();
            var statusBadge = new Label(gameConfig != null ? "● GameConfig Linked" : "○ PlayerSettings Fallback");
            statusBadge.style.fontSize = 10;
            statusBadge.style.color = gameConfig != null ? kGreen : kSilver;
            titleRow.Add(statusBadge);
            card.Add(titleRow);

            var descLbl = new Label(
                gameConfig != null
                    ? $"Linked with GameConfig '{gameConfig.name}'. Auto-Sync reads IDs and switches channels automatically when building with BuildPipeline or CLI."
                    : "Auto-sync reads bundle IDs and store parameters from Project PlayerSettings and BuildPipeline.");
            descLbl.style.fontSize = 10;
            descLbl.style.color = new Color(0.72f, 0.72f, 0.72f);
            descLbl.style.whiteSpace = WhiteSpace.Normal;
            descLbl.style.marginBottom = 8;
            card.Add(descLbl);

            var btnRow = new VisualElement();
            btnRow.style.flexDirection = FlexDirection.Row;
            btnRow.style.flexWrap = Wrap.Wrap;

            var syncBtn = new Button(() =>
            {
                var targetConfig = (RateConfig)target;
                bool changed = RateBuildPreprocessor.SyncConfig(targetConfig, EditorUserBuildSettings.activeBuildTarget);
                so.Update();
                EditorUtility.DisplayDialog(
                    "Auto-Sync Completed",
                    changed
                        ? "RateConfig was successfully synchronized from project settings and GameConfig!"
                        : "RateConfig is already up to date with project settings and GameConfig.",
                    "OK");
            }) { text = "⚡ Sync from Project & GameConfig" };
            syncBtn.style.fontSize = 10;
            syncBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            syncBtn.style.color = Color.white;
            syncBtn.style.backgroundColor = new Color(0.18f, 0.40f, 0.62f);
            syncBtn.style.marginRight = 6;
            syncBtn.style.marginBottom = 4;
            syncBtn.style.paddingLeft = syncBtn.style.paddingRight = 10;
            syncBtn.style.paddingTop = syncBtn.style.paddingBottom = 4;
            btnRow.Add(syncBtn);

            var pMac = new Button(() => ApplyPreset(so, MacOsChannel.MacAppStore, StandaloneChannel.None)) { text = "🍎 Mac App Store" };
            pMac.style.fontSize = 9;
            pMac.style.marginRight = 4;
            pMac.style.marginBottom = 4;
            btnRow.Add(pMac);

            var pMgs = new Button(() => ApplyPreset(so, MacOsChannel.MacGameStore, StandaloneChannel.None)) { text = "🎮 MacGameStore" };
            pMgs.style.fontSize = 9;
            pMgs.style.marginRight = 4;
            pMgs.style.marginBottom = 4;
            btnRow.Add(pMgs);

            var pSteam = new Button(() => ApplyPreset(so, MacOsChannel.Steam, StandaloneChannel.Steam)) { text = "♨️ Steam" };
            pSteam.style.fontSize = 9;
            pSteam.style.marginRight = 4;
            pSteam.style.marginBottom = 4;
            btnRow.Add(pSteam);

            card.Add(btnRow);
            return card;
        }

        private static void ApplyPreset(SerializedObject so, MacOsChannel mac, StandaloneChannel pc)
        {
            so.Update();
            so.FindProperty("MacOs").enumValueIndex = (int)mac;
            so.FindProperty("Windows").enumValueIndex = (int)pc;
            so.FindProperty("Linux").enumValueIndex = (int)pc;
            so.ApplyModifiedProperties();
        }

        private static VisualElement DistributionContent(SerializedObject so)
        {
            var c = new VisualElement();

            var note = new HelpBox(
                "Select the distribution channel for each desktop platform.\n" +
                "Android and iOS are always active — no selection needed.\n" +
                "None = rate and more-games disabled on that platform (e.g. itch.io / direct download).\n" +
                "A warning will appear at build time if the channel is None for the active build target.",
                HelpBoxMessageType.Info);
            note.style.marginBottom = 6;
            c.Add(note);

            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            bool isMac = activeTarget == BuildTarget.StandaloneOSX;
            bool isWin = activeTarget == BuildTarget.StandaloneWindows || activeTarget == BuildTarget.StandaloneWindows64;
            bool isLin = activeTarget == BuildTarget.StandaloneLinux64;

            c.Add(ChannelRow("macOS", so.FindProperty("MacOs"), kSilver, isMac));

            var mgsField = new PropertyField(so.FindProperty("MacGameStoreUrl"), "MacGameStore URL");
            mgsField.style.marginLeft = 74;
            mgsField.style.marginBottom = 6;
            mgsField.schedule.Execute(() =>
            {
                var p = so.FindProperty("MacOs");
                mgsField.style.display = p != null && p.enumValueIndex == (int)MacOsChannel.MacGameStore
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }).Every(300);
            c.Add(mgsField);

            c.Add(ChannelRow("Windows", so.FindProperty("Windows"), kBlue, isWin));
            c.Add(ChannelRow("Linux",   so.FindProperty("Linux"),   kSteam, isLin));

            return c;
        }

        private static VisualElement ChannelRow(string label, SerializedProperty prop, Color accent, bool isActive = false)
        {
            var row = new VisualElement();
            row.style.flexDirection       = FlexDirection.Row;
            row.style.alignItems          = Align.Center;
            row.style.marginBottom        = 3;
            row.style.paddingLeft         = 10;
            row.style.paddingRight        = 8;
            row.style.paddingTop          = 5;
            row.style.paddingBottom       = 5;
            row.style.backgroundColor     = new Color(0.21f, 0.21f, 0.21f);
            row.style.borderLeftColor     = accent;
            row.style.borderLeftWidth     = 3;
            row.style.borderTopColor      = new Color(0.27f, 0.27f, 0.27f);
            row.style.borderTopWidth      = 1;
            row.style.borderBottomColor   = new Color(0.27f, 0.27f, 0.27f);
            row.style.borderBottomWidth   = 1;
            row.style.borderRightColor    = new Color(0.27f, 0.27f, 0.27f);
            row.style.borderRightWidth    = 1;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius =
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 3;

            var lbl = new Label(isActive ? $"{label} [ACTIVE]" : label);
            lbl.style.fontSize                = 11;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color                   = isActive ? kYellow : accent;
            lbl.style.width                   = isActive ? 120 : 70;
            row.Add(lbl);

            var field = new PropertyField(prop, "");
            field.style.flexGrow = 1;
            row.Add(field);

            // Live warning dot when None
            var dot = new Label(isActive ? "⚠ None (Active Target)" : "⚠ None");
            dot.style.fontSize   = 9;
            dot.style.color      = kOrange;
            dot.style.marginLeft = 6;
            dot.style.alignSelf  = Align.Center;
            row.Add(dot);

            row.schedule.Execute(() =>
            {
                prop.serializedObject.Update();
                dot.style.display = prop.intValue == 0 ? DisplayStyle.Flex : DisplayStyle.None;
                row.style.borderLeftColor = prop.intValue == 0
                    ? kOrange
                    : accent;
            }).Every(300);

            return row;
        }

        private static VisualElement StoreIdsContent(SerializedObject so)
        {
            var c = new VisualElement();

            var note = new HelpBox(
                "Android auto-detected at runtime via Application.installerName (Google Play / Amazon).\n" +
                "iOS uses SKStoreReviewManager (always active). WSA uses Windows Store URI (always active).\n" +
                "macOS / Windows / Linux channels are set in Distribution Channels above.",
                HelpBoxMessageType.Info);
            note.style.marginBottom = 6;
            c.Add(note);

            c.Add(new PropertyField(so.FindProperty("AndroidPackageId")));
            c.Add(new PropertyField(so.FindProperty("iOSAppId")));
            c.Add(new PropertyField(so.FindProperty("MacAppStoreId")));
            c.Add(new PropertyField(so.FindProperty("SteamAppId")));
            return c;
        }

        private VisualElement MoreGamesContent(SerializedObject so)
        {
            var c = new VisualElement();

            c.Add(CollapsiblePlatform(
                "Google Play", kGreen,
                "Publisher name as shown on Google Play Console.",
                "play.google.com/console  →  Setup  →  App info  →  Developer name",
                "https://play.google.com/console",
                so.FindProperty("MoreGamesGoogleDeveloperName"),
                v => $"market://search?q=pub:{v}",
                v => $"https://play.google.com/store/apps/developer?id={v}"));

            c.Add(AmazonRow());

            c.Add(CollapsiblePlatform(
                "Apple App Store / Mac", kSilver,
                "Numeric Developer ID (not the bundle ID). Example: 964191738\n" +
                "Fastest: apps.apple.com → search your app → click developer name → copy number after /id in the URL.",
                "apps.apple.com  →  search your app  →  click developer name  →  copy ID from URL",
                "https://apps.apple.com",
                so.FindProperty("MoreGamesAppleDeveloperId"),
                v => $"https://apps.apple.com/developer/id{v}",
                v => $"https://apps.apple.com/developer/id{v}"));

            c.Add(CollapsiblePlatform(
                "MacGameStore", kOrange,
                "MacGameStore developer / catalog page URL.\nUsed when macOS channel is set to MacGameStore.",
                "macgamestore.com  →  publisher page URL",
                "https://www.macgamestore.com",
                so.FindProperty("MoreGamesMacGameStoreUrl"),
                v => v, v => v));

            c.Add(CollapsiblePlatform(
                "Windows Store", kBlue,
                "Publisher display name from Microsoft Partner Center.",
                "partner.microsoft.com/dashboard  →  [app]  →  Product identity  →  Publisher display name",
                "https://partner.microsoft.com/dashboard",
                so.FindProperty("MoreGamesWindowsPublisherName"),
                v => $"ms-windows-store://search/?query={v}",
                v => $"https://www.microsoft.com/store/apps/windows?search={v}"));

            c.Add(CollapsiblePlatform(
                "Steam  (Win · macOS · Linux)", kSteam,
                "Developer slug from your Steamworks page URL (part after /developer/).\nCovers Windows, macOS (Steam channel), and Linux builds.",
                "store.steampowered.com/developer/YOUR_SLUG  →  copy slug from the URL",
                "https://partner.steamgames.com",
                so.FindProperty("MoreGamesSteamDeveloperSlug"),
                v => $"https://store.steampowered.com/developer/{v}",
                v => $"https://store.steampowered.com/developer/{v}"));

            c.Add(CollapsiblePlatform(
                "Fallback / Website", kGray,
                "Used when no platform-specific field is set, or for Custom / sideloaded builds.",
                null, null,
                so.FindProperty("MoreGamesUrl"),
                v => v, v => v));

            return c;
        }

        private static VisualElement ThresholdsContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("EventsPerPrompt")));
            c.Add(new PropertyField(so.FindProperty("StartsBeforeFirstPrompt")));
            c.Add(new PropertyField(so.FindProperty("StartsBeforeSubsequentPrompts")));
            c.Add(new PropertyField(so.FindProperty("RemindLaterCooldownDays")));
            return c;
        }

        private static VisualElement SceneFilterContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("BlacklistedScenes")));
            return c;
        }

        private static VisualElement StorageContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("StorageKeyPrefix")));
            return c;
        }

        private static VisualElement UiContent(SerializedObject so)
        {
            var c = new VisualElement();

            var prefabProp = so.FindProperty("DialogPrefab");

            var warning = new HelpBox(
                "No Dialog Prefab assigned. Drag your RateDialog prefab here — " +
                "without it, OnPromptRequested fires but no UI will appear.",
                HelpBoxMessageType.Warning);
            warning.style.marginBottom = 6;
            c.Add(warning);

            c.Add(new PropertyField(prefabProp));

            c.schedule.Execute(() =>
            {
                prefabProp.serializedObject.Update();
                warning.style.display = prefabProp.objectReferenceValue == null
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(300);

            return c;
        }

        private static VisualElement LifecycleContent(SerializedObject so)
        {
            var c = new VisualElement();

            var autoSyncProp = so.FindProperty("AutoSyncOnBuild");
            if (autoSyncProp != null)
            {
                c.Add(new PropertyField(autoSyncProp, "Auto-Sync on Build"));
            }

            var note = new HelpBox(
                "RateControl is fully automated!\n\n" +
                "• Automatic Lifecycle: RateControl runs automatically via [RuntimeInitializeOnLoadMethod] — no RateControlBootstrap or scene components needed.\n" +
                "• BuildPipeline & CLI: RateBuildPreprocessor automatically syncs store IDs and switches channels based on target and GameConfig before building.\n" +
                "• Auto-Sync on Build: When checked, automatically synchronizes RateConfig from GameConfig and PlayerSettings on every build.",
                HelpBoxMessageType.Info);
            note.style.marginBottom = 6;
            c.Add(note);

            return c;
        }

        // ── Collapsible platform row ──────────────────────────────────────────────

        private VisualElement CollapsiblePlatform(
            string title, Color accent,
            string description,
            string howToFind, string consoleUrl,
            SerializedProperty prop,
            System.Func<string, string> buildPreview,
            System.Func<string, string> buildTestUrl)
        {
            var outer = new VisualElement();
            outer.style.marginBottom = 3;

            // ── Header row (always visible, clickable) ────────────────────────────
            var header = new VisualElement();
            header.style.flexDirection    = FlexDirection.Row;
            header.style.alignItems       = Align.Center;
            header.style.backgroundColor  = new Color(0.21f, 0.21f, 0.21f);
            header.style.borderLeftColor  = accent;
            header.style.borderLeftWidth  = 3;
            header.style.borderTopColor         = new Color(0.27f, 0.27f, 0.27f);
            header.style.borderTopWidth         = 1;
            header.style.borderBottomColor      = new Color(0.27f, 0.27f, 0.27f);
            header.style.borderBottomWidth      = 1;
            header.style.borderRightColor       = new Color(0.27f, 0.27f, 0.27f);
            header.style.borderRightWidth       = 1;
            header.style.borderTopLeftRadius    = 3;
            header.style.borderTopRightRadius   = 3;
            header.style.borderBottomLeftRadius = 3;
            header.style.borderBottomRightRadius = 3;
            header.style.paddingLeft    = 10;
            header.style.paddingRight   = 8;
            header.style.paddingTop     = 6;
            header.style.paddingBottom  = 6;

            var arrow = new Label("▶");
            arrow.style.color       = new Color(0.55f, 0.55f, 0.55f);
            arrow.style.fontSize    = 9;
            arrow.style.marginRight = 6;
            arrow.style.width       = 10;
            header.Add(arrow);

            var titleLabel = new Label(title);
            titleLabel.style.fontSize                = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color                   = accent;
            titleLabel.style.flexGrow                = 1;
            header.Add(titleLabel);

            var statusLabel = new Label("not configured");
            statusLabel.style.fontSize   = 10;
            statusLabel.style.color      = new Color(0.38f, 0.38f, 0.38f);
            statusLabel.style.marginLeft = 6;
            statusLabel.style.alignSelf  = Align.Center;
            header.Add(statusLabel);

            // ── Expanded content (hidden by default) ──────────────────────────────
            var content = new VisualElement();
            content.style.display           = DisplayStyle.None;
            content.style.backgroundColor   = new Color(0.17f, 0.17f, 0.17f);
            content.style.borderLeftColor   = accent;
            content.style.borderLeftWidth   = 3;
            content.style.borderBottomColor      = new Color(0.27f, 0.27f, 0.27f);
            content.style.borderBottomWidth      = 1;
            content.style.borderRightColor       = new Color(0.27f, 0.27f, 0.27f);
            content.style.borderRightWidth       = 1;
            content.style.borderBottomLeftRadius = 3;
            content.style.borderBottomRightRadius = 3;
            content.style.paddingLeft   = 12;
            content.style.paddingRight  = 10;
            content.style.paddingTop    = 8;
            content.style.paddingBottom = 8;

            // Description
            var desc = new Label(description);
            desc.style.whiteSpace   = WhiteSpace.Normal;
            desc.style.fontSize     = 10;
            desc.style.color        = new Color(0.58f, 0.58f, 0.58f);
            desc.style.marginBottom = 7;
            content.Add(desc);

            // How-to row (optional)
            if (!string.IsNullOrEmpty(howToFind))
            {
                var howRow = new VisualElement();
                howRow.style.flexDirection = FlexDirection.Row;
                howRow.style.alignItems    = Align.FlexStart;
                howRow.style.marginBottom  = 8;

                var howLabel = new Label(howToFind);
                howLabel.style.whiteSpace = WhiteSpace.Normal;
                howLabel.style.fontSize   = 10;
                howLabel.style.color      = new Color(0.38f, 0.72f, 0.98f);
                howLabel.style.flexGrow   = 1;
                howLabel.style.flexShrink = 1;
                howRow.Add(howLabel);

                if (!string.IsNullOrEmpty(consoleUrl))
                {
                    var btn = new Button(() => Application.OpenURL(consoleUrl)) { text = "Open ↗" };
                    SmallBtn(btn, accent);
                    howRow.Add(btn);
                }
                content.Add(howRow);
            }

            // Field
            content.Add(new PropertyField(prop, prop.displayName));

            // Divider
            var divider = new VisualElement();
            divider.style.height          = 1;
            divider.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            divider.style.marginTop       = 7;
            divider.style.marginBottom    = 6;
            content.Add(divider);

            // URL preview row
            var previewRow = new VisualElement();
            previewRow.style.flexDirection = FlexDirection.Row;
            previewRow.style.alignItems    = Align.FlexStart;
            content.Add(previewRow);

            var urlLabel = new Label("URL: (fill in field above)");
            urlLabel.style.fontSize                = 9;
            urlLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            urlLabel.style.color                   = new Color(0.40f, 0.40f, 0.40f);
            urlLabel.style.whiteSpace              = WhiteSpace.Normal;
            urlLabel.style.flexGrow                = 1;
            urlLabel.style.flexShrink              = 1;
            previewRow.Add(urlLabel);

            var openBtn = new Button { text = "Open ↗" };
            SmallBtn(openBtn, new Color(0.35f, 0.75f, 0.42f));
            openBtn.style.display = DisplayStyle.None;
            previewRow.Add(openBtn);

            // ── Toggle logic ──────────────────────────────────────────────────────
            bool expanded = false;
            void SetExpanded(bool v)
            {
                expanded = v;
                arrow.text                   = v ? "▼" : "▶";
                content.style.display        = v ? DisplayStyle.Flex : DisplayStyle.None;
                // round only bottom corners when expanded, all when collapsed
                header.style.borderBottomLeftRadius  = v ? 0 : 3;
                header.style.borderBottomRightRadius = v ? 0 : 3;
            }

            header.RegisterCallback<ClickEvent>(_ => SetExpanded(!expanded));

            // ── Poll: update status label + URL preview ───────────────────────────
            var currentTestUrl = new string[1];
            openBtn.clicked += () => { if (!string.IsNullOrEmpty(currentTestUrl[0])) Application.OpenURL(currentTestUrl[0]); };

            string lastValue = null;
            outer.schedule.Execute(() =>
            {
                prop.serializedObject.Update();
                var v = prop.stringValue;
                if (v == lastValue) return;
                lastValue = v;

                bool hasVal = !string.IsNullOrEmpty(v);

                // Header status
                statusLabel.text         = hasVal ? v : "not configured";
                statusLabel.style.color  = hasVal
                    ? new Color(0.45f, 0.80f, 0.50f)
                    : new Color(0.38f, 0.38f, 0.38f);

                // URL preview
                var preview = hasVal ? buildPreview?.Invoke(v) : null;
                currentTestUrl[0] = hasVal ? buildTestUrl?.Invoke(v) : null;
                bool hasUrl = !string.IsNullOrEmpty(preview);
                urlLabel.text       = hasUrl ? "URL: " + preview : "URL: (fill in field above)";
                urlLabel.style.color = hasUrl
                    ? new Color(0.42f, 0.82f, 0.48f)
                    : new Color(0.40f, 0.40f, 0.40f);
                openBtn.style.display = !string.IsNullOrEmpty(currentTestUrl[0])
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(300);

            outer.Add(header);
            outer.Add(content);
            return outer;
        }

        // ── Amazon compact row (nothing to configure) ─────────────────────────────

        private static VisualElement AmazonRow()
        {
            var row = new VisualElement();
            row.style.flexDirection       = FlexDirection.Row;
            row.style.alignItems          = Align.Center;
            row.style.backgroundColor     = new Color(0.21f, 0.21f, 0.21f);
            row.style.borderLeftColor     = kOrange;
            row.style.borderLeftWidth     = 3;
            row.style.borderTopColor      = new Color(0.27f, 0.27f, 0.27f);
            row.style.borderTopWidth      = 1;
            row.style.borderBottomColor   = new Color(0.27f, 0.27f, 0.27f);
            row.style.borderBottomWidth   = 1;
            row.style.borderRightColor    = new Color(0.27f, 0.27f, 0.27f);
            row.style.borderRightWidth    = 1;
            row.style.borderTopLeftRadius = row.style.borderTopRightRadius =
            row.style.borderBottomLeftRadius = row.style.borderBottomRightRadius = 3;
            row.style.paddingLeft   = 10;
            row.style.paddingRight  = 10;
            row.style.paddingTop    = 6;
            row.style.paddingBottom = 6;
            row.style.marginBottom  = 3;

            var title = new Label("Amazon Appstore");
            title.style.fontSize                = 11;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color                   = kOrange;
            title.style.flexGrow                = 1;
            row.Add(title);

            var badge = new Label("auto  ✓");
            badge.style.fontSize   = 10;
            badge.style.color      = new Color(0.95f, 0.58f, 0.10f);
            badge.style.marginLeft = 6;
            row.Add(badge);

            return row;
        }

        // ── Outer section foldout ─────────────────────────────────────────────────

        private static VisualElement Section(string title, Color accent, VisualElement content)
        {
            var wrapper = new VisualElement();
            wrapper.style.marginBottom    = 6;
            wrapper.style.borderLeftWidth = 2;
            wrapper.style.borderLeftColor = accent;

            var foldout = new Foldout { text = string.Empty, value = true };

            var toggle = foldout.Q<Toggle>();
            toggle.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            toggle.style.paddingTop      = 4;
            toggle.style.paddingBottom   = 4;
            toggle.style.paddingLeft     = 8;
            toggle.style.marginBottom    = 0;

            var check = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (check != null) check.style.display = DisplayStyle.None;

            var lbl = new Label(title.ToUpper());
            lbl.style.fontSize                = 10;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color                   = accent;
            lbl.style.flexGrow                = 1;
            lbl.style.alignSelf               = Align.Center;
            toggle.Add(lbl);

            var inner = foldout.Q<VisualElement>(className: "unity-foldout__content");
            if (inner != null) { inner.style.marginLeft = 0; inner.style.paddingTop = 4; }

            foldout.Add(content);
            wrapper.Add(foldout);
            return wrapper;
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static void OpenChecklistWindow()
        {
            SetupChecklistWindow.Open();
        }

        private static void OpenSetupGuide()
        {
            foreach (var t in TypeCache.GetTypesDerivedFrom<EditorWindow>())
            {
                if (t.FullName != "Wagenheimer.RateControl.Editor.RateControlDocWindow") continue;
                var m = t.GetMethod("Open",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (m != null) { m.Invoke(null, null); return; }
            }
            Application.OpenURL("https://github.com/wagenheimer/UnityRateControl#readme");
        }

        private static void SmallBtn(Button btn, Color accent)
        {
            btn.style.fontSize          = 9;
            btn.style.paddingTop        = 3;
            btn.style.paddingBottom     = 3;
            btn.style.paddingLeft       = 7;
            btn.style.paddingRight      = 7;
            btn.style.marginLeft        = 5;
            btn.style.alignSelf         = Align.Center;
            btn.style.borderTopColor    = btn.style.borderBottomColor =
            btn.style.borderLeftColor   = btn.style.borderRightColor = accent;
            btn.style.borderTopWidth    = btn.style.borderBottomWidth =
            btn.style.borderLeftWidth   = btn.style.borderRightWidth = 1;
        }
    }
}
