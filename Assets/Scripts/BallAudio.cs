using UnityEngine;

[System.Serializable]
public struct AudioSegment
{
    public AudioClip clip;
    [Tooltip("Start timestamp in seconds inside the audio clip")]
    public float startTime;
    [Tooltip("Duration of this specific slice in seconds")]
    public float duration;
    [Range(0.5f, 2f)] public float pitchMin;
    [Range(0.5f, 2f)] public float pitchMax;
}

[RequireComponent(typeof(Rigidbody))]
public class BallAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [SerializeField] private AudioSource bounceSource;
    [SerializeField] private AudioSource rollSource;

    [Header("Bounce Audio Slices / Variants")]
    [SerializeField] private AudioSegment[] bounceVariants;
    [SerializeField] private float minImpactVelocity = 1.5f;

    [Header("Roll Settings")]
    [SerializeField] private float minRollSpeed = 0.5f;
    [SerializeField] private float maxRollSpeed = 12f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded = false;
    private Coroutine stopSliceCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Set up bounce source if not assigned
        if (bounceSource == null)
        {
            bounceSource = gameObject.AddComponent<AudioSource>();
            bounceSource.playOnAwake = false;
        }

        // Set up roll source if not assigned
        if (rollSource == null)
        {
            rollSource = gameObject.AddComponent<AudioSource>();
            rollSource.playOnAwake = false;
            rollSource.loop = true;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce >= minImpactVelocity && bounceVariants != null && bounceVariants.Length > 0)
        {
            PlayRandomBounceSlice(impactForce);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    private void Update()
    {
        UpdateRollSound();
    }

    private void PlayRandomBounceSlice(float impactForce)
    {
        int index = Random.Range(0, bounceVariants.Length);
        AudioSegment segment = bounceVariants[index];

        if (segment.clip == null) return;

        bounceSource.clip = segment.clip;
        bounceSource.time = Mathf.Clamp(segment.startTime, 0f, segment.clip.length - 0.05f);
        
        float pitchRange = segment.pitchMax > segment.pitchMin ? Random.Range(segment.pitchMin, segment.pitchMax) : 1f;
        bounceSource.pitch = pitchRange;

        // Scale volume with impact speed
        bounceSource.volume = Mathf.Clamp01(impactForce / 10f);
        bounceSource.Play();

        if (stopSliceCoroutine != null) StopCoroutine(stopSliceCoroutine);
        if (segment.duration > 0f)
        {
            stopSliceCoroutine = StartCoroutine(StopSliceAfterDuration(segment.duration));
        }
    }

    private System.Collections.IEnumerator StopSliceAfterDuration(float duration)
    {
        yield return new WaitForSeconds(duration);
        bounceSource.Stop();
    }

    private void UpdateRollSound()
    {
        if (rollSource.clip == null) return;

        // Ground check horizontal speed
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVel.magnitude;

        if (isGrounded && speed > minRollSpeed)
        {
            if (!rollSource.isPlaying) rollSource.Play();

            float t = Mathf.InverseLerp(minRollSpeed, maxRollSpeed, speed);
            rollSource.volume = Mathf.Lerp(0.1f, 1f, t);
            rollSource.pitch = Mathf.Lerp(0.8f, 1.25f, t);
        }
        else
        {
            if (rollSource.isPlaying) rollSource.Stop();
        }
    }
}