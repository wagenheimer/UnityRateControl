#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="RateControl"/> that adds test buttons,
    /// debug overlay spawner, and a live state summary during Play Mode.
    /// </summary>
    [CustomEditor(typeof(RateControl))]
    internal sealed class RateControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug & QA Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Attach Rate Debug Overlay to Scene"))
            {
                var existing = Object.FindObjectOfType<Wagenheimer.RateControl.UI.RateDebugOverlay>();
                if (existing != null)
                {
                    Selection.activeGameObject = existing.gameObject;
                    EditorGUIUtility.PingObject(existing.gameObject);
                }
                else
                {
                    var go = new GameObject("RateDebugOverlay", typeof(Wagenheimer.RateControl.UI.RateDebugOverlay));
                    Undo.RegisterCreatedObjectUndo(go, "Create Rate Debug Overlay");
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                }
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to test rate prompt thresholds, simulate user actions, or open the in-game debug overlay.", MessageType.None);
                return;
            }

            var rc = target as RateControl;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Runtime Testing Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Force Show Prompt"))
                {
                    if (rc != null)
                        rc.ForceShowPrompt();
                }

                if (GUILayout.Button("Clear Remind Cooldown"))
                {
                    if (rc != null)
                        rc.ClearRemindCooldown();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Log Event (+1)"))
                {
                    RateControl.LogEvent();
                }

                if (GUILayout.Button("Log Start (+1)"))
                {
                    RateControl.LogStart();
                }

                if (GUILayout.Button("Reset All State"))
                {
                    RateControl.ResetAll();
                }
            }

            EditorGUILayout.HelpBox(
                "Shortcuts: Press F8 to force the prompt immediately. Press F9 or click 'RATE DBG' on screen to open the in-game debug overlay.",
                MessageType.Info);
        }
    }
}
#endif

