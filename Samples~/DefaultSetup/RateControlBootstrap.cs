using UnityEngine;
using Wagenheimer.RateControl;

/// <summary>
/// Minimal bootstrap example for Rate Control.
///
/// Attach this to a persistent GameObject in your first scene.
/// Assign your RateConfig asset in the Inspector.
///
/// The dialog prefab is loaded from Resources/ using the path
/// set in RateConfig.DialogResourcePath — no prefab reference needed here.
/// </summary>
public class RateControlBootstrap : MonoBehaviour
{
    [Header("Rate Control")]
    [Tooltip("RateConfig asset created via Tools > Rate Control > Create Rate Config Asset.")]
    [SerializeField] private RateConfig _config;

    private void Awake()
    {
        // Initialize with defaults: dialog loaded from Resources/, no blocker, Application.version.
        RateControl.Initialize(_config);
    }

    // Call this from your game events — level complete, puzzle solved, etc.
    public void OnLevelComplete() => RateControl.LogEvent();

    // Call this once per session start.
    public void OnSessionStart() => RateControl.LogStart();
}
