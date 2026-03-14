using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public abstract class IdDefinitionEditorBase : Editor
{
    protected void DrawIdDefinitionInspector(string memberName, Type sourceType, IdReferenceScope scope, string label)
    {
        serializedObject.Update();
        DrawDefaultInspector();
        DrawIdActionButtons(memberName, label);

        if (targets == null || targets.Length != 1)
            return;

        var targetObject = target;
        if (targetObject == null)
            return;

        if (!IdEditorUtility.TryGetMemberStringValue(targetObject, memberName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            EditorGUILayout.HelpBox($"{label} is empty.", MessageType.Warning);
            return;
        }

        List<UnityEngine.Object> duplicates = IdEditorUtility.FindDuplicateObjects(targetObject, sourceType, memberName, scope);
        if (duplicates.Count == 0)
            return;

        string message = $"{label} '{value}' is duplicated by:\n- " +
            string.Join("\n- ", duplicates.ConvertAll(IdEditorUtility.GetObjectLabel));
        EditorGUILayout.HelpBox(message, MessageType.Error);
    }

    private void DrawIdActionButtons(string memberName, string label)
    {
        var property = serializedObject.FindProperty(memberName);
        if (property == null || property.propertyType != SerializedPropertyType.String)
            return;

        EditorGUILayout.Space(4f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Suggest From Name"))
            {
                property.stringValue = IdEditorUtility.CreateSuggestedId(target);
                serializedObject.ApplyModifiedProperties();
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(property.stringValue)))
            {
                if (GUILayout.Button("Copy ID"))
                    EditorGUIUtility.systemCopyBuffer = property.stringValue;
            }
        }
    }
}

[CustomEditor(typeof(TaskHook), true)]
public class TaskHookIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(TaskHook.HookId), typeof(TaskHook), IdReferenceScope.SceneObjects, "Hook ID");
    }
}

[CustomEditor(typeof(HoldConditionSource), true)]
public class HoldConditionSourceIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(HoldConditionSource.SourceId), typeof(HoldConditionSource), IdReferenceScope.SceneObjects, "Condition Source ID");
    }
}

[CustomEditor(typeof(Zone))]
public class ZoneIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(Zone.ZoneId), typeof(Zone), IdReferenceScope.SceneObjects, "Zone ID");
    }
}

[CustomEditor(typeof(TaskDefinition), true)]
public class TaskDefinitionIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(TaskDefinition.TaskId), typeof(TaskDefinition), IdReferenceScope.ResourcesAssets, "Task ID");
    }
}

[CustomEditor(typeof(LoreDefinition))]
public class LoreDefinitionIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(LoreDefinition.LoreId), typeof(LoreDefinition), IdReferenceScope.ResourcesAssets, "Lore ID");
    }
}

[CustomEditor(typeof(HeldItemDefinition))]
public class HeldItemDefinitionIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(HeldItemDefinition.ItemId), typeof(HeldItemDefinition), IdReferenceScope.ResourcesAssets, "Item ID");
    }
}

[CustomEditor(typeof(NightModifierDefinition), true)]
public class NightModifierDefinitionIdEditor : IdDefinitionEditorBase
{
    public override void OnInspectorGUI()
    {
        DrawIdDefinitionInspector(nameof(NightModifierDefinition.ModifierId), typeof(NightModifierDefinition), IdReferenceScope.ResourcesAssets, "Modifier ID");
    }
}
