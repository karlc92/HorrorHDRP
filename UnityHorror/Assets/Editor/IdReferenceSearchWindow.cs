using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class IdReferenceSearchWindow : PopupWindowContent
{
    private readonly string title;
    private readonly List<IdEditorUtility.IdOption> options;
    private readonly Action<string> onSelected;
    private readonly string currentValue;

    private string search = string.Empty;
    private Vector2 scroll;
    private GUIStyle headerStyle;
    private bool hasFocusedSearchField;

    public IdReferenceSearchWindow(string title, IEnumerable<IdEditorUtility.IdOption> options, string currentValue, Action<string> onSelected)
    {
        this.title = title;
        this.options = options?.ToList() ?? new List<IdEditorUtility.IdOption>();
        this.currentValue = currentValue ?? string.Empty;
        this.onSelected = onSelected;
    }

    public override Vector2 GetWindowSize()
    {
        return new Vector2(420f, 320f);
    }

    public override void OnGUI(Rect rect)
    {
        headerStyle ??= new GUIStyle(EditorStyles.boldLabel);

        EditorGUILayout.LabelField(title, headerStyle);
        GUI.SetNextControlName("IdReferenceSearchField");
        search = EditorGUILayout.TextField(search);

        EditorGUILayout.Space(4f);

        using (var scrollView = new EditorGUILayout.ScrollViewScope(scroll))
        {
            scroll = scrollView.scrollPosition;

            DrawOption(string.Empty, "<None>", string.IsNullOrWhiteSpace(currentValue));

            foreach (var option in GetFilteredOptions())
            {
                DrawOption(option.Value, option.DisplayText, string.Equals(option.Value, currentValue, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (Event.current.type == EventType.Repaint)
        {
            if (!hasFocusedSearchField)
            {
                EditorGUI.FocusTextInControl("IdReferenceSearchField");
                hasFocusedSearchField = true;
            }
        }
    }

    private IEnumerable<IdEditorUtility.IdOption> GetFilteredOptions()
    {
        if (string.IsNullOrWhiteSpace(search))
            return options;

        return options.Where(o =>
            (!string.IsNullOrWhiteSpace(o.Value) && o.Value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
            (!string.IsNullOrWhiteSpace(o.Context) && o.Context.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private void DrawOption(string value, string displayText, bool isSelected)
    {
        var content = isSelected ? $"{displayText}  \u2713" : displayText;
        if (GUILayout.Button(content, EditorStyles.miniButton))
        {
            onSelected?.Invoke(value);
            editorWindow?.Close();
        }
    }
}
