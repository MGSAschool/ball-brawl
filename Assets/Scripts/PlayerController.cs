using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private enum States { idle, Move }
    private States currentState = States.idle;
    public Rigidbody rb;
    private readonly float moveSpeed = 10f;
    private readonly float maxSpeed = 13f;
    private readonly float deceleration = 3f;
    private Vector2 moveInput;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    void Start()
    {
    }

    void FixedUpdate()
    {
        Move();
    }
    // Update is called once per frame
    void Update()
    {
    }
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Debug.Log("Collided with Enemy");
        }
    }
    private void Move()
    {
        // Clamp the velocity to the maximum speed
        rb.linearVelocity = new Vector3(
            Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed), 
            rb.linearVelocity.y, 
            Mathf.Clamp(rb.linearVelocity.z, -maxSpeed, maxSpeed));

        if(moveInput != Vector2.zero)
        {
            rb.AddForce(new Vector3(moveInput.x, 0, moveInput.y) * moveSpeed);
        }
        else
        {
            DecaySpeed();
        }
        
    }
     private void DecaySpeed()
    {
        // If there is no input, gradually reduce the velocity to zero to simulate deceleration in physics
         if (moveInput == Vector2.zero)
    {
        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            new Vector3(0, rb.linearVelocity.y, 0),
            deceleration * Time.fixedDeltaTime
        );
    }

    }
}
