using System;
using System.Collections.Generic;

[Serializable]
public class LocalizationLanguageData
{
    public string Language;
    public List<LocalizationEntry> Entries = new List<LocalizationEntry>();
}

[Serializable]
public class LocalizationEntry
{
    public string Key;
    public string Value;
}
