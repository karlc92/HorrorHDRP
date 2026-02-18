using UnityEngine;
using Animancer;

public class MainMenuMonster : MonoBehaviour
{
    public AnimationClip idleAnim;
    private AnimancerComponent animancer;
    void Awake()
    {
        if (!animancer)
        {
            animancer = GetComponent<AnimancerComponent>();
        }

        if (!animancer.IsPlaying())
        {
            animancer.Play(idleAnim);
        }
        
    }

    void Update()
    {
        
    }
}
