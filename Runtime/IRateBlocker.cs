namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Optional gate that prevents the rate prompt from appearing at inopportune moments,
    /// such as during tutorial sequences, cutscenes, or active gameplay modals.
    ///
    /// Implement this in your game bootstrap and pass it to
    /// <see cref="RateControl.Initialize"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// public class MyRateSetup : MonoBehaviour, IRateBlocker
    /// {
    ///     public bool CanShowRate() =>
    ///         Main.main.BlockedUITickCount &lt;= 0 &amp;&amp;
    ///         GameUtils.ModalDialogs.Count == 0;
    /// }
    /// </code>
    /// </example>
    public interface IRateBlocker
    {
        /// <summary>
        /// Returns <c>true</c> when it is safe to display the rate prompt.
        /// Called every second by the internal poll loop until it returns <c>true</c>.
        /// </summary>
        bool CanShowRate();
    }

    /// <summary>Default: never blocks the prompt.</summary>
    internal sealed class AlwaysAllowBlocker : IRateBlocker
    {
        internal static readonly AlwaysAllowBlocker Instance = new();
        public bool CanShowRate() => true;
    }
}

