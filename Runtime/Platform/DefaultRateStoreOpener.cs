using System.Collections;
using UnityEngine;

#if RATECONTROL_GOOGLE_PLAY_REVIEW && UNITY_ANDROID
using Google.Play.Review;
#endif

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Built-in cross-platform store opener.
    ///
    /// <list type="bullet">
    ///   <item><b>Android:</b> Google Play In-App Review flow (requires <c>com.google.play.review</c>).
    ///     Falls back to <c>market://</c> if unavailable or the flow fails.</item>
    ///   <item><b>iOS:</b> <c>SKStoreReviewManager</c> native dialog, falls back to <c>itms-apps://</c>.</item>
    ///   <item><b>All others:</b> <c>Application.OpenURL</c> with platform-specific scheme.</item>
    /// </list>
    ///
    /// The asmdef <c>versionDefines</c> entry automatically defines
    /// <c>RATECONTROL_GOOGLE_PLAY_REVIEW</c> when <c>com.google.play.review</c> is in the
    /// project — no manual scripting define is required.
    /// </summary>
    internal sealed class DefaultRateStoreOpener : IRateStoreOpener
    {
        private readonly RateConfig _config;

        internal DefaultRateStoreOpener(RateConfig config) => _config = config;

        public IEnumerator OpenRatePage()
        {
            Debug.Log($"[RateControl] Opening rate page. Platform={_config.Platform} Id={_config.ResolvedAndroidId}");

            switch (_config.Platform)
            {
                case RatePlatform.GoogleAndroid: yield return OpenAndroid(); break;
                case RatePlatform.AmazonAndroid:
                    Application.OpenURL($"amzn://apps/android?p={_config.ResolvedAndroidId}");
                    break;
                case RatePlatform.iOS:
                    OpeniOS();
                    break;
                case RatePlatform.MacAppStore:
                    Application.OpenURL($"macappstore://apps.apple.com/app/id{_config.MacAppStoreId}?action=write-review");
                    break;
                case RatePlatform.Steam:
                    Debug.Log($"[RateControl] Steam URL: {_config.ResolvedSteamUrl}");
                    Application.OpenURL(_config.ResolvedSteamUrl);
                    break;
                case RatePlatform.WindowsStore:
                    OpenWindowsStore();
                    break;
                default:
                    Debug.LogWarning($"[RateControl] Platform '{_config.Platform}' has no built-in opener. Provide IRateStoreOpener.");
                    break;
            }
        }

        public void OpenMoreGames()
        {
            var url = BuildMoreGamesUrl();
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogWarning("[RateControl] ShowMoreGames() — no URL configured in RateConfig for this platform.");
                return;
            }
            Debug.Log($"[RateControl] Opening more games: {url}");
            Application.OpenURL(url);
        }

        private string BuildMoreGamesUrl()
        {
#if UNITY_ANDROID
            // Amazon: auto-generated from app's package identifier — no config needed.
            if (Application.installerName.Contains("com.amazon.venezia"))
                return $"amzn://apps/android?p={Application.identifier}&showAll=1";

            if (!string.IsNullOrEmpty(_config.MoreGamesGoogleDeveloperName))
                return $"market://search?q=pub:{_config.MoreGamesGoogleDeveloperName}";

            return _config.MoreGamesUrl;

#elif UNITY_IOS
            if (!string.IsNullOrEmpty(_config.MoreGamesAppleDeveloperId))
                return $"https://apps.apple.com/developer/id{_config.MoreGamesAppleDeveloperId}";
            return _config.MoreGamesUrl;

#elif UNITY_STANDALONE_OSX
            if (!string.IsNullOrEmpty(_config.MoreGamesAppleDeveloperId))
                return $"https://apps.apple.com/mac/developer/id{_config.MoreGamesAppleDeveloperId}";
            return _config.MoreGamesUrl;

#elif UNITY_WSA
            if (!string.IsNullOrEmpty(_config.MoreGamesWindowsPublisherName))
                return $"ms-windows-store://search/?query={_config.MoreGamesWindowsPublisherName}";
            return _config.MoreGamesUrl;

#elif UNITY_STANDALONE_WIN
            if (!string.IsNullOrEmpty(_config.MoreGamesSteamDeveloperSlug))
                return $"https://store.steampowered.com/developer/{_config.MoreGamesSteamDeveloperSlug}";
            return _config.MoreGamesUrl;

#else
            return _config.MoreGamesUrl;
#endif
        }

        // ── Android ───────────────────────────────────────────────────────────────

        private IEnumerator OpenAndroid()
        {
#if RATECONTROL_GOOGLE_PLAY_REVIEW && UNITY_ANDROID
            yield return TryInAppReview();
#else
            var url = $"market://details?id={_config.ResolvedAndroidId}";
            Debug.Log($"[RateControl] market URL (no In-App Review package): {url}");
            Application.OpenURL(url);
            yield break;
#endif
        }

#if RATECONTROL_GOOGLE_PLAY_REVIEW && UNITY_ANDROID
        private IEnumerator TryInAppReview()
        {
            Debug.Log("[RateControl] Requesting Google Play In-App Review flow.");
            var manager = new ReviewManager();
            var requestOp = manager.RequestReviewFlow();
            yield return requestOp;

            if (requestOp.Error != ReviewErrorCode.NoError)
            {
                Debug.LogWarning($"[RateControl] In-App Review request failed ({requestOp.Error}). Falling back to market://");
                Application.OpenURL($"market://details?id={_config.ResolvedAndroidId}");
                yield break;
            }

            var reviewInfo = requestOp.GetResult();
            var launchOp = manager.LaunchReviewFlow(reviewInfo);
            yield return launchOp;

            // Google does not expose whether the user submitted a review — flow completing is the best signal.
            if (launchOp.Error != ReviewErrorCode.NoError)
                Debug.LogWarning($"[RateControl] In-App Review launch warning: {launchOp.Error}");
            else
                Debug.Log("[RateControl] In-App Review flow completed.");
        }
#endif

        // ── iOS ───────────────────────────────────────────────────────────────────

        private void OpeniOS()
        {
#if UNITY_IOS
            Debug.Log("[RateControl] Requesting iOS store review via SKStoreReviewManager.");
            if (!UnityEngine.iOS.Device.RequestStoreReview())
            {
                var url = $"itms-apps://itunes.apple.com/app/id{_config.iOSAppId}";
                Debug.Log($"[RateControl] iOS fallback URL: {url}");
                Application.OpenURL(url);
            }
#endif
        }

        // ── Windows Store ──────────────────────────────────────────────────────────

        private static void OpenWindowsStore()
        {
#if NETFX_CORE
            var productId = Windows.ApplicationModel.Package.Current.Id.FamilyName;
            Application.OpenURL($"ms-windows-store://pdp/?ProductId={productId}");
#endif
        }
    }
}

