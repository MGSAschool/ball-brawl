using UnityEngine;
using UnityEngine.InputSystem;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PowerUpHandler))]
[RequireComponent(typeof(PlayerInputReader))]
public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private enum States { Idle, Move }
    private States currentState = States.Idle;
    public Rigidbody rb;
    [SerializeField] public float moveSpeed = 10f;
    [SerializeField] private float maxSpeed = 13f;
    private readonly float deceleration = 0.5f;
    private PlayerInputReader playerInputReader;
    private PowerUpHandler powerUpHandler;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        powerUpHandler = GetComponent<PowerUpHandler>();
        playerInputReader = GetComponent<PlayerInputReader>();
    }
    void Start()
    {
    }

    private void Update()
    {
        currentState = (playerInputReader.moveInput == Vector2.zero) ? States.Idle : States.Move;
    }

    private void FixedUpdate()
    {
        Vector2 readInput = playerInputReader.moveInput;
        switch (currentState)
        {
            case States.Idle:
                DecaySpeed();
                break;

            case States.Move:
                Move(readInput);
                break;
        }
    }
    private void Move(Vector2 moveInput)
    {
        Vector3 direction = new Vector3(moveInput.x, 0, moveInput.y).normalized;
        rb.AddForce(direction * GetModifiedSpeed(), ForceMode.Acceleration);
        ClampHorizontalSpeed();
        
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

    private void ClampHorizontalSpeed()
    {
        Vector3 horizontalVel = new Vector3(
            rb.linearVelocity.x, 
            0, 
            rb.linearVelocity.z);

        if(horizontalVel.magnitude > maxSpeed)
        {
            horizontalVel = horizontalVel.normalized * maxSpeed;
            
        }
        rb.linearVelocity = new Vector3(
            horizontalVel.x, 
            rb.linearVelocity.y, 
            horizontalVel.z);
    }

    private float GetModifiedSpeed()
    {
        if(powerUpHandler != null && powerUpHandler.ActivePowerUp is SpeedBoostPowerUp powerUp)
        {
           return powerUp.ModifySpeed(moveSpeed);
        }
        return moveSpeed;
    }
    
}
