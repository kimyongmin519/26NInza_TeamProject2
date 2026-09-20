using UnityEditor;
using UnityEngine;

namespace Member.YKJ.Bosses.Editor
{
    [CustomEditor(typeof(MimicBoss))]
    public sealed class MimicBossEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("Add Pattern"))
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Treasure"), false, () => AddPattern(new MimicTreasurePattern()));
                    menu.AddItem(new GUIContent("Jump"), false, () => AddPattern(new MimicJumpPattern()));
                    menu.ShowAsContext();
                }
            }

            var boss = (MimicBoss)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Encounter", boss.IsEncounterActive ? "Running" : "Stopped");
                EditorGUILayout.LabelField("Current Pattern", boss.Patterns.Current?.GetType().Name ?? "Idle");
                if (GUILayout.Button("Begin Encounter")) boss.BeginEncounter();
                if (GUILayout.Button("Stop Encounter")) boss.StopEncounter();
            }
        }

        private void AddPattern(MimicPattern pattern)
        {
            serializedObject.Update();
            SerializedProperty patterns = serializedObject.FindProperty("phaseOnePatterns");
            patterns.InsertArrayElementAtIndex(patterns.arraySize);
            patterns.GetArrayElementAtIndex(patterns.arraySize - 1).managedReferenceValue = pattern;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
