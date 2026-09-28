using KimLIb.EventSystem;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[CustomEditor(typeof(TimelineBubbleDialogSignal))]
public sealed class TimelineBubbleDialogSignalEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        using (new EditorGUI.DisabledScope(Application.isPlaying))
        {
            if (GUILayout.Button("Assign Default Channels"))
            {
                serializedObject.Update();
                serializedObject.FindProperty("bubbleDialogEventChannel").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<EventChannelSO>("Assets/Member/YKJ/GameAsset/Channel/BubbleDialogEventChannel.asset");
                serializedObject.FindProperty("cameraEventChannel").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<EventChannelSO>("Assets/Member/YKJ/GameAsset/Channel/CameraEventChannel.asset");
                serializedObject.FindProperty("playableDirector").objectReferenceValue =
                    ((TimelineBubbleDialogSignal)target).GetComponent<PlayableDirector>();
                serializedObject.ApplyModifiedProperties();
            }
            if (GUILayout.Button("Create Bubble Signal")) CreateSignal();
        }
    }

    private void CreateSignal()
    {
        string path = EditorUtility.SaveFilePanelInProject("Create Bubble Signal", "BubbleDialogSignal", "signal", "Choose the signal asset location.");
        if (string.IsNullOrEmpty(path)) return;
        var bridge = (TimelineBubbleDialogSignal)target;
        var director = serializedObject.FindProperty("playableDirector").objectReferenceValue as PlayableDirector;
        GameObject receiverObject = director != null ? director.gameObject : bridge.gameObject;
        SignalReceiver receiver = receiverObject.GetComponent<SignalReceiver>();
        if (receiver == null) receiver = Undo.AddComponent<SignalReceiver>(receiverObject);
        var signal = CreateInstance<SignalAsset>();
        AssetDatabase.CreateAsset(signal, AssetDatabase.GenerateUniqueAssetPath(path));
        Undo.RecordObject(receiver, "Connect Bubble Signal");
        var reaction = new UnityEvent();
        var npc = serializedObject.FindProperty("target").objectReferenceValue as Transform;
        UnityEventTools.AddObjectPersistentListener<Transform>(reaction, bridge.PlayBubbleDialogForTarget, npc);
        receiver.AddReaction(signal, reaction);
        EditorUtility.SetDirty(receiver);
        AssetDatabase.SaveAssetIfDirty(signal);
        EditorGUIUtility.PingObject(signal);
    }
}
