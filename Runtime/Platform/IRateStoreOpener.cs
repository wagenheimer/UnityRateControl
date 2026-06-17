using System.Collections;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Abstraction over the platform rate-page and "more games" actions.
    ///
    /// The default implementation (<see cref="DefaultRateStoreOpener"/>) handles all
    /// platforms listed in <see cref="RatePlatform"/>. Provide your own only when you
    /// need custom behavior (e.g. a proprietary in-app review SDK).
    /// </summary>
    public interface IRateStoreOpener
    {
        /// <summary>
        /// Opens the platform rating page.
        /// Coroutine to support async APIs such as Google Play In-App Review.
        /// </summary>
        IEnumerator OpenRatePage();

        /// <summary>
        /// Opens the publisher more-games page.
        /// Uses <see cref="RateConfig.MoreGamesUrl"/>; does nothing if that field is empty.
        /// </summary>
        void OpenMoreGames();
    }
}

