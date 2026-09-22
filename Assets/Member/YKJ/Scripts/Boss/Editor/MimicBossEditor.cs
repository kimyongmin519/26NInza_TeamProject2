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
                    menu.AddItem(new GUIContent("Treasure"), false, () => AddPattern<MimicTreasurePattern>("Treasure"));
                    menu.AddItem(new GUIContent("Jump"), false, () => AddPattern<MimicJumpPattern>("Jump"));
                    menu.AddItem(new GUIContent("Tongue"), false, () => AddPattern<MimicTonguePattern>("Tongue"));
                    menu.AddItem(new GUIContent("CoinRain"), false, () => AddPattern<MimicCoinRainPattern>("CoinRain"));
                    menu.AddItem(new GUIContent("FallingWeapons"), false, () => AddPattern<MimicFallingWeaponsPattern>("FallingWeapons"));
                    menu.AddItem(new GUIContent("Laser"), false, () => AddPattern<MimicLaserPattern>("Laser"));
                    menu.ShowAsContext();
                }
            }

            var boss = (MimicBoss)target;
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Encounter", boss.IsEncounterActive ? "Running" : "Stopped");
                EditorGUILayout.LabelField("Phase", boss.Phase.ToString());
                EditorGUILayout.LabelField("Current Pattern", boss.Patterns.Current?.GetType().Name ?? "Idle");
                if (GUILayout.Button("Begin Encounter")) boss.BeginEncounter();
                if (GUILayout.Button("Stop Encounter")) boss.StopEncounter();
            }
        }

        private void AddPattern<T>(string name) where T : MimicPattern
        {
            var boss = (MimicBoss)target;
            Transform root = boss.transform.Find("Patterns");
            if (root == null)
            {
                var folder = new GameObject("Patterns");
                Undo.RegisterCreatedObjectUndo(folder, "Create patterns root");
                folder.transform.SetParent(boss.transform, false);
                root = folder.transform;
            }
            var child = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(child, "Create pattern");
            child.transform.SetParent(root, false);
            T pattern = Undo.AddComponent<T>(child);
            var used = new System.Collections.Generic.HashSet<int>();
            foreach (MimicPattern existing in boss.GetComponentsInChildren<MimicPattern>(true))
                if (existing != pattern && existing.GetComponentInParent<MimicBoss>() == boss)
                    used.Add(existing.SkillId);
            int id = Mathf.Max(1, pattern.SkillId);
            while (used.Contains(id)) id++;
            var data = new SerializedObject(pattern);
            data.FindProperty("skillId").intValue = id;
            data.ApplyModifiedProperties();
            Selection.activeGameObject = child;
        }
    }
}
