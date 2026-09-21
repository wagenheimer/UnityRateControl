# Rate Control

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com)
[![UPM](https://img.shields.io/badge/UPM-com.wagenheimer.ratecontrol-green.svg)](https://github.com/wagenheimer/UnityRateControl)

A lightweight, platform-agnostic Unity package for prompting players to rate your game at the right moment — without getting in their way.

---

## Features

- **Google Play In-App Review** (no browser redirect) when `com.google.play.review` is present
- **Apple App Store** native review request via `SKStoreReviewManager`
- **Amazon, Mac App Store, Windows Store, Steam** fallbacks via deep-link URLs
- **Fully configurable thresholds** — events, sessions, cooldown days, and version resets
- **Custom UI** via abstract `RateDialog` — one default prefab included, override per game
- **Collision-safe PlayerPrefs** — each game sets its own `StorageKeyPrefix`
- **Dependency injection** — plug in `IRateBlocker` to suppress prompts during tutorials, `IRateVersionProvider` for custom versioning

---

## Requirements

| Dependency | Version |
|---|---|
| Unity | 2021.3 LTS or newer |
| TextMeshPro | Required for the default dialog |
| Google Play Review *(optional)* | `com.google.play.review` any version — auto-detected |

---

## Installation

Add the package via the Unity Package Manager **Add package from git URL**:

```
https://github.com/wagenheimer/UnityRateControl.git
```

Or add it manually to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.wagenheimer.ratecontrol": "https://github.com/wagenheimer/UnityRateControl.git"
  }
}
```

To lock a specific commit or tag:

```
https://github.com/wagenheimer/UnityRateControl.git#v1.0.0
```

---

## Quick Start

### 1. Create a config asset

**Tools → Wagenheimer → Rate Control → Create Rate Config Asset**

Adjust thresholds in the Inspector (events, sessions, cooldown days, etc.).

### 2. Create the default dialog prefab

**Tools → Wagenheimer → Rate Control → Create Default Prefab**

Then drag the prefab into the **Dialog Prefab** field of your `RateConfig` asset in the Inspector. No `Resources/` folder required.

### 3. Set your storage prefix

In the config asset, set `StorageKeyPrefix` to something unique for your game (e.g. `"MyPuzzleGame.Rate"`). This prevents key collisions when the player has multiple titles using this package.

### 4. Initialize at runtime

```csharp
using Wagenheimer.RateControl;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private RateConfig _rateConfig;

    private void Awake()
    {
        // Dialog is loaded from RateConfig.DialogPrefab automatically.
        RateControl.Initialize(_rateConfig);
    }
}
```

To use a prefab that lives inside your game's canvas (so UI sorting is correct), instantiate and parent it first:

```csharp
private void Awake()
{
    var dialog = Instantiate(_rateConfig.DialogPrefab, myCanvas.transform, false);
    RateControl.Initialize(_rateConfig, dialog: dialog);
}
```

### 5. Log player milestones

```csharp
// Call after completing a level, puzzle, or meaningful game event
RateControl.LogEvent();

// Call when the game session starts (app launch, level load)
RateControl.LogStart();
```

### 6. Verify with Setup & Checklist Window

Open **Tools → Wagenheimer → Rate Control → Setup & Checklist...** (or **Window → Wagenheimer → Rate Control → Setup & Checklist**).

The checklist automatically audits:
- **RateConfig & Storage**: Verifies asset presence, unique `StorageKeyPrefix`, and threshold balance.
- **Dialog & UI Setup**: Validates prefab assignment, `RateDialog` component, and button wiring.
- **Store IDs & URLs**: Checks active platform review links (Google Play In-App Review, iOS App Store, Steam, Mac App Store) with browser test buttons.
- **Project Code Scanner**: Automatically finds calls to `RateControl.Initialize()`, `RateControl.LogEvent()`, and `IRateBlocker` implementations across your scripts.
- **Live Testing & Simulation**: Real-time PlayerPrefs state inspector, event simulation, force show prompt, and reset buttons.

The package automatically evaluates thresholds and shows the dialog when conditions are met.

---

## Platform Setup

| Platform | Config value | Notes |
|---|---|---|
| Google Play | `RatePlatform.GoogleAndroid` | Uses In-App Review when available |
| Amazon | `RatePlatform.AmazonAndroid` | Opens `amzn://` deep link |
| Apple App Store | `RatePlatform.iOS` | Uses `SKStoreReviewManager` |
| Mac App Store | `RatePlatform.MacAppStore` | Opens `macappstore://` |
| Steam | `RatePlatform.Steam` | Opens review URL in browser |
| Windows Store | `RatePlatform.WindowsStore` | Opens `ms-windows-store://` |
| Custom | `RatePlatform.Custom` | Implement `IRateStoreOpener` |

