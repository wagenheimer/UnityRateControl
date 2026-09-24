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
    }
}
