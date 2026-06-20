# Default Setup Sample

This sample shows the minimal wiring to get Rate Control running in your project.

## Files

- **RateControlBootstrap.cs** — MonoBehaviour that initializes Rate Control on `Awake`.

## Steps

1. Install the package via UPM (git URL or local path).
2. Create a `RateConfig` asset: **Tools → Rate Control → Create Rate Config Asset**.
3. Create the default dialog prefab: **Tools → Rate Control → Create Default Prefab**,
   then drag it into the **Dialog Prefab** field of the `RateConfig` asset in the Inspector.
4. Set `StorageKeyPrefix` in the config to a unique value for your game
   (e.g. `"MyGame.Rate"`) to avoid PlayerPrefs collisions.
5. Attach `RateControlBootstrap` to a persistent GameObject in your first scene.
6. Assign the `RateConfig` asset to the `Config` field.
7. Call `OnLevelComplete()` or `OnSessionStart()` from your game events.

## Customization

- Override `RateDialog` to build your own UI instead of the generated default.
- Implement `IRateBlocker` and pass it to `RateControl.Initialize` to suppress
  the prompt during tutorials or active modals.
