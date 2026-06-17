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
        public override VisualElement CreateInspectorGUI()
        {
            var so = serializedObject;
            var root = new VisualElement();
            root.style.paddingTop = 4;

            root.Add(CreateSection("Platform", so, "Platform"));
            root.Add(CreateSection("Store IDs", so,
                "AndroidPackageId", "iOSAppId", "MacAppStoreId", "SteamAppId"));
            root.Add(CreateMoreGamesSection(so));
            root.Add(CreateSection("Trigger Thresholds", so,
                "EventsPerPrompt", "StartsBeforeFirstPrompt",
                "StartsBeforeSubsequentPrompts", "RemindLaterCooldownDays"));
            root.Add(CreateSection("Scene Filter", so, "BlacklistedScenes"));
            root.Add(CreateSection("Storage", so, "StorageKeyPrefix"));
            root.Add(CreateSection("UI", so, "DialogResourcePath"));

            return root;
        }

        // ── More Games ────────────────────────────────────────────────────────────

        private static VisualElement CreateMoreGamesSection(SerializedObject so)
        {
            var foldout = new Foldout { text = "More Games", value = true };
            foldout.style.marginBottom = 8;

            foldout.Add(CreatePlatformBlock(
                "Google Play",
                "Publisher name as shown on your Google Play listing.",
                "play.google.com/console → Setup → App info → Developer name",
                "https://play.google.com/console",
                new PropertyField(so.FindProperty("MoreGamesGoogleDeveloperName"), "Developer Name")));

            var amazonNote = new HelpBox(
                "Amazon Appstore: URL auto-generated from Application.identifier at runtime.\nNo field needed.",
                HelpBoxMessageType.Info);
            amazonNote.style.marginTop = 4;
            amazonNote.style.marginBottom = 4;
            foldout.Add(amazonNote);

            foldout.Add(CreatePlatformBlock(
                "Apple App Store  /  Mac App Store",
                "Numeric Developer ID — not the bundle ID. Looks like: 964191738",
                "appstoreconnect.apple.com → click your name (top-right) → View My Profile",
                "https://appstoreconnect.apple.com",
                new PropertyField(so.FindProperty("MoreGamesAppleDeveloperId"), "Apple Developer ID")));

            foldout.Add(CreatePlatformBlock(
                "Windows Store",
                "Publisher display name as registered in Partner Center.",
                "partner.microsoft.com/dashboard → [app] → Product management → Product identity → Publisher display name",
                "https://partner.microsoft.com/dashboard",
                new PropertyField(so.FindProperty("MoreGamesWindowsPublisherName"), "Publisher Name")));

            foldout.Add(CreatePlatformBlock(
                "Steam",
                "Developer slug from your Steamworks developer page URL (the part after /developer/).",
                "store.steampowered.com/developer/YOUR_SLUG — copy the slug from the URL",
                "https://partner.steamgames.com",
                new PropertyField(so.FindProperty("MoreGamesSteamDeveloperSlug"), "Developer Slug")));

            var fallback = new Foldout { text = "Fallback / Website", value = false };
            fallback.style.marginTop = 4;
            fallback.Add(new HelpBox(
                "Used when no platform-specific field is configured, or for sideloaded / Custom builds.",
                HelpBoxMessageType.None));
            fallback.Add(new PropertyField(so.FindProperty("MoreGamesUrl"), "Fallback URL"));
            foldout.Add(fallback);

            return foldout;
        }

        private static VisualElement CreatePlatformBlock(
            string title, string description, string howToFind, string link, PropertyField field)
        {
            var box = new Box();
            box.style.paddingTop    = 6;
            box.style.paddingBottom = 6;
            box.style.paddingLeft   = 8;
            box.style.paddingRight  = 8;
            box.style.marginTop     = 4;
            box.style.marginBottom  = 4;

            var header = new Label(title);
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 3;
            box.Add(header);

            var desc = new Label(description);
            desc.style.whiteSpace   = WhiteSpace.Normal;
            desc.style.fontSize     = 10;
            desc.style.color        = new Color(0.65f, 0.65f, 0.65f);
            desc.style.marginBottom = 4;
            box.Add(desc);

            var howToRow = new VisualElement();
            howToRow.style.flexDirection = FlexDirection.Row;
            howToRow.style.alignItems    = Align.FlexStart;
            howToRow.style.marginBottom  = 6;

            var howToLabel = new Label(howToFind);
            howToLabel.style.whiteSpace = WhiteSpace.Normal;
            howToLabel.style.fontSize   = 10;
            howToLabel.style.color      = new Color(0.45f, 0.75f, 1f);
            howToLabel.style.flexGrow   = 1;
            howToLabel.style.flexShrink = 1;
            howToRow.Add(howToLabel);

            var openBtn = new Button(() => Application.OpenURL(link)) { text = "Open ↗" };
            openBtn.style.fontSize     = 10;
            openBtn.style.paddingLeft  = 8;
            openBtn.style.paddingRight = 8;
            openBtn.style.marginLeft   = 6;
            openBtn.style.alignSelf    = Align.Center;
            howToRow.Add(openBtn);

            box.Add(howToRow);
            box.Add(field);

            return box;
        }

        // ── Generic section ───────────────────────────────────────────────────────

        private static VisualElement CreateSection(string title, SerializedObject so, params string[] propertyNames)
        {
            var foldout = new Foldout { text = title, value = true };
            foldout.style.marginBottom = 8;
            foreach (var name in propertyNames)
                foldout.Add(new PropertyField(so.FindProperty(name)));
            return foldout;
        }
    }
}
#endif
