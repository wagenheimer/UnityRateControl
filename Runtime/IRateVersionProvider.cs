using UnityEngine;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Provides the current game version string.
    /// Used to reset the "don't ask again" flag when the player upgrades to a new version,
    /// giving them a fresh chance to rate the improved game.
    ///
    /// Inject a custom implementation when your versioning differs from
    /// <c>Application.version</c> (e.g. a separate marketing version string).
    /// </summary>
    public interface IRateVersionProvider
    {
        /// <summary>Returns the current game version as a comparable string.</summary>
        string GetCurrentVersion();
    }

    /// <summary>Default: reads <see cref="Application.version"/>.</summary>
    internal sealed class ApplicationVersionProvider : IRateVersionProvider
    {
        internal static readonly ApplicationVersionProvider Instance = new();
        public string GetCurrentVersion() => Application.version;
    }
}

