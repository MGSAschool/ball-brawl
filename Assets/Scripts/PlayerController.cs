using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private enum States { idle, Move }
    private States currentState = States.idle;
    public Rigidbody rb;
    public readonly float moveSpeed = 10f;
    public readonly float maxSpeed = 13f;
    private readonly float deceleration = 0.5f;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {
    }

    private void Update()
    {
        switch (currentState)
        {
            case States.idle:
                if (moveInput != Vector2.zero)
                {
                    currentState = States.Move;
                }
                break;

            case States.Move:
                if (moveInput == Vector2.zero)
                {
                    currentState = States.idle;
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        switch (currentState)
        {
            case States.idle:
                DecaySpeed();
                break;

            case States.Move:
                Move();
                break;
        }
    }
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    private void Move()
    {
        // Clamp the velocity to the maximum speed
        rb.linearVelocity = new Vector3(
            Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), 
            rb.linearVelocity.y, 
            Mathf.Clamp(rb.linearVelocity.z, -maxSpeed, maxSpeed));

        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        rb.AddForce(direction * moveSpeed);
    }
     private void DecaySpeed()
    {
        // If there is no input, gradually reduce the velocity to zero to simulate deceleration in physics
         rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                new Vector3(0, rb.linearVelocity.y, 0),
                deceleration * Time.fixedDeltaTime
            );
    }

    
}
