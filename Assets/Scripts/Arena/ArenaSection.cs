using UnityEngine;

// One piece of the segmented arena floor. When triggered it shakes and flashes as a
// warning, drops into the void, stays gone for a while, then rises back into place
// (or stays gone for good when StayDown is set). Separately, a solid section can freeze:
// it flashes icy blue as a warning, turns to slippery ice for a while, then thaws.
[RequireComponent(typeof(MeshRenderer), typeof(MeshCollider), typeof(Rigidbody))]
public class ArenaSection : MonoBehaviour
{
    public enum SectionState { Solid, Warning, Falling, Gone, Rising }
    public enum FreezeState { None, Warning, Frozen }

    public SectionState State { get; private set; } = SectionState.Solid;
    public bool CanFall { get; set; } = true;
    // While set, a section that is gone never rises back
    public bool StayDown { get; set; }
    public bool IsSolid => State == SectionState.Solid;
    public FreezeState Freeze { get; private set; } = FreezeState.None;
    public bool IsFrozen => Freeze == FreezeState.Frozen;

    // Polar footprint relative to the arena center, used to pick points on this section
    public float InnerRadius { get; private set; }
    public float OuterRadius { get; private set; }
    public float StartAngle { get; private set; }
    public float EndAngle { get; private set; }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private const float ShakeAmplitude = 0.08f;
    // Frozen floor flickers for this long before it thaws
    private const float ThawWarning = 1f;

    private Rigidbody rb;
    private MeshRenderer rend;
    private MeshCollider col;
    private MaterialPropertyBlock block;
    private Vector3 homePosition;
    private Color baseColor;
    private Color warningColor;
    private float stateStart;
    private float warningDuration, fallDuration, goneDuration, riseDuration, fallDistance;
    private float freezeStart, freezeWarningDuration, frozenDuration;
    private Color iceColor;
    private PhysicsMaterial normalMaterial, iceMaterial;

    public void Init(float innerRadius, float outerRadius, float startAngle, float endAngle, Color baseColor, Color warningColor)
    {
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
        StartAngle = startAngle;
        EndAngle = endAngle;
        this.baseColor = baseColor;
        this.warningColor = warningColor;

        rb = GetComponent<Rigidbody>();
        rend = GetComponent<MeshRenderer>();
        col = GetComponent<MeshCollider>();
        block = new MaterialPropertyBlock();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        homePosition = transform.position;
        normalMaterial = col.sharedMaterial;
        SetColor(baseColor);
    }

    public void TriggerFreeze(float warning, float frozen, Color iceColor, PhysicsMaterial iceMaterial)
    {
        if (State != SectionState.Solid || Freeze != FreezeState.None) return;

        freezeWarningDuration = warning;
        frozenDuration = frozen;
        this.iceColor = iceColor;
        this.iceMaterial = iceMaterial;
        Freeze = FreezeState.Warning;
        freezeStart = Time.time;
    }

    public void TriggerFall(float warning, float fall, float gone, float rise, float distance)
    {
        if (State != SectionState.Solid || !CanFall) return;

        warningDuration = warning;
        fallDuration = fall;
        goneDuration = gone;
        riseDuration = rise;
        fallDistance = distance;
        Enter(SectionState.Warning);
    }

    // Moves the spot this section rests at (used to place the island on a new side each match)
    public void SetHome(Vector3 position)
    {
        homePosition = position;
        if (State == SectionState.Solid)
        {
            rb.position = position;
            transform.position = position;
        }
    }

    // Removed instantly and kept hidden until Emerge (used for the late-match island)
    public void HideImmediate()
    {
        ClearFreeze();
        StayDown = true;
        Enter(SectionState.Gone);
        rend.enabled = false;
        col.enabled = false;
    }

    // Rises up from `distance` below its home position into place
    public void Emerge(float rise, float distance)
    {
        StayDown = false;
        riseDuration = rise;
        fallDistance = distance;
        rb.position = homePosition + Vector3.down * distance;
        transform.position = rb.position;
        SetColor(baseColor);
        rend.enabled = true;
        col.enabled = true;
        Enter(SectionState.Rising);
    }

    public void ResetImmediate()
    {
        ClearFreeze();
        StayDown = false;
        Enter(SectionState.Solid);
        rb.position = homePosition;
        transform.position = homePosition;
        rend.enabled = true;
        col.enabled = true;
        SetColor(baseColor);
    }

