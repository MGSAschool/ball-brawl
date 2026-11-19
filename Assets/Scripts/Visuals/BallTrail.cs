using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(BallSkin))]
public class BallTrail : MonoBehaviour
{
    [SerializeField] private float trailSpeed = 12f;
    [SerializeField] private float trailTime = 0.25f;

    private BallSkin skin;
    private Rigidbody rb;
    private TrailRenderer trail;
    private bool wasShown = true;

    private void Awake()
    {
        skin = GetComponent<BallSkin>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        skin.Applied += BuildTrail;
    }

    private void OnDisable()
    {
        skin.Applied -= BuildTrail;
    }

    private void LateUpdate()
    {
        if (trail == null) return;

        bool shown = skin.IsShown;
        if (shown && !wasShown) trail.Clear();
        wasShown = shown;

        float speed = 0f;
        if (rb != null && !rb.isKinematic) speed = rb.linearVelocity.magnitude;
        trail.emitting = shown && speed > trailSpeed;
    }

    private void BuildTrail()
    {
        if (trail != null) return;

        GameObject go = new GameObject("Speed Trail");
        go.transform.SetParent(transform, false);
        trail = go.AddComponent<TrailRenderer>();
        trail.sharedMaterial = VfxMaterials.Alpha();
        trail.time = trailTime;
        trail.minVertexDistance = 0.1f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, skin.Diameter * 0.75f), new Keyframe(1f, 0f));
        trail.shadowCastingMode = ShadowCastingMode.Off;
        trail.emitting = false;

        Color color = skin.PlayerColor;
        GradientColorKey[] colors = { new GradientColorKey(Color.Lerp(color, Color.white, 0.4f), 0f), new GradientColorKey(color, 1f) };
        GradientAlphaKey[] alphas = { new GradientAlphaKey(0.7f, 0f), new GradientAlphaKey(0f, 1f) };
        Gradient gradient = new Gradient();
        gradient.SetKeys(colors, alphas);
        trail.colorGradient = gradient;
    }
}