Set `AppId` in the config to your store-specific identifier (e.g. Play package name, Apple numeric ID, or Steam App ID).

---

## Customizing the Dialog

Subclass `RateDialog` and override the two abstract methods. The three button callbacks (`OnRateNow`, `OnRemindLater`, `OnNoThanks`) are already implemented in the base class — just wire your UI buttons to them:

```csharp
using Wagenheimer.RateControl;
using UnityEngine;

public class MyGameRateDialog : RateDialog
{
    public override void Show() { /* animate in, set active, etc. */ }
    public override void Hide() { /* animate out */ }

    // Wire your "Rate Now" button → OnRateNow()
    // Wire your "Remind Later" button → OnRemindLater()
    // Wire your "No Thanks" button → OnNoThanks()
    // (all three are inherited from RateDialog — no override needed)
}
```

Drag the prefab into the **Dialog Prefab** field of your `RateConfig` asset.

### Text & Localization

`DefaultRateDialog` (and any `RateDialog` subclass) **never sets text on `TextMeshProUGUI` components** — all labels are controlled entirely by the prefab. This means localization systems work without any extra configuration:

- **I2 Localization**: attach a `Localize` component to each label in the prefab as normal.
- **Unity Localization**: use a `LocalizeStringEvent` on each label.
- **Manual**: set the text directly in the prefab; it will not be overwritten at runtime.

> The old `[Header("Default Text")]` Inspector fields were removed in v1.2.21 precisely to avoid overwriting localized strings on `Awake`.

---

## Optional Interfaces

These two interfaces are the extension points for the two most common customization needs. Both have sensible defaults, so you only implement them when the default isn't enough.

---

### IRateBlocker — suppress the prompt at bad moments

**Problem:** The internal poll loop checks thresholds every second. As soon as the thresholds are met it tries to show the dialog — but your game might be mid-tutorial, showing a cutscene, or already displaying another modal. Showing a rate dialog on top of those would feel jarring.

**Solution:** Implement `IRateBlocker` and return `false` whenever the prompt should wait. The poll loop will keep retrying every second until you return `true`.

```csharp
// Attach this to a persistent GameObject (the same one running your game state).
public class MyGameBootstrap : MonoBehaviour, IRateBlocker
{
    private bool _tutorialActive;
    private int  _openModals;

    // The poll loop calls this every second. Return true = safe to show.
    public bool CanShowRate() => !_tutorialActive && _openModals == 0;

    private void Awake()
    {
        // Pass 'this' because this MonoBehaviour implements IRateBlocker.
        RateControl.Initialize(_config, blocker: this);
    }

    // Call these from your game code to keep the state up to date.
    public void StartTutorial()  => _tutorialActive = true;
    public void FinishTutorial() => _tutorialActive = false;
    public void OpenModal()      => _openModals++;
    public void CloseModal()     => _openModals--;
}
```

If you omit `blocker`, the default implementation always returns `true` (never blocks).

---

### IRateVersionProvider — control when "new version" resets the prompt

