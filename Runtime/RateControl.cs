using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Wagenheimer.RateControl
{
    /// <summary>
    /// Core rate-prompt service. Initialize once from your game bootstrap and call
    /// <see cref="LogEvent"/> wherever meaningful events occur (e.g. level completions).
    ///
    /// <b>Minimal setup:</b>
    /// <code>
    /// // In your bootstrap MonoBehaviour.Awake():
    /// RateControl.Initialize(myRateConfig, blocker: this, version: this);
    ///
    /// // After each level or meaningful session:
    /// RateControl.LogEvent();
    /// </code>
    ///
    /// <b>PlayerPrefs keys</b> are namespaced using <see cref="RateConfig.StorageKeyPrefix"/>
    /// to avoid collisions between multiple games using this package.
    ///
    /// <b>Editor testing:</b> press <b>F8</b> in Play Mode to force the prompt immediately.
    /// Inspect the "Rate Control" GameObject at runtime to see live state.
    /// </summary>
    [AddComponentMenu("Tools/Rate Control")]
    public sealed class RateControl : MonoBehaviour
    {
        // ── Events ────────────────────────────────────────────────────────────────

        /// <summary>Fired when all thresholds are met and the dialog is ready to show.</summary>
        public static event Action OnPromptRequested;

        /// <summary>Fired after the user taps "Rate Now" (before the store page opens).</summary>
        public static event Action OnUserRated;

        /// <summary>Fired after the user taps "Remind Me Later".</summary>
        public static event Action OnUserRemindedLater;

        /// <summary>Fired after the user taps "No Thanks".</summary>
        public static event Action OnUserDeclined;

        // ── Singleton ─────────────────────────────────────────────────────────────

        /// <summary>The active instance. Null before <see cref="Initialize"/> is called.</summary>
        public static RateControl Instance { get; private set; }

        // ── Inspector state (read-only at runtime) ────────────────────────────────

        [Header("Runtime State (read-only)")]
        [SerializeField] private int _eventCount;
        [SerializeField] private int _startCount;
        [SerializeField] private bool _dontAsk;
        [SerializeField] private int _showCount;
        [SerializeField] private string _lastVersionRated;
        [SerializeField] private string _remindLaterUntil;

        /// <summary>When true, no further prompts will be shown until a new app version is detected.</summary>
        public bool DontAsk        { get => _dontAsk;          set => _dontAsk = value; }
        public string LastVersionRated { get => _lastVersionRated; set => _lastVersionRated = value; }

        // ── Dependencies ──────────────────────────────────────────────────────────

        private RateConfig _config;
        private IRateBlocker _blocker;
        private IRateVersionProvider _versionProvider;
        private IRateStoreOpener _storeOpener;
        private RateDialog _dialog;

        private bool _returningFromEvent;
        private bool _pendingPrompt;

        // ── Initialize ────────────────────────────────────────────────────────────

        /// <summary>
        /// Bootstraps the rate system. Call once from your game bootstrap
        /// (e.g. a persistent MonoBehaviour's <c>Awake</c>).
        /// Safe to call multiple times — subsequent calls are ignored with a warning.
        ///
        /// All parameters except <paramref name="config"/> are optional.
        /// </summary>
        /// <param name="config">Required: ScriptableObject with platform and threshold settings.</param>
        /// <param name="blocker">Prevents the prompt during modals or gameplay. Defaults to always-allow.</param>
        /// <param name="version">Custom version source. Defaults to <c>Application.version</c>.</param>
        /// <param name="opener">Custom store-page opener. Defaults to built-in multi-platform implementation.</param>
        /// <param name="dialog">
        /// Custom dialog component. If null, the system loads <see cref="RateConfig.DialogResourcePath"/>
        /// from Resources. If that prefab is also missing, subscribe to <see cref="OnPromptRequested"/>
        /// to implement a fully custom show flow.
        /// </param>
        public static void Initialize(
            RateConfig config,
            IRateBlocker blocker = null,
            IRateVersionProvider version = null,
            IRateStoreOpener opener = null,
            RateDialog dialog = null)
        {
            if (Instance != null)
            {
                Debug.LogWarning("[RateControl] Initialize() called more than once. Ignoring.");
                return;
            }

            if (config == null)
            {
                Debug.LogError("[RateControl] Initialize() requires a RateConfig asset. Rate system disabled.");
                return;
            }

            var go = new GameObject("Rate Control");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<RateControl>();
            Instance.Boot(config, blocker, version, opener, dialog);

            Debug.Log($"[RateControl] Initialized. EventsPerPrompt={config.EventsPerPrompt} KeyPrefix={config.StorageKeyPrefix}");
        }

        private void Boot(RateConfig config, IRateBlocker blocker, IRateVersionProvider version, IRateStoreOpener opener, RateDialog dialog)
        {
            _config          = config;
            _blocker         = blocker          ?? AlwaysAllowBlocker.Instance;
            _versionProvider = version          ?? ApplicationVersionProvider.Instance;
            _storeOpener     = opener           ?? new DefaultRateStoreOpener(config);

            LoadState();
            ResetDontAskIfNewVersion();
            RecordStart();

            _dialog = dialog ?? LoadDialogFromResources();

            StartCoroutine(PollLoop());
        }

        private RateDialog LoadDialogFromResources()
        {
            var path = _config.DialogResourcePath;
            var prefab = Resources.Load<RateDialog>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[RateControl] No RateDialog prefab found at Resources/{path}. " +
                                 "Create one via Tools > Rate Control > Create Default Prefab, " +
                                 "or subscribe to RateControl.OnPromptRequested for a custom show flow.");
                return null;
            }

            var instance = Instantiate(prefab);
            DontDestroyOnLoad(instance.gameObject);
            instance.gameObject.SetActive(false);
            return instance;
        }

        // ── Public static API ──────────────────────────────────────────────────────

        /// <summary>
        /// Records a meaningful game event (e.g. completing a level).
        /// When the cumulative count reaches a multiple of <see cref="RateConfig.EventsPerPrompt"/>,
        /// the prompt is queued.
        ///
        /// <b>Scene-gated variant:</b> call <see cref="SetReturningFromLevel"/> when leaving a level,
        /// then <c>LogEvent()</c> when the hub/map scene loads — the count only increments in the hub.
        /// </summary>
        public static void LogEvent()
        {
            if (Instance == null) return;

            if (Instance._returningFromEvent && SceneManager.GetActiveScene().name == "map")
            {
                Instance._eventCount++;
                Instance._returningFromEvent = false;
                Instance.EvaluateAndSave();
            }
        }

        /// <summary>
        /// Records an app start and evaluates thresholds.
        /// Called automatically by <see cref="Initialize"/> — do not call manually.
        /// </summary>
        public static void LogStart()
        {
            if (Instance != null) Instance.RecordStart();
        }

        /// <summary>
        /// Marks that the player just left a level and is returning to the hub.
        /// Pair with <see cref="LogEvent"/> called from the hub scene.
        /// </summary>
        public static void SetReturningFromLevel() => LogLevelCompleted();

        /// <summary>
        /// Signals that a significant gameplay unit (level, round, session) just ended.
        /// The event counter increments on the next <see cref="LogEvent"/> call.
        /// </summary>
        public static void LogLevelCompleted()
        {
            if (Instance != null) Instance._returningFromEvent = true;
        }

        /// <summary>
        /// Opens the platform rate page immediately, bypassing all thresholds.
        /// Triggers the In-App Review flow on Android/iOS when available.
        /// </summary>
        public static void RateNow()
        {
            if (Instance == null)
            {
                Debug.LogWarning("[RateControl] RateNow() called before Initialize().");
                return;
            }
            Instance.StartCoroutine(Instance._storeOpener.OpenRatePage());
        }

        /// <summary>Opens the publisher more-games page configured in <see cref="RateConfig.MoreGamesUrl"/>.</summary>
        public static void ShowMoreGames()
        {
            if (Instance == null) return;
            Instance._storeOpener.OpenMoreGames();
        }

        /// <summary>
        /// Reports the user action from the dialog. Called by <see cref="RateDialog"/> button methods.
        /// Call this directly when using the events-only flow (no dialog prefab).
        /// </summary>
        public static void UserActed(RateUserAction action)
        {
            if (Instance != null) Instance.HandleAction(action);
        }

        /// <summary>
        /// Resets all saved state. Useful for QA and testing.
        /// Run via <c>Tools → Rate Control → Reset Saved State</c> in the Editor menu.
        /// </summary>
        public static void ResetAll()
        {
            if (Instance != null)
            {
                PlayerPrefs.DeleteKey(Instance.Key("EventCount"));
                PlayerPrefs.DeleteKey(Instance.Key("StartCount"));
                PlayerPrefs.DeleteKey(Instance.Key("DontAsk"));
                PlayerPrefs.DeleteKey(Instance.Key("ShowCount"));
                PlayerPrefs.DeleteKey(Instance.Key("LastVersion"));
                PlayerPrefs.DeleteKey(Instance.Key("RemindLaterUntil"));
                Instance.LoadState();
            }
            Debug.Log("[RateControl] All saved state cleared.");
        }

        /// <summary>Persists current state to PlayerPrefs.</summary>
        public void Save() => SaveState();

        // ── Key helpers ───────────────────────────────────────────────────────────

        private string Key(string suffix) => $"{_config.StorageKeyPrefix}.{suffix}";

        // ── Internal logic ────────────────────────────────────────────────────────

        private IEnumerator PollLoop()
        {
            var wait = new WaitForSeconds(1f);
            while (true)
            {
                yield return wait;

                if (!_pendingPrompt)            continue;
                if (!_blocker.CanShowRate())    continue;
                if (_config.BlacklistedScenes.Contains(SceneManager.GetActiveScene().name)) continue;

                _pendingPrompt = false;
                ShowPrompt();
            }
        }

        private void ShowPrompt()
        {
            Debug.Log("[RateControl] Showing rate prompt.");
            OnPromptRequested?.Invoke();
            _dialog?.Show();
        }

        private void HandleAction(RateUserAction action)
        {
            switch (action)
            {
                case RateUserAction.RateNow:
                    _dontAsk = true;
                    _lastVersionRated = _versionProvider.GetCurrentVersion();
                    SaveState();
                    StartCoroutine(_storeOpener.OpenRatePage());
                    OnUserRated?.Invoke();
                    _dialog?.Hide();
                    break;

                case RateUserAction.RemindLater:
                    _remindLaterUntil = DateTime.UtcNow
                        .AddDays(_config.RemindLaterCooldownDays)
                        .ToString("o");
                    SaveState();
                    OnUserRemindedLater?.Invoke();
                    _dialog?.Hide();
                    break;

                case RateUserAction.Decline:
                    _dontAsk = true;
                    SaveState();
                    OnUserDeclined?.Invoke();
                    _dialog?.Hide();
                    break;
            }
        }

        private void RecordStart()
        {
            _startCount++;
            EvaluateAndSave();
        }

        private void EvaluateAndSave()
        {
            if (!_dontAsk && !IsInRemindCooldown())
            {
                if (_eventCount > 0 && _eventCount % _config.EventsPerPrompt == 0)
                {
                    _pendingPrompt = true;
                }
                else if (_startCount >= _config.StartsBeforeFirstPrompt && _showCount == 0)
                {
                    _startCount = 0;
                    _showCount++;
                    _pendingPrompt = true;
                }
                else if (_startCount >= _config.StartsBeforeSubsequentPrompts && _showCount > 0)
                {
                    _startCount = 0;
                    _showCount++;
                    _pendingPrompt = true;
                }
            }
            SaveState();
        }

        private bool IsInRemindCooldown()
        {
            if (string.IsNullOrEmpty(_remindLaterUntil)) return false;
            return DateTime.TryParse(_remindLaterUntil, null,
                       System.Globalization.DateTimeStyles.RoundtripKind,
                       out var until)
                   && DateTime.UtcNow < until;
        }

        private void ResetDontAskIfNewVersion()
        {
            var current = _versionProvider.GetCurrentVersion();
            if (!string.IsNullOrEmpty(_lastVersionRated) && _lastVersionRated != current)
            {
                Debug.Log($"[RateControl] New version detected ({_lastVersionRated} → {current}). Resetting DontAsk.");
                _dontAsk = false;
            }
        }

        private void LoadState()
        {
            _eventCount       = PlayerPrefs.GetInt(Key("EventCount"),  0);
            _startCount       = PlayerPrefs.GetInt(Key("StartCount"),  0);
            _dontAsk          = PlayerPrefs.GetInt(Key("DontAsk"),     0) == 1;
            _showCount        = PlayerPrefs.GetInt(Key("ShowCount"),   0);
            _lastVersionRated = PlayerPrefs.GetString(Key("LastVersion"),      "");
            _remindLaterUntil = PlayerPrefs.GetString(Key("RemindLaterUntil"), "");
        }

        private void SaveState()
        {
            PlayerPrefs.SetInt(Key("EventCount"),  _eventCount);
            PlayerPrefs.SetInt(Key("StartCount"),  _startCount);
            PlayerPrefs.SetInt(Key("DontAsk"),     _dontAsk ? 1 : 0);
            PlayerPrefs.SetInt(Key("ShowCount"),   _showCount);
            PlayerPrefs.SetString(Key("LastVersion"),      _lastVersionRated ?? "");
            PlayerPrefs.SetString(Key("RemindLaterUntil"), _remindLaterUntil ?? "");
        }

        // ── Editor helpers ────────────────────────────────────────────────────────

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
            {
                Debug.Log("[RateControl] F8: forcing rate prompt (bypasses blocker, blacklist, and thresholds).");
                ShowPrompt();
            }
        }
#endif
    }
}
