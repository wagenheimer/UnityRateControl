#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.RateControl.Editor
{
    [CustomEditor(typeof(RateConfig))]
    internal sealed class RateConfigEditor : UnityEditor.Editor
    {
        // Brand accent colors per platform
        private static readonly Color kPurple  = new Color(0.55f, 0.42f, 0.82f); // kept for potential future use
        private static readonly Color kCyan    = new Color(0.30f, 0.62f, 0.82f);
        private static readonly Color kGreen   = new Color(0.20f, 0.68f, 0.32f);
        private static readonly Color kOrange  = new Color(0.95f, 0.58f, 0.10f);
        private static readonly Color kSilver  = new Color(0.65f, 0.65f, 0.68f);
        private static readonly Color kBlue    = new Color(0.15f, 0.47f, 0.83f);
        private static readonly Color kSteam   = new Color(0.24f, 0.52f, 0.80f);
        private static readonly Color kGray    = new Color(0.48f, 0.48f, 0.48f);
        private static readonly Color kYellow  = new Color(0.85f, 0.68f, 0.20f);
        private static readonly Color kRed     = new Color(0.75f, 0.28f, 0.28f);

        public override VisualElement CreateInspectorGUI()
        {
            var so   = serializedObject;
            var root = new VisualElement();
            root.style.paddingBottom = 8;

            root.Add(BuildFoldout("Store IDs", kCyan, BuildStoreIdsContent(so)));
            root.Add(BuildFoldout("More Games", kGreen, BuildMoreGamesContent(so)));
            root.Add(BuildFoldout("Trigger Thresholds", kYellow, BuildThresholdsContent(so)));
            root.Add(BuildFoldout("Scene Filter", kRed, BuildSceneFilterContent(so)));
            root.Add(BuildFoldout("Storage", kGray, BuildStorageContent(so)));
            root.Add(BuildFoldout("UI", kGray, BuildUiContent(so)));

            return root;
        }

        // ── Section contents ──────────────────────────────────────────────────────

        private static VisualElement BuildStoreIdsContent(SerializedObject so)
        {
            var c = new VisualElement();

            var note = new HelpBox(
                "Platform is auto-detected at runtime — no manual selection needed.\n" +
                "Android: Application.installerName → Google Play or Amazon\n" +
                "iOS: compile-time (#if UNITY_IOS)\n" +
                "macOS: MacAppStoreId set → Mac App Store, else SteamAppId → Steam\n" +
                "Windows standalone: SteamAppId → Steam review page\n" +
                "WSA: Windows Store (ms-windows-store://)",
                HelpBoxMessageType.Info);
            note.style.marginBottom = 6;
            c.Add(note);

            c.Add(new PropertyField(so.FindProperty("AndroidPackageId")));
            c.Add(new PropertyField(so.FindProperty("iOSAppId")));
            c.Add(new PropertyField(so.FindProperty("MacAppStoreId")));
            c.Add(new PropertyField(so.FindProperty("SteamAppId")));
            return c;
        }

        private static VisualElement BuildThresholdsContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("EventsPerPrompt")));
            c.Add(new PropertyField(so.FindProperty("StartsBeforeFirstPrompt")));
            c.Add(new PropertyField(so.FindProperty("StartsBeforeSubsequentPrompts")));
            c.Add(new PropertyField(so.FindProperty("RemindLaterCooldownDays")));
            return c;
        }

        private static VisualElement BuildSceneFilterContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("BlacklistedScenes")));
            return c;
        }

        private static VisualElement BuildStorageContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("StorageKeyPrefix")));
            return c;
        }

        private static VisualElement BuildUiContent(SerializedObject so)
        {
            var c = new VisualElement();
            c.Add(new PropertyField(so.FindProperty("DialogResourcePath")));
            return c;
        }

        // ── More Games ────────────────────────────────────────────────────────────

        private VisualElement BuildMoreGamesContent(SerializedObject so)
        {
            var c = new VisualElement();

            c.Add(BuildPlatformCard(
                title:       "Google Play",
                accent:      kGreen,
                description: "Publisher name exactly as shown on your Google Play Console listing.",
                howToFind:   "play.google.com/console  →  Setup  →  App info  →  Developer name",
                consoleUrl:  "https://play.google.com/console",
                prop:        so.FindProperty("MoreGamesGoogleDeveloperName"),
                fieldLabel:  "Developer Name",
                previewUrl:  v => string.IsNullOrEmpty(v) ? null
                                  : $"market://search?q=pub:{v}",
                testUrl:     v => string.IsNullOrEmpty(v) ? null
                                  : $"https://play.google.com/store/apps/developer?id={v}"));

            c.Add(BuildAmazonCard());

            c.Add(BuildPlatformCard(
                title:       "Apple App Store  /  Mac App Store",
                accent:      kSilver,
                description: "Numeric Developer ID — not the bundle ID. Example: 964191738\n" +
                             "FASTEST: apps.apple.com → search your app → click developer name → copy the number after /id in the URL.\n" +
                             "ALTERNATIVE: App Store Connect → click your name (top-right) → View My Profile.",
                howToFind:   "apps.apple.com  →  search your app  →  click developer name  →  copy ID from URL",
                consoleUrl:  "https://apps.apple.com",
                prop:        so.FindProperty("MoreGamesAppleDeveloperId"),
                fieldLabel:  "Apple Developer ID",
                previewUrl:  v => string.IsNullOrEmpty(v) ? null
                                  : $"https://apps.apple.com/developer/id{v}",
                testUrl:     v => string.IsNullOrEmpty(v) ? null
                                  : $"https://apps.apple.com/developer/id{v}"));

            c.Add(BuildPlatformCard(
                title:       "Windows Store",
                accent:      kBlue,
                description: "Publisher display name as registered in Microsoft Partner Center.",
                howToFind:   "partner.microsoft.com/dashboard  →  [app]  →  Product identity  →  Publisher display name",
                consoleUrl:  "https://partner.microsoft.com/dashboard",
                prop:        so.FindProperty("MoreGamesWindowsPublisherName"),
                fieldLabel:  "Publisher Name",
                previewUrl:  v => string.IsNullOrEmpty(v) ? null
                                  : $"ms-windows-store://search/?query={v}",
                testUrl:     v => string.IsNullOrEmpty(v) ? null
                                  : $"https://www.microsoft.com/store/apps/windows?search={v}"));

            c.Add(BuildPlatformCard(
                title:       "Steam",
                accent:      kSteam,
                description: "Developer slug from your Steamworks page URL (part after /developer/).",
                howToFind:   "store.steampowered.com/developer/YOUR_SLUG  —  copy the slug from the URL",
                consoleUrl:  "https://partner.steamgames.com",
                prop:        so.FindProperty("MoreGamesSteamDeveloperSlug"),
                fieldLabel:  "Developer Slug",
                previewUrl:  v => string.IsNullOrEmpty(v) ? null
                                  : $"https://store.steampowered.com/developer/{v}",
                testUrl:     v => string.IsNullOrEmpty(v) ? null
                                  : $"https://store.steampowered.com/developer/{v}"));

            c.Add(BuildFallbackCard(so));

            return c;
        }

        // ── Card builders ─────────────────────────────────────────────────────────

        private VisualElement BuildPlatformCard(
            string title, Color accent,
            string description, string howToFind, string consoleUrl,
            SerializedProperty prop, string fieldLabel,
            System.Func<string, string> previewUrl,
            System.Func<string, string> testUrl)
        {
            var card = MakeCard(accent);

            // Title row
            var titleRow = new VisualElement();
            titleRow.style.flexDirection  = FlexDirection.Row;
            titleRow.style.alignItems     = Align.Center;
            titleRow.style.marginBottom   = 5;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize                = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color                   = accent;
            titleLabel.style.flexGrow                = 1;
            titleRow.Add(titleLabel);
            card.Add(titleRow);

            // Description
            var desc = new Label(description);
            desc.style.whiteSpace   = WhiteSpace.Normal;
            desc.style.fontSize     = 10;
            desc.style.color        = new Color(0.58f, 0.58f, 0.58f);
            desc.style.marginBottom = 5;
            card.Add(desc);

            // How-to row with Open Console button
            var howRow = new VisualElement();
            howRow.style.flexDirection = FlexDirection.Row;
            howRow.style.alignItems    = Align.FlexStart;
            howRow.style.marginBottom  = 7;

            var howLabel = new Label(howToFind);
            howLabel.style.whiteSpace = WhiteSpace.Normal;
            howLabel.style.fontSize   = 10;
            howLabel.style.color      = new Color(0.38f, 0.72f, 0.98f);
            howLabel.style.flexGrow   = 1;
            howLabel.style.flexShrink = 1;
            howRow.Add(howLabel);

            var consoleBtn = new Button(() => Application.OpenURL(consoleUrl))
                { text = "Open Console ↗" };
            StyleSmallButton(consoleBtn, accent);
            howRow.Add(consoleBtn);
            card.Add(howRow);

            // Field
            card.Add(new PropertyField(prop, fieldLabel));

            // Divider
            var divider = new VisualElement();
            divider.style.height          = 1;
            divider.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            divider.style.marginTop       = 7;
            divider.style.marginBottom    = 6;
            card.Add(divider);

            // URL preview row
            AddLiveUrlRow(card, prop, previewUrl, testUrl);

            return card;
        }

        private static VisualElement BuildAmazonCard()
        {
            var card = MakeCard(kOrange);

            var titleLabel = new Label("Amazon Appstore");
            titleLabel.style.fontSize                = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color                   = kOrange;
            titleLabel.style.marginBottom            = 5;
            card.Add(titleLabel);

            var info = new HelpBox(
                "URL auto-generated at runtime from Application.identifier — no configuration needed.",
                HelpBoxMessageType.Info);
            card.Add(info);

            var previewLabel = new Label("amzn://apps/android?p={Application.identifier}&showAll=1");
            previewLabel.style.fontSize                  = 9;
            previewLabel.style.color                     = new Color(0.50f, 0.50f, 0.50f);
            previewLabel.style.unityFontStyleAndWeight   = FontStyle.Italic;
            previewLabel.style.marginTop                 = 5;
            previewLabel.style.whiteSpace                = WhiteSpace.Normal;
            card.Add(previewLabel);

            return card;
        }

        private VisualElement BuildFallbackCard(SerializedObject so)
        {
            var card = MakeCard(kGray);

            var titleLabel = new Label("Fallback  /  Website");
            titleLabel.style.fontSize                = 11;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color                   = kGray;
            titleLabel.style.marginBottom            = 5;
            card.Add(titleLabel);

            var desc = new Label("Used when no platform-specific field is configured, or for Custom / sideloaded builds.");
            desc.style.whiteSpace   = WhiteSpace.Normal;
            desc.style.fontSize     = 10;
            desc.style.color        = new Color(0.55f, 0.55f, 0.55f);
            desc.style.marginBottom = 6;
            card.Add(desc);

            var prop = so.FindProperty("MoreGamesUrl");
            card.Add(new PropertyField(prop, "Fallback URL"));

            var divider = new VisualElement();
            divider.style.height          = 1;
            divider.style.backgroundColor = new Color(0.28f, 0.28f, 0.28f);
            divider.style.marginTop       = 7;
            divider.style.marginBottom    = 6;
            card.Add(divider);

            AddLiveUrlRow(card, prop, v => v, v => v);

            return card;
        }

        // ── Live URL preview ──────────────────────────────────────────────────────

        private void AddLiveUrlRow(
            VisualElement parent,
            SerializedProperty prop,
            System.Func<string, string> buildPreview,
            System.Func<string, string> buildTest)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems    = Align.FlexStart;

            var urlLabel = new Label("URL: (fill in the field above)");
            urlLabel.style.fontSize                = 9;
            urlLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            urlLabel.style.color                   = new Color(0.40f, 0.40f, 0.40f);
            urlLabel.style.whiteSpace              = WhiteSpace.Normal;
            urlLabel.style.flexGrow                = 1;
            urlLabel.style.flexShrink              = 1;
            row.Add(urlLabel);

            var openBtn = new Button { text = "Open ↗" };
            StyleSmallButton(openBtn, new Color(0.40f, 0.78f, 0.45f));
            openBtn.style.display = DisplayStyle.None;
            row.Add(openBtn);

            parent.Add(row);

            // Capture mutable URL for the click handler (registered once)
            var currentUrl = new string[1];
            openBtn.clicked += () =>
            {
                if (!string.IsNullOrEmpty(currentUrl[0]))
                    Application.OpenURL(currentUrl[0]);
            };

            // Poll every 300 ms to update preview
            string lastValue = null;
            row.schedule.Execute(() =>
            {
                prop.serializedObject.Update();
                var v = prop.stringValue;
                if (v == lastValue) return;
                lastValue = v;

                var preview = buildPreview?.Invoke(v);
                var test    = buildTest?.Invoke(v);
                currentUrl[0] = test;

                bool hasUrl = !string.IsNullOrEmpty(preview);
                urlLabel.text               = hasUrl ? "URL: " + preview : "URL: (fill in the field above)";
                urlLabel.style.color        = hasUrl
                    ? new Color(0.42f, 0.82f, 0.48f)
                    : new Color(0.40f, 0.40f, 0.40f);
                openBtn.style.display       = !string.IsNullOrEmpty(test)
                    ? DisplayStyle.Flex : DisplayStyle.None;
            }).Every(300);
        }

        // ── Section Foldout ───────────────────────────────────────────────────────

        private static VisualElement BuildFoldout(string title, Color accent, VisualElement content)
        {
            var wrapper = new VisualElement();
            wrapper.style.marginBottom    = 6;
            wrapper.style.borderLeftWidth = 2;
            wrapper.style.borderLeftColor = accent;

            var foldout = new Foldout { text = string.Empty, value = true };

            // Style the toggle label inside the foldout
            var toggle = foldout.Q<Toggle>();
            toggle.style.backgroundColor = new Color(0.20f, 0.20f, 0.20f);
            toggle.style.paddingTop      = 4;
            toggle.style.paddingBottom   = 4;
            toggle.style.paddingLeft     = 8;
            toggle.style.marginBottom    = 0;

            var checkmark = toggle.Q<VisualElement>(className: "unity-toggle__checkmark");
            if (checkmark != null) checkmark.style.display = DisplayStyle.None;

            var headerLabel = new Label(title.ToUpper());
            headerLabel.style.fontSize                = 10;
            headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            headerLabel.style.color                   = accent;
            headerLabel.style.flexGrow                = 1;
            headerLabel.style.alignSelf               = Align.Center;
            toggle.Add(headerLabel);

            var contentWrapper = foldout.Q<VisualElement>(className: "unity-foldout__content");
            if (contentWrapper != null)
            {
                contentWrapper.style.marginLeft = 0;
                contentWrapper.style.paddingTop = 4;
            }

            foldout.Add(content);
            wrapper.Add(foldout);
            return wrapper;
        }

        // ── Visual primitives ─────────────────────────────────────────────────────

        private static VisualElement MakeCard(Color accent)
        {
            var card = new VisualElement();
            card.style.backgroundColor       = new Color(0.17f, 0.17f, 0.17f);
            card.style.borderLeftColor        = accent;
            card.style.borderLeftWidth        = 3;
            card.style.borderTopColor         = new Color(0.26f, 0.26f, 0.26f);
            card.style.borderTopWidth         = 1;
            card.style.borderBottomColor      = new Color(0.26f, 0.26f, 0.26f);
            card.style.borderBottomWidth      = 1;
            card.style.borderRightColor       = new Color(0.26f, 0.26f, 0.26f);
            card.style.borderRightWidth       = 1;
            card.style.borderTopLeftRadius    = 3;
            card.style.borderTopRightRadius   = 3;
            card.style.borderBottomLeftRadius = 3;
            card.style.borderBottomRightRadius = 3;
            card.style.paddingTop             = 8;
            card.style.paddingBottom          = 8;
            card.style.paddingLeft            = 10;
            card.style.paddingRight           = 10;
            card.style.marginBottom           = 5;
            return card;
        }

        private static void StyleSmallButton(Button btn, Color accent)
        {
            btn.style.fontSize          = 9;
            btn.style.paddingTop        = 3;
            btn.style.paddingBottom     = 3;
            btn.style.paddingLeft       = 7;
            btn.style.paddingRight      = 7;
            btn.style.marginLeft        = 5;
            btn.style.alignSelf         = Align.Center;
            btn.style.borderTopColor    = accent;
            btn.style.borderBottomColor = accent;
            btn.style.borderLeftColor   = accent;
            btn.style.borderRightColor  = accent;
            btn.style.borderTopWidth    = 1;
            btn.style.borderBottomWidth = 1;
            btn.style.borderLeftWidth   = 1;
            btn.style.borderRightWidth  = 1;
        }
    }
}
#endif
