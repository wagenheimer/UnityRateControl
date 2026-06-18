using System.Collections.Generic;
using UnityEngine;

namespace Wagenheimer.RateControl
{
    [CreateAssetMenu(menuName = "Rate Control/Rate Config", fileName = "RateConfig", order = 0)]
    public sealed class RateConfig : ScriptableObject
    {
        // Store IDs

        [Header("Store IDs")]
        [Tooltip(
            "Android package name for Google Play and Amazon links.\n" +
            "Example: com.mystudio.mygame\n\n" +
            "Leave empty to use Application.identifier at runtime (recommended).")]
        public string AndroidPackageId = "";

        [Tooltip(
            "Numeric App Store ID for iOS and Mac App Store builds.\n" +
            "Found in App Store Connect > App Information > Apple ID.\n" +
            "Example: 123456789\n\n" +
            "Used as fallback itms-apps:// URL when SKStoreReviewManager is unavailable.")]
        public string iOSAppId = "";

        [Tooltip(
            "Numeric App Store ID for the Mac App Store.\n" +
            "Same as iOSAppId for universal purchases.\n" +
            "Used to build the macappstore:// review URL.")]
        public string MacAppStoreId = "";

        [Tooltip(
            "Steam App ID (numbers only). Found in the Steamworks dashboard URL.\n" +
            "Example: 123456\n\n" +
            "Opens: https://store.steampowered.com/app/{SteamAppId}/reviews/")]
        public string SteamAppId = "";

        // More Games

        [Header("More Games - opened by RateControl.ShowMoreGames()")]

        [Tooltip(
            "Your publisher/developer name exactly as it appears on Google Play.\n" +
            "Auto-builds: market://search?q=pub:{name}\n" +
            "Where to find: play.google.com/console → Setup → App info → Developer name\n\n" +
            "NOTE: Amazon Appstore URL is auto-generated from Application.identifier — no field needed.")]
        public string MoreGamesGoogleDeveloperName = "";

        [Tooltip(
            "Numeric Apple Developer ID (NOT the bundle ID). Example: 964191738\n" +
            "Auto-builds iOS URL:  https://apps.apple.com/developer/id{id}\n" +
            "Auto-builds Mac URL:  https://apps.apple.com/mac/developer/id{id}\n\n" +
            "FASTEST: apps.apple.com → search your app → click developer name → copy the number after /id in the URL.\n" +
            "ALTERNATIVE: appstoreconnect.apple.com → click your name (top-right) → View My Profile → copy Developer ID.")]
        public string MoreGamesAppleDeveloperId = "";

        [Tooltip(
            "Publisher display name as registered in Microsoft Partner Center.\n" +
            "Auto-builds: ms-windows-store://search/?query={name}\n" +
            "Where to find: partner.microsoft.com/dashboard → [app] → Product management → Product identity → Publisher display name")]
        public string MoreGamesWindowsPublisherName = "";

        [Tooltip(
            "Developer slug from your Steamworks developer page URL.\n" +
            "Example: for store.steampowered.com/developer/mystudio → type mystudio\n" +
            "Auto-builds: https://store.steampowered.com/developer/{slug}\n" +
            "Where to find: open your Steamworks developer page and copy the slug from the URL.")]
        public string MoreGamesSteamDeveloperSlug = "";

        [Tooltip(
            "Fallback URL when no platform-specific field is configured above.\n" +
            "Also used for Custom, sideloaded, or unknown distribution channels.\n" +
            "Example: https://mystudio.com/games\n" +
            "Leave empty to disable ShowMoreGames() on unsupported platforms.")]
        public string MoreGamesUrl = "";

        // Trigger Thresholds

        [Header("Trigger Thresholds")]
        [Tooltip(
            "Calls to RateControl.LogEvent() needed to queue the rate prompt.\n\n" +
            "Call LogEvent() at meaningful moments: level completions, puzzle solves, match wins.\n" +
            "Recommended: 5-15 depending on how often milestones occur.")]
        [Min(1)] public int EventsPerPrompt = 10;

        [Tooltip(
            "Minimum app launches before the very first prompt can appear.\n\n" +
            "Ensures players have had enough sessions to form an opinion before being asked.\n" +
            "Recommended: 3-5.")]
        [Min(1)] public int StartsBeforeFirstPrompt = 3;

        [Tooltip(
            "Minimum app launches between each subsequent prompt after the first\n" +
            "(e.g. after the player taps Remind Me Later).\n\n" +
            "Recommended: 7-14 to avoid feeling intrusive.")]
        [Min(1)] public int StartsBeforeSubsequentPrompts = 8;

        [Tooltip(
            "Days before re-showing the prompt after the player taps Remind Me Later.\n\n" +
            "Based on real calendar days from when the reminder was set.\n" +
            "Set to 0 for no day cooldown - re-queues on the next eligible session.")]
        [Min(0)] public int RemindLaterCooldownDays = 3;

        // Scene Filter

        [Header("Scene Filter")]
        [Tooltip(
            "Scene names where the prompt is always suppressed, regardless of thresholds.\n\n" +
            "Add loading screens, battle scenes, or cutscenes where an interruption breaks flow.\n" +
            "Use the exact name from Build Settings (no path, no .unity extension).")]
        public List<string> BlacklistedScenes = new();

        // Storage

        [Header("Storage")]
        [Tooltip(
            "Prefix for every PlayerPrefs key used by this package.\n\n" +
            "IMPORTANT: use a unique value per game. Two games sharing the same prefix on the\n" +
            "same device will overwrite each other state (event count, do-not-ask flag, etc.).\n\n" +
            "Recommended format: Studio.GameName.Rate\n" +
            "Example: PixelCrate.NordStorm.Rate")]
        public string StorageKeyPrefix = "RateControl";

        // UI

        [Header("UI")]
        [Tooltip(
            "Path inside any Resources/ folder where the rate dialog prefab lives,\n" +
            "WITHOUT the .prefab extension.\n\n" +
            "The system calls Resources.Load<RateDialog>(DialogResourcePath) at runtime.\n" +
            "The prefab must have a component that inherits from RateDialog.\n\n" +
            "Example: prefab at Assets/UI/Resources/MyPopup.prefab -> set to MyPopup.\n\n" +
            "Tip: pass the dialog instance directly to RateControl.Initialize() to skip this.")]
        public string DialogResourcePath = "RateDialog";

        // Internal helpers

        internal string ResolvedAndroidId =>
            string.IsNullOrEmpty(AndroidPackageId) ? Application.identifier : AndroidPackageId;

        internal string ResolvedSteamUrl =>
            $"https://store.steampowered.com/app/{SteamAppId}/reviews/";

        private static string FirstNonEmpty(string a, string b) =>
            !string.IsNullOrEmpty(a) ? a : b;
    }
}
