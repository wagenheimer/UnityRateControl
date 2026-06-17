using UnityEngine;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Base class for the rate dialog UI.
    ///
    /// <b>Option A — Use the default:</b>
    /// Run <c>PixelCrate → Rate → Create Default Prefab</c> in the Editor menu.
    /// The generated prefab uses <see cref="DefaultRateDialog"/> and works out of the box.
    ///
    /// <b>Option B — Custom per-game:</b>
    /// <list type="number">
    ///   <item>Create a MonoBehaviour that inherits <c>RateDialog</c>.</item>
    ///   <item>Override <see cref="Show"/> and <see cref="Hide"/>.</item>
    ///   <item>Wire your "Rate Now", "Remind Later", and "No Thanks" buttons to
    ///     <see cref="OnRateNow"/>, <see cref="OnRemindLater"/>, and <see cref="OnNoThanks"/>.</item>
    ///   <item>Pass an instance to <see cref="RateControl.Initialize"/>.</item>
    /// </list>
    ///
    /// <b>Option C — Events only (no prefab):</b>
    /// Subscribe to <see cref="RateControl.OnPromptRequested"/> and show your own UI,
    /// then call <see cref="RateControl.UserActed"/> with the appropriate <see cref="RateUserAction"/>.
    /// </summary>
    public abstract class RateDialog : MonoBehaviour
    {
        /// <summary>Called by <see cref="RateControl"/> when the dialog should appear.</summary>
        public abstract void Show();

        /// <summary>Called by <see cref="RateControl"/> when the dialog should be dismissed.</summary>
        public abstract void Hide();

        // ── Button callbacks — wire these to your UI buttons ────────────────────

        /// <summary>Call from your "Rate Now" button. Triggers the store page and suppresses future prompts.</summary>
        public void OnRateNow() => RateControl.UserActed(RateUserAction.RateNow);

        /// <summary>Call from your "Remind Me Later" button. Re-queues after the cooldown period.</summary>
        public void OnRemindLater() => RateControl.UserActed(RateUserAction.RemindLater);

        /// <summary>Call from your "No Thanks" button. Suppresses all future prompts.</summary>
        public void OnNoThanks() => RateControl.UserActed(RateUserAction.Decline);
    }
}

