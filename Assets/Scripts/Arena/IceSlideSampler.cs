using UnityEngine;

// Records a ball's velocity before the player's movement code runs each physics step (see IceSlide)
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
        slide.velocityBeforeSteering = rb.linearVelocity;
        slide.sampled = true;
    }
}
