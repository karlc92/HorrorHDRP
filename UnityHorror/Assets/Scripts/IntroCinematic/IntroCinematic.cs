using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class IntroCinematic : MonoBehaviour
{
    [System.Serializable]
    public class ShotTimedEvent
    {
        [Range(0f, 1f)]
        public float normalizedTime = 0.5f;

        public UnityEvent onTriggered;
    }

    [System.Serializable]
    public class CameraShot
    {
        [Header("Camera")]
        public Camera cameraToUse;

        [Min(0f)]
        public float duration = 4f;

        [Min(0f)]
        public float preDelay = 0f;

        [Header("Optional Movement")]
        public bool shouldLerp = false;
        public Transform lerpStartPoint;
        public Transform lerpEndPoint;
        public AnimationCurve lerpCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Events")]
        public UnityEvent onShotStarted;
        public UnityEvent onShotFinished;

        [Header("Timed Events During Shot")]
        public List<ShotTimedEvent> timedEvents = new List<ShotTimedEvent>();
    }

    [Header("UI References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text introText;

    [Header("Typing")]
    [TextArea(3, 8)]
    [SerializeField] private string textToType = "The city never slept. Neither did the road.";
    [SerializeField] private float initialBlackScreenDuration = 2f;

    [SerializeField, Min(0.01f)] private float charactersPerSecond = 22f;
    [SerializeField, Range(0f, 0.5f)] private float typingSpeedVariation = 0.1f;

    [SerializeField] private float holdAfterFullText = 3f;
    [SerializeField] private float backgroundFadeOutDuration = 1f;
    [SerializeField] private float textStayAfterFade = 2f;

    [Header("Natural Typing Pauses")]
    [SerializeField, Min(0f)] private float commaPauseExtra = 0.08f;
    [SerializeField, Min(0f)] private float sentencePauseExtra = 0.18f;
    [SerializeField, Min(0f)] private float ellipsisPauseExtra = 0.24f;
    [SerializeField, Min(0f)] private float lineBreakPauseExtra = 0.12f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip typewriterClip;
    [SerializeField, Range(0f, 1f)] private float typewriterVolume = 1f;
    [SerializeField] private bool randomizeTypePitch = true;
    [SerializeField] private Vector2 typePitchRange = new Vector2(0.96f, 1.04f);

    [Header("Camera Sequence")]
    [SerializeField] private List<CameraShot> cameraShots = new List<CameraShot>();
    [SerializeField] private float delayBeforeFirstShot = 3f;

    [Header("Ending")]
    [SerializeField] private float endFadeDuration = 1f;
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("Sequence Events")]
    [SerializeField] private UnityEvent onSequenceStarted;
    [SerializeField] private UnityEvent onSequenceFinishedBeforeLoad;

    private Coroutine sequenceRoutine;

    private void Awake()
    {
        ResetVisualState();
        ForceDisableAllCamerasExceptFirst();
    }

    private void Start()
    {
        Play();
    }

    public void Play()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

        ResetVisualState();
        ForceDisableAllCamerasExceptFirst();
        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    public void SkipToGame()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator PlaySequence()
    {
        onSequenceStarted?.Invoke();

        // Force the intro into a known state:
        // all cameras disabled except the first configured camera.
        ForceDisableAllCamerasExceptFirst();

        yield return new WaitForSeconds(initialBlackScreenDuration);

        if (introText != null)
        {
            yield return StartCoroutine(TypeText(textToType));
            yield return new WaitForSeconds(holdAfterFullText);
        }

        if (backgroundImage != null)
        {
            yield return StartCoroutine(FadeImageAlpha(backgroundImage, 1f, 0f, backgroundFadeOutDuration));
        }

        if (introText != null)
        {
            yield return new WaitForSeconds(textStayAfterFade);
            introText.gameObject.SetActive(false);
        }

        if (delayBeforeFirstShot > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFirstShot);
        }

        for (int i = 0; i < cameraShots.Count; i++)
        {
            yield return StartCoroutine(PlayShot(cameraShots[i]));
        }

        onSequenceFinishedBeforeLoad?.Invoke();
        yield return StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator TypeText(string fullText)
    {
        if (introText == null)
            yield break;

        introText.text = string.Empty;

        for (int i = 0; i < fullText.Length; i++)
        {
            char c = fullText[i];
            introText.text += c;

            if (c == ' ')
            {
                continue;
            }

            PlayTypeSound();

            float delay = GetCharacterDelay(c, i, fullText);
            yield return new WaitForSeconds(delay);
        }
    }

    private float GetCharacterDelay(char c, int index, string fullText)
    {
        float variedMultiplier = Random.Range(1f - typingSpeedVariation, 1f + typingSpeedVariation);
        float actualCharactersPerSecond = Mathf.Max(0.01f, charactersPerSecond * variedMultiplier);
        float delay = 1f / actualCharactersPerSecond;

        if (c == ',' || c == ';' || c == ':')
        {
            delay += commaPauseExtra;
        }
        else if (c == '.' || c == '!' || c == '?')
        {
            bool isEllipsis =
                c == '.' &&
                index + 2 < fullText.Length &&
                fullText[index + 1] == '.' &&
                fullText[index + 2] == '.';

            delay += isEllipsis ? ellipsisPauseExtra : sentencePauseExtra;
        }
        else if (c == '\n' || c == '\r')
        {
            delay += lineBreakPauseExtra;
        }

        return delay;
    }

    private void PlayTypeSound()
    {
        if (audioSource == null || typewriterClip == null)
            return;

        float originalPitch = audioSource.pitch;

        if (randomizeTypePitch)
        {
            audioSource.pitch = Random.Range(typePitchRange.x, typePitchRange.y);
        }

        audioSource.PlayOneShot(typewriterClip, typewriterVolume);
        audioSource.pitch = originalPitch;
    }

    private IEnumerator PlayShot(CameraShot shot)
    {
        if (shot == null || shot.cameraToUse == null)
            yield break;

        if (shot.preDelay > 0f)
        {
            yield return new WaitForSeconds(shot.preDelay);
        }

        SetAllCamerasInactive();
        shot.cameraToUse.gameObject.SetActive(true);
        shot.onShotStarted?.Invoke();

        Transform camTransform = shot.cameraToUse.transform;

        if (shot.shouldLerp)
        {
            Vector3 initialStartPos = GetShotStartPosition(shot, camTransform.position);
            Quaternion initialStartRot = GetShotStartRotation(shot, camTransform.rotation);
            camTransform.SetPositionAndRotation(initialStartPos, initialStartRot);
        }

        List<ShotTimedEvent> sortedEvents = new List<ShotTimedEvent>(shot.timedEvents);
        sortedEvents.Sort((a, b) => a.normalizedTime.CompareTo(b.normalizedTime));

        int nextEventIndex = 0;

        while (nextEventIndex < sortedEvents.Count && sortedEvents[nextEventIndex].normalizedTime <= 0f)
        {
            sortedEvents[nextEventIndex].onTriggered?.Invoke();
            nextEventIndex++;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, shot.duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            if (shot.shouldLerp)
            {
                float curveT = shot.lerpCurve != null ? shot.lerpCurve.Evaluate(normalized) : normalized;

                Vector3 liveStartPos = GetShotStartPosition(shot, camTransform.position);
                Quaternion liveStartRot = GetShotStartRotation(shot, camTransform.rotation);

                Vector3 liveEndPos = GetShotEndPosition(shot, liveStartPos);
                Quaternion liveEndRot = GetShotEndRotation(shot, liveStartRot);

                camTransform.position = Vector3.LerpUnclamped(liveStartPos, liveEndPos, curveT);
                camTransform.rotation = Quaternion.SlerpUnclamped(liveStartRot, liveEndRot, curveT);
            }

            while (nextEventIndex < sortedEvents.Count &&
                   normalized >= sortedEvents[nextEventIndex].normalizedTime)
            {
                sortedEvents[nextEventIndex].onTriggered?.Invoke();
                nextEventIndex++;
            }

            yield return null;
        }

        if (shot.shouldLerp)
        {
            Vector3 finalStartPos = GetShotStartPosition(shot, camTransform.position);
            Quaternion finalStartRot = GetShotStartRotation(shot, camTransform.rotation);

            Vector3 finalEndPos = GetShotEndPosition(shot, finalStartPos);
            Quaternion finalEndRot = GetShotEndRotation(shot, finalStartRot);

            camTransform.SetPositionAndRotation(finalEndPos, finalEndRot);
        }

        shot.onShotFinished?.Invoke();
        shot.cameraToUse.gameObject.SetActive(false);
    }

    private Vector3 GetShotStartPosition(CameraShot shot, Vector3 fallback)
    {
        if (shot != null && shot.lerpStartPoint != null)
            return shot.lerpStartPoint.position;

        return fallback;
    }

    private Quaternion GetShotStartRotation(CameraShot shot, Quaternion fallback)
    {
        if (shot != null && shot.lerpStartPoint != null)
            return shot.lerpStartPoint.rotation;

        return fallback;
    }

    private Vector3 GetShotEndPosition(CameraShot shot, Vector3 fallback)
    {
        if (shot != null && shot.lerpEndPoint != null)
            return shot.lerpEndPoint.position;

        return fallback;
    }

    private Quaternion GetShotEndRotation(CameraShot shot, Quaternion fallback)
    {
        if (shot != null && shot.lerpEndPoint != null)
            return shot.lerpEndPoint.rotation;

        return fallback;
    }

    private IEnumerator FadeAndLoadScene()
    {
        SetAllCamerasInactive();

        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            float currentAlpha = backgroundImage.color.a;
            yield return StartCoroutine(FadeImageAlpha(backgroundImage, currentAlpha, 1f, endFadeDuration));
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void ResetVisualState()
    {
        if (introText != null)
        {
            introText.text = string.Empty;
            introText.gameObject.SetActive(true);
        }

        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(true);
            SetImageAlpha(backgroundImage, 1f);
        }
    }

    private void ForceDisableAllCamerasExceptFirst()
    {
        for (int i = 0; i < cameraShots.Count; i++)
        {
            Camera cam = cameraShots[i] != null ? cameraShots[i].cameraToUse : null;
            if (cam != null)
            {
                cam.gameObject.SetActive(i == 0);
            }
        }
    }

    private void SetAllCamerasInactive()
    {
        for (int i = 0; i < cameraShots.Count; i++)
        {
            Camera cam = cameraShots[i] != null ? cameraShots[i].cameraToUse : null;
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator FadeImageAlpha(Image image, float from, float to, float duration)
    {
        if (image == null)
            yield break;

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Color color = image.color;
            color.a = Mathf.Lerp(from, to, t);
            image.color = color;

            yield return null;
        }

        Color finalColor = image.color;
        finalColor.a = to;
        image.color = finalColor;
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}