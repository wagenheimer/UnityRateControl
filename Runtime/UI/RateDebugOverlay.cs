using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Wagenheimer.RateControl;

namespace Wagenheimer.RateControl.UI
{
    /// <summary>
    /// In-game runtime UI Toolkit debug overlay for inspecting and testing RateControl.
    /// Provides real-time threshold counters, visual progress bars, blocker diagnostics,
    /// cooldown timers, and simulation triggers in Unity Editor and Development Builds.
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

        [Tooltip("Optional custom PanelSettings. If null, a high-priority runtime PanelSettings is created automatically.")]
        public PanelSettings customPanelSettings;

        [Header("Scale (mobile-friendly)")]
        [Tooltip("Initial UI zoom on touch platforms. Adjustable in-game with the A-/A+ header buttons (saved per device).")]
        [Range(1f, 3f)]
        public float mobileDefaultScale = 1.75f;

        [Tooltip("Initial UI zoom on desktop/Editor.")]
        [Range(0.75f, 3f)]
        public float desktopDefaultScale = 1f;

        #endregion

        #region Private Fields

        private UIDocument _uiDocument;
        private VisualElement _root;

        private const float ZoomMin = 0.75f;
        private const float ZoomMax = 3f;
        private const float ZoomStep = 0.25f;
        private const string ZoomPrefsKey = "RateDebugOverlay.Zoom";
        private static readonly Vector2Int BaseReferenceResolution = new Vector2Int(1920, 1080);
        private float _zoom = 1f;
        private bool _isMaximized;
        private Label _zoomLabel;
        private StyleLength _restoreLeft, _restoreRight, _restoreTop, _restoreWidth, _restoreHeight, _restoreMaxHeight;
        private VisualElement _floatingBtn;
        private VisualElement _window;
        private ScrollView _scrollView;

        private Label _statusBanner;
        private Label _statusSubtext;
        private Label _activeSceneLabel;
        private Label _blockerLabel;
        private Label _cooldownLabel;
        private Label _dontAskLabel;

        private Label _startsCountLabel;
        private VisualElement _startsProgressFill;
        private Label _eventsCountLabel;
        private VisualElement _eventsProgressFill;
        private Label _shownCountLabel;
        private Label _pendingLabel;

        private VisualElement _eventLogContainer;
        private readonly List<string> _eventHistory = new List<string>();
        private const int MaxHistoryCount = 12;

        private bool _isOpen;
        private float _lastRefreshTime;
        private const float RefreshInterval = 0.35f;

        // Drag state
        private bool _isDragging;
        private Vector2 _dragStartPointer;
        private Vector2 _dragStartWindowPos;

