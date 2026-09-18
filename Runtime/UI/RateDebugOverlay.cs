using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Wagenheimer.RateControl;

namespace Wagenheimer.RateControl.UI
{
    /// <summary>
    /// In-game runtime debug overlay for inspecting and testing RateControl.
    /// Provides real-time threshold counters, blocker diagnostics, cooldown status,
    /// and simulation triggers in Unity Editor and Development Builds.
    /// </summary>
    [AddComponentMenu("Tools/Wagenheimer/Rate Control/Rate Debug Overlay")]
    [DisallowMultipleComponent]
    public class RateDebugOverlay : MonoBehaviour
    {
        #region Settings

        [Header("Runtime Access")]
        [Tooltip("Hot key to toggle debug panel visibility in game.")]
        public KeyCode toggleKey = KeyCode.F9;

        [Tooltip("Whether to draw a small floating 'RATE DBG' button on screen.")]
        public bool showFloatingButton = true;

        [Tooltip("Allow overlay to run even in non-development / release builds. Strongly recommended FALSE for production.")]
        public bool enableInReleaseBuilds = false;

        #endregion

        #region Private Fields

        private bool _isOpen;
        private Rect _windowRect = new Rect(20, 20, 520, 580);
        private Vector2 _scrollPos;
        private string _statusLog = "Ready.";
        private readonly List<string> _eventHistory = new List<string>();
        private const int MaxHistoryCount = 8;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!Debug.isDebugBuild && !Application.isEditor && !enableInReleaseBuilds)
            {
                Destroy(this);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            RateControl.OnPromptRequested += HandlePromptRequested;
            RateControl.OnUserRated += HandleUserRated;
            RateControl.OnUserRemindedLater += HandleUserRemindedLater;
            RateControl.OnUserDeclined += HandleUserDeclined;
        }

