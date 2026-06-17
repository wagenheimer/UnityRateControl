using System.Collections.Generic;
using UnityEngine;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// ScriptableObject that holds all configuration for the rate prompt system.
    ///
    /// <b>Quick setup:</b>
    /// <list type="number">
    ///   <item>Right-click in the Project window → Create → Rate Control → Rate Config.</item>
    ///   <item>Set <see cref="Platform"/> to match your target store.</item>
    ///   <item>Fill in the store ID for your platform.</item>
    ///   <item>Assign the asset to your bootstrap component that calls <see cref="RateControl.Initialize"/>.</item>
    /// </list>
    /// </summary>
    [CreateAssetMenu(
        menuName = "Rate Control/Rate Config",
        fileName = "RateConfig",
        order = 0)]
    public sealed class RateConfig : ScriptableObject
    {
        // ── Platform ─────────────────────────────────────────────────────────────

        [Header("Platform")]
        [Tooltip("Which store the Rate Now button targets.")]
        public RatePlatform Platform = RatePlatform.GoogleAndroid;

        // ── Store IDs ─────────────────────────────────────────────────────────────

        [Header("Store IDs")]
        [Tooltip("Android package name. Leave empty to use Application.identifier at runtime.")]
        public string AndroidPackageId = "";

        [Tooltip("Numeric iOS App Store ID (e.g. 123456789).")]
        public string iOSAppId = "";

        [Tooltip("Numeric Mac App Store ID.")]
        public string MacAppStoreId = "";

        [Tooltip("Steam App ID (numeric). Used to build the review URL.")]
        public string SteamAppId = "";

        [Tooltip("URL for the More Games page. Leave empty to disable.")]
        public string MoreGamesUrl = "";

        // ── Trigger thresholds ────────────────────────────────────────────────────

        [Header("Trigger Thresholds")]
        [Tooltip("Queue the prompt after every N calls to RateControl.LogEvent().")]
        [Min(1)] public int EventsPerPrompt = 10;

        [Tooltip("Number of app launches before the very first prompt.")]
        [Min(1)] public int StartsBeforeFirstPrompt = 3;

        [Tooltip("Number of app launches between subsequent prompts.")]
        [Min(1)] public int StartsBeforeSubsequentPrompts = 8;

        [Tooltip("Days before re-prompting after Remind Me Later. Set 0 for no cooldown.")]
        [Min(0)] public int RemindLaterCooldownDays = 3;

        // ── Scene filter ──────────────────────────────────────────────────────────

        [Header("Scene Filter")]
        [Tooltip("Scene names where the rate prompt is suppressed (e.g. loading screens, battle scenes).")]
        public List<string> BlacklistedScenes = new();

        // ── Storage ───────────────────────────────────────────────────────────────

        [Header("Storage")]
        [Tooltip("Prefix for all PlayerPrefs keys. Change per game to avoid key collisions between titles using this package.")]
        public string StorageKeyPrefix = "RateControl";

        // ── UI ────────────────────────────────────────────────────────────────────

        [Header("UI")]
        [Tooltip("Path inside Resources/ for the rate dialog prefab. Example: 'RateDialog' loads Resources/RateDialog.prefab")]
        public string DialogResourcePath = "RateDialog";

        // ── Internal helpers ──────────────────────────────────────────────────────

        internal string ResolvedAndroidId =>
            string.IsNullOrEmpty(AndroidPackageId) ? Application.identifier : AndroidPackageId;

        internal string ResolvedSteamUrl =>
            $"https://store.steampowered.com/app/{SteamAppId}/reviews/";
    }
}