        // Floating button drag state
        private bool _isFloatingDragging;
        private Vector2 _floatingDragStartPointer;
        private Vector2 _floatingDragStartPos;
        private bool _hasDraggedFloating;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (!Debug.isDebugBuild && !Application.isEditor && !enableInReleaseBuilds)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            InitializeUI();
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
                SetOpen(!_isOpen);
            }

            if (_isOpen && Time.unscaledTime - _lastRefreshTime >= RefreshInterval)
            {
                _lastRefreshTime = Time.unscaledTime;
                RefreshData();
            }
        }

        #endregion

        #region UI Toolkit Initialization

        private void InitializeUI()
        {
            _uiDocument = gameObject.GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                _uiDocument = gameObject.AddComponent<UIDocument>();
            }

            EnsurePanelSettings();

            // Runtime clone: zoom must never modify the shared PanelSettings asset.
            _uiDocument.panelSettings = Instantiate(_uiDocument.panelSettings);
            _zoom = LoadZoom();
            ApplyZoom();

            _root = _uiDocument.rootVisualElement;
            _root.Clear();
            _root.pickingMode = PickingMode.Ignore;

            BuildFloatingButton();
            BuildWindow();

            SetOpen(false);
            RefreshData();
        }

        private float LoadZoom()
        {
            var fallback = Application.isMobilePlatform ? mobileDefaultScale : desktopDefaultScale;
            return Mathf.Clamp(PlayerPrefs.GetFloat(ZoomPrefsKey, fallback), ZoomMin, ZoomMax);
        }

        private void SetZoom(float value)
        {
            _zoom = Mathf.Clamp(Mathf.Round(value / ZoomStep) * ZoomStep, ZoomMin, ZoomMax);
            PlayerPrefs.SetFloat(ZoomPrefsKey, _zoom);
            PlayerPrefs.Save();
            ApplyZoom();
        }

        /// <summary>Zoom works by shrinking the panel reference resolution (ScaleWithScreenSize).</summary>
        private void ApplyZoom()
        {
            _uiDocument.panelSettings.referenceResolution = new Vector2Int(
                Mathf.RoundToInt(BaseReferenceResolution.x / _zoom),
                Mathf.RoundToInt(BaseReferenceResolution.y / _zoom));

            if (_zoomLabel != null) _zoomLabel.text = $"{_zoom:0.##}x";
        }

        private void ToggleMaximize()
        {
            _isMaximized = !_isMaximized;
            var st = _window.style;

            if (_isMaximized)
            {
                _restoreLeft = st.left; _restoreRight = st.right; _restoreTop = st.top;
                _restoreWidth = st.width; _restoreHeight = st.height; _restoreMaxHeight = st.maxHeight;

                st.left = 0; st.right = 0; st.top = 0;
                st.width = new StyleLength(new Length(100, LengthUnit.Percent));
                st.height = new StyleLength(new Length(100, LengthUnit.Percent));
                st.maxHeight = new StyleLength(new Length(100, LengthUnit.Percent));
                return;
            }

            st.left = _restoreLeft; st.right = _restoreRight; st.top = _restoreTop;
            st.width = _restoreWidth; st.height = _restoreHeight; st.maxHeight = _restoreMaxHeight;
        }

        private void EnsurePanelSettings()
        {
            if (_uiDocument.panelSettings != null) return;

            if (customPanelSettings != null)
            {
                _uiDocument.panelSettings = customPanelSettings;
                return;
            }

            // Project-wide override first, then the PanelSettings+theme shipped with this package.
            // The shipped asset is required: in player builds no ThemeStyleSheet is loaded, so a
            // runtime-created PanelSettings would render nothing.
            var loaded = Resources.Load<PanelSettings>("Wagenheimer/DebugPanelSettings")
                         ?? Resources.Load<PanelSettings>("Wagenheimer/RateDebugPanelSettings");
            if (loaded != null)
            {
                _uiDocument.panelSettings = loaded;
                return;
            }

            var ps = ScriptableObject.CreateInstance<PanelSettings>();
            ps.name = "RateDebugPanelSettings";
            ps.sortingOrder = 9998;
            ps.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            ps.referenceResolution = new Vector2Int(1920, 1080);
            ps.match = 0.5f;

            var themes = Resources.FindObjectsOfTypeAll<ThemeStyleSheet>();
            if (themes != null && themes.Length > 0)
            {
                ps.themeStyleSheet = themes[0];
            }

            _uiDocument.panelSettings = ps;
        }

        #endregion

        #region Floating Button

        private void BuildFloatingButton()
        {
            _floatingBtn = new VisualElement();
            _floatingBtn.name = "RateDebugFloatingButton";
            _floatingBtn.pickingMode = PickingMode.Position;
            _floatingBtn.style.position = Position.Absolute;
            _floatingBtn.style.bottom = 18;
            _floatingBtn.style.right = 18;
            _floatingBtn.style.height = 34;
            _floatingBtn.style.paddingLeft = 12;
            _floatingBtn.style.paddingRight = 12;
            _floatingBtn.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.15f, 0.94f));
            _floatingBtn.style.borderTopWidth = 1;
            _floatingBtn.style.borderBottomWidth = 1;
            _floatingBtn.style.borderLeftWidth = 1;
            _floatingBtn.style.borderRightWidth = 1;
            _floatingBtn.style.borderTopColor = new StyleColor(new Color(0.35f, 0.55f, 0.95f, 0.8f));
            _floatingBtn.style.borderBottomColor = new StyleColor(new Color(0.35f, 0.55f, 0.95f, 0.8f));
            _floatingBtn.style.borderLeftColor = new StyleColor(new Color(0.35f, 0.55f, 0.95f, 0.8f));
            _floatingBtn.style.borderRightColor = new StyleColor(new Color(0.35f, 0.55f, 0.95f, 0.8f));
            _floatingBtn.style.borderTopLeftRadius = 17;
            _floatingBtn.style.borderTopRightRadius = 17;
            _floatingBtn.style.borderBottomLeftRadius = 17;
            _floatingBtn.style.borderBottomRightRadius = 17;
            _floatingBtn.style.flexDirection = FlexDirection.Row;
            _floatingBtn.style.alignItems = Align.Center;
            _floatingBtn.style.justifyContent = Justify.Center;

            var dot = new VisualElement();
            dot.style.width = 8;
            dot.style.height = 8;
            dot.style.borderTopLeftRadius = 4;
            dot.style.borderTopRightRadius = 4;
            dot.style.borderBottomLeftRadius = 4;
            dot.style.borderBottomRightRadius = 4;
            dot.style.backgroundColor = new StyleColor(new Color(0.24f, 0.82f, 0.45f));
            dot.style.marginRight = 6;
            _floatingBtn.Add(dot);

            var label = new Label("⭐ RATE DBG");
            label.style.color = new StyleColor(Color.white);
            label.style.fontSize = 11.5f;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            _floatingBtn.Add(label);

            // Drag / Click handling
            _floatingBtn.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0 || _isMaximized) return;
                _isFloatingDragging = true;
                _hasDraggedFloating = false;
                _floatingDragStartPointer = evt.position;
                _floatingDragStartPos = new Vector2(_floatingBtn.resolvedStyle.left, _floatingBtn.resolvedStyle.top);
                _floatingBtn.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            _floatingBtn.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_isFloatingDragging) return;
                Vector2 delta = (Vector2)evt.position - _floatingDragStartPointer;
                if (delta.sqrMagnitude > 16f) _hasDraggedFloating = true;

                if (_hasDraggedFloating)
                {
                    _floatingBtn.style.bottom = StyleKeyword.Auto;
                    _floatingBtn.style.right = StyleKeyword.Auto;
                    _floatingBtn.style.left = Mathf.Max(0, _floatingDragStartPos.x + delta.x);
                    _floatingBtn.style.top = Mathf.Max(0, _floatingDragStartPos.y + delta.y);
                }
                evt.StopPropagation();
            });

            _floatingBtn.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!_isFloatingDragging) return;
                _isFloatingDragging = false;
                _floatingBtn.ReleasePointer(evt.pointerId);
                evt.StopPropagation();

                if (!_hasDraggedFloating)
                {
                    SetOpen(true);
                }
            });

            _root.Add(_floatingBtn);
        }

        #endregion

        #region Main Window

        private void BuildWindow()
        {
            _window = new VisualElement();
            _window.name = "RateDebugWindow";
            _window.pickingMode = PickingMode.Position;
            _window.style.position = Position.Absolute;
            _window.style.right = 24;
            _window.style.top = 30;
            _window.style.width = 460;
            _window.style.maxWidth = new StyleLength(new Length(96, LengthUnit.Percent));
            _window.style.maxHeight = new StyleLength(new Length(86, LengthUnit.Percent));
            _window.style.backgroundColor = new StyleColor(new Color(0.09f, 0.09f, 0.11f, 0.96f));
            _window.style.borderTopWidth = 1;
            _window.style.borderBottomWidth = 1;
            _window.style.borderLeftWidth = 1;
            _window.style.borderRightWidth = 1;
            _window.style.borderTopColor = new StyleColor(new Color(0.25f, 0.26f, 0.32f));
            _window.style.borderBottomColor = new StyleColor(new Color(0.25f, 0.26f, 0.32f));
            _window.style.borderLeftColor = new StyleColor(new Color(0.25f, 0.26f, 0.32f));
            _window.style.borderRightColor = new StyleColor(new Color(0.25f, 0.26f, 0.32f));
            _window.style.borderTopLeftRadius = 10;
            _window.style.borderTopRightRadius = 10;
            _window.style.borderBottomLeftRadius = 10;
            _window.style.borderBottomRightRadius = 10;
            _window.style.overflow = Overflow.Hidden;

            // Header (Draggable)
            var header = BuildHeader();
            _window.Add(header);

            // Scrollable Content
            _scrollView = new ScrollView(ScrollViewMode.Vertical);
            _scrollView.style.flexGrow = 1;
            _scrollView.style.paddingLeft = 12;
            _scrollView.style.paddingRight = 12;
            _scrollView.style.paddingTop = 10;
            _scrollView.style.paddingBottom = 12;

            _scrollView.Add(BuildStatusSection());
            _scrollView.Add(BuildCountersSection());
            _scrollView.Add(BuildSimulationSection());
            _scrollView.Add(BuildStoreSection());
            _scrollView.Add(BuildEventLogSection());

            _window.Add(_scrollView);
            _root.Add(_window);
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.height = 38;
            header.style.paddingLeft = 12;
            header.style.paddingRight = 8;
            header.style.backgroundColor = new StyleColor(new Color(0.13f, 0.14f, 0.18f));
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = new StyleColor(new Color(0.22f, 0.23f, 0.28f));

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;

            var titleLbl = new Label("⭐ Rate Control Debug");
            titleLbl.style.fontSize = 13;
            titleLbl.style.color = new StyleColor(Color.white);
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(titleLbl);

            var liveBadge = CreatePill("LIVE", new Color(0.15f, 0.5f, 0.25f), Color.white);
            liveBadge.style.marginLeft = 8;
            titleRow.Add(liveBadge);

            header.Add(titleRow);

            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.alignItems = Align.Center;

            var zoomOutBtn = CreateSmallButton("A-", () => SetZoom(_zoom - ZoomStep));
            actions.Add(zoomOutBtn);

            _zoomLabel = new Label($"{_zoom:0.##}x");
            _zoomLabel.style.minWidth = 34;
            _zoomLabel.style.fontSize = 10;
            _zoomLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _zoomLabel.style.color = new StyleColor(new Color(0.65f, 0.68f, 0.75f));
            actions.Add(_zoomLabel);

            var zoomInBtn = CreateSmallButton("A+", () => SetZoom(_zoom + ZoomStep));
            zoomInBtn.style.marginRight = 6;
            actions.Add(zoomInBtn);

            var maxBtn = CreateSmallButton("[ ]", ToggleMaximize);
            maxBtn.style.fontSize = 9;
            maxBtn.style.marginRight = 4;
            actions.Add(maxBtn);

            var minBtn = CreateSmallButton("—", () => SetOpen(false));
            minBtn.style.marginRight = 4;
            actions.Add(minBtn);

            var closeBtn = CreateSmallButton("✕", () => SetOpen(false));
            actions.Add(closeBtn);

            header.Add(actions);

            // Drag handling for header
            header.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                _isDragging = true;
                _dragStartPointer = evt.position;
                _dragStartWindowPos = new Vector2(_window.resolvedStyle.left, _window.resolvedStyle.top);
                header.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            });

            header.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!_isDragging) return;
                Vector2 delta = (Vector2)evt.position - _dragStartPointer;
                _window.style.right = StyleKeyword.Auto;
                _window.style.left = Mathf.Max(0, _dragStartWindowPos.x + delta.x);
                _window.style.top = Mathf.Max(0, _dragStartWindowPos.y + delta.y);
                evt.StopPropagation();
            });

            header.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!_isDragging) return;
                _isDragging = false;
                header.ReleasePointer(evt.pointerId);
                evt.StopPropagation();
            });

            return header;
        }

        #endregion

        #region Content Sections

        private VisualElement BuildStatusSection()
        {
            var card = CreateCard("Gate & Readiness Status");

            _statusBanner = new Label("INITIALIZING...");
            _statusBanner.style.fontSize = 13;
            _statusBanner.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusBanner.style.paddingTop = 4;
            _statusBanner.style.paddingBottom = 4;
            _statusBanner.style.paddingLeft = 8;
            _statusBanner.style.paddingRight = 8;
            _statusBanner.style.borderTopLeftRadius = 4;
            _statusBanner.style.borderTopRightRadius = 4;
            _statusBanner.style.borderBottomLeftRadius = 4;
            _statusBanner.style.borderBottomRightRadius = 4;
            _statusBanner.style.marginBottom = 6;
            card.Add(_statusBanner);

            _statusSubtext = new Label("");
            _statusSubtext.style.fontSize = 10.5f;
            _statusSubtext.style.color = new StyleColor(new Color(0.75f, 0.75f, 0.8f));
            _statusSubtext.style.marginBottom = 6;
            card.Add(_statusSubtext);

            _activeSceneLabel = CreateRow(card, "Active Scene", "");
            _blockerLabel = CreateRow(card, "Blocker (IRateBlocker)", "");
            _cooldownLabel = CreateRow(card, "Remind Cooldown", "");
            _dontAskLabel = CreateRow(card, "DontAsk Flag", "");

            return card;
        }

        private VisualElement BuildCountersSection()
        {
            var card = CreateCard("Counters & Thresholds");

            var startsRow = new VisualElement();
            startsRow.style.flexDirection = FlexDirection.Row;
            startsRow.style.justifyContent = Justify.SpaceBetween;
            var startsTitle = new Label("App Launches (Starts)");
            startsTitle.style.fontSize = 11;
            startsTitle.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.9f));
            _startsCountLabel = new Label("0 / 3");
            _startsCountLabel.style.fontSize = 11;
            _startsCountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _startsCountLabel.style.color = new StyleColor(Color.white);
            startsRow.Add(startsTitle);
            startsRow.Add(_startsCountLabel);
            card.Add(startsRow);

            var startsBar = CreateProgressBar(out _startsProgressFill, new Color(0.2f, 0.6f, 0.95f));
            card.Add(startsBar);

            var eventsRow = new VisualElement();
            eventsRow.style.flexDirection = FlexDirection.Row;
            eventsRow.style.justifyContent = Justify.SpaceBetween;
            eventsRow.style.marginTop = 6;
            var eventsTitle = new Label("Milestone Events");
            eventsTitle.style.fontSize = 11;
            eventsTitle.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.9f));
            _eventsCountLabel = new Label("0 / 10");
            _eventsCountLabel.style.fontSize = 11;
            _eventsCountLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _eventsCountLabel.style.color = new StyleColor(Color.white);
            eventsRow.Add(eventsTitle);
            eventsRow.Add(_eventsCountLabel);
            card.Add(eventsRow);

            var eventsBar = CreateProgressBar(out _eventsProgressFill, new Color(0.24f, 0.82f, 0.45f));
            card.Add(eventsBar);

            _shownCountLabel = CreateRow(card, "Times Prompt Displayed", "0");
            _pendingLabel = CreateRow(card, "Pending In Polling Loop", "NO");

            return card;
        }

        private VisualElement BuildSimulationSection()
        {
            var card = CreateCard("Simulation & QA Controls");

            // Primary: Force Prompt
            var forceBtn = CreateButton("⚡ Force Show Prompt (Bypass Gates)", new Color(0.18f, 0.52f, 0.32f), Color.white, () =>
            {
                var rc = RateControl.Instance;
                if (rc != null) rc.ForceShowPrompt();
                else Debug.LogWarning("[RateControl] RateControl instance is not active.");
            });
            forceBtn.style.height = 30;
            forceBtn.style.marginBottom = 6;
            card.Add(forceBtn);

            // Counter triggers row
            var counterRow = new VisualElement();
            counterRow.style.flexDirection = FlexDirection.Row;
            counterRow.style.marginBottom = 6;

            var logEventBtn = CreateButton("📈 +1 Event", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                RateControl.LogEvent();
                RefreshData();
            });
            logEventBtn.style.flexGrow = 1;
            logEventBtn.style.marginRight = 4;
            counterRow.Add(logEventBtn);

            var log5EventsBtn = CreateButton("🔥 +5 Events", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                for (int i = 0; i < 5; i++) RateControl.LogEvent();
                RefreshData();
            });
            log5EventsBtn.style.flexGrow = 1;
            log5EventsBtn.style.marginRight = 4;
            counterRow.Add(log5EventsBtn);

            var logStartBtn = CreateButton("🚀 +1 Start", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                RateControl.LogStart();
                RefreshData();
            });
            logStartBtn.style.flexGrow = 1;
            counterRow.Add(logStartBtn);

            card.Add(counterRow);

            // State modifiers row
            var modRow = new VisualElement();
            modRow.style.flexDirection = FlexDirection.Row;
            modRow.style.marginBottom = 6;

            var clearCooldownBtn = CreateButton("⏳ Clear Cooldown", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                var rc = RateControl.Instance;
                if (rc != null) rc.ClearRemindCooldown();
                RefreshData();
            });
            clearCooldownBtn.style.flexGrow = 1;
            clearCooldownBtn.style.marginRight = 4;
            modRow.Add(clearCooldownBtn);

            var toggleDontAskBtn = CreateButton("🚫 Toggle DontAsk", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                var rc = RateControl.Instance;
                if (rc != null)
                {
                    rc.DontAsk = !rc.DontAsk;
                    rc.Save();
                }
                RefreshData();
            });
            toggleDontAskBtn.style.flexGrow = 1;
            toggleDontAskBtn.style.marginRight = 4;
            modRow.Add(toggleDontAskBtn);

            var togglePendingBtn = CreateButton("🔄 Toggle Pending", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                var rc = RateControl.Instance;
                if (rc != null) rc.SetPendingPrompt(!rc.IsPendingPrompt);
                RefreshData();
            });
            togglePendingBtn.style.flexGrow = 1;
            modRow.Add(togglePendingBtn);

            card.Add(modRow);

            // Dialog simulation actions row
            var simActionsRow = new VisualElement();
            simActionsRow.style.flexDirection = FlexDirection.Row;
            simActionsRow.style.marginBottom = 6;

            var simRateBtn = CreateButton("Rate Now", new Color(0.18f, 0.38f, 0.62f), Color.white, () =>
            {
                RateControl.UserActed(RateUserAction.RateNow);
                RefreshData();
            });
            simRateBtn.style.flexGrow = 1;
            simRateBtn.style.marginRight = 4;
            simActionsRow.Add(simRateBtn);

            var simRemindBtn = CreateButton("Remind Later", new Color(0.48f, 0.36f, 0.12f), Color.white, () =>
            {
                RateControl.UserActed(RateUserAction.RemindLater);
                RefreshData();
            });
            simRemindBtn.style.flexGrow = 1;
            simRemindBtn.style.marginRight = 4;
            simActionsRow.Add(simRemindBtn);

            var simDeclineBtn = CreateButton("Decline", new Color(0.45f, 0.20f, 0.22f), Color.white, () =>
            {
                RateControl.UserActed(RateUserAction.Decline);
                RefreshData();
            });
            simDeclineBtn.style.flexGrow = 1;
            simActionsRow.Add(simDeclineBtn);

            card.Add(simActionsRow);

            // Danger Reset button
            var resetBtn = CreateButton("🗑️ Reset All Saved State (PlayerPrefs)", new Color(0.55f, 0.18f, 0.20f), Color.white, () =>
            {
                RateControl.ResetAll();
                RefreshData();
            });
            resetBtn.style.height = 26;
            card.Add(resetBtn);

            return card;
        }

        private VisualElement BuildStoreSection()
        {
            var card = CreateCard("Store & System Info");

            var rc = RateControl.Instance;
            var cfg = rc != null ? rc.Config : null;

            CreateRow(card, "Platform / Installer", $"{Application.platform} / \"{Application.installerName}\"");
            CreateRow(card, "App Identifier / Version", $"{Application.identifier} / v{Application.version}");

            if (cfg != null)
            {
                CreateRow(card, "Key Prefix", cfg.StorageKeyPrefix);
                CreateRow(card, "Android Package ID", cfg.ResolvedAndroidId);
                CreateRow(card, "iOS / Steam App ID", $"{(string.IsNullOrEmpty(cfg.iOSAppId) ? "-" : cfg.iOSAppId)} / {(string.IsNullOrEmpty(cfg.SteamAppId) ? "-" : cfg.SteamAppId)}");
            }

            var linksRow = new VisualElement();
            linksRow.style.flexDirection = FlexDirection.Row;
            linksRow.style.marginTop = 6;

            var testRateBtn = CreateButton("Test RateNow()", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                RateControl.RateNow();
            });
            testRateBtn.style.flexGrow = 1;
            testRateBtn.style.marginRight = 4;
            linksRow.Add(testRateBtn);

            var testMoreGamesBtn = CreateButton("Test MoreGames()", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                RateControl.ShowMoreGames();
            });
            testMoreGamesBtn.style.flexGrow = 1;
            testMoreGamesBtn.style.marginRight = 4;
            linksRow.Add(testMoreGamesBtn);

            var copyReportBtn = CreateButton("📋 Copy Report", new Color(0.22f, 0.23f, 0.28f), Color.white, () =>
            {
                string report = GenerateDiagnosticReport();
                GUIUtility.systemCopyBuffer = report;
                Debug.Log($"[RateDebugOverlay] Diagnostic report copied:\n{report}");
            });
            copyReportBtn.style.flexGrow = 1;
            linksRow.Add(copyReportBtn);

            card.Add(linksRow);
            return card;
        }

        private VisualElement BuildEventLogSection()
        {
            var card = CreateCard("Recent Event Log");

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;
            headerRow.style.alignItems = Align.Center;
            headerRow.style.marginBottom = 6;

            var logTitle = new Label("Captured Events");
            logTitle.style.fontSize = 11;
            logTitle.style.color = new StyleColor(new Color(0.7f, 0.7f, 0.75f));
            headerRow.Add(logTitle);

            var clearLogBtn = CreateSmallButton("Clear", () =>
            {
                _eventHistory.Clear();
                RefreshEventLog();
            });
            headerRow.Add(clearLogBtn);
            card.Add(headerRow);

            _eventLogContainer = new VisualElement();
            _eventLogContainer.style.backgroundColor = new StyleColor(new Color(0.06f, 0.06f, 0.08f));
            _eventLogContainer.style.borderTopWidth = 1;
            _eventLogContainer.style.borderBottomWidth = 1;
            _eventLogContainer.style.borderLeftWidth = 1;
            _eventLogContainer.style.borderRightWidth = 1;
            _eventLogContainer.style.borderTopColor = new StyleColor(new Color(0.18f, 0.18f, 0.22f));
            _eventLogContainer.style.borderBottomColor = new StyleColor(new Color(0.18f, 0.18f, 0.22f));
            _eventLogContainer.style.borderLeftColor = new StyleColor(new Color(0.18f, 0.18f, 0.22f));
            _eventLogContainer.style.borderRightColor = new StyleColor(new Color(0.18f, 0.18f, 0.22f));
            _eventLogContainer.style.borderTopLeftRadius = 6;
            _eventLogContainer.style.borderTopRightRadius = 6;
            _eventLogContainer.style.borderBottomLeftRadius = 6;
            _eventLogContainer.style.borderBottomRightRadius = 6;
            _eventLogContainer.style.paddingTop = 6;
            _eventLogContainer.style.paddingBottom = 6;
            _eventLogContainer.style.paddingLeft = 8;
            _eventLogContainer.style.paddingRight = 8;
            _eventLogContainer.style.minHeight = 60;

            card.Add(_eventLogContainer);
            RefreshEventLog();

            return card;
        }

        #endregion

        #region Data Refresh

        private void RefreshData()
        {
            var rc = RateControl.Instance;
            if (rc == null)
            {
                _statusBanner.text = "NOT INITIALIZED";
                _statusBanner.style.backgroundColor = new StyleColor(new Color(0.6f, 0.18f, 0.2f));
                _statusBanner.style.color = new StyleColor(Color.white);
                _statusSubtext.text = "RateControl.Initialize() has not yet run in this session.";
                return;
            }

            var cfg = rc.Config;
            string activeScene = SceneManager.GetActiveScene().name;
            bool isBlacklisted = rc.IsSceneBlacklisted;
            bool blockerAllows = rc.BlockerAllowsPrompt;
            bool inCooldown = rc.InRemindCooldown;

            // Status evaluation
            if (rc.DontAsk)
            {
                _statusBanner.text = "DISABLED (DONT ASK = TRUE)";
                _statusBanner.style.backgroundColor = new StyleColor(new Color(0.35f, 0.35f, 0.40f));
                _statusBanner.style.color = new StyleColor(new Color(0.9f, 0.9f, 0.9f));
                _statusSubtext.text = $"Player previously rated or declined. Last rated version: {rc.LastVersionRated ?? "(none)"}";
            }
            else if (inCooldown)
            {
                _statusBanner.text = "SUPPRESSED (COOLDOWN ACTIVE)";
                _statusBanner.style.backgroundColor = new StyleColor(new Color(0.65f, 0.45f, 0.12f));
                _statusBanner.style.color = new StyleColor(Color.white);
                _statusSubtext.text = "Prompt postponed via 'Remind Me Later'.";
            }
            else if (isBlacklisted)
            {
                _statusBanner.text = "SUPPRESSED (SCENE BLACKLISTED)";
                _statusBanner.style.backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f));
                _statusBanner.style.color = new StyleColor(Color.white);
                _statusSubtext.text = $"Scene '{activeScene}' is marked as blacklisted in RateConfig.";
            }
            else if (!blockerAllows)
            {
                _statusBanner.text = "BLOCKED (IRATEBLOCKER REJECTED)";
                _statusBanner.style.backgroundColor = new StyleColor(new Color(0.6f, 0.2f, 0.2f));
                _statusBanner.style.color = new StyleColor(Color.white);
                _statusSubtext.text = "Custom IRateBlocker.CanShowRate() returned false.";
            }
            else
            {
                _statusBanner.text = "ELIGIBLE / READY";
                _statusBanner.style.backgroundColor = new StyleColor(new Color(0.15f, 0.55f, 0.28f));
                _statusBanner.style.color = new StyleColor(Color.white);
                _statusSubtext.text = "All gates clear. Prompt will appear once threshold conditions are satisfied.";
            }

            // Detail rows
            _activeSceneLabel.text = $"{activeScene} {(isBlacklisted ? "[BLACKLISTED]" : "[ALLOWED]")}";
            _blockerLabel.text = blockerAllows ? "CanShowRate = TRUE" : "CanShowRate = FALSE";

            if (inCooldown && !string.IsNullOrEmpty(rc.RemindLaterUntil))
            {
                if (DateTime.TryParse(rc.RemindLaterUntil, null, System.Globalization.DateTimeStyles.RoundtripKind, out var until))
                {
                    TimeSpan rem = until - DateTime.UtcNow;
                    _cooldownLabel.text = $"{rem.Days}d {rem.Hours}h {rem.Minutes}m {rem.Seconds}s remaining";
                }
                else
                {
                    _cooldownLabel.text = rc.RemindLaterUntil;
                }
            }
            else
            {
                _cooldownLabel.text = "None (Inactive)";
            }

            _dontAskLabel.text = rc.DontAsk ? "TRUE" : "FALSE";

            // Counters & Progress
            int eventsPerPrompt = cfg != null ? cfg.EventsPerPrompt : 10;
            int startsFirst = cfg != null ? cfg.StartsBeforeFirstPrompt : 3;
            int startsSubsequent = cfg != null ? cfg.StartsBeforeSubsequentPrompts : 8;
            int reqStarts = (rc.ShowCount == 0) ? startsFirst : startsSubsequent;

            _startsCountLabel.text = $"{rc.StartCount} / {reqStarts}";
            float startPct = Mathf.Clamp01((float)rc.StartCount / Mathf.Max(1, reqStarts)) * 100f;
            _startsProgressFill.style.width = new StyleLength(new Length(startPct, LengthUnit.Percent));

            _eventsCountLabel.text = $"{rc.EventCount} / {eventsPerPrompt}";
            float eventPct = Mathf.Clamp01((float)(rc.EventCount % eventsPerPrompt) / Mathf.Max(1, eventsPerPrompt)) * 100f;
            if (rc.EventCount > 0 && rc.EventCount % eventsPerPrompt == 0) eventPct = 100f;
            _eventsProgressFill.style.width = new StyleLength(new Length(eventPct, LengthUnit.Percent));

            _shownCountLabel.text = rc.ShowCount.ToString();
            _pendingLabel.text = rc.IsPendingPrompt ? "YES (Waiting clearance)" : "NO";
        }

        private void RefreshEventLog()
        {
            if (_eventLogContainer == null) return;
            _eventLogContainer.Clear();

            if (_eventHistory.Count == 0)
            {
                var empty = new Label("No events logged yet in this session.");
                empty.style.fontSize = 10.5f;
                empty.style.color = new StyleColor(new Color(0.5f, 0.5f, 0.55f));
                _eventLogContainer.Add(empty);
                return;
            }

            foreach (var item in _eventHistory)
            {
                var lbl = new Label(item);
                lbl.style.fontSize = 10.5f;
                lbl.style.color = new StyleColor(new Color(0.75f, 0.85f, 0.95f));
                lbl.style.marginBottom = 2;
                _eventLogContainer.Add(lbl);
            }
        }

        #endregion

        #region Event Logging

        private void HandlePromptRequested() => LogEvent("OnPromptRequested fired.");
        private void HandleUserRated() => LogEvent("OnUserRated (Rate Now clicked).");
        private void HandleUserRemindedLater() => LogEvent("OnUserRemindedLater fired.");
        private void HandleUserDeclined() => LogEvent("OnUserDeclined (No Thanks clicked).");

        private void LogEvent(string msg)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            _eventHistory.Insert(0, entry);
            if (_eventHistory.Count > MaxHistoryCount)
            {
                _eventHistory.RemoveAt(_eventHistory.Count - 1);
            }
            RefreshEventLog();
        }

        #endregion

        #region Helpers & UI Factories

        public void SetOpen(bool open)
        {
            _isOpen = open;
            if (_window != null)
            {
                _window.style.display = _isOpen ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_floatingBtn != null)
            {
                _floatingBtn.style.display = (showFloatingButton && !_isOpen) ? DisplayStyle.Flex : DisplayStyle.None;
            }
            if (_isOpen)
            {
                RefreshData();
            }
        }

        private VisualElement CreateCard(string title)
        {
            var card = new VisualElement();
            card.style.backgroundColor = new StyleColor(new Color(0.12f, 0.13f, 0.16f, 0.9f));
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = new StyleColor(new Color(0.20f, 0.21f, 0.26f));
            card.style.borderBottomColor = new StyleColor(new Color(0.20f, 0.21f, 0.26f));
            card.style.borderLeftColor = new StyleColor(new Color(0.20f, 0.21f, 0.26f));
            card.style.borderRightColor = new StyleColor(new Color(0.20f, 0.21f, 0.26f));
            card.style.borderTopLeftRadius = 8;
            card.style.borderTopRightRadius = 8;
            card.style.borderBottomLeftRadius = 8;
            card.style.borderBottomRightRadius = 8;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.marginBottom = 8;

            var titleLbl = new Label(title);
            titleLbl.style.fontSize = 11.5f;
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.color = new StyleColor(new Color(0.6f, 0.8f, 1f));
            titleLbl.style.marginBottom = 6;
            card.Add(titleLbl);

            return card;
        }

        private Label CreateRow(VisualElement parent, string key, string value)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;

            var keyLbl = new Label(key);
            keyLbl.style.fontSize = 11;
            keyLbl.style.color = new StyleColor(new Color(0.68f, 0.70f, 0.76f));
            row.Add(keyLbl);

            var valLbl = new Label(value);
            valLbl.style.fontSize = 11;
            valLbl.style.color = new StyleColor(Color.white);
            valLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(valLbl);

            parent.Add(row);
            return valLbl;
        }

        private VisualElement CreateProgressBar(out VisualElement fill, Color fillColor)
        {
            var track = new VisualElement();
            track.style.height = 8;
            track.style.backgroundColor = new StyleColor(new Color(0.07f, 0.07f, 0.09f));
            track.style.borderTopLeftRadius = 4;
            track.style.borderTopRightRadius = 4;
            track.style.borderBottomLeftRadius = 4;
            track.style.borderBottomRightRadius = 4;
            track.style.overflow = Overflow.Hidden;
            track.style.marginTop = 3;
            track.style.marginBottom = 4;

            fill = new VisualElement();
            fill.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
            fill.style.width = new StyleLength(new Length(0, LengthUnit.Percent));
            fill.style.backgroundColor = new StyleColor(fillColor);
            fill.style.borderTopLeftRadius = 4;
            fill.style.borderTopRightRadius = 4;
            fill.style.borderBottomLeftRadius = 4;
            fill.style.borderBottomRightRadius = 4;
            track.Add(fill);

            return track;
        }

        private Button CreateButton(string text, Color bg, Color textCol, Action onClick)
        {
            var btn = new Button(onClick);
            btn.text = text;
            btn.style.backgroundColor = new StyleColor(bg);
            btn.style.color = new StyleColor(textCol);
            btn.style.fontSize = 11;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.borderTopLeftRadius = 5;
            btn.style.borderTopRightRadius = 5;
            btn.style.borderBottomLeftRadius = 5;
            btn.style.borderBottomRightRadius = 5;
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            btn.style.paddingTop = 4;
            btn.style.paddingBottom = 4;
            btn.style.paddingLeft = 8;
            btn.style.paddingRight = 8;
            return btn;
        }

        private Button CreateSmallButton(string text, Action onClick)
        {
            var btn = new Button(onClick);
            btn.text = text;
            btn.style.width = 22;
            btn.style.height = 22;
            btn.style.fontSize = 11;
            btn.style.unityFontStyleAndWeight = FontStyle.Bold;
            btn.style.backgroundColor = new StyleColor(new Color(0.22f, 0.23f, 0.28f));
            btn.style.color = new StyleColor(Color.white);
            btn.style.borderTopLeftRadius = 4;
            btn.style.borderTopRightRadius = 4;
            btn.style.borderBottomLeftRadius = 4;
            btn.style.borderBottomRightRadius = 4;
            btn.style.borderTopWidth = 0;
            btn.style.borderBottomWidth = 0;
            btn.style.borderLeftWidth = 0;
            btn.style.borderRightWidth = 0;
            return btn;
        }

        private VisualElement CreatePill(string text, Color bg, Color textCol)
        {
            var pill = new VisualElement();
            pill.style.backgroundColor = new StyleColor(bg);
            pill.style.borderTopLeftRadius = 4;
            pill.style.borderTopRightRadius = 4;
            pill.style.borderBottomLeftRadius = 4;
            pill.style.borderBottomRightRadius = 4;
            pill.style.paddingTop = 1;
            pill.style.paddingBottom = 1;
            pill.style.paddingLeft = 5;
            pill.style.paddingRight = 5;

            var lbl = new Label(text);
            lbl.style.fontSize = 9.5f;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            lbl.style.color = new StyleColor(textCol);
            pill.Add(lbl);

            return pill;
        }

        private string GenerateDiagnosticReport()
        {
            var rc = RateControl.Instance;
            var cfg = rc != null ? rc.Config : null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== Rate Control Diagnostic Report ===");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Initialized: {rc != null}");
            sb.AppendLine($"Active Scene: {SceneManager.GetActiveScene().name}");
            sb.AppendLine($"Is Scene Blacklisted: {rc?.IsSceneBlacklisted}");
            sb.AppendLine($"Blocker Allows Prompt: {rc?.BlockerAllowsPrompt}");
            sb.AppendLine($"DontAsk: {rc?.DontAsk}");
            sb.AppendLine($"Last Version Rated: {rc?.LastVersionRated}");
            sb.AppendLine($"In Remind Cooldown: {rc?.InRemindCooldown}");
            sb.AppendLine($"Remind Later Until: {rc?.RemindLaterUntil}");
            sb.AppendLine($"Start Count: {rc?.StartCount}");
            sb.AppendLine($"Event Count: {rc?.EventCount}");
            sb.AppendLine($"Show Count: {rc?.ShowCount}");
            sb.AppendLine($"Pending Prompt: {rc?.IsPendingPrompt}");
            if (cfg != null)
            {
                sb.AppendLine($"Storage Prefix: {cfg.StorageKeyPrefix}");
                sb.AppendLine($"Android Package ID: {cfg.ResolvedAndroidId}");
                sb.AppendLine($"iOS App ID: {cfg.iOSAppId}");
                sb.AppendLine($"Steam App ID: {cfg.SteamAppId}");
                sb.AppendLine($"Events Per Prompt: {cfg.EventsPerPrompt}");
                sb.AppendLine($"Starts Before First: {cfg.StartsBeforeFirstPrompt}");
                sb.AppendLine($"Starts Before Subsequent: {cfg.StartsBeforeSubsequentPrompts}");
                sb.AppendLine($"Cooldown Days: {cfg.RemindLaterCooldownDays}");
            }
            sb.AppendLine("======================================");
            return sb.ToString();
        }

        #endregion

        #region Factory Method

        /// <summary>
        /// Spawns or finds the RateDebugOverlay GameObject in the active scene.
        /// </summary>
        public static RateDebugOverlay CreateOverlay()
        {
            var existing = FindObjectOfType<RateDebugOverlay>();
            if (existing != null) return existing;

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
