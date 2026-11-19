using UnityEngine;

[RequireComponent(typeof(ArenaSection), typeof(SectionPaint))]
public class SectionFreeze : MonoBehaviour
{
    public enum FreezeState { None, Warning, Frozen }

    private const float ThawWarning = 1f;

    private ArenaSection section;
    private SectionPaint paint;
    private MeshCollider col;
    private PhysicsMaterial normalMaterial;
    private PhysicsMaterial iceMaterial;
    private Color iceColor;
    private float startTime;
    private float warningDuration;
    private float frozenDuration;

    public FreezeState State { get; private set; } = FreezeState.None;
    public bool IsFrozen { get { return State == FreezeState.Frozen; } }

    private void Awake()
    {
        section = GetComponent<ArenaSection>();
        paint = GetComponent<SectionPaint>();
        col = GetComponent<MeshCollider>();
        normalMaterial = col.sharedMaterial;
    }

    public void TriggerFreeze(float warning, float frozen, Color ice, PhysicsMaterial slippery)
    {
        if (!section.IsSolid || State != FreezeState.None) return;

        warningDuration = warning;
        frozenDuration = frozen;
        iceColor = ice;
        iceMaterial = slippery;
        State = FreezeState.Warning;
        startTime = Time.time;
    }

    public void Clear()
    {
        if (State == FreezeState.None) return;
        State = FreezeState.None;
        col.sharedMaterial = normalMaterial;
    }

    private void Update()
    {
        if (State == FreezeState.None) return;

        float elapsed = Time.time - startTime;
        if (State == FreezeState.Warning)
        {
            float t = Mathf.Clamp01(elapsed / warningDuration);
            float flash = Mathf.PingPong(elapsed * Mathf.Lerp(3f, 10f, t), 1f);
            paint.Show(Color.Lerp(paint.BaseColor, iceColor, flash));

            if (elapsed >= warningDuration)
            {
                State = FreezeState.Frozen;
                startTime = Time.time;
                Sfx.Play(Sound.Freeze, transform.position, 0.6f);
                col.sharedMaterial = iceMaterial;
                paint.Show(iceColor);
            }
        }
        else if (elapsed >= frozenDuration)
        {
            Clear();
            paint.ShowBase();
        }
        else if (elapsed >= frozenDuration - ThawWarning)
        {
            float flicker = Mathf.PingPong(elapsed * 6f, 1f) * 0.6f;
            paint.Show(Color.Lerp(iceColor, paint.BaseColor, flicker));
        }
    }
}
