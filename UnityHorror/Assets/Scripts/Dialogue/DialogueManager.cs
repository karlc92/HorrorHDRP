using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject dialogueBg;
    [SerializeField] TextMeshProUGUI dialogueText;

    private bool dialogSequencePlaying = false;

    public static DialogueManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (dialogueBg.activeSelf)
        {
            dialogueBg.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PlayDialogueSequence(string sequenceName, AudioSource externalAudioSource = null)
    {
        AudioSource targetAudioSource;

        if (dialogSequencePlaying)
        {
            Console.Print("Dialog sequence already playing");
            return;
        }

        if (externalAudioSource != null)
        {
            targetAudioSource = externalAudioSource;
        }
        else
        {
            targetAudioSource = audioSource;
        }

        if (audioSource == null || dialogueText == null || dialogueBg == null)
        {
            Console.Print("DialogueManager is missing references");
            return;
        }

        var sequencePath = "Dialogue/" + Game.Settings.Language.ToString() + "/" + sequenceName + "/" + sequenceName;
        var sequenceObject = ResourceCache.Get<GameObject>(sequencePath);
        if (sequenceObject == null)
        {
            Console.Print("Error: Unable to find " + sequencePath);
            return;
        }

        var sequence = sequenceObject.GetComponent<DialogueSequence>();
        if (sequence == null)
        {
            Console.Print("Unable to find DialogSequence on " + sequencePath);
            return;
        }

        dialogSequencePlaying = true;
        StartCoroutine(PlaySequence());

        IEnumerator PlaySequence()
        {
            dialogueBg.SetActive(true);

            var lineCount = sequence.TextLines != null ? sequence.TextLines.Count : 0;
            var clipCount = sequence.AudioClips != null ? sequence.AudioClips.Count : 0;
            var count = Mathf.Min(lineCount, clipCount);

            for (var i = 0; i < count; i++)
            {
                dialogueText.text = sequence.TextLines[i] ?? string.Empty;

                var clip = sequence.AudioClips[i];
                if (clip == null)
                    continue;

                var c = dialogueText.color;
                c.a = 0f;
                dialogueText.color = c;

                targetAudioSource.Stop();
                targetAudioSource.volume = 0.5f * Game.Settings.MasterVolume;
                targetAudioSource.clip = clip;
                targetAudioSource.Play();

                var fadeDuration = 0.15f;
                if (clip.length > 0f)
                    fadeDuration = Mathf.Min(fadeDuration, clip.length * 0.5f);

                var t = 0f;
                while (t < fadeDuration)
                {
                    t += Time.deltaTime;
                    c.a = Mathf.Clamp01(t / fadeDuration);
                    dialogueText.color = c;
                    yield return null;
                }

                c.a = 1f;
                dialogueText.color = c;

                var fadeOutStart = Mathf.Max(clip.length - fadeDuration, 0f);
                while (targetAudioSource.isPlaying && targetAudioSource.time < fadeOutStart)
                    yield return null;

                t = 0f;
                while (t < fadeDuration && targetAudioSource.isPlaying)
                {
                    t += Time.deltaTime;
                    c.a = 1f - Mathf.Clamp01(t / fadeDuration);
                    dialogueText.color = c;
                    yield return null;
                }

                c.a = 0f;
                dialogueText.color = c;

                while (targetAudioSource.isPlaying)
                    yield return null;
            }

            targetAudioSource.Stop();
            targetAudioSource.clip = null;
            dialogueText.text = string.Empty;
            dialogueBg.SetActive(false);
            dialogSequencePlaying = false;
        }
    }
}