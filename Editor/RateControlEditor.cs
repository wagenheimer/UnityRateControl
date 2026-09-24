#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Wagenheimer.RateControl.UI;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Custom UI Toolkit Inspector for <see cref="RateControl"/> with interactive test buttons,
    /// debug overlay spawner, and live state summary during Play Mode.
    /// </summary>
    [CustomEditor(typeof(RateControl))]
    internal sealed class RateControlEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            RateControlUIStyle.Apply(root);

            var defaultCard = RateControlUIStyle.CreateCard("⭐ Rate Control Component", "Manages automated user review prompts, session counters, and platform store redirection.");
            InspectorElement.FillDefaultInspector(defaultCard, serializedObject, this);
            root.Add(defaultCard);

            // Debug & QA Tools Card
            var qaCard = RateControlUIStyle.CreateCard("🛠️ Debug & QA Tools");
            bool overlayActive = UnityEngine.Object.FindObjectOfType<RateDebugOverlay>() != null;
            qaCard.Add(RateControlUIStyle.CreateBadge(
                overlayActive ? "Rate Debug Overlay: ON" : "Rate Debug Overlay: OFF (auto-attaches on Initialize)",
                overlayActive ? "pass" : "info"));

            if (!Application.isPlaying)
            {
                qaCard.Add(RateControlUIStyle.CreateCallout(
                    "Enter Play Mode to test rate prompt thresholds, simulate user actions, or open the in-game debug overlay.",
                    "info"));
            }
            else
            {
                var rc = target as RateControl;
                qaCard.Add(new Label("Runtime Testing Actions") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6, marginBottom = 4 } });

                var btnRow1 = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 } };

                var forceBtn = new Button(() => { if (rc != null) rc.ForceShowPrompt(); }) { text = "⚡ Force Show Prompt" };
                forceBtn.AddToClassList("rc-btn");
                forceBtn.AddToClassList("rc-btn-primary");
                btnRow1.Add(forceBtn);

                var clearCooldownBtn = new Button(() => { if (rc != null) rc.ClearRemindCooldown(); }) { text = "⏳ Clear Remind Cooldown" };
                clearCooldownBtn.AddToClassList("rc-btn");
                btnRow1.Add(clearCooldownBtn);

                qaCard.Add(btnRow1);

                var btnRow2 = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 } };

                var logEventBtn = new Button(RateControl.LogEvent) { text = "📈 Log Event (+1)" };
                logEventBtn.AddToClassList("rc-btn");
                btnRow2.Add(logEventBtn);

                var logStartBtn = new Button(RateControl.LogStart) { text = "🚀 Log Start (+1)" };
                logStartBtn.AddToClassList("rc-btn");
                btnRow2.Add(logStartBtn);

                var resetBtn = new Button(RateControl.ResetAll) { text = "↺ Reset All State" };
                resetBtn.AddToClassList("rc-btn");
                btnRow2.Add(resetBtn);

                qaCard.Add(btnRow2);

                qaCard.Add(RateControlUIStyle.CreateCallout(
                    "Shortcuts: Press F8 to force the prompt immediately. Press F9 or click 'RATE DBG' on screen to open the in-game debug overlay.",
                    "info"));
            }

            root.Add(qaCard);
            return root;
        }
    }
}
#endif
