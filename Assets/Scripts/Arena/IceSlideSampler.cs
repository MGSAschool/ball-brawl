using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class IceSlideSampler : MonoBehaviour
{
    private IceSlide slide;
    private Rigidbody rb;

    private void Awake()
    {
        slide = GetComponent<IceSlide>();
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (slide == null || rb == null) return;
        slide.RecordVelocity(rb.linearVelocity);
    }
}
