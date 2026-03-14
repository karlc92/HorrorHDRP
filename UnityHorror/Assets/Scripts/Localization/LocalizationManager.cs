using System;
using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [SerializeField] private string fallbackLanguage = "English";

    private readonly Dictionary<string, string> valuesByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Reload();
    }

    public void Reload()
    {
        valuesByKey.Clear();

        string language = Game.Settings.Language.ToString();
        if (!TryLoadLanguage(language) && !string.Equals(language, fallbackLanguage, StringComparison.OrdinalIgnoreCase))
            TryLoadLanguage(fallbackLanguage);
    }

    public string Get(string key, string fallback = "")
    {
        if (string.IsNullOrWhiteSpace(key))
            return fallback;

        return valuesByKey.TryGetValue(key, out var value) ? value : fallback;
    }

    private bool TryLoadLanguage(string language)
    {
        var asset = Resources.Load<TextAsset>($"Localization/{language}");
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            return false;

        var data = JsonUtility.FromJson<LocalizationLanguageData>(asset.text);
        if (data == null || data.Entries == null)
            return false;

        foreach (var entry in data.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                continue;

            valuesByKey[entry.Key] = entry.Value ?? string.Empty;
        }

        return true;
    }
}
