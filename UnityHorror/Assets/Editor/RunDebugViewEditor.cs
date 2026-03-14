using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RunDebugView))]
public class RunDebugViewEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Run Debug", EditorStyles.boldLabel);

        DrawStateSummary();

        EditorGUILayout.Space();
        DrawActions();
    }

    private static void DrawStateSummary()
    {
        var state = Game.State;
        if (state == null)
        {
            EditorGUILayout.HelpBox("Game.State is null.", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField("Has Progression", (state.Progression != null).ToString());
        EditorGUILayout.LabelField("Has Active Run", (state.Run != null).ToString());

        if (state.Run == null)
            return;

        EditorGUILayout.LabelField("Seed", state.Run.Seed.ToString());
        EditorGUILayout.LabelField("Current Night", state.Run.CurrentNightNumber.ToString());
        EditorGUILayout.LabelField("Night Started", state.Run.NightStarted.ToString());
        EditorGUILayout.LabelField("Can End Night", state.Run.CurrentNightState != null && state.Run.CurrentNightState.CanEndNight ? "True" : "False");

        if (state.Run.Plan != null)
        {
            EditorGUILayout.LabelField("Planned Nights", state.Run.Plan.Nights != null ? state.Run.Plan.Nights.Count.ToString() : "0");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Current Night Tasks", EditorStyles.boldLabel);

        var tasks = state.Run.CurrentNightState != null ? state.Run.CurrentNightState.Tasks : null;
        if (tasks == null || tasks.Count == 0)
        {
            EditorGUILayout.LabelField("No current-night tasks.");
            return;
        }

        foreach (var task in tasks)
        {
            if (task == null)
                continue;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Definition", task.TaskDefinitionId);
                EditorGUILayout.LabelField("Instance", task.TaskInstanceId);
                EditorGUILayout.LabelField("Stage", task.CurrentStageIndex.ToString());
                EditorGUILayout.LabelField("Completed", task.Completed.ToString());
                EditorGUILayout.LabelField("Required", task.RequiredForNightCompletion.ToString());
            }
        }

        EditorGUILayout.Space();
        var plan = RunManager.Instance != null ? RunManager.Instance.GetCurrentNightPlan() : null;
        if (plan != null)
        {
            EditorGUILayout.LabelField("Current Night Plan", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Plan Night", plan.NightNumber.ToString());
            EditorGUILayout.LabelField("Planned Tasks", plan.Tasks != null ? plan.Tasks.Count.ToString() : "0");
            EditorGUILayout.LabelField("Planned Lore", plan.Lore != null ? plan.Lore.Count.ToString() : "0");
            EditorGUILayout.LabelField("Active Zones", plan.ActiveZoneIds != null ? plan.ActiveZoneIds.Count.ToString() : "0");
        }
    }

    private static void DrawActions()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Game", EditorStyles.boldLabel);

            if (GUILayout.Button("Load Game State"))
            {
                Game.LoadGameState();
                RepaintAllInspectors();
            }

            if (GUILayout.Button("Save Game State"))
            {
                Game.SaveGameState();
                RepaintAllInspectors();
            }

            if (GUILayout.Button("Start New Run"))
            {
                Game.StartNewRun();
                RepaintAllInspectors();
            }

            GUI.enabled = Game.HasActiveRun();
            if (GUILayout.Button("Continue Run"))
            {
                Game.ContinueRun();
            }

            if (GUILayout.Button("Give Up Run"))
            {
                Game.GiveUpRun();
                RepaintAllInspectors();
            }
            GUI.enabled = true;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Night", EditorStyles.boldLabel);

            GUI.enabled = Application.isPlaying && RunManager.Instance != null && Game.State?.Run != null;

            if (GUILayout.Button("Start Night"))
            {
                RunManager.Instance.DebugStartNight();
                RepaintAllInspectors();
            }

            if (GUILayout.Button("Complete Current Tasks"))
            {
                RunManager.Instance.DebugCompleteCurrentNightTasks();
                RepaintAllInspectors();
            }

            if (GUILayout.Button("Refresh Can End Night"))
            {
                RunManager.Instance.DebugRefreshNightState();
                RepaintAllInspectors();
            }

            if (GUILayout.Button("End Night"))
            {
                RunManager.Instance.TryEndNight();
                RepaintAllInspectors();
            }

            if (GUILayout.Button("Retry Current Night"))
            {
                RunManager.Instance.RetryCurrentNight();
                RepaintAllInspectors();
            }

            GUI.enabled = true;
        }
    }

    private static void RepaintAllInspectors()
    {
        ActiveEditorTracker.sharedTracker.ForceRebuild();
    }
}
