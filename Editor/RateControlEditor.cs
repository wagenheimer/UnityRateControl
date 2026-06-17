#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Wagenheimer.RateControl.Editor
{
    /// <summary>
    /// Custom Inspector for <see cref="RateControl"/> that adds test buttons
    /// and a live state summary during Play Mode.
    /// </summary>
    [CustomEditor(typeof(RateControl))]
    internal sealed class RateControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("QA Actions", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Force Prompt (F8)"))
                    RateControl.UserActed(RateUserAction.RateNow);  // preview action
                    // Note: actual force is via F8 or the field _pendingPrompt.
                    // For true force use the menu item below.

                if (GUILayout.Button("Reset All State"))
                {
                    RateControl.ResetAll();
                    Debug.Log("[RateControl] State reset from Inspector.");
                }
            }

            EditorGUILayout.HelpBox(
                "Press F8 in Play Mode to force the rate prompt without changing thresholds.",
                MessageType.Info);
        }
    }
}
#endif

