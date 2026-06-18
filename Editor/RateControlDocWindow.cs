using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.RateControl.Editor
{
    internal sealed class RateControlDocWindow : EditorWindow
    {
        [MenuItem("Tools/Rate Control/Setup Guide", priority = 1)]
        [MenuItem("Help/Rate Control Setup Guide")]
        public static void Open()
        {
            var w = GetWindow<RateControlDocWindow>(false, "Rate Control — Setup Guide");
            w.minSize = new Vector2(720, 480);
            w.Show();
        }

        // ── Colors ────────────────────────────────────────────────────────────────
        private static readonly Color kBg      = new Color(0.16f, 0.16f, 0.16f);
        private static readonly Color kSidebar = new Color(0.13f, 0.13f, 0.13f);
        private static readonly Color kAccent  = new Color(0.30f, 0.62f, 0.82f);
        private static readonly Color kGreen   = new Color(0.20f, 0.68f, 0.32f);
        private static readonly Color kOrange  = new Color(0.95f, 0.58f, 0.10f);
        private static readonly Color kYellow  = new Color(0.85f, 0.68f, 0.20f);
        private static readonly Color kSilver  = new Color(0.65f, 0.65f, 0.68f);
        private static readonly Color kSteam   = new Color(0.24f, 0.52f, 0.80f);
        private static readonly Color kCode    = new Color(0.12f, 0.12f, 0.18f);
        private static readonly Color kHeading = new Color(0.88f, 0.88f, 0.92f);
        private static readonly Color kBody    = new Color(0.68f, 0.68f, 0.70f);
        private static readonly Color kMuted   = new Color(0.45f, 0.45f, 0.48f);
        private static readonly Color kSelect  = new Color(0.22f, 0.38f, 0.55f);

        // ── TOC pages ─────────────────────────────────────────────────────────────
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
            var root = rootVisualElement;
            root.style.flexDirection   = FlexDirection.Row;
            root.style.backgroundColor = kBg;
            root.style.flexGrow        = 1;

            // ── Sidebar ───────────────────────────────────────────────────────────
            var sidebar = new ScrollView(ScrollViewMode.Vertical);
            sidebar.style.width            = 190;
            sidebar.style.minWidth         = 190;
            sidebar.style.maxWidth         = 190;
            sidebar.style.backgroundColor  = kSidebar;
            sidebar.style.borderRightColor = new Color(0.22f, 0.22f, 0.22f);
            sidebar.style.borderRightWidth = 1;
            sidebar.style.paddingTop       = 14;
            sidebar.style.paddingBottom    = 14;
            root.Add(sidebar);

            var title = new Label("Rate Control");
            title.style.fontSize                = 14;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color                   = kAccent;
            title.style.paddingLeft             = 14;
            title.style.marginBottom            = 2;
            sidebar.Add(title);

            var subtitle = new Label("v1.2.1  ·  Setup Guide");
            subtitle.style.fontSize     = 9;
            subtitle.style.color        = kMuted;
            subtitle.style.paddingLeft  = 14;
            subtitle.style.marginBottom = 10;
            sidebar.Add(subtitle);

            sidebar.Add(HRule());

            _tocBtns.Clear();
            for (int i = 0; i < Pages.Length; i++)
            {
                var idx = i;
                var btn = new Button(() => SelectPage(idx)) { text = Pages[i].label };
                btn.style.unityTextAlign          = TextAnchor.MiddleLeft;
                btn.style.fontSize                = 11;
                btn.style.paddingLeft             = 14;
                btn.style.paddingTop              = 7;
                btn.style.paddingBottom           = 7;
                btn.style.marginLeft = btn.style.marginRight =
                btn.style.marginTop  = btn.style.marginBottom = 0;
                btn.style.borderTopWidth    = btn.style.borderBottomWidth =
                btn.style.borderLeftWidth   = btn.style.borderRightWidth  = 0;
                btn.style.borderTopLeftRadius    = btn.style.borderTopRightRadius =
                btn.style.borderBottomLeftRadius = btn.style.borderBottomRightRadius = 0;
                btn.style.backgroundColor        = new Color(0, 0, 0, 0);
                btn.style.color                  = kBody;
                btn.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (idx != _selectedPage)
                        btn.style.backgroundColor = new Color(0.18f, 0.20f, 0.24f);
                });
                btn.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    if (idx != _selectedPage)
                        btn.style.backgroundColor = new Color(0, 0, 0, 0);
                });
                sidebar.Add(btn);
                _tocBtns.Add(btn);
            }

            sidebar.Add(HRule());

            var ghBtn = new Button(() => Application.OpenURL("https://github.com/wagenheimer/UnityRateControl"))
                { text = "GitHub  ↗" };
            ghBtn.style.unityTextAlign = TextAnchor.MiddleLeft;
            ghBtn.style.fontSize       = 10;
            ghBtn.style.color          = kMuted;
            ghBtn.style.backgroundColor = new Color(0, 0, 0, 0);
            ghBtn.style.borderTopWidth    = ghBtn.style.borderBottomWidth =
            ghBtn.style.borderLeftWidth   = ghBtn.style.borderRightWidth  = 0;
            ghBtn.style.paddingLeft       = 14;
            ghBtn.style.paddingTop        = 6;
            ghBtn.style.paddingBottom     = 6;
            ghBtn.style.marginTop = ghBtn.style.marginBottom =
            ghBtn.style.marginLeft = ghBtn.style.marginRight = 0;
            ghBtn.style.borderTopLeftRadius    = ghBtn.style.borderTopRightRadius =
            ghBtn.style.borderBottomLeftRadius = ghBtn.style.borderBottomRightRadius = 0;
            sidebar.Add(ghBtn);

            // ── Content area ──────────────────────────────────────────────────────
            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow     = 1;
            scroll.style.paddingLeft  = 28;
            scroll.style.paddingRight = 28;
            scroll.style.paddingTop   = 22;
            scroll.style.paddingBottom = 28;
            root.Add(scroll);

            _content = new VisualElement();
            _content.style.maxWidth = 680;
            scroll.Add(_content);

            SelectPage(0);
        }

        private void SelectPage(int idx)
        {
            _selectedPage = idx;
            for (int i = 0; i < _tocBtns.Count; i++)
            {
                var sel = i == idx;
                _tocBtns[i].style.backgroundColor = sel ? kSelect : new Color(0, 0, 0, 0);
                _tocBtns[i].style.color           = sel ? Color.white : kBody;
                _tocBtns[i].style.borderLeftWidth = sel ? 3 : 0;
                _tocBtns[i].style.borderLeftColor = kAccent;
            }
            _content.Clear();
            _content.Add(Pages[idx].build());
        }

        // ── Page builders ─────────────────────────────────────────────────────────

        private static VisualElement BuildQuickStart()
        {
            var p = Page();
            p.Add(H1("Quick Start", kAccent));
            p.Add(Body("Get Rate Control running in your project in under 5 minutes."));
            p.Add(Gap());

            p.Add(H2("1 · Create a RateConfig asset"));
            p.Add(Body("Go to " + B("Tools → Rate Control → Create Config") + " — this creates a " +
                       B("RateConfig.asset") + " in your project. " +
                       "This ScriptableObject holds all settings for your game."));

            p.Add(H2("2 · Create the Rate Dialog prefab"));
            p.Add(Body("Go to " + B("Tools → Rate Control → Create Default Dialog") + " — " +
                       "saves a prefab with the built-in dialog UI inside a " + C("Resources/") + " folder. " +
                       "The default resource path is " + C("\"RateDialog\"") + ". " +
                       "If you save elsewhere, update " + B("Dialog Resource Path") + " in RateConfig."));

            p.Add(H2("3 · Set Distribution Channels"));
            p.Add(Body("Open RateConfig → " + B("Distribution Channels") + ". " +
                       "Choose the store for each desktop platform you ship on:"));
            p.Add(Bullets(
                "macOS → MacAppStore  or  Steam",
                "Windows → Steam  (or None for itch.io / direct download)",
                "Linux → Steam  (or None)",
                "Android and iOS are always active — no selection needed"));

            p.Add(H2("4 · Fill in Store IDs"));
            p.Add(Body("Fill in IDs for the platforms you publish to. " +
                       "See the " + B("Store IDs") + " page for where to find each one."));

            p.Add(H2("5 · Initialize in your bootstrap"));
            p.Add(Code(
                "using Wagenheimer.RateControl;\n\n" +
                "public class Bootstrap : MonoBehaviour\n" +
                "{\n" +
                "    [SerializeField] RateConfig _config;\n\n" +
                "    void Awake() => RateControl.Initialize(_config);\n" +
                "}"));

            p.Add(H2("6 · Log events at meaningful moments"));
            p.Add(Body("Call " + C("RateControl.LogEvent()") + " when the player does something positive — " +
                       "completing a level, winning a match, finishing a puzzle. " +
                       "The system shows the prompt when thresholds are met."));
            p.Add(Code("// e.g. in your LevelComplete handler:\nRateControl.LogEvent();"));

            p.Add(Gap(12));
            p.Add(Info("That's it. Rate Control handles platform detection, cooldowns, " +
                       "\"remind me later\" state, and opening the correct store automatically."));
            return p;
        }

        private static VisualElement BuildDistribution()
        {
            var p = Page();
            p.Add(H1("Distribution Channels", kOrange));
            p.Add(Body("The " + B("Distribution Channels") + " section in RateConfig selects which store " +
                       "mechanism is used per desktop platform."));
            p.Add(Gap());

            p.Add(H2("Mobile — always active"));
            p.Add(TableEl(new[,] {
                { "Platform",            "Mechanism",                    "Config needed" },
                { "Android (Google Play)", "Google Play In-App Review", "AndroidPackageId (optional)" },
                { "Android (Amazon)",    "Auto-detected at runtime",     "none — uses Application.identifier" },
                { "iOS",                 "SKStoreReviewManager",         "iOSAppId (for fallback URL)" },
                { "WSA (Windows Store)", "ms-windows-store:// URI",      "none" },
            }));

            p.Add(H2("Desktop — choose a channel"));
            p.Add(TableEl(new[,] {
                { "Platform", "Channel",      "What it opens" },
                { "macOS",    "MacAppStore",  "macappstore:// review URL  (needs MacAppStoreId)" },
                { "macOS",    "Steam",        "Steam review page  (needs SteamAppId)" },
                { "macOS",    "None",         "Rate skipped — silent no-op" },
                { "Windows",  "Steam",        "Steam review page  (needs SteamAppId)" },
                { "Windows",  "None",         "Rate skipped — silent no-op" },
                { "Linux",    "Steam",        "Steam review page  (needs SteamAppId)" },
                { "Linux",    "None",         "Rate skipped — silent no-op" },
            }));
            p.Add(Gap());

            p.Add(H2("Build-time warnings"));
            p.Add(Body("Before every build, " + C("RateConfigValidator") + " checks your config and " +
                       "logs a warning when:"));
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
            p.Add(H1("Store IDs", kAccent));
            p.Add(Body("Found in the " + B("Store IDs") + " section of RateConfig. " +
                       "All fields are optional where Rate Control can auto-detect the value at runtime."));
            p.Add(Gap());

            p.Add(H2("Android Package ID", kGreen));
            p.Add(Body("Leave empty — Rate Control uses " + C("Application.identifier") + " automatically. " +
                       "Only set if your package name differs from the Unity project identifier."));
            p.Add(Hint("Example: com.mystudio.mygame"));

            p.Add(H2("iOS App ID", kSilver));
            p.Add(Body("Numeric Apple ID used as a fallback URL when SKStoreReviewManager is unavailable."));
            p.Add(Body(B("Where to find:") + " App Store Connect → [your app] → App Information → Apple ID"));
            p.Add(Hint("Example: 123456789"));

            p.Add(H2("Mac App Store ID", kSilver));
            p.Add(Body("Same numeric Apple ID as the iOS App ID for universal apps. " +
                       "Builds the " + C("macappstore://") + " review URL."));
            p.Add(Body(B("Where to find:") + " App Store Connect → [your app] → App Information → Apple ID"));

            p.Add(H2("Steam App ID", kSteam));
            p.Add(Body("The numeric Steam Application ID. Used by macOS/Windows/Linux Steam builds."));
            p.Add(Body(B("Where to find:") + " Steamworks dashboard — the number in the URL after " +
                       C("/apps/") + ", or the App Admin page."));
            p.Add(Hint("Example: 1203050  →  store.steampowered.com/app/1203050/reviews/"));
            return p;
        }

        private static VisualElement BuildMoreGames()
        {
            var p = Page();
            p.Add(H1("More Games", kGreen));
            p.Add(Body("Call " + C("RateControl.ShowMoreGames()") + " (e.g. from a \"More Games\" button) " +
                       "to open your publisher page on the relevant store."));
            p.Add(Gap());

            p.Add(H2("Google Play  —  Developer name", kGreen));
            p.Add(Body("Your publisher name exactly as shown on Google Play. " +
                       "Auto-builds " + C("market://search?q=pub:{name}") + "."));
            p.Add(Body(B("Where to find:") + " play.google.com/console → Setup → App info → Developer name"));
            p.Add(Hint("Example: Pixel Crate Games"));

            p.Add(H2("Amazon Appstore  —  automatic", kOrange));
            p.Add(Body("No configuration needed. Uses " + C("Application.identifier") + " automatically."));

            p.Add(H2("Apple App Store / Mac  —  Developer ID", kSilver));
            p.Add(Body("The numeric Apple Developer ID (not the bundle ID). " +
                       "Auto-builds " + C("apps.apple.com/developer/id{id}") + "."));
            p.Add(Body(B("Fastest way:") + " apps.apple.com → search your app → click developer name " +
                       "→ copy the number after " + C("/id") + " in the URL."));
            p.Add(Body(B("Alternative:") + " appstoreconnect.apple.com → your name (top-right) " +
                       "→ View My Profile → Developer ID."));
            p.Add(Hint("Example: 1780103848"));

            p.Add(H2("Windows Store  —  Publisher display name", kAccent));
            p.Add(Body("Your publisher display name from Microsoft Partner Center. " +
                       "Auto-builds " + C("ms-windows-store://search/?query={name}") + "."));
            p.Add(Body(B("Where to find:") + " partner.microsoft.com/dashboard → [app] → " +
                       "Product management → Product identity → Publisher display name"));

            p.Add(H2("Steam  —  Developer slug", kSteam));
            p.Add(Body("The slug from your developer page URL (part after " + C("/developer/") + "). " +
                       "Covers Windows, macOS (Steam channel), and Linux."));
            p.Add(Body(B("Where to find:") + " Open store.steampowered.com/developer/YOUR_SLUG " +
                       "and copy the slug from the URL."));
            p.Add(Hint("Example: sevensailsgames"));

            p.Add(H2("Fallback / Website"));
            p.Add(Body("Used when no platform-specific field is set, or for sideloaded builds. " +
                       "Any URL — your website, itch.io page, etc."));
            return p;
        }

        private static VisualElement BuildThresholds()
        {
            var p = Page();
            p.Add(H1("Trigger Thresholds", kYellow));
            p.Add(Body("These values control when the rate prompt appears. " +
                       "Tune them to catch players at the right moment without feeling intrusive."));
            p.Add(Gap());

            p.Add(H2("Events Per Prompt"));
            p.Add(Body("Calls to " + C("RateControl.LogEvent()") + " needed before the prompt is queued."));
            p.Add(Body("Log events at positive milestones: level complete, puzzle solved, match won."));
            p.Add(Hint("Recommended: 5–15.  Default: 10"));

            p.Add(H2("Starts Before First Prompt"));
            p.Add(Body("Minimum app launches before the very first prompt. " +
                       "Ensures players have had time to form an opinion."));
            p.Add(Hint("Recommended: 3–5.  Default: 3"));

            p.Add(H2("Starts Before Subsequent Prompts"));
            p.Add(Body("Minimum launches between re-prompts (e.g. after \"Remind Me Later\"). " +
                       "Higher values feel less intrusive."));
            p.Add(Hint("Recommended: 7–14.  Default: 8"));

            p.Add(H2("Remind Later Cooldown Days"));
            p.Add(Body("Real calendar days before re-showing after \"Remind Me Later\". " +
                       "Set to 0 to rely only on session count."));
            p.Add(Hint("Recommended: 3–7.  Default: 3"));

            p.Add(H2("Blacklisted Scenes"));
            p.Add(Body("Scene names where the prompt is always suppressed regardless of thresholds. " +
                       "Add loading screens, cutscenes, or high-intensity sequences."));
            p.Add(Body("Use the exact name from Build Settings — no path, no " + C(".unity") + " extension."));
            p.Add(Hint("Example: Boss_Fight,  Cinematic_Intro,  LoadingScreen"));
            return p;
        }

        private static VisualElement BuildApi()
        {
            var p = Page();
            p.Add(H1("Runtime API", kAccent));
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
                "// When count reaches EventsPerPrompt, the prompt is queued.\n" +
                "RateControl.LogEvent();"));

            p.Add(H2("ShowMoreGames"));
            p.Add(Code(
                "// Opens the publisher page on the relevant store.\n" +
                "RateControl.ShowMoreGames();"));

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
            p.Add(H1("Custom Dialog", kGreen));
            p.Add(Body("To replace the built-in dialog UI, create a prefab with a " +
                       "component that inherits from " + C("RateDialog") + "."));
            p.Add(Gap());

            p.Add(H2("1 · Inherit from RateDialog"));
            p.Add(Code(
                "using Wagenheimer.RateControl;\n\n" +
                "public class MyRateDialog : RateDialog\n" +
                "{\n" +
                "    public override void OnRateNow()     => StartCoroutine(Opener.OpenRatePage());\n" +
                "    public override void OnRemindLater() => Controller.RemindLater();\n" +
                "    public override void OnDecline()     => Controller.Decline();\n" +
                "}"));

            p.Add(H2("2 · Place in a Resources folder"));
            p.Add(Body("Put the prefab inside any " + C("Resources/") + " folder. " +
                       "The file name (without extension) is the resource path."));
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
            p.Add(H1("Advanced / Extending", kSilver));
            p.Add(Gap());

            p.Add(H2("Custom Store Opener  (IRateStoreOpener)"));
            p.Add(Body("Implement " + C("IRateStoreOpener") + " to override how the store is opened. " +
                       "Useful for proprietary in-app review SDKs or unsupported platforms."));
            p.Add(Code(
                "public class MyOpener : IRateStoreOpener\n" +
                "{\n" +
                "    public IEnumerator OpenRatePage()\n" +
                "    {\n" +
                "        // your logic here\n" +
                "        yield break;\n" +
                "    }\n" +
                "    public void OpenMoreGames() { /* your logic */ }\n" +
                "}\n\n" +
                "RateControl.Initialize(config, opener: new MyOpener());"));

            p.Add(H2("Custom Blocker  (IRateBlocker)"));
            p.Add(Body("Implement " + C("IRateBlocker") + " to suppress the prompt based on game state — " +
                       "e.g. during a tutorial, boss fight, or active multiplayer session."));
            p.Add(Code(
                "public class MyBlocker : IRateBlocker\n" +
                "{\n" +
                "    public bool IsBlocked => TutorialManager.IsActive || BossManager.InFight;\n" +
                "}\n\n" +
                "RateControl.Initialize(config, blocker: new MyBlocker());"));

            p.Add(H2("Google Play In-App Review"));
            p.Add(Body("The native flow is used automatically when " +
                       C("com.google.play.review") + " is in your project. " +
                       "The asmdef " + C("versionDefines") + " entry auto-defines " +
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

        private static Label H1(string text, Color? c = null)
        {
            var l = new Label(text);
            l.style.fontSize                = 20;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.color                   = c ?? kHeading;
            l.style.marginBottom            = 8;
            l.style.whiteSpace              = WhiteSpace.Normal;
            return l;
        }

        private static Label H2(string text, Color? c = null)
        {
            var l = new Label(text);
            l.style.fontSize                = 13;
            l.style.unityFontStyleAndWeight = FontStyle.Bold;
            l.style.color                   = c ?? kHeading;
            l.style.marginTop               = 16;
            l.style.marginBottom            = 4;
            l.style.whiteSpace              = WhiteSpace.Normal;
            return l;
        }

        private static Label Body(string text)
        {
            var l = new Label(text);
            l.style.fontSize     = 11;
            l.style.color        = kBody;
            l.style.whiteSpace   = WhiteSpace.Normal;
            l.style.marginBottom = 5;
            return l;
        }

        private static Label Hint(string text)
        {
            var l = new Label(text);
            l.style.fontSize     = 10;
            l.style.color        = kMuted;
            l.style.whiteSpace   = WhiteSpace.Normal;
            l.style.marginBottom = 5;
            l.style.marginLeft   = 8;
            return l;
        }

        private static VisualElement Code(string code)
        {
            var box = new VisualElement();
            box.style.backgroundColor = kCode;
            box.style.borderTopLeftRadius = box.style.borderTopRightRadius =
            box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 4;
            box.style.paddingLeft   = 12;
            box.style.paddingRight  = 12;
            box.style.paddingTop    = 10;
            box.style.paddingBottom = 10;
            box.style.marginTop     = 6;
            box.style.marginBottom  = 10;
            box.style.borderLeftColor = kAccent;
            box.style.borderLeftWidth = 2;

            var l = new Label(code);
            l.style.fontSize   = 10;
            l.style.color      = new Color(0.78f, 0.88f, 0.98f);
            l.style.whiteSpace = WhiteSpace.Normal;
            box.Add(l);
            return box;
        }

        private static VisualElement Info(string text)
        {
            var box = new VisualElement();
            box.style.backgroundColor = new Color(0.14f, 0.24f, 0.36f);
            box.style.borderTopLeftRadius = box.style.borderTopRightRadius =
            box.style.borderBottomLeftRadius = box.style.borderBottomRightRadius = 4;
            box.style.paddingLeft   = 12;
            box.style.paddingRight  = 12;
            box.style.paddingTop    = 10;
            box.style.paddingBottom = 10;
            box.style.marginTop     = 12;
            box.style.marginBottom  = 4;
            box.style.borderLeftColor = kAccent;
            box.style.borderLeftWidth = 3;

            var l = new Label("ℹ  " + text);
            l.style.fontSize   = 11;
            l.style.color      = new Color(0.72f, 0.85f, 0.98f);
            l.style.whiteSpace = WhiteSpace.Normal;
            box.Add(l);
            return box;
        }

        private static VisualElement Bullets(params string[] items)
        {
            var box = new VisualElement();
            box.style.marginLeft   = 8;
            box.style.marginBottom = 5;
            foreach (var item in items)
            {
                var l = new Label("•  " + item);
                l.style.fontSize     = 11;
                l.style.color        = kBody;
                l.style.whiteSpace   = WhiteSpace.Normal;
                l.style.marginBottom = 2;
                box.Add(l);
            }
            return box;
        }

        private static VisualElement TableEl(string[,] data)
        {
            int rows = data.GetLength(0), cols = data.GetLength(1);
            var t = new VisualElement();
            t.style.marginTop    = 6;
            t.style.marginBottom = 10;
            t.style.overflow     = Overflow.Hidden;
            t.style.borderTopLeftRadius = t.style.borderTopRightRadius =
            t.style.borderBottomLeftRadius = t.style.borderBottomRightRadius = 4;

            for (int r = 0; r < rows; r++)
            {
                bool hdr = r == 0;
                var row = new VisualElement();
                row.style.flexDirection   = FlexDirection.Row;
                row.style.backgroundColor = hdr
                    ? new Color(0.20f, 0.24f, 0.30f)
                    : r % 2 == 0 ? new Color(0.17f, 0.17f, 0.17f) : new Color(0.19f, 0.19f, 0.19f);
                row.style.paddingTop = row.style.paddingBottom = hdr ? 7 : 5;
                row.style.paddingLeft  = 10;
                row.style.paddingRight = 10;

                for (int c = 0; c < cols; c++)
                {
                    var cell = new Label(data[r, c]);
                    cell.style.fontSize                = hdr ? 10 : 11;
                    cell.style.unityFontStyleAndWeight = hdr ? FontStyle.Bold : FontStyle.Normal;
                    cell.style.color                   = hdr ? kAccent : kBody;
                    cell.style.whiteSpace              = WhiteSpace.Normal;
                    cell.style.flexGrow                = 1;
                    cell.style.flexBasis               = 0;
                    cell.style.paddingRight            = 10;
                    row.Add(cell);
                }
                t.Add(row);
            }
            return t;
        }

        private static VisualElement HRule()
        {
            var e = new VisualElement();
            e.style.height          = 1;
            e.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            e.style.marginTop       = 8;
            e.style.marginBottom    = 8;
            return e;
        }

        private static VisualElement Gap(float h = 10)
        {
            var e = new VisualElement();
            e.style.height = h;
            return e;
        }

        private static string B(string t) => $"<b>{t}</b>";
        private static string C(string t) => $"<color=#7ec8e3>{t}</color>";
    }
}
