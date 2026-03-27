using System;
using System.Collections.Generic;
using UnityEngine;

public class StoryGameManager : MonoBehaviour
{
    [Serializable]
    private class StoryBeatDefinition
    {
        public string BeatId = string.Empty;
        public string Title = "Objective";

        [TextArea]
        public string Detail = string.Empty;

        [Tooltip("If set, only this trigger can advance the active story beat.")]
        public string CompletionTriggerId = string.Empty;

        [Tooltip("Optional checkpoint label stored in the save when this beat becomes active.")]
        public string CheckpointId = string.Empty;

        public Transform Checkpoint;
    }

    public static StoryGameManager Instance { get; private set; }

    [SerializeField] private Transform defaultCheckpoint;
    [SerializeField] private List<StoryBeatDefinition> storyBeats = new List<StoryBeatDefinition>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (Game.State == null)
            return;

        Game.State.EnsureInitialized();
        EnsureStoryState();
    }

    public string GetCurrentObjectiveTitle()
    {
        return Game.State?.Story?.CurrentObjectiveTitle ?? "Objective";
    }

    public string GetCurrentObjectiveDetail()
    {
        return Game.State?.Story?.CurrentObjectiveDetail ?? string.Empty;
    }

    public bool IsTriggerRelevant(string triggerId)
    {
        if (string.IsNullOrWhiteSpace(triggerId))
            return false;

        var story = Game.State?.Story;
        if (story == null || story.Completed)
            return false;

        if (storyBeats.Count == 0 || string.IsNullOrWhiteSpace(story.CurrentTriggerId))
            return true;

        return string.Equals(story.CurrentTriggerId, triggerId, StringComparison.OrdinalIgnoreCase);
    }

    public bool TryAdvanceFromTrigger(string triggerId)
    {
        if (!IsTriggerRelevant(triggerId))
            return false;

        return AdvanceStory();
    }

    public bool AdvanceStory()
    {
        if (Game.State?.Story == null)
            return false;

        var story = Game.State.Story;
        if (story.Completed)
            return false;

        string completedBeatId = GetCurrentBeatId();
        if (!string.IsNullOrWhiteSpace(completedBeatId) && !story.CompletedBeatIds.Contains(completedBeatId))
            story.CompletedBeatIds.Add(completedBeatId);

        if (storyBeats.Count == 0 || story.CurrentBeatIndex >= storyBeats.Count - 1)
        {
            story.Completed = true;
            story.CurrentObjectiveTitle = "Story Complete";
            story.CurrentObjectiveDetail = "This save slot has reached the end of the current story setup.";
            story.CurrentTriggerId = string.Empty;
            Game.SaveGameState();
            return true;
        }

        story.CurrentBeatIndex++;
        ApplyBeat(storyBeats[story.CurrentBeatIndex]);
        Game.SaveGameState();
        return true;
    }

    public void CaptureCheckpointAtPlayer(string checkpointId = null)
    {
        if (Game.State == null)
            return;

        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
            return;

        Game.State.PlayerPos = player.transform.position;
        Game.State.PlayerRot = player.transform.rotation;
        Game.State.Story.LastCheckpointId = string.IsNullOrWhiteSpace(checkpointId)
            ? GetCurrentBeatId()
            : checkpointId;
    }

    public void NotifyStoryStarted()
    {
        if (Game.State?.Story == null)
            return;

        Game.State.Story.Started = true;
        Game.SaveGameState();
    }

    private void EnsureStoryState()
    {
        var story = Game.State.Story;
        story.Started = true;

        if (storyBeats.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(story.CurrentBeatId))
                story.CurrentBeatId = "opening";

            if (string.IsNullOrWhiteSpace(story.CurrentObjectiveTitle))
                story.CurrentObjectiveTitle = "Begin the story";

            if (string.IsNullOrWhiteSpace(story.CurrentObjectiveDetail))
                story.CurrentObjectiveDetail = "Explore the space and define the next critical story interaction.";

            return;
        }

        int beatIndex = Mathf.Clamp(story.CurrentBeatIndex, 0, storyBeats.Count - 1);
        story.CurrentBeatIndex = beatIndex;
        ApplyBeat(storyBeats[beatIndex], saveCheckpointPose: false);
    }

    private void ApplyBeat(StoryBeatDefinition beat, bool saveCheckpointPose = true)
    {
        if (beat == null || Game.State?.Story == null)
            return;

        Game.State.Story.CurrentBeatId = string.IsNullOrWhiteSpace(beat.BeatId)
            ? $"beat-{Game.State.Story.CurrentBeatIndex + 1}"
            : beat.BeatId;
        Game.State.Story.CurrentObjectiveTitle = beat.Title;
        Game.State.Story.CurrentObjectiveDetail = beat.Detail;
        Game.State.Story.CurrentTriggerId = beat.CompletionTriggerId ?? string.Empty;

        Transform checkpoint = beat.Checkpoint != null ? beat.Checkpoint : defaultCheckpoint;
        if (checkpoint == null)
            return;

        Game.State.Story.LastCheckpointId = string.IsNullOrWhiteSpace(beat.CheckpointId)
            ? Game.State.Story.CurrentBeatId
            : beat.CheckpointId;

        if (!saveCheckpointPose)
            return;

        Game.State.PlayerPos = checkpoint.position;
        Game.State.PlayerRot = checkpoint.rotation;
    }

    private string GetCurrentBeatId()
    {
        return Game.State?.Story?.CurrentBeatId ?? string.Empty;
    }
}
