using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] GameObject dialogueBg;
    [SerializeField] TextMeshProUGUI dialogueText;

    private bool dialogSequencePlaying = false;
    private Coroutine playRoutine = null;
    private AudioSource currentAudioSource = null;
    private RawImage dialogueBgImage = null;
    private float dialogueBgBaseAlpha = 1f;

    public static DialogueManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (dialogueBg != null)
            dialogueBgImage = dialogueBg.GetComponentInChildren<RawImage>(true);

        if (dialogueBgImage != null)
            dialogueBgBaseAlpha = dialogueBgImage.color.a;

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

    public void StopDialogueSequence()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (currentAudioSource != null)
        {
            currentAudioSource.Stop();
            currentAudioSource.clip = null;
            currentAudioSource = null;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
            var c = dialogueText.color;
            c.a = 0f;
            dialogueText.color = c;
        }

        if (dialogueBgImage != null)
        {
            var bgc = dialogueBgImage.color;
            bgc.a = 0f;
            dialogueBgImage.color = bgc;
        }

        if (dialogueBg != null && dialogueBg.activeSelf)
            dialogueBg.SetActive(false);

        dialogSequencePlaying = false;
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

        if (targetAudioSource == null || dialogueText == null || dialogueBg == null)
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

        dialogueText.text = "";
        dialogSequencePlaying = true;
        currentAudioSource = targetAudioSource;
        playRoutine = StartCoroutine(PlaySequence());

        IEnumerator PlaySequence()
        {
            var bgImage = dialogueBgImage != null ? dialogueBgImage : dialogueBg.GetComponentInChildren<RawImage>(true);
            var bgFadeDuration = 0.15f;
            var bgTargetAlpha = dialogueBgBaseAlpha;

            if (bgImage != null)
            {
                var bgc = bgImage.color;
                bgc.a = 0f;
                bgImage.color = bgc;
            }

            dialogueBg.SetActive(true);

            IEnumerator FadeBg(float from, float to, float duration)
            {
                if (bgImage == null)
                    yield break;

                if (duration <= 0f)
                {
                    var c0 = bgImage.color;
                    c0.a = to;
                    bgImage.color = c0;
                    yield break;
                }

                var t0 = 0f;
                var c = bgImage.color;
                while (t0 < duration)
                {
                    t0 += Time.unscaledDeltaTime;
                    c.a = Mathf.Lerp(from, to, Mathf.Clamp01(t0 / duration));
                    bgImage.color = c;
                    yield return null;
                }

                c.a = to;
                bgImage.color = c;
            }

            if (bgImage != null)
                yield return FadeBg(0f, bgTargetAlpha, bgFadeDuration);

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

                if (targetAudioSource.isPlaying)
                    targetAudioSource.Stop();

                targetAudioSource.volume = 0.5f * Game.Settings.MasterVolume;
                targetAudioSource.clip = clip;
                targetAudioSource.Play();

                var fadeDuration = 0.15f;
                if (clip.length > 0f)
                    fadeDuration = Mathf.Min(fadeDuration, clip.length * 0.5f);

                while (targetAudioSource.isPlaying && targetAudioSource.time < fadeDuration)
                {
                    c.a = Mathf.Clamp01(targetAudioSource.time / fadeDuration);
                    dialogueText.color = c;
                    yield return null;
                }

                c.a = 1f;
                dialogueText.color = c;

                var fadeOutStart = Mathf.Max(clip.length - fadeDuration, 0f);
                while (targetAudioSource.isPlaying && targetAudioSource.time < fadeOutStart)
                    yield return null;

                while (targetAudioSource.isPlaying)
                {
                    var remaining = clip.length - targetAudioSource.time;
                    c.a = Mathf.Clamp01(remaining / fadeDuration);
                    dialogueText.color = c;

                    if (remaining <= 0f)
                        break;

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

            if (bgImage != null)
                yield return FadeBg(bgTargetAlpha, 0f, bgFadeDuration);

            dialogueBg.SetActive(false);

            dialogSequencePlaying = false;
            currentAudioSource = null;
            playRoutine = null;
        }
    }
}