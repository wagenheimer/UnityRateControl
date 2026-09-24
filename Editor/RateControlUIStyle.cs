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
            var btn = new Button(onClick) { text = text };
            btn.AddToClassList("rc-btn");
            if (!string.IsNullOrEmpty(styleClass))
            {
                btn.AddToClassList(styleClass);
            }
            return btn;
        }
    }
}
