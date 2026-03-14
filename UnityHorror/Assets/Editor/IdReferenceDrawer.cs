using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(IdReferenceAttribute))]
public class IdReferenceDrawer : PropertyDrawer
{
    private const float LineHeight = 18f;
    private const float VerticalSpacing = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (IsStringArray(property))
        {
            if (!property.isExpanded)
                return LineHeight;

            int lines = 2 + property.arraySize;
            return (LineHeight * lines) + (VerticalSpacing * (lines - 1));
        }

        return LineHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attributeData = (IdReferenceAttribute)attribute;
        var options = CollectOptions(attributeData);

        if (property.propertyType == SerializedPropertyType.String)
        {
            DrawStringPopup(position, property, label, attributeData, options);
            return;
        }

        if (IsStringArray(property))
        {
            DrawStringArray(position, property, label, attributeData, options);
            return;
        }

        EditorGUI.PropertyField(position, property, label, true);
    }

    private static bool IsStringArray(SerializedProperty property)
    {
        return property.isArray && property.propertyType != SerializedPropertyType.String;
    }

    private static void DrawStringPopup(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        IdReferenceAttribute attributeData,
        List<string> options)
    {
        EditorGUI.BeginProperty(position, label, property);

        var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
        var buttonRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);
        EditorGUI.LabelField(labelRect, label);

        string displayText = string.IsNullOrWhiteSpace(property.stringValue) ? "<None>" : property.stringValue;
        if (GUI.Button(buttonRect, displayText, EditorStyles.popup))
        {
            var optionsWithContext = BuildOptionEntries(attributeData, property.stringValue);
            OpenSearchWindow(buttonRect, label.text, property.serializedObject, property.propertyPath, property.stringValue, optionsWithContext);
        }

        EditorGUI.EndProperty();
    }

    private static void DrawStringArray(
        Rect position,
        SerializedProperty property,
        GUIContent label,
        IdReferenceAttribute attributeData,
        List<string> options)
    {
        var foldoutRect = new Rect(position.x, position.y, position.width, LineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;

        var sizeRect = new Rect(position.x, position.y + LineHeight + VerticalSpacing, position.width, LineHeight);
        int newSize = Math.Max(0, EditorGUI.IntField(sizeRect, "Size", property.arraySize));
        if (newSize != property.arraySize)
            property.arraySize = newSize;

        float y = sizeRect.y + LineHeight + VerticalSpacing;
        for (int i = 0; i < property.arraySize; i++)
        {
            var element = property.GetArrayElementAtIndex(i);
            var elementRect = new Rect(position.x, y, position.width, LineHeight);
            DrawStringPopup(elementRect, element, new GUIContent($"Element {i}"), attributeData, options);
            y += LineHeight + VerticalSpacing;
        }

        EditorGUI.indentLevel--;
    }

    private static List<string> CollectOptions(IdReferenceAttribute attributeData)
    {
        return IdEditorUtility.CollectOptions(attributeData.SourceType, attributeData.IdMemberName, attributeData.Scope);
    }

    private static List<IdEditorUtility.IdOption> BuildOptionEntries(IdReferenceAttribute attributeData, string currentValue)
    {
        var entries = new List<IdEditorUtility.IdOption>
        {
            new IdEditorUtility.IdOption(string.Empty, string.Empty)
        };
        entries.AddRange(IdEditorUtility.CollectOptionEntries(attributeData.SourceType, attributeData.IdMemberName, attributeData.Scope));

        if (!string.IsNullOrWhiteSpace(currentValue) && !entries.Exists(e => string.Equals(e.Value, currentValue, System.StringComparison.OrdinalIgnoreCase)))
            entries.Add(new IdEditorUtility.IdOption(currentValue, "<Missing>"));
        else if (!string.IsNullOrWhiteSpace(currentValue) && entries.Count == 1)
            entries.Add(new IdEditorUtility.IdOption(currentValue, "<Missing>"));

        return entries;
    }

    private static void OpenSearchWindow(
        Rect buttonRect,
        string label,
        SerializedObject serializedObject,
        string propertyPath,
        string currentValue,
        List<IdEditorUtility.IdOption> options)
    {
        PopupWindow.Show(buttonRect, new IdReferenceSearchWindow(
            label,
            options,
            currentValue,
            selectedValue =>
            {
                if (serializedObject == null || serializedObject.targetObject == null)
                    return;

                serializedObject.Update();
                var refreshedProperty = serializedObject.FindProperty(propertyPath);
                if (refreshedProperty == null || refreshedProperty.propertyType != SerializedPropertyType.String)
                    return;

                refreshedProperty.stringValue = selectedValue ?? string.Empty;
                serializedObject.ApplyModifiedProperties();
            }));
    }
}