**Problem:** When a player taps "No Thanks" or "Rate Now", Rate Control sets a flag so it never bothers them again. That's correct — but if you ship a major update, you may want to ask again. The package handles this automatically: if the stored version doesn't match the current version, the "don't ask" flag is cleared.

By default it reads `Application.version` (the value in **Project Settings → Player → Version**). That's fine for most games, but if your game uses a separate marketing version string that differs from the Unity build version, the comparison will fire at the wrong time.

**Solution:** Implement `IRateVersionProvider` to return exactly the string you want to compare between sessions.

```csharp
// Simple class — no MonoBehaviour needed.
public class MarketingVersionProvider : IRateVersionProvider
{
    // Return whatever version string your game considers a "meaningful upgrade".
    public string GetCurrentVersion() => MyGame.MarketingVersion; // e.g. "2.0"
}

// Pass a new instance to Initialize.
RateControl.Initialize(_config, version: new MarketingVersionProvider());
```

If you omit `version`, the default reads `Application.version`.

---

### Using both at once

All parameters are independent and optional — combine freely:

```csharp
RateControl.Initialize(
    _config,
    blocker:  this,                          // IRateBlocker
    version:  new MarketingVersionProvider() // IRateVersionProvider
);
```

---

## In-Game Debug & QA Overlay

`RateControl` includes a full in-game runtime debug overlay (`RateDebugOverlay`) to inspect live state and test prompting logic in Play Mode and Development Builds.

### Features
- **Diagnostics & Status**: Inspects current eligibility (`Eligible`, `Suppressed`, `Blocked`), active scene blacklist status, `IRateBlocker` evaluation, `DontAsk` state, and remaining cooldown time for "Remind Me Later".
- **Counters & Thresholds**: Live progress of `EventCount` vs `EventsPerPrompt`, `StartCount` vs threshold, and `ShowCount`.
- **Simulation Actions**:
  - `Log Event (+1)`: Simulates game progress events.
  - `Log Start (+1)`: Simulates application launches.
  - `Force Show Prompt`: Immediately displays the dialog, bypassing blockers, scene blacklist, and thresholds.
  - `Clear Remind Cooldown`: Resets the countdown so the prompt can trigger again right away.
  - `Simulate Actions`: Test "Rate Now", "Remind Later", and "No Thanks" flows without leaving the editor.
  - `Reset All State`: Wipes PlayerPrefs keys for clean-slate testing.
- **Store & Platform Info**: Inspects detected runtime platform, installer name, package identifiers, and includes test buttons for `RateNow()` and `ShowMoreGames()`.
- **Live Event Log**: Displays an ongoing log of fired events (`OnPromptRequested`, `OnUserRated`, etc.).

### How to use
- **Hotkey**: Press **`F9`** in Play Mode to toggle the panel.
- **On-Screen Button**: Tap the floating **`RATE DBG`** button on the bottom-right corner.
- **Auto-attach**: The overlay is controlled by **`EnableDebugOverlay`** in `RateConfig` (default: **on**) and only attaches in the Unity Editor and Development Builds — never in release builds.
  - Toggle via the `RateConfig` inspector (**Debug & QA** section) or the menu **Tools → Wagenheimer → Rate Control → Debug Overlay Enabled** (checkmark = on).
  - Or in code: `RateDebugOverlay.CreateOverlay();`

---

## Editor Utilities

| Menu | Action |
|---|---|
| Tools → Wagenheimer → Rate Control → Debug Overlay Enabled | Toggle `RateConfig.EnableDebugOverlay` (Editor & Development Builds only) |
| Tools → Wagenheimer → Rate Control → Create Default Prefab | Generate the default `RateDialog` prefab |
| Tools → Wagenheimer → Rate Control → Create Rate Config Asset | Create a new `RateConfig` ScriptableObject |
| Tools → Wagenheimer → Rate Control → Reset Saved State (PlayerPrefs) | Clear all PlayerPrefs keys for testing |

---

## License

MIT — see [LICENSE](LICENSE).
