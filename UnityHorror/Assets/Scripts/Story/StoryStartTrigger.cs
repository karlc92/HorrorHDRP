using UnityEngine;

public class StoryStartTrigger : MonoBehaviour
{
    private bool hasStartedStory;

    public void NotifyStoryStarted()
    {
        if (hasStartedStory)
            return;

        hasStartedStory = true;
        StoryGameManager.Instance?.NotifyStoryStarted();
    }
}
