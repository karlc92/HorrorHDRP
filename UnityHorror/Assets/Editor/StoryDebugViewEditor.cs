using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StoryDebugView))]
public class StoryDebugViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Story Debug", EditorStyles.boldLabel);

        var state = Game.State;
        if (state == null)
        {
            EditorGUILayout.HelpBox("Game.State is null.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Active Slot", Game.ActiveSlot.ToString());
        EditorGUILayout.LabelField("Story Started", (state.Story?.Started ?? false).ToString());
        EditorGUILayout.LabelField("Story Complete", (state.Story?.Completed ?? false).ToString());
        EditorGUILayout.LabelField("Current Beat", state.Story != null ? state.Story.CurrentBeatId : string.Empty);
        EditorGUILayout.LabelField("Objective", state.Story != null ? state.Story.CurrentObjectiveTitle : string.Empty);
        EditorGUILayout.LabelField("Checkpoint", state.Story != null ? state.Story.LastCheckpointId : string.Empty);

        EditorGUILayout.Space();

        if (GUILayout.Button("Load Active Slot"))
            Game.LoadGameState();

        if (GUILayout.Button("Save Active Slot"))
            Game.SaveGameState();

        if (GUILayout.Button("Start New Game In Active Slot"))
            Game.StartNewGame(Game.ActiveSlot);

        if (GUILayout.Button("Continue Active Slot"))
            Game.ContinueGame();

        if (GUILayout.Button("Advance Story"))
            StoryGameManager.Instance?.AdvanceStory();
    }
}
