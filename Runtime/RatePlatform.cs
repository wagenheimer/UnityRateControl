namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Target distribution platform.
    /// Set this in your <see cref="RateConfig"/> ScriptableObject to control which
    /// store URL or native API is used when the user taps "Rate Now".
    /// </summary>
    public enum RatePlatform
    {
        /// <summary>Google Play Store. Uses In-App Review API when available, falls back to market://.</summary>
        GoogleAndroid,

        /// <summary>Amazon Appstore (amzn:// scheme).</summary>
        AmazonAndroid,

        /// <summary>Apple App Store. Uses SKStoreReviewManager, falls back to itms-apps://.</summary>
        iOS,

        /// <summary>Mac App Store (macappstore:// scheme).</summary>
        MacAppStore,

        /// <summary>Steam reviews page opened in the system browser.</summary>
        Steam,

        /// <summary>Microsoft Store (ms-windows-store:// scheme, UWP only).</summary>
        WindowsStore,

        /// <summary>No built-in opener — implement <see cref="IRateStoreOpener"/> for custom behavior.</summary>
        Custom
    }

    /// <summary>Action the user took when shown the rate dialog.</summary>
    public enum RateUserAction
    {
        /// <summary>User tapped "Rate Now" — open the store page and suppress future prompts.</summary>
        RateNow,

        /// <summary>User tapped "Remind Me Later" — re-queue after the cooldown period.</summary>
        RemindLater,

        /// <summary>User tapped "No Thanks" — suppress all future prompts.</summary>
        Decline
    }
}

