using UnityEngine;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Optional. Implement on the same object passed as <c>blocker</c> to <see cref="RateControl.Initialize"/>
    /// to make the rate dialog spawn under the game's own canvas instead of a standalone root object.
    /// Return the persistent front canvas (preferred) or the main UI canvas; return null to use the default.
    /// </summary>
    public interface IRateCanvasProvider
    {
        Transform GetDialogParent();
    }
}
