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
    [SerializeField] private float characterDelay = 0.045f;
    [SerializeField] private float holdAfterFullText = 3f;
    [SerializeField] private float backgroundFadeOutDuration = 1f;
    [SerializeField] private float textStayAfterFade = 2f;

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
        if (introText != null)
        {
            introText.text = string.Empty;
            introText.gameObject.SetActive(true);
        }

        SetAllCamerasInactive();

        if (backgroundImage != null)
        {
            SetImageAlpha(backgroundImage, 1f);
            backgroundImage.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        sequenceRoutine = StartCoroutine(PlaySequence());
    }

    public void Play()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
        }

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

        // Start on black screen.
        yield return new WaitForSeconds(initialBlackScreenDuration);

        // Type intro text.
        if (introText != null)
        {
            yield return StartCoroutine(TypeText(textToType));
            yield return new WaitForSeconds(holdAfterFullText);
        }

        // Fade black background away.
        if (backgroundImage != null)
        {
            yield return StartCoroutine(FadeImageAlpha(backgroundImage, 1f, 0f, backgroundFadeOutDuration));
        }

        // Keep text visible briefly after fade.
        if (introText != null)
        {
            yield return new WaitForSeconds(textStayAfterFade);
            introText.gameObject.SetActive(false);
        }

        // Wait before camera sequence begins.
        if (delayBeforeFirstShot > 0f)
        {
            yield return new WaitForSeconds(delayBeforeFirstShot);
        }

        // Play all camera shots in order.
        for (int i = 0; i < cameraShots.Count; i++)
        {
            yield return StartCoroutine(PlayShot(cameraShots[i]));
        }

        onSequenceFinishedBeforeLoad?.Invoke();

        // Fade black back in, then load game scene.
        yield return StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator TypeText(string fullText)
    {
        introText.text = string.Empty;

        for (int i = 0; i < fullText.Length; i++)
        {
            char c = fullText[i];
            introText.text += c;

            // Spaces appear instantly with no sound and no delay.
            if (c == ' ')
            {
                continue;
            }

            PlayTypeSound();
            yield return new WaitForSeconds(characterDelay);
        }
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
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;

        Vector3 targetStartPos = startPos;
        Quaternion targetStartRot = startRot;
        Vector3 targetEndPos = startPos;
        Quaternion targetEndRot = startRot;

        if (shot.shouldLerp)
        {
            if (shot.lerpStartPoint != null)
            {
                targetStartPos = shot.lerpStartPoint.position;
                targetStartRot = shot.lerpStartPoint.rotation;
            }

            if (shot.lerpEndPoint != null)
            {
                targetEndPos = shot.lerpEndPoint.position;
                targetEndRot = shot.lerpEndPoint.rotation;
            }
            else
            {
                targetEndPos = targetStartPos;
                targetEndRot = targetStartRot;
            }

            camTransform.SetPositionAndRotation(targetStartPos, targetStartRot);
        }

        List<ShotTimedEvent> sortedEvents = new List<ShotTimedEvent>(shot.timedEvents);
        sortedEvents.Sort((a, b) => a.normalizedTime.CompareTo(b.normalizedTime));
        int nextEventIndex = 0;

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, shot.duration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            if (shot.shouldLerp)
            {
                float curvedT = shot.lerpCurve != null ? shot.lerpCurve.Evaluate(normalized) : normalized;
                camTransform.position = Vector3.LerpUnclamped(targetStartPos, targetEndPos, curvedT);
                camTransform.rotation = Quaternion.SlerpUnclamped(targetStartRot, targetEndRot, curvedT);
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
            camTransform.SetPositionAndRotation(targetEndPos, targetEndRot);
        }

        shot.onShotFinished?.Invoke();
        shot.cameraToUse.gameObject.SetActive(false);
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

    private void SetAllCamerasInactive()
    {
        for (int i = 0; i < cameraShots.Count; i++)
        {
            if (cameraShots[i] != null && cameraShots[i].cameraToUse != null)
            {
                cameraShots[i].cameraToUse.gameObject.SetActive(false);
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