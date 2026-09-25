# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.21.0] - 2026-09-25

### Added
- Debug overlay zoom for phones: A-/A+ header buttons (0.75x-3x, saved in PlayerPrefs), a maximize button, and a mobile default zoom (`mobileDefaultScale`, now actually applied). Zoom uses a runtime clone of the PanelSettings.

## [1.21.0] - 2026-09-25

## [1.20.0] - 2026-09-24

### Added
- `IRateCanvasProvider`: optional interface (implement on the blocker) so the dialog spawns under the game's front canvas instead of a standalone root canvas.


## [1.19.2] - 2026-09-25

### Fixed
- ship PanelSettings + theme so debug overlay renders in player builds (v1.19.1)

## [1.19.1] - 2026-09-24

### Fixed
- Debug overlay did not render in player builds: no ThemeStyleSheet was available at runtime. The package now ships a PanelSettings + default runtime theme in Runtime/Resources/Wagenheimer (a project-level Resources/Wagenheimer/DebugPanelSettings still takes precedence).

## [1.19.0] - 2026-09-24

### Added
- overhaul RateDebugOverlay to modern UI Toolkit

## [1.18.0] - 2026-09-24

### Added
- **UI Toolkit In-Game Overlay (`RateDebugOverlay`)**: Fully migrated the in-game debug HUD from legacy IMGUI (`OnGUI()`) to native UI Toolkit (`UIDocument`).
  - Sleek dark-slate design system with draggable floating window and minimize controls.
  - Interactive floating launcher badge (`⭐ RATE DBG`) with live status indicator.
  - Real-time visual progress bars for milestone events and session starts towards trigger thresholds.
  - Live gate evaluation banner (`ELIGIBLE`, `COOLDOWN`, `SCENE BLACKLISTED`, `BLOCKER REJECTED`).
  - Interactive simulation buttons (`Force Show Prompt`, `+1 Event`, `+5 Events`, `+1 Start`, `Clear Cooldown`, `Toggle DontAsk`, simulated dialog actions).
  - Copyable full diagnostic markdown report for QA bug reports.
  - Live event history log with timestamped chips.
## [1.17.0] - 2026-09-24

### Added
- **Centralized UI Toolkit Dashboard (`RateControlHubWindow`)**: Unified multi-tab dashboard (`Tools > Wagenheimer > Rate Control > Dashboard...`) aggregating Setup & Diagnostics, Live QA & Testing, Configuration overview, Documentation guide, and Package updates.
- **Enhanced Setup & Diagnostic Checker**: Deep verification for active build targets, `RateConfig` resource location, dialog prefab UI bindings, code scanner for `RateControl.Initialize`/`LogEvent`/`IRateBlocker`, and release readiness quality gates with one-click quick fixes.
- **Interactive Live QA & Tester**: Real-time PlayerPrefs inspector with visual progress bars for app starts and milestone events, trigger readiness status, simulation controls, and debug overlay toggle.
- **Clean Menu Architecture**: Reorganized menu items with standardized priorities (100–200) and structured `Quick Actions` submenu, eliminating redundant and obsolete menu clutter.

## [1.15.0] - 2026-09-23

### Added
- **UI Toolkit Design System**: Modernized interface with `RateControlCommon.uss` and `RateControlUIStyle.cs` providing unified dark theme cards, badges (`pass`, `warn`, `fail`, `info`), and action buttons.
- **UI Toolkit Custom Inspector for RateControl**: Converted `RateControlEditor` from legacy `OnInspectorGUI()` to native `CreateInspectorGUI()` with Play Mode testing controls (`Force Show Prompt`, `Clear Remind Cooldown`, `Log Event`, `Log Start`, `Reset All State`), live debug overlay state indicator, and F8/F9 shortcut guide.
- **Enhanced RateConfigEditor**: Integrated `RateControlUIStyle` stylesheet loading to eliminate repetitive inline style definitions.

## [1.14.6] - 2026-09-21

### Changed
- chore(deps): bump PackageHub bootstrap to v1.0.5

