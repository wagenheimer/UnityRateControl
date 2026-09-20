using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Wagenheimer.PackageHub.Editor;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Comprehensive UI Toolkit checklist and diagnostic tool that inspects the project
    /// and verifies whether every step of the RateControl integration is configured 100%.
    ///
    /// Checks configuration assets, dialog prefabs, platform store IDs, code scan for API
    /// calls and blockers, live PlayerPrefs state inspection, and interactive testing tools.
    ///
    /// Open via <b>Tools > Wagenheimer > Rate Control > Setup & Checklist...</b>
    /// or <b>Window > Wagenheimer > Rate Control > Setup & Checklist...</b>
    /// </summary>
    internal sealed class SetupChecklistWindow : EditorWindow
    {
        // ── Model ─────────────────────────────────────────────────────────────────

        internal enum CheckStatus
        {
            Pass,
            Warning,
            Fail,
            Manual,
            Info
        }

        internal sealed class CheckResult
        {
            public string Title = "";
            public CheckStatus Status = CheckStatus.Info;
            public string Detail = "";
            public readonly List<string> Facts = new();

            public CheckResult WithFacts(params string[] facts)
            {
                Facts.AddRange(facts);
                return this;
            }

            public string DocsUrl;
            public string ActionLabel;
            public Action Action;
            public string Prompt;
            public Func<VisualElement> CustomControl;
        }

        internal sealed class Section
        {
            public string Title = "";
            public string Subtitle = "";
            public readonly List<CheckResult> Items = new();

            public int AutomatedTotal => Items.Count(i => i.Status == CheckStatus.Pass
                                                        || i.Status == CheckStatus.Warning
                                                        || i.Status == CheckStatus.Fail);
            public int AutomatedPassed => Items.Count(i => i.Status == CheckStatus.Pass);
            public bool HasFail => Items.Any(i => i.Status == CheckStatus.Fail);
            public bool HasWarning => Items.Any(i => i.Status == CheckStatus.Warning);

            public CheckStatus WorstStatus
            {
                get
                {
                    if (HasFail) return CheckStatus.Fail;
                    if (HasWarning) return CheckStatus.Warning;
                    return CheckStatus.Pass;
                }
            }
        }

        // ── Palette ───────────────────────────────────────────────────────────────

        private static readonly Color ColPass   = new(0.298f, 0.686f, 0.314f);
        private static readonly Color ColWarn   = new(1.000f, 0.690f, 0.125f);
        private static readonly Color ColFail   = new(0.898f, 0.282f, 0.302f);
        private static readonly Color ColManual = new(0.620f, 0.620f, 0.620f);
        private static readonly Color ColInfo   = new(0.290f, 0.565f, 0.851f);
        private static readonly Color ColText   = new(0.85f, 0.85f, 0.85f);
        private static readonly Color ColTextDim= new(0.65f, 0.65f, 0.65f);
        private static readonly Color ColRow    = new(1f, 1f, 1f, 0.035f);
        private static readonly Color ColCard   = new(1f, 1f, 1f, 0.055f);

        // ── State ─────────────────────────────────────────────────────────────────

        private readonly List<Section> _sections = new();
        private DateTime _lastRun;
        private ScrollView _scroll;
        private VisualElement _headerHost;
        private VisualElement _bodyHost;

        private RateConfig _activeConfig;
        private string _activeConfigPath;
        private string _packageVersion = "1.7.8";

        // Code scan results
        private bool _initFound;
        private List<string> _initLocations = new();
        private bool _eventsFound;
        private List<string> _eventLocations = new();
        private bool _blockersFound;
        private List<string> _blockerLocations = new();
        private bool _versionProviderFound;
        private List<string> _versionProviderLocations = new();
        private bool _moreGamesFound;
        private List<string> _moreGamesLocations = new();

        // ── Entry Points ──────────────────────────────────────────────────────────

        [MenuItem("Tools/Wagenheimer/Rate Control/Setup & Checklist...", priority = 130)]
        [MenuItem("Window/Wagenheimer/Rate Control/Setup & Checklist", priority = 210)]
        public static void Open()
        {
            var window = GetWindow<SetupChecklistWindow>();
            window.titleContent = new GUIContent("Rate Control Checklist");
            window.minSize = new Vector2(650, 540);
            window.Show();
            window.RunChecks();
        }

        private void OnEnable()
        {
            var pkg = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(SetupChecklistWindow).Assembly);
            if (pkg != null && !string.IsNullOrEmpty(pkg.version))
                _packageVersion = pkg.version;

            RunChecks();
        }

        // ── UI Construction ───────────────────────────────────────────────────────

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;

            _headerHost = new VisualElement();
            root.Add(_headerHost);

            _scroll = new ScrollView(ScrollViewMode.Vertical);
            _scroll.style.flexGrow = 1;
            root.Add(_scroll);

            _bodyHost = new VisualElement();
            _scroll.Add(_bodyHost);

            BuildChrome();
            Rebuild();
        }

        private void BuildChrome()
        {
            _headerHost.Clear();

            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            _headerHost.Add(titleRow);

            var title = new Label("Rate Control — Setup & Checklist");
            title.style.fontSize = 17;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = ColText;
            titleRow.Add(title);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            titleRow.Add(spacer);

            titleRow.Add(ToolbarButton("Setup Guide", () => RateControlDocWindow.Open()));
            titleRow.Add(ToolbarButton("GitHub", () => Application.OpenURL("https://github.com/wagenheimer/UnityRateControl")));
            titleRow.Add(ToolbarButton("Package Hub", () => PackageHubWindow.OpenToPackage("com.wagenheimer.ratecontrol")));
            titleRow.Add(ToolbarButton("Refresh", RunChecks, ColInfo));

            var sub = new Label();
            sub.style.fontSize = 10;
            sub.style.color = ColTextDim;
            sub.style.marginTop = 2;
            sub.name = "subtitle";
            _headerHost.Add(sub);
        }

        private static Button ToolbarButton(string text, Action clicked, Color? accent = null)
        {
            var button = new Button(clicked) { text = text };
            button.style.height = 22;
            button.style.marginLeft = 4;
            button.style.paddingLeft = 10;
            button.style.paddingRight = 10;
            button.style.fontSize = 11;
            if (accent.HasValue)
            {
                button.style.backgroundColor = new Color(accent.Value.r, accent.Value.g, accent.Value.b, 0.35f);
                button.style.color = ColText;
            }
            return button;
        }

        private void Rebuild()
        {
            if (_bodyHost == null) return;

            _bodyHost.Clear();

            int autoTotal = _sections.Sum(s => s.AutomatedTotal);
            int autoPass  = _sections.Sum(s => s.AutomatedPassed);
            int fails     = _sections.SelectMany(s => s.Items).Count(i => i.Status == CheckStatus.Fail);
            int warns     = _sections.SelectMany(s => s.Items).Count(i => i.Status == CheckStatus.Warning);

            _bodyHost.Add(SummaryCard(autoPass, autoTotal, warns, fails));

            foreach (var section in _sections)
                _bodyHost.Add(SectionCard(section));

            _bodyHost.Add(Footer());

            var sub = _headerHost.Q<Label>("subtitle");
            if (sub != null)
            {
                var target = EditorUserBuildSettings.activeBuildTarget.ToString();
                sub.text = $"v{_packageVersion}  |  Active Build Target: {target}  |  Checked {_lastRun:HH:mm:ss}";
            }
        }

        private VisualElement SummaryCard(int pass, int total, int warnings, int failures)
        {
            var status = failures > 0 ? CheckStatus.Fail
                       : warnings > 0 ? CheckStatus.Warning
                       : CheckStatus.Pass;
            var accent = StatusColor(status);

            var card = Card(accent);
            card.style.marginBottom = 10;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            card.Add(row);

            var headline = new Label(failures > 0
                ? $"{failures} issue(s) need attention for 100% operation"
                : warnings > 0
                    ? "Ready to run, with recommendations"
                    : "Rate Control is 100% configured and verified");
            headline.style.fontSize = 14;
            headline.style.unityFontStyleAndWeight = FontStyle.Bold;
            headline.style.color = accent;
            row.Add(headline);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            row.Add(Chip($"{pass}/{total} automated checks", accent));
            if (failures > 0) row.Add(Chip($"{failures} fail", ColFail));
            if (warnings > 0) row.Add(Chip($"{warnings} warn", ColWarn));

            var pending = _sections.SelectMany(s => s.Items)
                .Where(i => i.Status == CheckStatus.Fail || i.Status == CheckStatus.Warning)
                .ToList();
            if (pending.Count > 0)
            {
                var copyAll = new Button(() => CopyPendingPrompts(pending)) { text = "Copy pending as prompt" };
                copyAll.style.height = 20;
                copyAll.style.fontSize = 10;
                copyAll.style.marginLeft = 8;
                row.Add(copyAll);
            }

            var track = new VisualElement();
            track.style.height = 6;
            track.style.marginTop = 8;
            track.style.backgroundColor = new Color(0f, 0f, 0f, 0.35f);
            track.style.borderTopLeftRadius = 3;
            track.style.borderTopRightRadius = 3;
            track.style.borderBottomLeftRadius = 3;
            track.style.borderBottomRightRadius = 3;
            card.Add(track);

            var fill = new VisualElement();
            fill.style.height = 6;
            fill.style.width = Length.Percent(total == 0 ? 0 : Mathf.Round(100f * pass / total));
            fill.style.backgroundColor = accent;
            fill.style.borderTopLeftRadius = 3;
            fill.style.borderTopRightRadius = 3;
            fill.style.borderBottomLeftRadius = 3;
            fill.style.borderBottomRightRadius = 3;
            track.Add(fill);

            return card;
        }

        private VisualElement SectionCard(Section section)
        {
            var accent = section.WorstStatus == CheckStatus.Pass && section.AutomatedTotal == 0
                ? ColManual
                : StatusColor(section.WorstStatus);

            var card = Card(accent);
            var expanded = true;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;
            card.Add(head);

            var chevron = new Label("v");
            chevron.style.width = 14;
            chevron.style.fontSize = 11;
            chevron.style.color = accent;
            head.Add(chevron);

            var name = new Label(section.Title);
            name.style.fontSize = 12.5f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.color = ColText;
            head.Add(name);

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            head.Add(spacer);

            var counter = $"{section.AutomatedPassed}/{section.AutomatedTotal}";
            if (section.AutomatedTotal == 0)
                counter = section.Items.Any(i => i.Status != CheckStatus.Manual) ? "info" : "tools";
            head.Add(Chip(counter, accent));

            var body = new VisualElement();
            body.style.marginTop = 6;
            card.Add(body);

            if (!string.IsNullOrEmpty(section.Subtitle))
            {
                var note = new Label(section.Subtitle);
                note.style.fontSize = 10;
                note.style.color = ColTextDim;
                note.style.whiteSpace = WhiteSpace.Normal;
                note.style.marginBottom = 4;
                body.Add(note);
            }

            foreach (var item in section.Items)
                body.Add(Row(item));

            head.RegisterCallback<ClickEvent>(_ =>
            {
                expanded = !expanded;
                body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;
                chevron.text = expanded ? "v" : ">";
            });

            return card;
        }

        private VisualElement Row(CheckResult item)
        {
            var accent = StatusColor(item.Status);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.backgroundColor = ColRow;
            row.style.marginBottom = 3;
            row.style.paddingTop = 6;
            row.style.paddingBottom = 6;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.borderLeftWidth = 3;
            row.style.borderLeftColor = accent;
            row.style.borderTopLeftRadius = 3;
            row.style.borderBottomLeftRadius = 3;

            var glyph = new Label(Glyph(item.Status));
            glyph.style.width = 18;
            glyph.style.fontSize = 12;
            glyph.style.unityFontStyleAndWeight = FontStyle.Bold;
            glyph.style.color = accent;
            row.Add(glyph);

            var column = new VisualElement();
            column.style.flexGrow = 1;
            column.style.flexShrink = 1;
            row.Add(column);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.alignItems = Align.Center;
            column.Add(head);

            var title = new Label(item.Title);
            title.style.fontSize = 11.5f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = ColText;
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.flexShrink = 1;
            head.Add(title);

            if (!string.IsNullOrEmpty(item.Detail))
            {
                var detail = new Label(item.Detail);
                detail.style.fontSize = 10.5f;
                detail.style.color = ColTextDim;
                detail.style.whiteSpace = WhiteSpace.Normal;
                detail.style.marginTop = 2;
                column.Add(detail);
            }

            if (item.CustomControl != null)
            {
                var custom = item.CustomControl();
                if (custom != null)
                {
                    custom.style.marginTop = 6;
                    column.Add(custom);
                }
            }

            if (item.Facts.Count > 0)
            {
                var facts = new VisualElement();
                facts.style.marginTop = 4;
                facts.style.display = DisplayStyle.None;
                foreach (var fact in item.Facts)
                {
                    var line = new Label("• " + fact);
                    line.style.fontSize = 10;
                    line.style.color = ColTextDim;
                    line.style.whiteSpace = WhiteSpace.Normal;
                    facts.Add(line);
                }
                column.Add(facts);

                var visible = false;
                var toggle = new Button(() =>
                {
                    visible = !visible;
                    facts.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                })
                { text = $"details ({item.Facts.Count})" };
                toggle.style.fontSize = 9;
                toggle.style.height = 15;
                toggle.style.marginTop = 3;
                toggle.style.alignSelf = Align.FlexStart;
                column.Add(toggle);
            }

            if (!string.IsNullOrEmpty(item.ActionLabel) && item.Action != null)
            {
                var actionBtn = new Button(item.Action) { text = item.ActionLabel };
                actionBtn.style.height = 19;
                actionBtn.style.fontSize = 9.5f;
                actionBtn.style.marginLeft = 6;
                actionBtn.style.paddingLeft = 8;
                actionBtn.style.paddingRight = 8;
                actionBtn.style.alignSelf = Align.FlexStart;
                row.Add(actionBtn);
            }

            if (!string.IsNullOrEmpty(item.DocsUrl))
            {
                var docs = new Button(() => Application.OpenURL(item.DocsUrl)) { text = "Docs" };
                docs.style.width = 44;
                docs.style.height = 19;
                docs.style.fontSize = 9.5f;
                docs.style.marginLeft = 4;
                docs.style.alignSelf = Align.FlexStart;
                row.Add(docs);
            }

            if (!string.IsNullOrEmpty(item.Prompt))
            {
                var promptBtn = new Button(() => CopyPrompt(item.Prompt)) { text = "Copy prompt" };
                promptBtn.style.height = 19;
                promptBtn.style.fontSize = 9.5f;
                promptBtn.style.marginLeft = 4;
                promptBtn.style.alignSelf = Align.FlexStart;
                row.Add(promptBtn);
            }

            return row;
        }

        private static VisualElement Card(Color accent)
        {
            var card = new VisualElement();
            card.style.backgroundColor = ColCard;
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 10;
            card.style.paddingRight = 10;
            card.style.marginBottom = 6;
            card.style.borderLeftWidth = 3;
            card.style.borderLeftColor = accent;
            return card;
        }

        private static Label Chip(string text, Color accent)
        {
            var chip = new Label(text);
            chip.style.fontSize = 10;
            chip.style.color = ColText;
            chip.style.backgroundColor = new Color(accent.r, accent.g, accent.b, 0.28f);
            chip.style.borderTopLeftRadius = 3;
            chip.style.borderTopRightRadius = 3;
            chip.style.borderBottomLeftRadius = 3;
            chip.style.borderBottomRightRadius = 3;
            chip.style.paddingLeft = 6;
            chip.style.paddingRight = 6;
            chip.style.paddingTop = 2;
            chip.style.paddingBottom = 2;
            chip.style.marginLeft = 4;
            return chip;
        }

        private static VisualElement Footer()
        {
            var box = new VisualElement();
            box.style.marginTop = 12;
            box.style.paddingTop = 8;
            box.style.paddingBottom = 8;
            box.style.alignItems = Align.Center;

            var label = new Label("Wagenheimer UnityRateControl  •  Automated Diagnostics & Quality Gates");
            label.style.fontSize = 10;
            label.style.color = ColTextDim;
            box.Add(label);

            return box;
        }

        private static Color StatusColor(CheckStatus status) => status switch
        {
            CheckStatus.Pass => ColPass,
            CheckStatus.Warning => ColWarn,
            CheckStatus.Fail => ColFail,
            CheckStatus.Manual => ColManual,
            CheckStatus.Info => ColInfo,
            _ => ColInfo
        };

        private static string Glyph(CheckStatus status) => status switch
        {
            CheckStatus.Pass => "✓",
            CheckStatus.Warning => "!",
            CheckStatus.Fail => "✗",
            CheckStatus.Manual => "○",
            CheckStatus.Info => "i",
            _ => "•"
        };

        private static void CopyPrompt(string prompt)
        {
            EditorGUIUtility.systemCopyBuffer = prompt;
            Debug.Log("[RateControl Checklist] Prompt copied to clipboard:\n" + prompt);
        }

        private static void CopyPendingPrompts(IEnumerable<CheckResult> pending)
        {
            var lines = new List<string>
            {
                "Please fix the following issues in the Unity RateControl integration:"
            };
            int count = 1;
            foreach (var item in pending)
            {
                lines.Add($"\n{count++}. {item.Title}");
                if (!string.IsNullOrEmpty(item.Detail)) lines.Add($"   Detail: {item.Detail}");
                if (item.Facts.Count > 0)
                {
                    lines.Add("   Facts:");
                    foreach (var f in item.Facts) lines.Add($"   - {f}");
                }
                if (!string.IsNullOrEmpty(item.Prompt)) lines.Add($"   Action needed: {item.Prompt}");
            }

            var text = string.Join("\n", lines);
            EditorGUIUtility.systemCopyBuffer = text;
            Debug.Log("[RateControl Checklist] All pending items copied to clipboard as an AI prompt.");
        }

        // ── Check Runner ──────────────────────────────────────────────────────────

        private void RunChecks()
        {
            _lastRun = DateTime.Now;
            _sections.Clear();

            FindActiveConfig();
            ScanProjectScripts();

            _sections.Add(BuildSection1Config());
            _sections.Add(BuildSection2Dialog());
            _sections.Add(BuildSection3Stores());
            _sections.Add(BuildSection4CodeIntegration());
            _sections.Add(BuildSection5LiveTesting());
            _sections.Add(BuildSection6ReleaseReadiness());

            Rebuild();
        }

        private void FindActiveConfig()
        {
            _activeConfig = null;
            _activeConfigPath = null;

            var guids = AssetDatabase.FindAssets("t:RateConfig");
            if (guids.Length > 0)
            {
                _activeConfigPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _activeConfig = AssetDatabase.LoadAssetAtPath<RateConfig>(_activeConfigPath);
            }
        }

        private void ScanProjectScripts()
        {
            _initLocations.Clear();
            _eventLocations.Clear();
            _blockerLocations.Clear();
            _versionProviderLocations.Clear();
            _moreGamesLocations.Clear();

            var scriptGuids = AssetDatabase.FindAssets("t:MonoScript");
            foreach (var guid in scriptGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                // Skip package itself and test folders
                if (path.StartsWith("Packages/com.wagenheimer.ratecontrol/") ||
                    path.Contains("/Tests/") || path.EndsWith("Test.cs"))
                    continue;

                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) continue;

                try
                {
                    var text = File.ReadAllText(path);

                    if (Regex.IsMatch(text, @"RateControl\s*\.\s*(Initialize|Init)\b"))
                        _initLocations.Add(path);

                    if (Regex.IsMatch(text, @"RateControl\s*\.\s*(LogEvent|Event)\b"))
                        _eventLocations.Add(path);

                    if (Regex.IsMatch(text, @":\s*([a-zA-Z0-9_.,\s]*\b)?IRateBlocker\b"))
                        _blockerLocations.Add(path);

                    if (Regex.IsMatch(text, @":\s*([a-zA-Z0-9_.,\s]*\b)?IRateVersionProvider\b"))
                        _versionProviderLocations.Add(path);

                    if (Regex.IsMatch(text, @"RateControl\s*\.\s*ShowMoreGames\b"))
                        _moreGamesLocations.Add(path);
                }
                catch
                {
                    // Ignore unreadable files
                }
            }

            _initFound = _initLocations.Count > 0;
            _eventsFound = _eventLocations.Count > 0;
            _blockersFound = _blockerLocations.Count > 0;
            _versionProviderFound = _versionProviderLocations.Count > 0;
            _moreGamesFound = _moreGamesLocations.Count > 0;
        }

        // ── Section 1: Configuration Asset ────────────────────────────────────────

        private Section BuildSection1Config()
        {
            var sec = new Section
            {
                Title = "1. Configuration Asset (RateConfig)",
                Subtitle = "A RateConfig asset holds thresholds, storage keys, and store settings."
            };

            // Check 1: Asset exists
            if (_activeConfig != null)
            {
                var check = new CheckResult
                {
                    Title = "RateConfig asset found",
                    Status = CheckStatus.Pass,
                    Detail = $"Located at: {_activeConfigPath}",
                    ActionLabel = "Select Asset",
                    Action = () =>
                    {
                        Selection.activeObject = _activeConfig;
                        EditorGUIUtility.PingObject(_activeConfig);
                    }
                };
                check.WithFacts(
                    $"Asset path: {_activeConfigPath}",
                    $"StorageKeyPrefix: \"{_activeConfig.StorageKeyPrefix}\"",
                    $"EventsPerPrompt: {_activeConfig.EventsPerPrompt}",
                    $"StartsBeforeFirstPrompt: {_activeConfig.StartsBeforeFirstPrompt}",
                    $"RemindLaterCooldownDays: {_activeConfig.RemindLaterCooldownDays} day(s)");
                sec.Items.Add(check);
            }
            else
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "RateConfig asset missing",
                    Status = CheckStatus.Fail,
                    Detail = "No RateConfig asset found in project. RateControl requires a configuration asset.",
                    ActionLabel = "Create RateConfig",
                    Action = () =>
                    {
                        var path = EditorUtility.SaveFilePanelInProject("Save Rate Config", "RateConfig", "asset", "Choose where to save RateConfig");
                        if (!string.IsNullOrEmpty(path))
                        {
                            var asset = ScriptableObject.CreateInstance<RateConfig>();
                            AssetDatabase.CreateAsset(asset, path);
                            AssetDatabase.SaveAssets();
                            Selection.activeObject = asset;
                            EditorGUIUtility.PingObject(asset);
                            RunChecks();
                        }
                    },
                    Prompt = "Create a new RateConfig asset under Assets/_Game/Config/ (or project config folder) and assign it to the game bootstrap."
                });
            }

            // Check 2: Storage Key Prefix
            if (_activeConfig != null)
            {
                var prefix = _activeConfig.StorageKeyPrefix;
                if (string.IsNullOrWhiteSpace(prefix) || prefix == "RateControl")
                {
                    sec.Items.Add(new CheckResult
                    {
                        Title = "StorageKeyPrefix is default or empty",
                        Status = CheckStatus.Warning,
                        Detail = "Using the default 'RateControl' prefix risks PlayerPrefs collisions with other games from the studio on the same user device.",
                        ActionLabel = "Select Config",
                        Action = () => { Selection.activeObject = _activeConfig; EditorGUIUtility.PingObject(_activeConfig); },
                        Prompt = "Set RateConfig.StorageKeyPrefix to a unique identifier for this game, e.g., 'Studio.GameName.Rate'."
                    }.WithFacts("Current prefix: \"" + prefix + "\"", "Recommended format: Studio.GameName.Rate"));
                }
                else
                {
                    sec.Items.Add(new CheckResult
                    {
                        Title = $"StorageKeyPrefix is unique (\"{prefix}\")",
                        Status = CheckStatus.Pass,
                        Detail = "Namespace avoids PlayerPrefs collision with other studio titles."
                    });
                }

                // Check 3: Thresholds
                if (_activeConfig.EventsPerPrompt < 1 || _activeConfig.StartsBeforeFirstPrompt < 1)
                {
                    sec.Items.Add(new CheckResult
                    {
                        Title = "Threshold values too aggressive",
                        Status = CheckStatus.Warning,
                        Detail = "EventsPerPrompt or StartsBeforeFirstPrompt is less than 1. Prompts will appear prematurely.",
                        ActionLabel = "Fix Thresholds",
                        Action = () =>
                        {
                            _activeConfig.EventsPerPrompt = Mathf.Max(1, _activeConfig.EventsPerPrompt);
                            _activeConfig.StartsBeforeFirstPrompt = Mathf.Max(1, _activeConfig.StartsBeforeFirstPrompt);
                            EditorUtility.SetDirty(_activeConfig);
                            AssetDatabase.SaveAssets();
                            RunChecks();
                        }
                    });
                }
                else
                {
                    sec.Items.Add(new CheckResult
                    {
                        Title = "Trigger thresholds are balanced",
                        Status = CheckStatus.Pass,
                        Detail = $"First prompt after {_activeConfig.StartsBeforeFirstPrompt} launches and {_activeConfig.EventsPerPrompt} events; " +
                                 $"cooldown is {_activeConfig.RemindLaterCooldownDays} days."
                    });
                }
            }

            return sec;
        }

        // ── Section 2: Dialog & UI Integration ────────────────────────────────────

        private Section BuildSection2Dialog()
        {
            var sec = new Section
            {
                Title = "2. Dialog Prefab & UI Setup",
                Subtitle = "Verifies the rating dialog popup prefab and its button components."
            };

            GameObject prefabGo = null;
            string sourceDesc = "";

            if (_activeConfig != null && _activeConfig.DialogPrefab != null)
            {
                prefabGo = _activeConfig.DialogPrefab.gameObject;
                sourceDesc = "Assigned in RateConfig.DialogPrefab";
            }
            else if (_activeConfig != null && !string.IsNullOrEmpty(_activeConfig.DialogResourcePath))
            {
                var loaded = Resources.Load<GameObject>(_activeConfig.DialogResourcePath);
                if (loaded != null)
                {
                    prefabGo = loaded;
                    sourceDesc = $"Loaded via Resources/{_activeConfig.DialogResourcePath}";
                }
            }

            if (prefabGo != null)
            {
                var dialogComponent = prefabGo.GetComponent<RateDialog>();
                var assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabGo);
                if (string.IsNullOrEmpty(assetPath)) assetPath = AssetDatabase.GetAssetPath(prefabGo);

                var check = new CheckResult
                {
                    Title = "Dialog prefab verified",
                    Status = CheckStatus.Pass,
                    Detail = $"{sourceDesc} ({prefabGo.name})",
                    ActionLabel = "Ping Prefab",
                    Action = () =>
                    {
                        Selection.activeObject = prefabGo;
                        EditorGUIUtility.PingObject(prefabGo);
                    }
                };
                check.WithFacts(
                    $"Prefab name: {prefabGo.name}",
                    $"Asset path: {assetPath}",
                    $"Component: {(dialogComponent != null ? dialogComponent.GetType().Name : "Missing RateDialog component!")}");

                if (dialogComponent == null)
                {
                    check.Status = CheckStatus.Fail;
                    check.Detail = "Prefab exists but does NOT have a component inheriting from RateDialog!";
                    check.Prompt = $"Add a DefaultRateDialog (or custom RateDialog subclass) component to {prefabGo.name} and wire the Rate Now, Remind Later, and No Thanks buttons.";
                }

                sec.Items.Add(check);
            }
            else
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "Dialog prefab not found",
                    Status = CheckStatus.Fail,
                    Detail = "Neither DialogPrefab nor a valid Resources/ path is set in RateConfig.",
                    ActionLabel = "Create Default Prefab",
                    Action = () =>
                    {
                        EditorApplication.ExecuteMenuItem("Tools/Wagenheimer/Rate Control/Create Default Prefab");
                    },
                    Prompt = "Generate the default dialog prefab via Tools > Wagenheimer > Rate Control > Create Default Prefab and assign it to RateConfig.DialogPrefab."
                });
            }

            return sec;
        }

        // ── Section 3: Platform Store & Distribution Setup ────────────────────────

        private Section BuildSection3Stores()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var sec = new Section
            {
                Title = "3. Store IDs & URLs by Platform",
                Subtitle = $"Validates review links and More Games targets (Active Target: {target})."
            };

            if (_activeConfig == null)
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "RateConfig not found",
                    Status = CheckStatus.Fail,
                    Detail = "Create RateConfig first to configure store targets."
                });
                return sec;
            }

            // Android
            var androidId = _activeConfig.ResolvedAndroidId;
            var isAndroidActive = target == BuildTarget.Android;
            var androidUrl = $"https://play.google.com/store/apps/details?id={androidId}";
            var androidStatus = string.IsNullOrEmpty(androidId) ? CheckStatus.Fail : CheckStatus.Pass;

            var androidCheck = new CheckResult
            {
                Title = $"Android Google Play Review {(isAndroidActive ? "[Active Target]" : "")}",
                Status = androidStatus,
                Detail = $"Package ID: {androidId}",
                ActionLabel = "Test Store URL",
                Action = () => Application.OpenURL(androidUrl)
            };
            androidCheck.WithFacts(
                $"Package identifier: {androidId}",
                $"Web preview URL: {androidUrl}",
                $"In-App Review API: {(Type.GetType("Google.Play.Review.ReviewManager, Google.Play.Review") != null ? "Supported (package found)" : "Standard market:// fallback")}");
            sec.Items.Add(androidCheck);

            // iOS
            var isIosActive = target == BuildTarget.iOS;
            var iosId = _activeConfig.iOSAppId;
            var hasIosId = !string.IsNullOrWhiteSpace(iosId);
            var iosStatus = hasIosId ? CheckStatus.Pass : (isIosActive ? CheckStatus.Fail : CheckStatus.Warning);
            var iosUrl = hasIosId ? $"https://apps.apple.com/app/id{iosId}" : "";

            var iosCheck = new CheckResult
            {
                Title = $"iOS App Store {(isIosActive ? "[Active Target]" : "")}",
                Status = iosStatus,
                Detail = hasIosId ? $"Apple App ID: {iosId}" : "iOSAppId is empty — SKStoreReviewManager fallback URL will fail!",
                ActionLabel = hasIosId ? "Test Store URL" : "Set in Config",
                Action = () =>
                {
                    if (hasIosId) Application.OpenURL(iosUrl);
                    else { Selection.activeObject = _activeConfig; EditorGUIUtility.PingObject(_activeConfig); }
                }
            };
            if (hasIosId) iosCheck.WithFacts($"App ID: {iosId}", $"Web preview URL: {iosUrl}");
            else iosCheck.Prompt = "In RateConfig, set iOSAppId with the numeric Apple ID from App Store Connect.";
            sec.Items.Add(iosCheck);

            // Desktop Standalones
            // Windows
            var isWinActive = target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64;
            var winChannel = _activeConfig.Windows;
            var winStatus = winChannel == StandaloneChannel.None ? CheckStatus.Info
                          : string.IsNullOrEmpty(_activeConfig.SteamAppId) ? (isWinActive ? CheckStatus.Fail : CheckStatus.Warning)
                          : CheckStatus.Pass;

            var winCheck = new CheckResult
            {
                Title = $"Windows Standalone {(isWinActive ? "[Active Target]" : "")}",
                Status = winStatus,
                Detail = winChannel == StandaloneChannel.None
                    ? "Channel is None (Rating disabled on Windows standalone)"
                    : $"Channel: Steam (App ID: {_activeConfig.SteamAppId})",
                ActionLabel = (winChannel == StandaloneChannel.Steam && !string.IsNullOrEmpty(_activeConfig.SteamAppId)) ? "Test Steam URL" : null,
                Action = () => Application.OpenURL(_activeConfig.ResolvedSteamUrl)
            };
            if (winChannel == StandaloneChannel.Steam)
                winCheck.WithFacts($"Steam App ID: {_activeConfig.SteamAppId}", $"URL: {_activeConfig.ResolvedSteamUrl}");
            sec.Items.Add(winCheck);

            // macOS
            var isMacActive = target == BuildTarget.StandaloneOSX;
            var macChannel = _activeConfig.MacOs;
            var macStatus = macChannel == MacOsChannel.None ? CheckStatus.Info
                          : macChannel == MacOsChannel.MacAppStore && string.IsNullOrEmpty(_activeConfig.MacAppStoreId) ? (isMacActive ? CheckStatus.Fail : CheckStatus.Warning)
                          : macChannel == MacOsChannel.Steam && string.IsNullOrEmpty(_activeConfig.SteamAppId) ? (isMacActive ? CheckStatus.Fail : CheckStatus.Warning)
                          : macChannel == MacOsChannel.MacGameStore && string.IsNullOrEmpty(_activeConfig.MacGameStoreUrl) ? (isMacActive ? CheckStatus.Fail : CheckStatus.Warning)
                          : CheckStatus.Pass;

            var macCheck = new CheckResult
            {
                Title = $"macOS Standalone {(isMacActive ? "[Active Target]" : "")}",
                Status = macStatus,
                Detail = macChannel switch
                {
                    MacOsChannel.MacAppStore => $"Mac App Store (ID: {_activeConfig.MacAppStoreId})",
                    MacOsChannel.Steam => $"Steam (App ID: {_activeConfig.SteamAppId})",
                    MacOsChannel.MacGameStore => $"MacGameStore (URL: {_activeConfig.ResolvedMacGameStoreUrl})",
                    _ => "Channel is None (Rating disabled on macOS)"
                },
                ActionLabel = macChannel switch
                {
                    MacOsChannel.MacAppStore when !string.IsNullOrEmpty(_activeConfig.MacAppStoreId) => "Test MAS URL",
                    MacOsChannel.Steam when !string.IsNullOrEmpty(_activeConfig.SteamAppId) => "Test Steam URL",
                    MacOsChannel.MacGameStore when !string.IsNullOrEmpty(_activeConfig.MacGameStoreUrl) => "Test MGS URL",
                    _ => null
                },
                Action = () =>
                {
                    if (macChannel == MacOsChannel.MacAppStore) Application.OpenURL($"macappstore://apps.apple.com/app/id{_activeConfig.MacAppStoreId}?action=write-review");
                    else if (macChannel == MacOsChannel.Steam) Application.OpenURL(_activeConfig.ResolvedSteamUrl);
                    else if (macChannel == MacOsChannel.MacGameStore) Application.OpenURL(_activeConfig.ResolvedMacGameStoreUrl);
                }
            };
            if (macChannel == MacOsChannel.MacGameStore)
                macCheck.WithFacts($"MacGameStore URL: {_activeConfig.ResolvedMacGameStoreUrl}");
            sec.Items.Add(macCheck);

            // More Games
            var hasMoreGames = !string.IsNullOrEmpty(_activeConfig.MoreGamesGoogleDeveloperName) ||
                               !string.IsNullOrEmpty(_activeConfig.MoreGamesAppleDeveloperId) ||
                               !string.IsNullOrEmpty(_activeConfig.MoreGamesSteamDeveloperSlug) ||
                               !string.IsNullOrEmpty(_activeConfig.MoreGamesMacGameStoreUrl) ||
                               !string.IsNullOrEmpty(_activeConfig.MoreGamesUrl);

            var moreGamesCheck = new CheckResult
            {
                Title = "More Games Catalog Links",
                Status = hasMoreGames ? CheckStatus.Pass : CheckStatus.Info,
                Detail = hasMoreGames ? "Developer/publisher profile URLs configured." : "Optional: Configure developer links for cross-promotion."
            };
            if (!string.IsNullOrEmpty(_activeConfig.MoreGamesUrl))
                moreGamesCheck.WithFacts($"Fallback URL: {_activeConfig.MoreGamesUrl}");
            if (!string.IsNullOrEmpty(_activeConfig.MoreGamesGoogleDeveloperName))
                moreGamesCheck.WithFacts($"Google Pub: {_activeConfig.MoreGamesGoogleDeveloperName}");
            if (!string.IsNullOrEmpty(_activeConfig.MoreGamesAppleDeveloperId))
                moreGamesCheck.WithFacts($"Apple Dev ID: {_activeConfig.MoreGamesAppleDeveloperId}");
            if (!string.IsNullOrEmpty(_activeConfig.MoreGamesSteamDeveloperSlug))
                moreGamesCheck.WithFacts($"Steam Dev Slug: {_activeConfig.MoreGamesSteamDeveloperSlug}");
            if (!string.IsNullOrEmpty(_activeConfig.MoreGamesMacGameStoreUrl))
                moreGamesCheck.WithFacts($"MacGameStore URL: {_activeConfig.MoreGamesMacGameStoreUrl}");

            sec.Items.Add(moreGamesCheck);

            return sec;
        }

        // ── Section 4: Code Integration Scanner ───────────────────────────────────

        private Section BuildSection4CodeIntegration()
        {
            var sec = new Section
            {
                Title = "4. Project Code Scanner (Runtime Integration)",
                Subtitle = "Scans project scripts for initialization, event logging, and blockers."
            };

            // Init check
            if (_initFound)
            {
                var check = new CheckResult
                {
                    Title = "RateControl.Initialize() call detected",
                    Status = CheckStatus.Pass,
                    Detail = $"Found in {_initLocations.Count} script(s)."
                };
                foreach (var loc in _initLocations) check.WithFacts(loc);
                check.ActionLabel = "Open Script";
                check.Action = () => OpenFirstScript(_initLocations);
                sec.Items.Add(check);
            }
            else if (_activeConfig != null && _activeConfig.AutoInitialize)
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "Auto-Initialize on Boot enabled in RateConfig",
                    Status = CheckStatus.Pass,
                    Detail = "RateControl will automatically initialize itself on game boot (AfterSceneLoad) using Resources/RateConfig."
                }.WithFacts("AutoInitialize = true", "Resources.Load<RateConfig>(\"RateConfig\")"));
            }
            else
            {
                var check = new CheckResult
                {
                    Title = "RateControl.Initialize() not found in scripts",
                    Status = CheckStatus.Fail,
                    Detail = "RateControl must be initialized from your game startup (e.g. Main.cs or GameManager.cs), or enable AutoInitialize in RateConfig.",
                    Prompt = "In your Main.cs or GameManager.cs Awake(), add: RateControl.Initialize(blocker: this); RateControl.LogStart();",
                    ActionLabel = "Copy Main.cs Snippet",
                    Action = () =>
                    {
                        EditorGUIUtility.systemCopyBuffer =
                            "// Add to your Main.cs or GameManager.cs:\n" +
                            "using UnityEngine;\n" +
                            "using Wagenheimer.RateControl;\n\n" +
                            "public class Main : MonoBehaviour, IRateBlocker\n" +
                            "{\n" +
                            "    private void Awake()\n" +
                            "    {\n" +
                            "        // Initializes RateControl automatically using Resources/RateConfig\n" +
                            "        RateControl.Initialize(blocker: this);\n" +
                            "        RateControl.LogStart();\n" +
                            "    }\n\n" +
                            "    public bool CanShowRate() => true; // Add condition (e.g. !isModalOpen)\n" +
                            "}\n";
                        EditorUtility.DisplayDialog("Snippet Copied", "Main.cs integration snippet copied to clipboard! Paste it into your bootstrap or main manager script.", "OK");
                    }
                };
                check.WithFacts(
                    "Option A: Add RateControl.Initialize(blocker: this) to Main.cs Awake()",
                    "Option B: Enable 'AutoInitialize' in RateConfig for zero-code boot");
                sec.Items.Add(check);
            }

            // LogEvent check
            if (_eventsFound)
            {
                var check = new CheckResult
                {
                    Title = "RateControl.LogEvent() milestone calls detected",
                    Status = CheckStatus.Pass,
                    Detail = $"Found in {_eventLocations.Count} script(s)."
                };
                foreach (var loc in _eventLocations) check.WithFacts(loc);
                check.ActionLabel = "Open Script";
                check.Action = () => OpenFirstScript(_eventLocations);
                sec.Items.Add(check);
            }
            else
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "No RateControl.LogEvent() milestone triggers found",
                    Status = CheckStatus.Warning,
                    Detail = "Without LogEvent() calls (e.g. after level complete or stage win), the rating prompt will never be queued.",
                    Prompt = "Add RateControl.LogEvent() at milestone moments such as completing a level, winning a match, or finishing a chapter."
                });
            }

            // IRateBlocker check
            if (_blockersFound)
            {
                var check = new CheckResult
                {
                    Title = "Custom IRateBlocker implementation detected",
                    Status = CheckStatus.Pass,
                    Detail = $"Found in {_blockerLocations.Count} script(s)."
                };
                foreach (var loc in _blockerLocations) check.WithFacts(loc);
                sec.Items.Add(check);
            }
            else
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "No IRateBlocker detected (Recommended)",
                    Status = CheckStatus.Info,
                    Detail = "Implementing IRateBlocker prevents prompts from popping during tense gameplay, tutorials, or active ads.",
                    Prompt = "Implement IRateBlocker on your game manager: public bool CanShowRatePrompt => !IsPlayingCutscene && !IsAdShowing;"
                });
            }

            // IRateVersionProvider check
            if (_versionProviderFound)
            {
                var check = new CheckResult
                {
                    Title = "Custom IRateVersionProvider detected",
                    Status = CheckStatus.Pass,
                    Detail = $"Found in {_versionProviderLocations.Count} script(s)."
                };
                foreach (var loc in _versionProviderLocations) check.WithFacts(loc);
                sec.Items.Add(check);
            }
            else
            {
                sec.Items.Add(new CheckResult
                {
                    Title = "Standard Application.version used",
                    Status = CheckStatus.Info,
                    Detail = "Version tracking uses Application.version (default behavior)."
                });
            }

            return sec;
        }

        // ── Section 5: Interactive Testing & Live State ───────────────────────────

        private Section BuildSection5LiveTesting()
        {
            var sec = new Section
            {
                Title = "5. Live Testing & Simulation Tools",
                Subtitle = "Inspect saved PlayerPrefs state and simulate rating events in real time."
            };

            var prefix = _activeConfig != null ? _activeConfig.StorageKeyPrefix : "RateControl";

            int eventCount = PlayerPrefs.GetInt($"{prefix}.EventCount", 0);
            int startCount = PlayerPrefs.GetInt($"{prefix}.StartCount", 0);
            bool dontAsk = PlayerPrefs.GetInt($"{prefix}.DontAsk", 0) == 1;
            int showCount = PlayerPrefs.GetInt($"{prefix}.ShowCount", 0);
            string lastVer = PlayerPrefs.GetString($"{prefix}.LastVersion", "(none)");
            string remindUntil = PlayerPrefs.GetString($"{prefix}.RemindLaterUntil", "(none)");

            var stateCheck = new CheckResult
            {
                Title = "Current Saved State (PlayerPrefs)",
                Status = CheckStatus.Info,
                Detail = $"Prefix: \"{prefix}\"  |  Events: {eventCount}  |  Starts: {startCount}  |  ShowCount: {showCount}  |  DontAsk: {dontAsk}"
            };
            stateCheck.WithFacts(
                $"EventCount: {eventCount} / {(_activeConfig != null ? _activeConfig.EventsPerPrompt : 10)}",
                $"StartCount: {startCount} / {(_activeConfig != null ? _activeConfig.StartsBeforeFirstPrompt : 3)}",
                $"ShowCount: {showCount}",
                $"DontAsk (rated/declined): {dontAsk}",
                $"RemindLaterUntil: {remindUntil}",
                $"LastVersionRated: {lastVer}");
            sec.Items.Add(stateCheck);

            // Simulation actions
            var toolsCheck = new CheckResult
            {
                Title = "Simulation & Trigger Actions",
                Status = CheckStatus.Manual,
                Detail = "Use these actions to test the dialog flow without waiting days.",
                CustomControl = () =>
                {
                    var box = new VisualElement();
                    box.style.flexDirection = FlexDirection.Row;
                    box.style.flexWrap = Wrap.Wrap;

                    var btnEvent = new Button(() =>
                    {
                        if (Application.isPlaying)
                        {
                            RateControl.LogEvent();
                            Debug.Log("[RateControl Checklist] Triggered RateControl.LogEvent() in Play Mode.");
                        }
                        else
                        {
                            var cur = PlayerPrefs.GetInt($"{prefix}.EventCount", 0);
                            PlayerPrefs.SetInt($"{prefix}.EventCount", cur + 1);
                            PlayerPrefs.Save();
                            Debug.Log($"[RateControl Checklist] Incremented EventCount to {cur + 1}.");
                        }
                        RunChecks();
                    })
                    { text = "+1 Milestone Event" };
                    btnEvent.style.height = 22;
                    btnEvent.style.marginRight = 6;
                    btnEvent.style.marginBottom = 4;
                    box.Add(btnEvent);

                    var btnStart = new Button(() =>
                    {
                        var cur = PlayerPrefs.GetInt($"{prefix}.StartCount", 0);
                        PlayerPrefs.SetInt($"{prefix}.StartCount", cur + 1);
                        PlayerPrefs.Save();
                        Debug.Log($"[RateControl Checklist] Incremented StartCount to {cur + 1}.");
                        RunChecks();
                    })
                    { text = "+1 App Start" };
                    btnStart.style.height = 22;
                    btnStart.style.marginRight = 6;
                    btnStart.style.marginBottom = 4;
                    box.Add(btnStart);

                    var btnClearCooldown = new Button(() =>
                    {
                        PlayerPrefs.DeleteKey($"{prefix}.RemindLaterUntil");
                        PlayerPrefs.Save();
                        Debug.Log("[RateControl Checklist] Cleared RemindLaterUntil cooldown.");
                        RunChecks();
                    })
                    { text = "Clear Cooldown" };
                    btnClearCooldown.style.height = 22;
                    btnClearCooldown.style.marginRight = 6;
                    btnClearCooldown.style.marginBottom = 4;
                    box.Add(btnClearCooldown);

                    var btnReset = new Button(() =>
                    {
                        if (EditorUtility.DisplayDialog("Reset RateControl State",
                            $"Are you sure you want to clear all PlayerPrefs for prefix \"{prefix}\"?", "Reset", "Cancel"))
                        {
                            PlayerPrefs.DeleteKey($"{prefix}.EventCount");
                            PlayerPrefs.DeleteKey($"{prefix}.StartCount");
                            PlayerPrefs.DeleteKey($"{prefix}.DontAsk");
                            PlayerPrefs.DeleteKey($"{prefix}.ShowCount");
                            PlayerPrefs.DeleteKey($"{prefix}.LastVersion");
                            PlayerPrefs.DeleteKey($"{prefix}.RemindLaterUntil");
                            PlayerPrefs.Save();
                            if (Application.isPlaying && RateControl.Instance != null)
                                RateControl.ResetAll();
                            Debug.Log($"[RateControl Checklist] Reset all saved state for prefix '{prefix}'.");
                            RunChecks();
                        }
                    })
                    { text = "Reset Saved State" };
                    btnReset.style.height = 22;
                    btnReset.style.marginRight = 6;
                    btnReset.style.marginBottom = 4;
                    btnReset.style.backgroundColor = new Color(ColFail.r, ColFail.g, ColFail.b, 0.25f);
                    box.Add(btnReset);

                    if (Application.isPlaying)
                    {
                        var btnForcePrompt = new Button(() =>
                        {
                            if (RateControl.Instance != null)
                            {
                                RateControl.Instance.ForceShowPrompt();
                                Debug.Log("[RateControl Checklist] Force-show prompt invoked in Play Mode.");
                            }
                            else
                            {
                                Debug.LogWarning("[RateControl Checklist] RateControl.Instance is null. Has Initialize() been called?");
                            }
                        })
                        { text = "Force Show Prompt (Play Mode)" };
                        btnForcePrompt.style.height = 22;
                        btnForcePrompt.style.marginRight = 6;
                        btnForcePrompt.style.marginBottom = 4;
                        btnForcePrompt.style.backgroundColor = new Color(ColPass.r, ColPass.g, ColPass.b, 0.35f);
                        box.Add(btnForcePrompt);
                    }

                    return box;
                }
            };
            sec.Items.Add(toolsCheck);

            return sec;
        }

        // ── Section 6: Release Readiness Checklist ────────────────────────────────

        private Section BuildSection6ReleaseReadiness()
        {
            var sec = new Section
            {
                Title = "6. Release Readiness & Policy Compliance",
                Subtitle = "Store policy guidelines before submitting to Google Play, App Store, or Steam."
            };

            sec.Items.Add(new CheckResult
            {
                Title = "Google Play: No incentivized reviews policy",
                Status = CheckStatus.Manual,
                Detail = "Google Play policy strictly forbids offering gold, gems, or any in-game reward for rating."
            }.WithFacts("Verify the prompt UI does not promise in-game rewards for 5-star ratings."));

            sec.Items.Add(new CheckResult
            {
                Title = "Apple App Store: SKStoreReviewManager limits",
                Status = CheckStatus.Manual,
                Detail = "iOS enforces a maximum of 3 native review prompts per 365-day period."
            }.WithFacts(
                "Ensure StartsBeforeFirstPrompt and RemindLaterCooldownDays are high enough so you don't exhaust review opportunities prematurely."));

            sec.Items.Add(new CheckResult
            {
                Title = "Clean User Experience (No Interruption during Core Loops)",
                Status = CheckStatus.Manual,
                Detail = "Ensure RateControl.LogEvent() is called AFTER a rewarding moment, never during action or combat."
            }.WithFacts("Best times: Level complete victory screen, opening a reward chest, returning to map menu."));

            return sec;
        }

        private static void OpenFirstScript(List<string> locations)
        {
            if (locations == null || locations.Count == 0) return;
            var asset = AssetDatabase.LoadAssetAtPath<MonoScript>(locations[0]);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
        }
    }
}
