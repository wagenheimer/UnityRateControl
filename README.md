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

**Tools → Rate Control → Create Rate Config Asset**

Adjust thresholds in the Inspector (events, sessions, cooldown days, etc.).

### 2. Create the default dialog prefab

**Tools → Rate Control → Create Default Prefab**

Save it inside any `Resources/` folder in your project. Set `DialogResourcePath` in the config to match the filename (without `.prefab`).

### 3. Set your storage prefix

In the config asset, set `StorageKeyPrefix` to something unique for your game (e.g. `"MyPuzzleGame.Rate"`). This prevents key collisions when the player has multiple titles using this package.

### 4. Initialize at runtime

```csharp
using Wagenheimer.RateControl;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private RateConfig _rateConfig;
    [SerializeField] private MyRateDialog _rateDialog; // your prefab, optional

    private void Awake()
    {
        RateControl.Initialize(_rateConfig, dialog: _rateDialog);
    }
}
```

Or let the package load the dialog from `Resources/` automatically — no argument needed:

```csharp
RateControl.Initialize(_rateConfig);
```

### 5. Log player milestones

```csharp
// Call after completing a level, puzzle, or meaningful game event
RateControl.LogEvent();

// Call when the game session starts (app launch, level load)
RateControl.LogStart();
```

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

Subclass `RateDialog` and implement the three abstract methods:

```csharp
using Wagenheimer.RateControl;
using UnityEngine;

public class MyGameRateDialog : RateDialog
{
    protected override void OnShow() { /* animate in */ }
    protected override void OnHide() { /* animate out */ }
    protected override void OnUserAction(RateUserAction action)
    {
        // action is RateNow, RemindLater, or Decline
        Respond(action); // must call this to report back to RateControl
    }
}
```

Assign your prefab to `RateControl.Initialize(config, rateDialog: myDialogInstance)`.

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

## Editor Utilities

| Menu | Action |
|---|---|
| Tools → Rate Control → Create Default Prefab | Generate the default `RateDialog` prefab |
| Tools → Rate Control → Create Rate Config Asset | Create a new `RateConfig` ScriptableObject |
| Tools → Rate Control → Reset Saved State (PlayerPrefs) | Clear all PlayerPrefs keys for testing |

---

## License

MIT — see [LICENSE](LICENSE).