        private void OnDisable()
        {
            RateControl.OnPromptRequested -= HandlePromptRequested;
            RateControl.OnUserRated -= HandleUserRated;
            RateControl.OnUserRemindedLater -= HandleUserRemindedLater;
            RateControl.OnUserDeclined -= HandleUserDeclined;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                _isOpen = !_isOpen;
            }
        }

        private void OnGUI()
        {
            if (!Debug.isDebugBuild && !Application.isEditor && !enableInReleaseBuilds)
                return;

            GUI.depth = -9999;

            // Floating button on bottom right, so it never collides with IAP DBG (bottom left)
            if (showFloatingButton && !_isOpen)
            {
                var buttonRect = new Rect(Screen.width - 105, Screen.height - 40, 95, 30);
                if (GUI.Button(buttonRect, "RATE DBG"))
                {
                    _isOpen = true;
                }
            }

            if (_isOpen)
            {
                _windowRect = GUI.Window(888124, _windowRect, DrawDebugWindow, "Rate Control - In-Game Debug Panel");
            }
        }

        #endregion

        #region Event Callbacks

        private void HandlePromptRequested() => LogEvent("Event: OnPromptRequested fired.");
        private void HandleUserRated() => LogEvent("Event: OnUserRated fired (Rate Now clicked).");
        private void HandleUserRemindedLater() => LogEvent("Event: OnUserRemindedLater fired.");
        private void HandleUserDeclined() => LogEvent("Event: OnUserDeclined fired (No Thanks clicked).");

        private void LogEvent(string msg)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            _statusLog = entry;
            _eventHistory.Insert(0, entry);
            if (_eventHistory.Count > MaxHistoryCount)
                _eventHistory.RemoveAt(_eventHistory.Count - 1);
        }

        #endregion

        #region GUI Layout

        private void DrawDebugWindow(int windowId)
        {
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 60, 25));

            if (GUI.Button(new Rect(_windowRect.width - 55, 4, 50, 20), "Close"))
            {
                _isOpen = false;
                return;
            }

            GUILayout.Space(25);

            var rc = RateControl.Instance;
            if (rc == null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<color=red><b>RateControl instance not found in scene!</b></color>");
                GUILayout.Label("Ensure RateControl.Initialize(config) is called at bootstrap.");
                GUILayout.EndVertical();
                return;
            }

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            DrawStatusCard(rc);
            GUILayout.Space(6);

            DrawCountersCard(rc);
            GUILayout.Space(6);

            DrawActionsCard(rc);
            GUILayout.Space(6);

            DrawStoreAndPlatformCard(rc);
            GUILayout.Space(6);

            DrawEventHistoryCard();

            GUILayout.EndScrollView();
        }

        private void DrawStatusCard(RateControl rc)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Live Status & Diagnostics</b>");

            var cfg = rc.Config;
            string activeScene = SceneManager.GetActiveScene().name;
            bool isBlacklisted = rc.IsSceneBlacklisted;
            bool blockerAllows = rc.BlockerAllowsPrompt;
            bool inCooldown = rc.InRemindCooldown;

            // System status
            string readinessColor = (!rc.DontAsk && !inCooldown && !isBlacklisted && blockerAllows) ? "lime" : "yellow";
            string readinessText = rc.DontAsk ? "Disabled (DontAsk = true)" :
                                   inCooldown ? "Suppressed (Cooldown active)" :
                                   isBlacklisted ? "Suppressed (Scene Blacklisted)" :
                                   !blockerAllows ? "Blocked (IRateBlocker)" : "Eligible / Ready";

            GUILayout.Label($"<b>Status:</b> <color={readinessColor}>{readinessText}</color>");
            GUILayout.Label($"<b>DontAsk (Rated/Declined):</b> {(rc.DontAsk ? "<color=orange>True</color>" : "<color=lime>False</color>")} | <b>Last Version Rated:</b> {(string.IsNullOrEmpty(rc.LastVersionRated) ? "(None)" : rc.LastVersionRated)}");
            GUILayout.Label($"<b>Active Scene:</b> \"{activeScene}\" {(isBlacklisted ? "<color=red>[BLACKLISTED]</color>" : "<color=lime>[Allowed]</color>")}");
            GUILayout.Label($"<b>Blocker (IRateBlocker):</b> {(blockerAllows ? "<color=lime>CanShowRate = TRUE</color>" : "<color=red>CanShowRate = FALSE</color>")}");

            // Cooldown string
            if (inCooldown && !string.IsNullOrEmpty(rc.RemindLaterUntil))
            {
                if (DateTime.TryParse(rc.RemindLaterUntil, null, System.Globalization.DateTimeStyles.RoundtripKind, out var until))
                {
                    TimeSpan remaining = until - DateTime.UtcNow;
                    GUILayout.Label($"<b>Remind Cooldown:</b> <color=yellow>{remaining.Days}d {remaining.Hours}h {remaining.Minutes}m {remaining.Seconds}s remaining</color>");
                }
                else
                {
                    GUILayout.Label($"<b>Remind Cooldown:</b> <color=yellow>{rc.RemindLaterUntil}</color>");
                }
            }
            else
            {
                GUILayout.Label("<b>Remind Cooldown:</b> <color=lime>None (Inactive)</color>");
            }

            GUILayout.EndVertical();
        }

        private void DrawCountersCard(RateControl rc)
        {
            var cfg = rc.Config;
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Counters & Thresholds</b>");

            int eventsPerPrompt = cfg != null ? cfg.EventsPerPrompt : 10;
            int startsFirst = cfg != null ? cfg.StartsBeforeFirstPrompt : 3;
            int startsSubsequent = cfg != null ? cfg.StartsBeforeSubsequentPrompts : 8;
            int requiredStarts = (rc.ShowCount == 0) ? startsFirst : startsSubsequent;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>Events:</b> {rc.EventCount} / {eventsPerPrompt}");
            GUILayout.Label($"<b>Starts:</b> {rc.StartCount} / {requiredStarts}");
            GUILayout.Label($"<b>Times Shown:</b> {rc.ShowCount}");
            GUILayout.EndHorizontal();

            GUILayout.Label($"<b>Pending Prompt:</b> {(rc.IsPendingPrompt ? "<color=yellow>YES (Waiting in poll loop)</color>" : "<color=lime>NO</color>")}");

            GUILayout.EndVertical();
        }

        private void DrawActionsCard(RateControl rc)
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Simulation & QA Triggers</b>");

            // Row 1: Increments
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Log Event (+1)"))
            {
                RateControl.LogEvent();
                LogEvent($"LogEvent() called. Current count: {rc.EventCount}");
            }

            if (GUILayout.Button("Log Start (+1)"))
            {
                RateControl.LogStart();
                LogEvent($"LogStart() called. Current count: {rc.StartCount}");
            }

            if (GUILayout.Button("Toggle Pending"))
            {
                rc.SetPendingPrompt(!rc.IsPendingPrompt);
                LogEvent($"PendingPrompt toggled to: {rc.IsPendingPrompt}");
            }
            GUILayout.EndHorizontal();

            // Row 2: Prompt Show and Cooldown
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Show Prompt"))
            {
                rc.ForceShowPrompt();
                LogEvent("ForceShowPrompt() called.");
            }

            if (GUILayout.Button("Clear Remind Cooldown"))
            {
                rc.ClearRemindCooldown();
                LogEvent("ClearRemindCooldown() called.");
            }

            if (GUILayout.Button("Toggle DontAsk"))
            {
                rc.DontAsk = !rc.DontAsk;
                rc.Save();
                LogEvent($"DontAsk toggled to: {rc.DontAsk}");
            }
            GUILayout.EndHorizontal();

            // Row 3: Action simulations
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Simulate: Rate Now"))
            {
                RateControl.UserActed(RateUserAction.RateNow);
                LogEvent("UserActed(RateNow) simulated.");
            }

            if (GUILayout.Button("Simulate: Remind Later"))
            {
                RateControl.UserActed(RateUserAction.RemindLater);
                LogEvent("UserActed(RemindLater) simulated.");
            }

            if (GUILayout.Button("Simulate: No Thanks"))
            {
                RateControl.UserActed(RateUserAction.Decline);
                LogEvent("UserActed(Decline) simulated.");
            }
            GUILayout.EndHorizontal();

            // Row 4: Reset
            GUILayout.Space(2);
            if (GUILayout.Button("Reset All State (PlayerPrefs)"))
            {
                RateControl.ResetAll();
                LogEvent("RateControl.ResetAll() called. State wiped.");
            }

            GUILayout.EndVertical();
        }

        private void DrawStoreAndPlatformCard(RateControl rc)
        {
            var cfg = rc.Config;
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Store & Platform Information</b>");

            GUILayout.Label($"<b>Platform:</b> {Application.platform} | <b>Installer:</b> \"{Application.installerName}\"");
            GUILayout.Label($"<b>App Identifier:</b> {Application.identifier} | <b>App Version:</b> {Application.version}");

            if (cfg != null)
            {
                GUILayout.Label($"<b>Storage Prefix:</b> {cfg.StorageKeyPrefix}");
                GUILayout.Label($"<b>Android Package:</b> {cfg.ResolvedAndroidId}");
                GUILayout.Label($"<b>iOS App ID:</b> {(string.IsNullOrEmpty(cfg.iOSAppId) ? "(None)" : cfg.iOSAppId)} | <b>Steam App ID:</b> {(string.IsNullOrEmpty(cfg.SteamAppId) ? "(None)" : cfg.SteamAppId)}");
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test RateNow()"))
            {
                RateControl.RateNow();
                LogEvent("RateControl.RateNow() called.");
            }

            if (GUILayout.Button("Test ShowMoreGames()"))
            {
                RateControl.ShowMoreGames();
                LogEvent("RateControl.ShowMoreGames() called.");
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawEventHistoryCard()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label($"<b>Event History (Last {_eventHistory.Count}):</b>");

            if (_eventHistory.Count == 0)
            {
                GUILayout.Label("<color=grey>(No events logged yet)</color>");
            }
            else
            {
                foreach (var item in _eventHistory)
                {
                    GUILayout.Label(item);
                }
            }

            GUILayout.EndVertical();
        }

        #endregion

        #region Factory Method

        /// <summary>
        /// Spawns or finds the RateDebugOverlay GameObject in the active scene.
        /// </summary>
        public static RateDebugOverlay CreateOverlay()
        {
            var existing = FindObjectOfType<RateDebugOverlay>();
            if (existing != null)
                return existing;

            var go = new GameObject("RateDebugOverlay", typeof(RateDebugOverlay));
            return go.GetComponent<RateDebugOverlay>();
        }

        #endregion
    }
}

/// <summary>
/// Global alias for convenience in inspector or scripts without namespace import.
/// </summary>
public class RateDebugOverlay : Wagenheimer.RateControl.UI.RateDebugOverlay
{
}