## [1.14.5] - 2026-09-21

### Changed
- docs(readme): clarify Initialize records session start and LogStart is optional

## [1.14.4] - 2026-09-21

### Fixed
- drive debug overlay from RateConfig with inspector and menu toggle

## [1.14.3] - 2026-09-20

### Fixed
- import System.Text.RegularExpressions in RateLegacyMigrator

## [1.14.2] - 2026-09-20

### Fixed
- add missing `System.Text.RegularExpressions` import in `RateLegacyMigrator` (CS0103 on `Regex`)

## [1.13.1] - 2026-09-20

### Changed
- perf(migrator): speed up Detect by avoiding full prefab disk read

## [1.14.0] - 2026-09-20

### Added
- add RateLegacyMigrator with automated formRate upgrade and cleanup v1.13.0

## [1.13.0] - 2026-09-20

### Added
- **Automated Legacy RateControl Migrator (`RateLegacyMigrator`)**:
  - Automatically detects projects with legacy RateControl setups (in-house `RateControl.cs` MonoBehaviour, obsolete `RateControlBootstrap.cs`, unmigrated `formRate.cs`, missing components on `Main.prefab`).
  - **1-Click Migration**:
    - Automatically upgrades `formRate.cs` to inherit from `RateDialog`, preserving custom button animations (DOTween) and localized texts (I2 Loc), while wiring button callbacks to modern `RateDialog` events.
    - Relocates or creates `RateConfig.asset` in a `Resources/` folder and links `formRate.prefab` to `RateConfig.DialogPrefab`.
    - Automatically synchronizes all store IDs and distribution channels from `GameConfig` via `RateBuildPreprocessor`.
    - Safely removes obsolete legacy scripts (`RateControl.cs`, `RateControlBootstrap.cs`) and cleans missing MonoBehaviour components from `Main.prefab`.
  - **Checklist & Inspector Integration**:
    - Renders an attention banner with `⚡ Run 1-Click Migration & Cleanup` at the top of `SetupChecklistWindow`.
    - Contextual "Upgrade to RateDialog" action on the Dialog Prefab checklist card.
    - Added `Tools > Wagenheimer > Rate Control > Migrate Legacy RateControl...` menu entry and a button in `RateConfigEditor`.

## [1.12.3] - 2026-09-20

### Changed
- perf(editor): optimize SetupChecklistWindow scan and centering

## [1.12.2] - 2026-09-20

### Fixed
- make SetupChecklistWindow public, centered and focused on open

## [1.12.1] - 2026-09-20

### Fixed
- add missing .meta file for RateBuildPreprocessor

## [1.12.0] - 2026-09-20

### Added
- add MacGameStore, auto-sync build preprocessor, and enhanced inspector v1.11.0

## [1.11.0] - 2026-09-20

### Added
- **BuildPipeline & CLI Preprocessor (`RateBuildPreprocessor`)**:
  - Automatically synchronizes `RateConfig` before any build (CLI, BuildPipeline, or Editor).
  - Automatically detects and links with `UnityBuildPipeline`'s `GameConfig`: reads `MacAppStoreID`, `iOSAppIDFree/Full`, `AndroidFree/Full`, and dynamically switches active distribution channel (`MacAppStore`, `MacGameStore`, `Steam`).
  - Fallback automatic synchronization from `PlayerSettings`.
- **MacGameStore Support**:
  - Added `MacOsChannel.MacGameStore` distribution channel.
  - Added direct product review link (`MacGameStoreUrl`) and developer catalog link (`MoreGamesMacGameStoreUrl`).
  - Added store verification and "Test MGS URL" to `SetupChecklistWindow`.
- **RateConfig Custom Inspector Enhancements**:
  - Added `⚡ BuildPipeline & Store Auto-Sync` header card with live GameConfig status, one-click manual sync, and quick presets (Mac App Store, MacGameStore, Steam).
  - Real-time active build target detection and highlight badges (`[ACTIVE TARGET]`).
  - Conditional field display for MacGameStore URLs when channel is selected.
  - Added `Bootstrap & Automation` lifecycle section.

