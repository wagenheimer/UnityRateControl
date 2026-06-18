using System.Collections.Generic;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.RateControl.Editor
{
    internal sealed class RateControlDocWindow : EditorWindow
    {
        private const string kUxml =
            "Packages/com.wagenheimer.ratecontrol/Editor/RateControlDocWindow.uxml";

        [MenuItem("Tools/Rate Control/Setup Guide", priority = 1)]
        [MenuItem("Help/Rate Control Setup Guide")]
        public static void Open()
        {
            var w = GetWindow<RateControlDocWindow>(true, "Rate Control — Setup Guide", true);
            w.minSize = new Vector2(720, 480);
            w.Show();
            w.Focus();
        }

        private static readonly (string label, System.Func<VisualElement> build)[] Pages =
        {
            ("Quick Start",           BuildQuickStart),
            ("Distribution Channels", BuildDistribution),
            ("Store IDs",             BuildStoreIds),
            ("More Games",            BuildMoreGames),
            ("Trigger Thresholds",    BuildThresholds),
            ("Runtime API",           BuildApi),
            ("Custom Dialog",         BuildCustomDialog),
            ("Advanced / Extending",  BuildAdvanced),
        };

        private int _selectedPage;
        private VisualElement _content;
        private readonly List<Button> _tocBtns = new();

        private void CreateGUI()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(kUxml);
            if (uxml == null)
            {
                rootVisualElement.Add(new Label($"[RateControl] Could not load {kUxml}"));
                return;
            }

            uxml.CloneTree(rootVisualElement);

            // Version subtitle
            var pkg = PackageInfo.FindForAssembly(typeof(RateControlDocWindow).Assembly);
            var sub = rootVisualElement.Q<Label>("subtitle-label");
            if (sub != null) sub.text = pkg != null ? $"v{pkg.version}  ·  Setup Guide" : "Setup Guide";

            // TOC buttons
            var toc = rootVisualElement.Q("toc");
            _tocBtns.Clear();
            for (int i = 0; i < Pages.Length; i++)
            {
                var idx = i;
                var btn = new Button(() => SelectPage(idx)) { text = Pages[i].label };
                btn.AddToClassList("rc-toc-btn");
                toc.Add(btn);
                _tocBtns.Add(btn);
            }

            rootVisualElement.Q<Button>("github-btn").clicked +=
                () => Application.OpenURL("https://github.com/wagenheimer/UnityRateControl");

            _content = rootVisualElement.Q("content");
            SelectPage(0);
        }

        private void SelectPage(int idx)
        {
            if (idx < 0 || idx >= Pages.Length) return;
            _selectedPage = idx;
            for (int i = 0; i < _tocBtns.Count; i++)
            {
                if (i == idx) _tocBtns[i].AddToClassList("rc-toc-btn--active");
                else          _tocBtns[i].RemoveFromClassList("rc-toc-btn--active");
            }
            _content.Clear();
            _content.Add(Pages[idx].build());
        }

        // ── Page builders ─────────────────────────────────────────────────────────

        private static VisualElement BuildQuickStart()
        {
            var p = Page();
            p.Add(H1("Quick Start", "rc-h1--accent"));
            p.Add(Body("Get Rate Control running in your project in under 5 minutes."));
            p.Add(Gap());
            p.Add(H2("1 · Create a RateConfig asset"));
            p.Add(Body("Go to " + B("Tools → Rate Control → Create Config") + " — this creates a " +
                       B("RateConfig.asset") + " in your project."));
            p.Add(H2("2 · Create the Rate Dialog prefab"));
            p.Add(Body("Go to " + B("Tools → Rate Control → Create Default Dialog") + " — saves a prefab " +
                       "inside a " + C("Resources/") + " folder. Default path is " + C("\"RateDialog\"") +
                       ". Update " + B("Dialog Resource Path") + " in RateConfig if you move it."));
            p.Add(H2("3 · Set Distribution Channels"));
            p.Add(Body("Open RateConfig → " + B("Distribution Channels") + ". Choose the store for each desktop platform:"));
            p.Add(Bullets(
                "macOS → MacAppStore  or  Steam",
                "Windows → Steam  (or None for itch.io / direct download)",
                "Linux → Steam  (or None)",
                "Android and iOS are always active — no selection needed"));
            p.Add(H2("4 · Fill in Store IDs"));
            p.Add(Body("Fill in IDs for the platforms you publish to. See the " + B("Store IDs") + " page for where to find each one."));
            p.Add(H2("5 · Initialize in your bootstrap"));
            p.Add(Code(
                "using Wagenheimer.RateControl;\n\n" +
                "public class Bootstrap : MonoBehaviour\n{\n" +
                "    [SerializeField] RateConfig _config;\n\n" +
                "    void Awake() => RateControl.Initialize(_config);\n}"));
            p.Add(H2("6 · Log events at meaningful moments"));
            p.Add(Body("Call " + C("RateControl.LogEvent()") + " when the player does something positive — " +
                       "completing a level, winning a match, finishing a puzzle."));
            p.Add(Code("// e.g. in your LevelComplete handler:\nRateControl.LogEvent();"));
            p.Add(Gap(12));
            p.Add(Info("That's it. Rate Control handles platform detection, cooldowns, " +
                       "\"remind me later\" state, and opening the correct store automatically."));
            return p;
        }

        private static VisualElement BuildDistribution()
        {
            var p = Page();
            p.Add(H1("Distribution Channels", "rc-h1--orange"));
            p.Add(Body("The " + B("Distribution Channels") + " section in RateConfig selects which store " +
                       "mechanism is used per desktop platform."));
            p.Add(Gap());
            p.Add(H2("Mobile — always active"));
            p.Add(TableEl(new[,] {
                { "Platform",              "Mechanism",                    "Config needed" },
                { "Android (Google Play)", "Google Play In-App Review",    "AndroidPackageId (optional)" },
                { "Android (Amazon)",      "Auto-detected at runtime",     "none — uses Application.identifier" },
                { "iOS",                   "SKStoreReviewManager",         "iOSAppId (for fallback URL)" },
                { "WSA (Windows Store)",   "ms-windows-store:// URI",      "none" },
            }));
            p.Add(H2("Desktop — choose a channel"));
            p.Add(TableEl(new[,] {
                { "Platform", "Channel",     "What it opens" },
                { "macOS",   "MacAppStore",  "macappstore:// review URL  (needs MacAppStoreId)" },
                { "macOS",   "Steam",        "Steam review page  (needs SteamAppId)" },
                { "macOS",   "None",         "Rate skipped — silent no-op" },
                { "Windows", "Steam",        "Steam review page  (needs SteamAppId)" },
                { "Windows", "None",         "Rate skipped — silent no-op" },
                { "Linux",   "Steam",        "Steam review page  (needs SteamAppId)" },
                { "Linux",   "None",         "Rate skipped — silent no-op" },
            }));
            p.Add(Gap());
            p.Add(H2("Build-time warnings"));
            p.Add(Body("Before every build, " + C("RateConfigValidator") + " checks your config and logs a warning when:"));
            p.Add(Bullets(
                "The channel for the active build target is None",
                "Channel is Steam but SteamAppId is empty",
                "Channel is MacAppStore but MacAppStoreId is empty",
                "Building for iOS with an empty iOSAppId"));
            p.Add(Info("Warnings are advisory — they do not block the build. " +
                       "None is a valid choice when you intentionally skip rate on a platform."));
            return p;
        }

        private static VisualElement BuildStoreIds()
        {
            var p = Page();
            p.Add(H1("Store IDs", "rc-h1--accent"));
            p.Add(Body("Found in the " + B("Store IDs") + " section of RateConfig. " +
                       "All fields are optional where Rate Control can auto-detect the value at runtime."));
            p.Add(Gap());
            p.Add(H2("Android Package ID", "rc-h2--green"));
            p.Add(Body("Leave empty — Rate Control uses " + C("Application.identifier") + " automatically. " +
                       "Only set if your package name differs from the Unity project identifier."));
            p.Add(Hint("Example: com.mystudio.mygame"));
            p.Add(H2("iOS App ID", "rc-h2--silver"));
            p.Add(Body("Numeric Apple ID used as a fallback URL when SKStoreReviewManager is unavailable."));
            p.Add(Body(B("Where to find:") + " App Store Connect → [your app] → App Information → Apple ID"));
            p.Add(Hint("Example: 123456789"));
            p.Add(H2("Mac App Store ID", "rc-h2--silver"));
            p.Add(Body("Same numeric Apple ID as iOS for universal apps. Builds the " + C("macappstore://") + " review URL."));
            p.Add(Body(B("Where to find:") + " App Store Connect → [your app] → App Information → Apple ID"));
            p.Add(H2("Steam App ID", "rc-h2--steam"));
            p.Add(Body("The numeric Steam Application ID. Used by macOS / Windows / Linux Steam builds."));
            p.Add(Body(B("Where to find:") + " Steamworks dashboard — the number after " + C("/apps/") + " in the URL."));
            p.Add(Hint("Example: 1203050  →  store.steampowered.com/app/1203050/reviews/"));
            return p;
        }

        private static VisualElement BuildMoreGames()
        {
            var p = Page();
            p.Add(H1("More Games", "rc-h1--green"));
            p.Add(Body("Call " + C("RateControl.ShowMoreGames()") + " (e.g. from a \"More Games\" button) " +
                       "to open your publisher page on the relevant store."));
            p.Add(Gap());
            p.Add(H2("Google Play  —  Developer name", "rc-h2--green"));
            p.Add(Body("Your publisher name exactly as shown on Google Play. " +
                       "Auto-builds " + C("market://search?q=pub:{name}") + "."));
            p.Add(Body(B("Where to find:") + " play.google.com/console → Setup → App info → Developer name"));
            p.Add(Hint("Example: Pixel Crate Games"));
            p.Add(H2("Amazon Appstore  —  automatic", "rc-h2--orange"));
            p.Add(Body("No configuration needed. Uses " + C("Application.identifier") + " automatically."));
            p.Add(H2("Apple App Store / Mac  —  Developer ID", "rc-h2--silver"));
            p.Add(Body("Numeric Apple Developer ID. Auto-builds " + C("apps.apple.com/developer/id{id}") + "."));
            p.Add(Body(B("Fastest way:") + " apps.apple.com → search your app → click developer name → copy number after " + C("/id") + " in the URL."));
            p.Add(Hint("Example: 1780103848"));
            p.Add(H2("Windows Store  —  Publisher display name", "rc-h2--accent"));
            p.Add(Body("Your publisher display name from Microsoft Partner Center. " +
                       "Auto-builds " + C("ms-windows-store://search/?query={name}") + "."));
            p.Add(Body(B("Where to find:") + " partner.microsoft.com/dashboard → [app] → Product identity → Publisher display name"));
            p.Add(H2("Steam  —  Developer slug", "rc-h2--steam"));
            p.Add(Body("The slug from your developer page URL (part after " + C("/developer/") + "). Covers Win, macOS, and Linux."));
            p.Add(Body(B("Where to find:") + " Open store.steampowered.com/developer/YOUR_SLUG and copy the slug from the URL."));
            p.Add(Hint("Example: sevensailsgames"));
            p.Add(H2("Fallback / Website"));
            p.Add(Body("Used when no platform-specific field is set, or for sideloaded builds. Any URL."));
            return p;
        }

        private static VisualElement BuildThresholds()
        {
            var p = Page();
            p.Add(H1("Trigger Thresholds", "rc-h1--yellow"));
            p.Add(Body("These values control when the rate prompt appears."));
            p.Add(Gap());
            p.Add(H2("Events Per Prompt"));
            p.Add(Body("Calls to " + C("RateControl.LogEvent()") + " needed before the prompt is queued."));
            p.Add(Hint("Recommended: 5–15.  Default: 10"));
            p.Add(H2("Starts Before First Prompt"));
            p.Add(Body("Minimum app launches before the very first prompt."));
            p.Add(Hint("Recommended: 3–5.  Default: 3"));
            p.Add(H2("Starts Before Subsequent Prompts"));
            p.Add(Body("Minimum launches between re-prompts (e.g. after \"Remind Me Later\")."));
            p.Add(Hint("Recommended: 7–14.  Default: 8"));
            p.Add(H2("Remind Later Cooldown Days"));
            p.Add(Body("Real calendar days before re-showing after \"Remind Me Later\". Set to 0 to rely only on session count."));
            p.Add(Hint("Recommended: 3–7.  Default: 3"));
            p.Add(H2("Blacklisted Scenes"));
            p.Add(Body("Scene names where the prompt is always suppressed. Add loading screens, cutscenes, or boss fights."));
            p.Add(Body("Use the exact name from Build Settings — no path, no " + C(".unity") + " extension."));
            p.Add(Hint("Example: Boss_Fight,  Cinematic_Intro,  LoadingScreen"));
            return p;
        }

        private static VisualElement BuildApi()
        {
            var p = Page();
            p.Add(H1("Runtime API", "rc-h1--accent"));
            p.Add(Body("All public methods are on the static " + C("RateControl") + " class."));
            p.Add(Gap());
            p.Add(H2("Initialize"));
            p.Add(Code(
                "// Call once in Awake(). DontDestroyOnLoad is applied automatically.\n" +
                "RateControl.Initialize(rateConfig);\n\n" +
                "// Optional overloads:\n" +
                "RateControl.Initialize(config, blocker: myBlocker);\n" +
                "RateControl.Initialize(config, opener: myOpener);\n" +
                "RateControl.Initialize(config, dialog: myDialogInstance);"));
            p.Add(H2("LogEvent"));
            p.Add(Code(
                "// Call at meaningful positive moments (level complete, puzzle solved, etc.).\n" +
                "RateControl.LogEvent();"));
            p.Add(H2("ShowMoreGames"));
            p.Add(Code("RateControl.ShowMoreGames();"));
            p.Add(H2("ForceShow"));
            p.Add(Code(
                "// Immediately shows the dialog, bypassing all thresholds.\n" +
                "// Wire this to a \"Rate Us\" button in your settings menu.\n" +
                "RateControl.ForceShow();"));
            p.Add(H2("ResetSavedState"));
            p.Add(Code(
                "// Clears all PlayerPrefs state — use during development to test the full flow.\n" +
                "RateControl.ResetSavedState();"));
            p.Add(H2("Storage Key Prefix"));
            p.Add(Body("Rate Control stores state in " + C("PlayerPrefs") + " using the prefix in " +
                       B("Storage Key Prefix") + ". Use a unique value per game to avoid collisions."));
            p.Add(Hint("Format: Studio.GameName.Rate   e.g. PixelCrate.NordStorm.Rate"));
            return p;
        }

        private static VisualElement BuildCustomDialog()
        {
            var p = Page();
            p.Add(H1("Custom Dialog", "rc-h1--green"));
            p.Add(Body("To replace the built-in dialog UI, create a prefab with a component that inherits from " + C("RateDialog") + "."));
            p.Add(Gap());
            p.Add(H2("1 · Inherit from RateDialog"));
            p.Add(Code(
                "using Wagenheimer.RateControl;\n\n" +
                "public class MyRateDialog : RateDialog\n{\n" +
                "    public override void OnRateNow()     => StartCoroutine(Opener.OpenRatePage());\n" +
                "    public override void OnRemindLater() => Controller.RemindLater();\n" +
                "    public override void OnDecline()     => Controller.Decline();\n}"));
            p.Add(H2("2 · Place in a Resources folder"));
            p.Add(Body("Put the prefab inside any " + C("Resources/") + " folder. The file name (without extension) is the resource path."));
            p.Add(Hint("Assets/UI/Resources/MyDialog.prefab  →  Dialog Resource Path = \"MyDialog\""));
            p.Add(H2("3 · Update Dialog Resource Path in RateConfig"));
            p.Add(Body("Set " + B("Dialog Resource Path") + " in RateConfig to match your prefab name."));
            p.Add(H2("Alternative: pass instance directly"));
            p.Add(Code(
                "// If the dialog is already in your scene, skip Resources.Load entirely:\n" +
                "RateControl.Initialize(config, dialog: myDialogInstance);"));
            return p;
        }

        private static VisualElement BuildAdvanced()
        {
            var p = Page();
            p.Add(H1("Advanced / Extending", "rc-h1--silver"));
            p.Add(Gap());
            p.Add(H2("Custom Store Opener  (IRateStoreOpener)"));
            p.Add(Body("Implement " + C("IRateStoreOpener") + " to override how the store is opened."));
            p.Add(Code(
                "public class MyOpener : IRateStoreOpener\n{\n" +
                "    public IEnumerator OpenRatePage()\n    {\n" +
                "        // your logic here\n        yield break;\n    }\n" +
                "    public void OpenMoreGames() { /* your logic */ }\n}\n\n" +
                "RateControl.Initialize(config, opener: new MyOpener());"));
            p.Add(H2("Custom Blocker  (IRateBlocker)"));
            p.Add(Body("Implement " + C("IRateBlocker") + " to suppress the prompt based on game state."));
            p.Add(Code(
                "public class MyBlocker : IRateBlocker\n{\n" +
                "    public bool IsBlocked => TutorialManager.IsActive || BossManager.InFight;\n}\n\n" +
                "RateControl.Initialize(config, blocker: new MyBlocker());"));
            p.Add(H2("Google Play In-App Review"));
            p.Add(Body("The native flow is used automatically when " + C("com.google.play.review") +
                       " is in your project. The asmdef " + C("versionDefines") + " entry auto-defines " +
                       C("RATECONTROL_GOOGLE_PLAY_REVIEW") + " — no manual scripting define needed. " +
                       "Without it, falls back to " + C("market://details?id={packageId}") + "."));
            return p;
        }

        // ── UI helpers ────────────────────────────────────────────────────────────

        private static VisualElement Page()
        {
            var e = new VisualElement();
            e.style.flexGrow = 1;
            return e;
        }

        private static Label H1(string text, string colorClass = null)
        {
            var l = new Label(text);
            l.AddToClassList("rc-h1");
            if (colorClass != null) l.AddToClassList(colorClass);
            return l;
        }

        private static Label H2(string text, string colorClass = null)
        {
            var l = new Label(text);
            l.AddToClassList("rc-h2");
            if (colorClass != null) l.AddToClassList(colorClass);
            return l;
        }

        private static Label Body(string text)
        {
            var l = new Label(text);
            l.AddToClassList("rc-body");
            return l;
        }

        private static Label Hint(string text)
        {
            var l = new Label(text);
            l.AddToClassList("rc-hint");
            return l;
        }

        private static VisualElement Code(string code)
        {
            var box = new VisualElement();
            box.AddToClassList("rc-code");
            var l = new Label(code);
            l.AddToClassList("rc-code__text");
            box.Add(l);
            return box;
        }

        private static VisualElement Info(string text)
        {
            var box = new VisualElement();
            box.AddToClassList("rc-info");
            var l = new Label("ℹ  " + text);
            l.AddToClassList("rc-info__text");
            box.Add(l);
            return box;
        }

        private static VisualElement Bullets(params string[] items)
        {
            var box = new VisualElement();
            box.AddToClassList("rc-bullets");
            foreach (var item in items)
            {
                var l = new Label("•  " + item);
                l.AddToClassList("rc-bullet");
                box.Add(l);
            }
            return box;
        }

        private static VisualElement Gap(float h = 10)
        {
            var e = new VisualElement();
            e.AddToClassList(h > 10 ? "rc-gap--lg" : "rc-gap");
            return e;
        }

        private static VisualElement TableEl(string[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            var t = new VisualElement();
            t.AddToClassList("rc-table");
            for (int r = 0; r < rows; r++)
            {
                bool hdr = r == 0;
                var row = new VisualElement();
                row.AddToClassList("rc-table-row");
                if (hdr)         row.AddToClassList("rc-table-row--header");
                else if (r % 2 != 0) row.AddToClassList("rc-table-row--alt");
                for (int c = 0; c < cols; c++)
                {
                    var cell = new Label(data[r, c]);
                    cell.AddToClassList("rc-table-cell");
                    if (hdr) cell.AddToClassList("rc-table-cell--header");
                    row.Add(cell);
                }
                t.Add(row);
            }
            return t;
        }

        private static string B(string t) => $"<b>{t}</b>";
        private static string C(string t) => $"<color=#7EC8E3>{t}</color>";
    }
}
