using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BrawlPlayer))]
public class BallSkin : MonoBehaviour
{
    private static readonly Quaternion ModelFacing = Quaternion.Euler(0f, 90f, 0f);

    public event Action Applied;

    [SerializeField] private float sizeMultiplier = 1.05f;

    [Header("Rolling")]
    [SerializeField] private float rollGrip = 12f;
    [SerializeField] private float airSpinDrag = 0.3f;
    [SerializeField] private float settleSpeed = 1.2f;
    [SerializeField] private float settleRate = 5f;
    [SerializeField] private float idleBlendRate = 6f;

    [Header("Squash")]
    [SerializeField] private float squashPerSpeed = 0.035f;
    [SerializeField] private float maxSquash = 0.3f;
    [SerializeField] private float squashSpring = 260f;
    [SerializeField] private float squashDamping = 9f;

    [Header("Speed Stretch")]
    [SerializeField] private float stretchStartSpeed = 13f;
    [SerializeField] private float stretchFullSpeed = 30f;
    [SerializeField] private float maxStretch = 0.4f;
    [SerializeField] private float stretchResponse = 14f;

    private struct BonePose
    {
        public Transform Bone;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    private BrawlPlayer player;
    private Rigidbody rb;
    private Transform stretchNode;
    private Transform squashNode;
    private Transform model;
    private Renderer modelRenderer;
    private readonly List<BonePose> restPose = new List<BonePose>();
    private float idleWeight = 1f;
    private Quaternion roll = Quaternion.identity;
    private Vector3 spin;
    private float squash;
    private float squashVelocity;
    private Vector3 squashNormal = Vector3.up;
    private float stretch;
    private Vector3 stretchAxis = Vector3.forward;

    public float Diameter { get; private set; } = 1f;
    public Color PlayerColor { get; private set; } = Color.white;
    public Vector3 Facing { get; private set; } = Vector3.back;
    public bool HasModel { get { return model != null; } }

    public bool IsShown
    {
        get { return modelRenderer == null || modelRenderer.enabled; }
    }

    private void Awake()
    {
        player = GetComponent<BrawlPlayer>();
        rb = GetComponent<Rigidbody>();
        SphereCollider sphere = GetComponent<SphereCollider>();
        if (sphere != null) Diameter = sphere.radius * 2f * transform.lossyScale.x;
    }

    public bool Apply(int playerIndex, Color color)
    {
        PlayerColor = color;
        GameArt art = GameArt.Instance;
        if (art == null || art.ballSkins == null || art.ballSkins.Length == 0) return false;

        GameObject prefab = art.ballSkins[playerIndex % art.ballSkins.Length];
        if (prefab == null) return false;

        if (stretchNode != null) Destroy(stretchNode.gameObject);
        MeshRenderer plainSphere = GetComponent<MeshRenderer>();
        if (plainSphere != null) Destroy(plainSphere);

        stretchNode = new GameObject("Ball Stretch").transform;
        stretchNode.SetParent(transform, false);
        squashNode = new GameObject("Ball Squash").transform;
        squashNode.SetParent(stretchNode, false);
        model = ArtUtil.FitModel(prefab, squashNode, Diameter * sizeMultiplier, "Ball Skin");
        roll = Quaternion.LookRotation(Facing, Vector3.up) * ModelFacing;

        modelRenderer = null;
        foreach (Renderer rend in model.GetComponentsInChildren<Renderer>())
        {
            if (rend.enabled)
            {
                modelRenderer = rend;
                break;
            }
        }

        RememberRestPose();
        foreach (Animation anim in model.GetComponentsInChildren<Animation>())
        {
            anim.wrapMode = WrapMode.Loop;
            anim.Play();
        }

        if (Applied != null) Applied();
        return true;
    }

    private void LateUpdate()
    {
        if (model == null) return;
        float dt = Time.deltaTime;

        Vector3 velocity = Vector3.zero;
        if (rb != null && !rb.isKinematic) velocity = rb.linearVelocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);
        if (flat.sqrMagnitude > 0.5f) Facing = flat.normalized;

        bool grounded = player.IsGrounded;
        bool resting = grounded && flat.sqrMagnitude < settleSpeed * settleSpeed;

