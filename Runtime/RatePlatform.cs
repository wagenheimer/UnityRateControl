namespace Wagenheimer.RateControl
{
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

