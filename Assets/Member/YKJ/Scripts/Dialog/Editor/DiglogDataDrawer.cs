using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(DiglogData))]
public class DiglogDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        DialogDataSO dialogDataSO = property.serializedObject.targetObject as DialogDataSO;
        SerializedProperty idProperty = property.FindPropertyRelative("Id");
        SerializedProperty speakerNameProperty = property.FindPropertyRelative("SpeakerName");
        SerializedProperty speakerIconProperty = property.FindPropertyRelative("SpeakeIcon");
        SerializedProperty descriptionProperty = property.FindPropertyRelative("Description");

        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            Rect popupRect = GetLineRect(position, 1);
            DrawSpeakerPopup(popupRect, dialogDataSO, idProperty, speakerNameProperty);

            Rect iconRect = GetLineRect(position, 2);
            EditorGUI.PropertyField(iconRect, speakerIconProperty);

            Rect descriptionRect = GetDescriptionRect(position, descriptionProperty);
            EditorGUI.PropertyField(descriptionRect, descriptionProperty, true);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (property.isExpanded == false)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        SerializedProperty descriptionProperty = property.FindPropertyRelative("Description");
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float lineGap = EditorGUIUtility.standardVerticalSpacing;
        float descriptionHeight = Mathf.Max(EditorGUI.GetPropertyHeight(descriptionProperty, true), lineHeight * 3f);

        return lineHeight * 3f + lineGap * 3f + descriptionHeight;
    }

    private void DrawSpeakerPopup(Rect rect, DialogDataSO dialogDataSO, SerializedProperty idProperty, SerializedProperty speakerNameProperty)
    {
        if (dialogDataSO == null || dialogDataSO.DialogInitList == null || dialogDataSO.DialogInitList.Count == 0)
        {
            EditorGUI.LabelField(rect, "Speaker", "DialogInitList is empty");
            return;
        }

        string[] speakerNames = new string[dialogDataSO.DialogInitList.Count];
        int selectedIndex = 0;

        for (int i = 0; i < dialogDataSO.DialogInitList.Count; i++)
        {
            InitDialogData initData = dialogDataSO.DialogInitList[i];
            speakerNames[i] = string.IsNullOrEmpty(initData.SpeakerName) ? $"None ({i})" : initData.SpeakerName;

            if (initData.Id == idProperty.intValue)
            {
                selectedIndex = i;
            }
        }

        int newIndex = EditorGUI.Popup(rect, "Speaker", selectedIndex, speakerNames);
        InitDialogData selectedData = dialogDataSO.DialogInitList[newIndex];

        idProperty.intValue = selectedData.Id;
        speakerNameProperty.stringValue = selectedData.SpeakerName;
    }

    private Rect GetLineRect(Rect position, int line)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float lineGap = EditorGUIUtility.standardVerticalSpacing;
        return new Rect(position.x, position.y + (lineHeight + lineGap) * line, position.width, lineHeight);
    }

    private Rect GetDescriptionRect(Rect position, SerializedProperty descriptionProperty)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float lineGap = EditorGUIUtility.standardVerticalSpacing;
        float y = position.y + (lineHeight + lineGap) * 3f;
        float height = Mathf.Max(EditorGUI.GetPropertyHeight(descriptionProperty, true), lineHeight * 3f);

        return new Rect(position.x, y, position.width, height);
    }
}