    // Random point on this section's top surface (world space), kept away from the edges
    public Vector3 RandomTopPoint(Vector3 arenaCenter, float topY, float margin)
    {
        float r0 = InnerRadius + margin;
        float r1 = Mathf.Max(r0, OuterRadius - margin);
        float angle = Random.Range(StartAngle, EndAngle);
        // Uniform by area
        float r = Mathf.Sqrt(Mathf.Lerp(r0 * r0, r1 * r1, Random.value));
        return arenaCenter + new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r) + Vector3.up * (topY - arenaCenter.y);
    }

    private void Enter(SectionState state)
    {
        State = state;
        stateStart = Time.time;
    }

    private void Update()
    {
        if (Freeze == FreezeState.None) return;

        float elapsed = Time.time - freezeStart;
        if (Freeze == FreezeState.Warning)
        {
            // Same idea as the fall warning: flash faster as the freeze gets closer
            float t = Mathf.Clamp01(elapsed / freezeWarningDuration);
            float flash = Mathf.PingPong(elapsed * Mathf.Lerp(3f, 10f, t), 1f);
            SetColor(Color.Lerp(baseColor, iceColor, flash));

            if (elapsed >= freezeWarningDuration)
            {
                Freeze = FreezeState.Frozen;
                freezeStart = Time.time;
                col.sharedMaterial = iceMaterial;
                SetColor(iceColor);
            }
        }
        else if (elapsed >= frozenDuration)
        {
            ClearFreeze();
            SetColor(baseColor);
        }
        else if (elapsed >= frozenDuration - ThawWarning)
        {
            // Flicker back toward normal just before thawing
            SetColor(Color.Lerp(iceColor, baseColor, Mathf.PingPong(elapsed * 6f, 1f) * 0.6f));
        }
    }

    private void ClearFreeze()
    {
        if (Freeze == FreezeState.None) return;
        Freeze = FreezeState.None;
        if (col == null) col = GetComponent<MeshCollider>();
        col.sharedMaterial = normalMaterial;
    }

    private void FixedUpdate()
    {
        if (State == SectionState.Solid) return;

        float elapsed = Time.time - stateStart;
        switch (State)
        {
            case SectionState.Warning:
            {
                // Shake harder and flash faster as the drop gets closer
                float t = Mathf.Clamp01(elapsed / warningDuration);
                Vector2 jitter = Random.insideUnitCircle * ShakeAmplitude * (0.4f + t);
                rb.MovePosition(homePosition + new Vector3(jitter.x, 0f, jitter.y));
                float flash = Mathf.PingPong(elapsed * Mathf.Lerp(3f, 10f, t), 1f);
                SetColor(Color.Lerp(baseColor, warningColor, flash));

                if (elapsed >= warningDuration)
                {
                    SetColor(warningColor);
                    Enter(SectionState.Falling);
                }
                break;
            }

            case SectionState.Falling:
            {
                float t = Mathf.Clamp01(elapsed / fallDuration);
                rb.MovePosition(homePosition + Vector3.down * (fallDistance * t * t));
                if (t >= 1f)
                {
                    rend.enabled = false;
                    col.enabled = false;
                    Enter(SectionState.Gone);
                }
                break;
            }

            case SectionState.Gone:
                if (!StayDown && elapsed >= goneDuration)
                {
                    SetColor(baseColor);
                    rend.enabled = true;
                    col.enabled = true;
                    Enter(SectionState.Rising);
                }
                break;

            case SectionState.Rising:
            {
                float t = Mathf.Clamp01(elapsed / riseDuration);
                float eased = 1f - (1f - t) * (1f - t);
                rb.MovePosition(homePosition + Vector3.down * (fallDistance * (1f - eased)));
                if (t >= 1f)
                {
                    rb.MovePosition(homePosition);
                    Enter(SectionState.Solid);
                }
                break;
            }
        }
    }

    private void SetColor(Color color)
    {
        // Runtime-only references are lost if scripts recompile during play mode
        if (rend == null) rend = GetComponent<MeshRenderer>();
        if (block == null) block = new MaterialPropertyBlock();
        rend.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        rend.SetPropertyBlock(block);
    }
}
