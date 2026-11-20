using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(MeshCollider), typeof(Rigidbody))]
public class ArenaSection : MonoBehaviour
{
    public enum SectionState { Solid, Warning, Falling, Gone, Rising }

    private const float ShakeAmount = 0.08f;

    private Rigidbody rb;
    private MeshCollider col;
    private SectionPaint paint;
    private Vector3 homePosition;
    private Color warningColor;
    private float stateStart;
    private float warningDuration;
    private float fallDuration;
    private float goneDuration;
    private float riseDuration;
    private float fallDistance;

    public SectionState State { get; private set; } = SectionState.Solid;
    public bool CanFall { get; set; } = true;
    public bool StayDown { get; set; }
    public bool IsSolid { get { return State == SectionState.Solid; } }

    public float InnerRadius { get; private set; }
    public float OuterRadius { get; private set; }
    public float StartAngle { get; private set; }
    public float EndAngle { get; private set; }

    public void Init(float innerRadius, float outerRadius, float startAngle, float endAngle, Color baseColor, Color warning)
    {
        InnerRadius = innerRadius;
        OuterRadius = outerRadius;
        StartAngle = startAngle;
        EndAngle = endAngle;
        warningColor = warning;

        rb = GetComponent<Rigidbody>();
        col = GetComponent<MeshCollider>();
        paint = GetComponent<SectionPaint>();
        if (paint == null) paint = gameObject.AddComponent<SectionPaint>();

        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        homePosition = transform.position;
        paint.SetBaseColor(baseColor);
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
        Sfx.Play(Sound.Rumble, transform.position, 0.6f);
    }

    public void SetHome(Vector3 position)
    {
        homePosition = position;
        if (State == SectionState.Solid)
        {
            rb.position = position;
            transform.position = position;
        }
    }

    public void HideImmediate()
    {
        ClearIce();
        StayDown = true;
        Enter(SectionState.Gone);
        SetVisible(false);
        col.enabled = false;
    }

    public void Emerge(float rise, float distance)
    {
        StayDown = false;
        riseDuration = rise;
        fallDistance = distance;
        rb.position = homePosition + Vector3.down * distance;
        transform.position = rb.position;
        paint.ShowBase();
        SetVisible(true);
        col.enabled = true;
        Enter(SectionState.Rising);
    }

    public void ResetImmediate()
    {
        ClearIce();
        StayDown = false;
        Enter(SectionState.Solid);
        rb.position = homePosition;
        transform.position = homePosition;
        SetVisible(true);
        col.enabled = true;
        paint.ShowBase();
    }

    public void Tint(Color color, float amount)
    {
        if (State != SectionState.Solid) return;
        paint.Show(Color.Lerp(paint.BaseColor, color, Mathf.Clamp01(amount)));
    }

    public Vector3 RandomTopPoint(Vector3 arenaCenter, float topY, float margin)
    {
        float r0 = InnerRadius + margin;
        float r1 = Mathf.Max(r0, OuterRadius - margin);
        float angle = Random.Range(StartAngle, EndAngle);
        float r = Mathf.Sqrt(Mathf.Lerp(r0 * r0, r1 * r1, Random.value));

        Vector3 flat = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        return arenaCenter + flat + Vector3.up * (topY - arenaCenter.y);
    }

    private void FixedUpdate()
    {
        if (State == SectionState.Solid) return;

        float elapsed = Time.time - stateStart;
        if (State == SectionState.Warning) UpdateWarning(elapsed);
        else if (State == SectionState.Falling) UpdateFalling(elapsed);
        else if (State == SectionState.Gone) UpdateGone(elapsed);
        else if (State == SectionState.Rising) UpdateRising(elapsed);
    }

    private void UpdateWarning(float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / warningDuration);
        Vector2 jitter = Random.insideUnitCircle * ShakeAmount * (0.4f + t);
        rb.MovePosition(homePosition + new Vector3(jitter.x, 0f, jitter.y));

        float flash = Mathf.PingPong(elapsed * Mathf.Lerp(3f, 10f, t), 1f);
        paint.Show(Color.Lerp(paint.BaseColor, warningColor, flash));

        if (elapsed >= warningDuration)
        {
            paint.Show(warningColor);
            Enter(SectionState.Falling);
            Sfx.Play(Sound.Crumble, transform.position, 0.6f);

            Bounds bounds = GetComponent<MeshRenderer>().bounds;
            Vfx.Dust(bounds.center + Vector3.up * bounds.extents.y, 1f);
        }
    }

    private void UpdateFalling(float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / fallDuration);
        rb.MovePosition(homePosition + Vector3.down * (fallDistance * t * t));
        if (t >= 1f)
        {
            SetVisible(false);
            col.enabled = false;
            Enter(SectionState.Gone);
        }
    }

    private void UpdateGone(float elapsed)
    {
        if (StayDown || elapsed < goneDuration) return;

        paint.ShowBase();
        SetVisible(true);
        col.enabled = true;
        Enter(SectionState.Rising);
    }

    private void UpdateRising(float elapsed)
    {
        float t = Mathf.Clamp01(elapsed / riseDuration);
        float eased = 1f - (1f - t) * (1f - t);
        rb.MovePosition(homePosition + Vector3.down * (fallDistance * (1f - eased)));
        if (t >= 1f)
        {
            rb.MovePosition(homePosition);
            Enter(SectionState.Solid);
        }
    }

    private void Enter(SectionState state)
    {
        State = state;
        stateStart = Time.time;
    }

    private void ClearIce()
    {
        SectionFreeze ice = GetComponent<SectionFreeze>();
        if (ice != null) ice.Clear();
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>(true))
        {
            rend.enabled = visible;
        }
    }
}
