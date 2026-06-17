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
        RateControl.Initialize(_rateConfig, rateDialog: _rateDialog);
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

### IRateBlocker

Prevent the dialog from appearing at inconvenient times:

```csharp
public class MyRateBlocker : MonoBehaviour, IRateBlocker
{
    public bool CanShowRate() =>
        !_tutorialActive && !_modalOpen;
}

// Pass to Initialize
RateControl.Initialize(config, blocker: GetComponent<IRateBlocker>());
```

### IRateVersionProvider

Override how the "new version" detection reads the game version:

```csharp
public class MarketingVersionProvider : IRateVersionProvider
{
    public string GetCurrentVersion() => MyGame.MarketingVersion;
}

RateControl.Initialize(config, versionProvider: new MarketingVersionProvider());
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