## [1.10.0] - 2026-09-20

### Added
- add AutoInitialize, optional config in Initialize, and snippet copy in checklist

## [1.9.1] - 2026-09-20

### Fixed
- use RateDialog namespace and expose config store helpers

## [1.9.0] - 2026-09-20

### Added
- add Setup & Checklist window with UI Toolkit, diagnostics, and testing suite

## [1.8.0] - 2026-09-20

### Added
- **Setup & Checklist Window** (`SetupChecklistWindow`): Comprehensive diagnostic and QA tool built in UI Toolkit, accessible via `Tools > Wagenheimer > Rate Control > Setup & Checklist...` and `Window > Wagenheimer > Rate Control > Setup & Checklist`.
  - **Automated Readiness Audit**: Scans project assets, scene setup, platform store IDs, and project C# code with clear Pass/Warning/Fail indicators.
  - **Asset & Threshold Validator**: Detects `RateConfig`, verifies unique `StorageKeyPrefix` namespace to avoid multi-game PlayerPrefs collisions, and validates trigger thresholds.
  - **Dialog Prefab Inspector**: Validates `RateDialog` and `DefaultRateDialog` setup, component wiring, button events, and Resources path fallback.
  - **Store IDs & URL Verification**: Real-time validation of active build target (Google Play In-App Review, iOS App Store, Steam, Mac App Store) with "Test Store URL" and "Test More Games URL" buttons opening directly in browser.
  - **Project Code Scanner**: Automatically audits project codebase for `RateControl.Initialize()`, `RateControl.LogEvent()`, `IRateBlocker`, and `IRateVersionProvider` implementations with direct "Open Script" navigation.
  - **Live Testing & Simulation Suite**: Inspects live PlayerPrefs values, simulates milestone events, simulates app launches, resets saved state, and provides "Force Show Prompt" in Play Mode.
  - **AI Prompt Generators ("Copy Prompt")**: Generates context-aware, ready-to-paste prompts for AI coding assistants for every failing or warning check.
- Added direct `📋 Setup & Checklist` button to the `RateConfig` Custom Inspector header alongside `Setup Guide`.

### Changed
- Reorganized Editor menu hierarchy under `Tools > Wagenheimer > Rate Control` and `Window > Wagenheimer > Rate Control` with ordered priorities and logical separators.

## [1.7.8] - 2026-09-19

### Changed
- ci: skip past existing tags in bump-version workflow

## [1.7.7] - 2026-09-19

### Added
- Auto-installs `com.wagenheimer.packagehub` via git if missing, using a zero-dependency Editor bootstrap assembly (`PackageHubBootstrap`). Installing this package now pulls in PackageHub automatically, with no manual manifest edits or scoped registry required.

### Changed
- Reverted the `com.wagenheimer.packagehub` OpenUPM registry dependency added in 1.7.6: it required every consumer to configure a scoped registry manually, which defeats the "install one package, get everything" goal. The git-based auto-bootstrap replaces it.

## [1.7.6] - 2026-09-19

