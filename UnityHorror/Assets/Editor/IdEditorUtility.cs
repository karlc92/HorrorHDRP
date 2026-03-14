using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class IdEditorUtility
{
    public readonly struct IdOption
    {
        public readonly string Value;
        public readonly string Context;

        public IdOption(string value, string context)
        {
            Value = value;
            Context = context;
        }

        public string DisplayText => string.IsNullOrWhiteSpace(Context) ? Value : $"{Value}    [{Context}]";
    }

    public readonly struct IdMatch
    {
        public readonly UnityEngine.Object Object;
        public readonly string Value;

        public IdMatch(UnityEngine.Object obj, string value)
        {
            Object = obj;
            Value = value;
        }
    }

    public static List<string> CollectOptions(Type sourceType, string memberName, IdReferenceScope scope)
    {
        return CollectOptionEntries(sourceType, memberName, scope)
            .Select(m => m.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<IdOption> CollectOptionEntries(Type sourceType, string memberName, IdReferenceScope scope)
    {
        return CollectMatches(sourceType, memberName, scope)
            .Where(m => !string.IsNullOrWhiteSpace(m.Value))
            .GroupBy(m => m.Value, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                return new IdOption(first.Value, GetObjectLabel(first.Object));
            })
            .OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<UnityEngine.Object> FindDuplicateObjects(UnityEngine.Object target, Type sourceType, string memberName, IdReferenceScope scope)
    {
        if (target == null || !TryGetMemberStringValue(target, memberName, out var currentValue) || string.IsNullOrWhiteSpace(currentValue))
            return new List<UnityEngine.Object>();

        return CollectMatches(sourceType, memberName, scope)
            .Where(m => m.Object != null)
            .Where(m => !ReferenceEquals(m.Object, target))
            .Where(m => string.Equals(m.Value, currentValue, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Object)
            .Distinct()
            .ToList();
    }

    public static bool TryGetMemberStringValue(object target, string memberName, out string value)
    {
        value = null;
        if (target == null || string.IsNullOrWhiteSpace(memberName))
            return false;

        var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var field = target.GetType().GetField(memberName, flags);
        if (field != null && field.FieldType == typeof(string))
        {
            value = field.GetValue(target) as string;
            return true;
        }

        var property = target.GetType().GetProperty(memberName, flags);
        if (property != null && property.PropertyType == typeof(string) && property.GetIndexParameters().Length == 0)
        {
            value = property.GetValue(target) as string;
            return true;
        }

        return false;
    }

    public static string GetObjectLabel(UnityEngine.Object obj)
    {
        if (obj == null)
            return "<Missing>";

        if (EditorUtility.IsPersistent(obj))
        {
            string assetPath = AssetDatabase.GetAssetPath(obj);
            return string.IsNullOrWhiteSpace(assetPath) ? obj.name : assetPath;
        }

        if (obj is Component component)
            return $"{BuildHierarchyPath(component.transform)} ({component.GetType().Name})";

        if (obj is GameObject gameObject)
            return BuildHierarchyPath(gameObject.transform);

        return obj.name;
    }

    public static string CreateSuggestedId(UnityEngine.Object obj)
    {
        if (obj == null)
            return string.Empty;

        string rawName = obj is Component component ? component.gameObject.name : obj.name;
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        rawName = rawName.Trim().ToLowerInvariant();
        var chars = new List<char>(rawName.Length);
        bool previousWasSeparator = false;
        foreach (char c in rawName)
        {
            bool valid = char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_';
            if (valid)
            {
                chars.Add(c);
                previousWasSeparator = false;
                continue;
            }

            if (char.IsWhiteSpace(c) || c == '/' || c == '\\')
            {
                if (!previousWasSeparator)
                {
                    chars.Add('_');
                    previousWasSeparator = true;
                }
            }
        }

        var value = new string(chars.ToArray()).Trim('_', '.', '-');
        return value;
    }

    private static List<IdMatch> CollectMatches(Type sourceType, string memberName, IdReferenceScope scope)
    {
        return (scope == IdReferenceScope.ResourcesAssets
                ? CollectAssetMatches(sourceType, memberName)
                : CollectSceneMatches(sourceType, memberName))
            .Where(m => m.Object != null)
            .ToList();
    }

    private static IEnumerable<IdMatch> CollectSceneMatches(Type sourceType, string memberName)
    {
        foreach (var obj in Resources.FindObjectsOfTypeAll(sourceType))
        {
            if (obj is not UnityEngine.Object unityObject || EditorUtility.IsPersistent(unityObject))
                continue;

            if (unityObject is Component component && !component.gameObject.scene.IsValid())
                continue;

            if (TryGetMemberStringValue(unityObject, memberName, out var value))
                yield return new IdMatch(unityObject, value);
        }
    }

    private static IEnumerable<IdMatch> CollectAssetMatches(Type sourceType, string memberName)
    {
        foreach (var guid in AssetDatabase.FindAssets($"t:{sourceType.Name}"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath(path, sourceType);
            if (asset == null)
                continue;

            if (TryGetMemberStringValue(asset, memberName, out var value))
                yield return new IdMatch(asset, value);
        }
    }

    private static string BuildHierarchyPath(Transform transform)
    {
        var names = new Stack<string>();
        var current = transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }
}
