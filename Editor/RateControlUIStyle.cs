using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wagenheimer.RateControl.Editor
{
    internal static class RateControlUIStyle
    {
        private const string PackageUssPath = "Packages/com.wagenheimer.ratecontrol/Editor/RateControlCommon.uss";
        private const string LocalUssPath = "Assets/Editor/RateControlCommon.uss";

        public static void Apply(VisualElement element)
        {
            if (element == null) return;

            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(PackageUssPath);
            if (sheet == null)
            {
                sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(LocalUssPath);
            }

            if (sheet != null)
            {
                element.styleSheets.Add(sheet);
            }
        }

        public static VisualElement CreateCard(string title, string subtitle = null)
        {
            var card = new VisualElement();
            card.AddToClassList("rc-card");

            var header = new VisualElement();
            header.AddToClassList("rc-card-header");

            var titleCol = new VisualElement();
            var titleLabel = new Label(title);
            titleLabel.AddToClassList("rc-card-title");
            titleCol.Add(titleLabel);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subLabel = new Label(subtitle);
                subLabel.AddToClassList("rc-card-subtitle");
                titleCol.Add(subLabel);
            }

            header.Add(titleCol);
            card.Add(header);

            return card;
        }

        public static Label CreateBadge(string text, string severity)
        {
            var badge = new Label(text);
            badge.AddToClassList("rc-badge");

            switch (severity?.ToLowerInvariant())
            {
                case "pass":
                case "ok":
                    badge.AddToClassList("rc-badge-pass");
                    break;
                case "warn":
                case "warning":
                    badge.AddToClassList("rc-badge-warn");
                    break;
                case "fail":
                case "error":
                    badge.AddToClassList("rc-badge-fail");
                    break;
                default:
                    badge.AddToClassList("rc-badge-info");
                    break;
            }

            return badge;
        }

        public static VisualElement CreateCallout(string message, string level = "info")
        {
            var box = new VisualElement();
            box.AddToClassList("rc-callout");

            switch (level?.ToLowerInvariant())
            {
                case "pass":
                case "ok":
                    box.AddToClassList("rc-callout-pass");
                    break;
                case "warn":
                case "warning":
                    box.AddToClassList("rc-callout-warn");
                    break;
                default:
                    box.AddToClassList("rc-callout-info");
                    break;
            }

            var lbl = new Label(message) { style = { whiteSpace = WhiteSpace.Normal } };
            box.Add(lbl);
            return box;
        }

        public static VisualElement CreateHeader(string title, string subtitle, string version)
        {
            var header = new VisualElement();
            header.AddToClassList("rc-header");

            var row = new VisualElement();
            row.AddToClassList("rc-header-row");

            var left = new VisualElement();
            left.AddToClassList("rc-header-left");

            var titleLbl = new Label(title);
            titleLbl.AddToClassList("rc-header-title");

            var versionLbl = new Label($"v{version}");
            versionLbl.AddToClassList("rc-header-version");

            left.Add(titleLbl);
            left.Add(versionLbl);
            row.Add(left);
            header.Add(row);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subLbl = new Label(subtitle);
                subLbl.AddToClassList("rc-header-subtitle");
                header.Add(subLbl);
            }

            return header;
        }

        public static VisualElement CreateMetricCard(string label, string initialValue, out Label valueLabel)
        {
            var card = new VisualElement();
            card.AddToClassList("rc-metric-card");

            valueLabel = new Label(initialValue);
            valueLabel.AddToClassList("rc-metric-val");

            var lbl = new Label(label);
            lbl.AddToClassList("rc-metric-lbl");

            card.Add(valueLabel);
            card.Add(lbl);

            return card;
        }

        public static Button CreateButton(string text, string styleClass, Action onClick)
        {
            var btn = new Button(onClick);
            ApplyIconText(btn, text);
            btn.AddToClassList("rc-btn");
            if (!string.IsNullOrEmpty(styleClass))
            {
                btn.AddToClassList(styleClass);
            }
            return btn;
        }

        public static void ApplyIconText(Button button, string text)
        {
            for (int i = button.childCount - 1; i >= 0; i--)
            {
                var child = button[i];
                if (child.ClassListContains("wui-btn-icon") || child.ClassListContains("wui-btn-text"))
                    child.RemoveFromHierarchy();
            }

            SplitLeadingIcon(text, out var icon, out var label);

            if (string.IsNullOrEmpty(icon))
            {
                button.text = text;
                return;
            }

            button.text = string.Empty;
            button.style.flexDirection = FlexDirection.Row;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;

            var iconElement = new Label(icon);
            iconElement.AddToClassList("wui-btn-icon");
            iconElement.style.minWidth = 14;
            iconElement.style.marginRight = string.IsNullOrEmpty(label) ? 0 : 6;
            iconElement.style.flexShrink = 0;
            iconElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            iconElement.pickingMode = PickingMode.Ignore;
            button.Add(iconElement);

            if (!string.IsNullOrEmpty(label))
            {
                var textLabel = new Label(label);
                textLabel.AddToClassList("wui-btn-text");
                textLabel.style.flexShrink = 0;
                textLabel.pickingMode = PickingMode.Ignore;
                button.Add(textLabel);
            }
        }

        public static VisualElement CreateIconLabel(string text)
        {
            SplitLeadingIcon(text, out var icon, out var rest);
            if (string.IsNullOrEmpty(icon))
                return new Label(text);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var iconElement = new Label(icon);
            iconElement.AddToClassList("wui-btn-icon");
            iconElement.style.minWidth = 14;
            iconElement.style.marginRight = string.IsNullOrEmpty(rest) ? 0 : 6;
            iconElement.style.flexShrink = 0;
            iconElement.style.unityTextAlign = TextAnchor.MiddleCenter;
            iconElement.pickingMode = PickingMode.Ignore;
            row.Add(iconElement);

            var label = new Label(rest);
            label.pickingMode = PickingMode.Ignore;
            row.Add(label);
            return row;
        }

        internal static void SplitLeadingIcon(string text, out string icon, out string label)
        {
            icon = null;
            label = text;
            if (string.IsNullOrEmpty(text)) return;

            int i = 0;
            while (i < text.Length)
            {
                int codePoint = char.IsHighSurrogate(text[i]) && i + 1 < text.Length
                    ? char.ConvertToUtf32(text[i], text[i + 1])
                    : text[i];

                if (!IsIconCodePoint(codePoint)) break;
                i += char.IsHighSurrogate(text[i]) ? 2 : 1;
            }

            if (i == 0) return;

            icon = text.Substring(0, i).TrimEnd();
            label = text.Substring(i).TrimStart();
        }

        private static bool IsIconCodePoint(int codePoint) =>
            (codePoint >= 0x2190 && codePoint <= 0x2BFF)
            || (codePoint >= 0x1F000 && codePoint <= 0x1FAFF)
            || codePoint == 0xFE0F
            || codePoint == 0x20E3;
    }
}
