using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Horror/Run Generation Settings")]
public class RunGenerationSettings : ScriptableObject
{
    public int NightCount = 7;
    public List<int> NightDifficultyBudgets = new List<int> { 3, 4, 5, 6, 7, 8, 9 };
    public int MaxLorePerRun = 2;
}