### Changed
- Re-added `com.wagenheimer.packagehub` as a proper semver dependency (`1.0.4`) now that it is published on the [OpenUPM registry](https://openupm.com/packages/com.wagenheimer.packagehub/). Consumers need the `com.wagenheimer` scope added to their `scopedRegistries`.

## [1.7.5] - 2026-09-18

### Fixed
- Removed `com.wagenheimer.packagehub` from `dependencies` in package.json: UPM does not support a git URL as a dependency version, which made this package fail to resolve/update in any consuming project. PackageHub must still be added directly to the consumer's manifest.json.

## [1.7.4] - 2026-09-18

### Changed
- Standardized menu item priorities under `Tools > Wagenheimer > Rate Control` (base priority 130) for cohesive editor grouping and ordering.
- Updated `com.wagenheimer.packagehub` dependency to `v1.0.4`.

## [1.7.3] - 2026-09-18

### Changed
- **Centralized Update Management**: Replaced standalone update checker with dependency on `com.wagenheimer.packagehub` (`UnityPackageHub`). Updates, changelogs, and package management are now handled centrally through the unified Wagenheimer Package Hub.

## [1.7.2] - 2026-09-18
## [1.7.1] - 2026-09-18

### Added
- **Auto-attach `RateDebugOverlay`**: New `EnableDebugOverlay` field on `RateConfig`. When enabled, `RateControl.Initialize()` automatically attaches `RateDebugOverlay` to the Rate Control GameObject in the Unity Editor and Development Builds. No scene setup or code required — just call `Initialize()` as usual.

## [1.7.0] - 2026-09-17

### Added
- In-Game `RateDebugOverlay` IMGUI component (`Wagenheimer.RateControl.UI.RateDebugOverlay`):
  - Hotkey toggle via `F9` key and unobtrusive floating on-screen button ("RATE DBG").
  - Real-time diagnostic cards inspecting `DontAsk` state, `LastVersionRated`, scene blacklist status, `IRateBlocker` status, and exact remaining cooldown time for "Remind Me Later".
  - Counters and thresholds inspection: Events vs `EventsPerPrompt`, Starts vs threshold, and `ShowCount`.
  - Comprehensive QA simulation triggers: `Log Event (+1)`, `Log Start (+1)`, `Force Show Prompt`, `Clear Remind Cooldown`, `Toggle DontAsk`, and button simulations (`Rate Now`, `Remind Later`, `No Thanks`).
  - Store & Platform diagnostics displaying detected platform, installer, package identifiers, and test buttons for `RateNow()` and `ShowMoreGames()`.
  - Real-time event history logging (`OnPromptRequested`, `OnUserRated`, `OnUserRemindedLater`, `OnUserDeclined`).
  - Factory helper `RateDebugOverlay.CreateOverlay()` for programmatic instantiation.
- Exposed public diagnostic properties on `RateControl`: `EventCount`, `StartCount`, `ShowCount`, `RemindLaterUntil`, `IsPendingPrompt`, `Config`, `Blocker`, `VersionProvider`, `StoreOpener`, `Dialog`, `InRemindCooldown`, `IsSceneBlacklisted`, and `BlockerAllowsPrompt`.
- Added public QA helper methods on `RateControl`: `ForceShowPrompt()`, `ClearRemindCooldown()`, and `SetPendingPrompt()`.
- Added Editor menu item: `Tools → Wagenheimer → Rate Control → Add Debug Overlay to Scene`.
- Enhanced `RateControlEditor` custom Inspector with "Attach Rate Debug Overlay to Scene" and runtime QA actions.

## [1.6.0] - 2026-07-09

### Added
- show dialog on manual check (up to date / errors), redesign update popup

## [1.5.0] - 2026-07-07

### Added
- show changelog excerpt and one-click update in the popup

## [1.4.2] - 2026-07-07

### Fixed
- pass commit message via env to avoid shell breakage/injection

### Changed
- refactor: unify Editor menus under Tools/Wagenheimer/Rate Control

## [1.4.1] - 2026-07-07

### Fixed
- resolve CS0104 ambiguous PackageInfo reference

## [1.4.0] - 2026-07-07

### Added
- auto-generate CHANGELOG.md entry on version bump

## [1.3.0] - 2026-07-07

### Added
- add self-update checker and CI version auto-bump workflow — Editor checker compara a versão instalada com a do branch `master` e avisa quando há uma nova; GitHub Actions agora faz bump automático de versão, tag e release a cada push

## [1.2.22] - 2026-06-20

### Changed

- README: fixed `RateDialog` code example (correct abstract API: `Show`/`Hide`; button methods are inherited, not overridden); added **Text & Localization** section explaining I2/Unity Localization compatibility
- Setup Guide window: added localization note to Quick Start step 2; fixed Custom Dialog code example; added **Text & Localization** section to Custom Dialog page

## [1.2.21] - 2026-06-20

### Changed

- `DefaultRateDialog`: removed `[Header("Labels")]` section (`_titleLabel`, `_subtitleLabel`) and all `[Header("Default Text")]` string fields — text is now owned entirely by the prefab and/or localization system (e.g. I2 Localization). The script no longer overwrites any `TextMeshProUGUI` content on `Awake`

## [1.2.20] - 2026-06-20

### Changed

- `RateConfigEditor`: removed legacy `Dialog Resource Path` field from Inspector — `Dialog Prefab` is now the only UI option
- `RateDialog`: added `Debug.Log` to all three button callbacks (`OnRateNow`, `OnRemindLater`, `OnNoThanks`) with a description of what will happen next

## [1.2.19] - 2026-06-20

### Fixed

- `RateConfigEditor`: `Dialog Prefab` field was missing from the Inspector — the custom editor only rendered `Dialog Resource Path`. Now shows `Dialog Prefab` as the primary field with a live warning HelpBox when it is unassigned, and `Dialog Resource Path` below as a clearly labelled legacy fallback

## [1.2.18] - 2026-06-20

### Added

- Auto-Canvas: when a dialog prefab has no `Canvas` component on its root, the package automatically adds a `ScreenSpaceOverlay` Canvas (sort order 100) so the dialog renders without any manual canvas parenting — `RateControl.Initialize(config)` now works out of the box for any prefab

## [1.2.17] - 2026-06-20

### Changed

- README, Setup Guide window, and sample README updated to reflect `RateConfig.DialogPrefab` as the primary setup path — `Resources/` folder and `DialogResourcePath` demoted to legacy fallback

## [1.2.16] - 2026-06-20

### Added

- `RateConfig.DialogPrefab`: drag-and-drop prefab reference — no `Resources/` folder required. Takes priority over `DialogResourcePath` when set.
- `RateControl` now checks `config.DialogPrefab` before falling back to `Resources.Load`

## [1.2.15] - 2026-06-20

### Fixed

- `RateControl` Editor F8 shortcut: now calls `ShowPrompt()` directly instead of setting `_pendingPrompt = true`, so it bypasses the blocker (`IRateBlocker.CanShowRate()`), blacklisted scene check, and threshold evaluation — the dialog appears immediately regardless of game state

## [1.2.14] - 2026-06-20

### Fixed

- `DefaultRateStoreOpener.OpenRatePage()`: added `#if UNITY_EDITOR` guard so pressing "Rate Now" in Play Mode no longer triggers the real Android/iOS/Steam store flow — instead logs which build target would be used and returns immediately

## [1.2.13] - 2026-06-18

### Changed

- README: fixed `Initialize()` parameter names in examples (`rateDialog:` → `dialog:`, `versionProvider:` → `version:`)
- README: rewrote `IRateBlocker` and `IRateVersionProvider` sections with problem/solution framing and combined usage example
- Setup Guide window: added **Copy** button to every code block — clicking copies to clipboard and shows "✓ Copied" feedback for 1.5 s

## [1.0.0] - 2024-01-01

### Added

- `RateControl` singleton with configurable event and session thresholds
- `RateConfig` ScriptableObject with `StorageKeyPrefix` (per-game PlayerPrefs isolation)
  and `DialogResourcePath` (runtime-loadable dialog prefab)
- Google Play In-App Review integration (auto-detected when `com.google.play.review` is present)
- Apple App Store native review request via `SKStoreReviewManager`
- Fallback deep-link URLs for Amazon, Mac App Store, Windows Store, and Steam
- Abstract `RateDialog` base class for fully custom dialog UI
- Default `RateDialog` prefab generated by Editor tool (TextMeshPro-based)
- `IRateBlocker` interface to suppress prompts during tutorials or modals
- `IRateVersionProvider` interface to override version detection
- `IRateStoreOpener` interface for custom store redirect behavior
- Editor utilities: Create Default Prefab, Create Rate Config Asset, Reset Saved State
- Unity Package Manager (UPM) support via Git URL
- `Samples~/DefaultSetup` with a minimal MonoBehaviour bootstrap example
