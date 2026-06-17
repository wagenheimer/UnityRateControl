#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Editor utilities for the Rate Control package.
    /// Accessible via <b>Tools → Rate Control</b> in the Unity menu bar.
    /// </summary>
    internal static class RateEditorMenuItems
    {
        // ── Menu items ────────────────────────────────────────────────────────────

        [MenuItem("Tools/Rate Control/Create Default Prefab", priority = 1)]
        private static void CreateDefaultPrefab()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Rate Dialog Prefab",
                "RateDialog",
                "prefab",
                "Choose where to save the Rate Dialog prefab.\n" +
                "Place it inside a Resources/ folder so RateControl can load it at runtime.");

            if (string.IsNullOrEmpty(path)) return;

            var prefab = BuildDialogHierarchy();
            var saved  = SavePrefab(prefab, path);
            Object.DestroyImmediate(prefab);

            if (saved != null)
            {
                EditorGUIUtility.PingObject(saved);
                Debug.Log($"[RateControl] Default prefab saved to: {path}");
                EditorUtility.DisplayDialog(
                    "Rate Dialog Created",
                    $"Prefab saved to:\n{path}\n\n" +
                    "Make sure it is inside a Resources/ folder, then set DialogResourcePath " +
                    "in your RateConfig to match the filename (without .prefab extension).",
                    "OK");
            }
        }

        [MenuItem("Tools/Rate Control/Create Rate Config Asset", priority = 2)]
        private static void CreateRateConfigAsset()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Rate Config",
                "RateConfig",
                "asset",
                "Choose where to save the RateConfig asset.");

            if (string.IsNullOrEmpty(path)) return;

            var asset = ScriptableObject.CreateInstance<RateConfig>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[RateControl] RateConfig asset created at: {path}");
        }

        [MenuItem("Tools/Rate Control/Reset Saved State (PlayerPrefs)", priority = 20)]
        private static void ResetSavedState()
        {
            if (Application.isPlaying)
            {
                RateControl.ResetAll();
                return;
            }

            // In edit mode, find the key prefix from the first RateConfig in the project
            var prefix = "RateControl";
            var guids  = AssetDatabase.FindAssets("t:RateConfig");
            if (guids.Length > 0)
            {
                var cfg = AssetDatabase.LoadAssetAtPath<RateConfig>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
                if (cfg != null && !string.IsNullOrEmpty(cfg.StorageKeyPrefix))
                    prefix = cfg.StorageKeyPrefix;
            }

            PlayerPrefs.DeleteKey($"{prefix}.EventCount");
            PlayerPrefs.DeleteKey($"{prefix}.StartCount");
            PlayerPrefs.DeleteKey($"{prefix}.DontAsk");
            PlayerPrefs.DeleteKey($"{prefix}.ShowCount");
            PlayerPrefs.DeleteKey($"{prefix}.LastVersion");
            PlayerPrefs.DeleteKey($"{prefix}.RemindLaterUntil");
            PlayerPrefs.Save();

            Debug.Log($"[RateControl] Saved state cleared (prefix: {prefix}).");
            EditorUtility.DisplayDialog("Rate Control",
                $"Saved state cleared.\nKey prefix used: \"{prefix}\"", "OK");
        }

        // ── Prefab builder ────────────────────────────────────────────────────────

        /// <summary>
        /// Builds the default dialog hierarchy in memory.
        ///
        /// Visual structure:
        ///   Root (Canvas, backdrop Image, DefaultRateDialog)
        ///     Card (white panel, VerticalLayoutGroup)
        ///       Stars (decorative row of 5 stars)
        ///       Title (TextMeshProUGUI)
        ///       Subtitle (TextMeshProUGUI)
        ///       BtnRateNow (amber Button)
        ///       BtnRemindLater (ghost Button)
        ///       BtnNoThanks (ghost Button, smaller)
        /// </summary>
        private static GameObject BuildDialogHierarchy()
        {
            // Root — full-screen canvas overlay
            var root = new GameObject("RateDialog");
            var rootRect = root.AddComponent<RectTransform>();
            StretchFull(rootRect);
            var backdropGroup = root.AddComponent<CanvasGroup>();

            var canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder    = 100;
            root.AddComponent<GraphicRaycaster>();

            var backdropImg = root.AddComponent<Image>();
            backdropImg.color = new Color(0f, 0f, 0f, 0.65f);
            backdropImg.raycastTarget = true;

            // Card
            var card      = CreatePanel(root.transform, "Card", new Color(0.98f, 0.97f, 0.95f, 1f), new Vector2(420, 340));
            var cardGroup = card.gameObject.AddComponent<CanvasGroup>();

            // Stars row (decorative)
            var starsRow = CreateLayoutGroup(card.transform, "Stars", new Vector2(0, 60));
            starsRow.GetComponent<HorizontalLayoutGroup>().spacing = 6f;
            for (int i = 0; i < 5; i++)
                CreateStarLabel(starsRow.transform);

            // Text
            var title    = CreateTMP(card.transform, "Title",    "Enjoying the game?",              28, FontStyles.Bold,   new Color(0.15f, 0.15f, 0.15f), new Vector2(380, 40));
            var subtitle = CreateTMP(card.transform, "Subtitle", "A quick review means the world!", 16, FontStyles.Normal, new Color(0.45f, 0.45f, 0.45f), new Vector2(360, 36));

            CreateSpacer(card.transform, 8);

            // Buttons
            var btnRate   = CreateButton(card.transform, "BtnRateNow",     "Rate Now  ★",      new Color(0.94f, 0.56f, 0.14f), new Color(1f,   1f,   1f),  new Vector2(340, 52), 18);
            var btnRemind = CreateButton(card.transform, "BtnRemindLater", "Remind Me Later",  new Color(0f,    0f,   0f,    0f), new Color(0.4f, 0.4f, 0.4f), new Vector2(340, 40), 14);
            var btnNo     = CreateButton(card.transform, "BtnNoThanks",    "No Thanks",        new Color(0f,    0f,   0f,    0f), new Color(0.65f, 0.65f, 0.65f), new Vector2(200, 32), 12);

            // Layout group on card
            var vg = card.gameObject.AddComponent<VerticalLayoutGroup>();
            vg.childAlignment      = TextAnchor.MiddleCenter;
            vg.spacing             = 10f;
            vg.padding             = new RectOffset(24, 24, 28, 24);
            vg.childControlWidth   = true;
            vg.childControlHeight  = false;
            vg.childForceExpandWidth  = true;
            vg.childForceExpandHeight = false;

            // Wire DefaultRateDialog component
            var dialog = root.AddComponent<DefaultRateDialog>();
            SetField(dialog, "_backdrop",          backdropGroup);
            SetField(dialog, "_card",              card);
            SetField(dialog, "_cardGroup",         cardGroup);
            SetField(dialog, "_titleLabel",        title);
            SetField(dialog, "_subtitleLabel",     subtitle);
            SetField(dialog, "_rateNowButton",     btnRate.GetComponent<Button>());
            SetField(dialog, "_remindLaterButton", btnRemind.GetComponent<Button>());
            SetField(dialog, "_noThanksButton",    btnNo.GetComponent<Button>());

            return root;
        }

        // ── UI helpers ────────────────────────────────────────────────────────────

        private static RectTransform CreatePanel(Transform parent, string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return rt;
        }

        private static RectTransform CreateLayoutGroup(Transform parent, string name, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment     = TextAnchor.MiddleCenter;
            hlg.childControlWidth  = false;
            hlg.childControlHeight = false;
            return rt;
        }

        private static void CreateStarLabel(Transform parent)
        {
            var go = new GameObject("Star");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = "★";
            tmp.fontSize  = 26;
            tmp.color     = new Color(0.94f, 0.75f, 0.08f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
            float size, FontStyles style, Color color, Vector2 sizeDelta)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = sizeDelta;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text             = text;
            tmp.fontSize         = size;
            tmp.fontStyle        = style;
            tmp.color            = color;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static RectTransform CreateButton(Transform parent, string name, string label,
            Color bgColor, Color textColor, Vector2 size, float fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt  = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            go.AddComponent<Image>().color = bgColor;
            go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            StretchFull(labelGo.AddComponent<RectTransform>());
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = fontSize;
            tmp.color     = textColor;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = fontSize >= 16 ? FontStyles.Bold : FontStyles.Normal;

            return rt;
        }

        private static void CreateSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin        = Vector2.zero;
            rt.anchorMax        = Vector2.one;
            rt.sizeDelta        = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            AssetDatabase.Refresh();
            return prefab;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType()
                  .GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                  ?.SetValue(target, value);
        }
    }
}
#endif