        Roll(flat, grounded, resting, dt);
        BlendIdle(resting, dt);
        Deform(velocity, dt);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (squashNode == null || collision.contactCount == 0) return;

        Vector3 normal = collision.GetContact(0).normal;
        float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, normal));
        float amount = Mathf.Min(impact * squashPerSpeed, maxSquash);
        if (amount >= 0.03f && amount >= squash)
        {
            squash = amount;
            squashVelocity = 0f;
            squashNormal = normal;
        }
    }

    private void RememberRestPose()
    {
        restPose.Clear();
        foreach (Transform bone in model.GetComponentsInChildren<Transform>())
        {
            BonePose pose = new BonePose();
            pose.Bone = bone;
            pose.Position = bone.localPosition;
            pose.Rotation = bone.localRotation;
            pose.Scale = bone.localScale;
            restPose.Add(pose);
        }
    }

    private void Roll(Vector3 flatVelocity, bool grounded, bool resting, float dt)
    {
        float radius = Diameter * 0.5f;
        if (grounded)
        {
            Vector3 rollingSpin = Vector3.Cross(Vector3.up, flatVelocity) / radius;
            spin = Vector3.Lerp(spin, rollingSpin, 1f - Mathf.Exp(-rollGrip * dt));
        }
        else
        {
            spin *= Mathf.Exp(-airSpinDrag * dt);
        }

        float angle = spin.magnitude * dt * Mathf.Rad2Deg;
        if (angle > 1e-4f) roll = Quaternion.AngleAxis(angle, spin.normalized) * roll;

        if (resting)
        {
            Quaternion upright = Quaternion.LookRotation(Facing, Vector3.up) * ModelFacing;
            roll = Quaternion.Slerp(roll, upright, 1f - Mathf.Exp(-settleRate * dt));
        }
        roll = Quaternion.Normalize(roll);
    }

    private void BlendIdle(bool resting, float dt)
    {
        float target = 0f;
        if (resting) target = 1f;
        idleWeight = Mathf.MoveTowards(idleWeight, target, idleBlendRate * dt);
        if (idleWeight >= 0.999f) return;

        float pullToRest = 1f - idleWeight;
        foreach (BonePose pose in restPose)
        {
            if (pose.Bone == null) continue;
            pose.Bone.localPosition = Vector3.Lerp(pose.Bone.localPosition, pose.Position, pullToRest);
            pose.Bone.localRotation = Quaternion.Slerp(pose.Bone.localRotation, pose.Rotation, pullToRest);
            pose.Bone.localScale = Vector3.Lerp(pose.Bone.localScale, pose.Scale, pullToRest);
        }
    }

    private void Deform(Vector3 velocity, float dt)
    {
        squashVelocity += (-squashSpring * squash - squashDamping * squashVelocity) * dt;
        squash += squashVelocity * dt;
        if (Mathf.Abs(squash) < 1e-4f && Mathf.Abs(squashVelocity) < 1e-3f)
        {
            squash = 0f;
            squashVelocity = 0f;
        }

        float speed = velocity.magnitude;
        float target = Mathf.InverseLerp(stretchStartSpeed, stretchFullSpeed, speed) * maxStretch;
        stretch = Mathf.Lerp(stretch, target, 1f - Mathf.Exp(-stretchResponse * dt));
        if (speed > 1f) stretchAxis = velocity / speed;

        Quaternion toStretch = Quaternion.FromToRotation(Vector3.up, stretchAxis);
        Quaternion toSquash = Quaternion.FromToRotation(Vector3.up, squashNormal);
        float across = 1f / Mathf.Sqrt(1f + stretch);

        stretchNode.localRotation = Quaternion.Inverse(transform.rotation) * toStretch;
        stretchNode.localScale = new Vector3(across, 1f + stretch, across);
        squashNode.localRotation = Quaternion.Inverse(toStretch) * toSquash;
        squashNode.localScale = new Vector3(1f + squash * 0.5f, 1f - squash, 1f + squash * 0.5f);
        model.localRotation = Quaternion.Inverse(toSquash) * roll;
    }
}
