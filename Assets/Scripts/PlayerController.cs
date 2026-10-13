using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
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
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float maxSpeed = 13f;
    [SerializeField] private float dashForce = 50f;
    [SerializeField] private float dashTime = 0.3f;
    private readonly float decelerationRate = 1f;
    private PlayerInputReader playerInputReader;
    private PowerUpHandler powerUpHandler;
    public bool IsAlive {get; private set;} = true;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        powerUpHandler = GetComponent<PowerUpHandler>();
        playerInputReader = GetComponent<PlayerInputReader>();
    }
    void OnEnable() => playerInputReader.DashPressed += OnDash;
    void OnDisable() => playerInputReader.DashPressed -= OnDash;

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
                Decelerate();
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
     private void Decelerate()
    {
        // If there is no input, gradually reduce the velocity to zero to simulate deceleration in physics
         rb.linearVelocity = Vector3.Lerp(
                rb.linearVelocity,
                new Vector3(0, rb.linearVelocity.y, 0),
                decelerationRate * Time.fixedDeltaTime
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
           return powerUp.ModifyMultiplier(moveSpeed);
        }
        return moveSpeed;
    }
    private void OnDash()
    {
        if(rb.linearVelocity == Vector3.zero) return;
        
        if(powerUpHandler.ActivePowerUp != null && powerUpHandler.ActivePowerUp is DashPowerUp)
        {
            StartCoroutine(Dash());
        }
        
    }
    IEnumerator Dash()
    {
        float startTime = Time.time;
        Vector3 direction = new Vector3(playerInputReader.moveInput.x, 0, playerInputReader.moveInput.y).normalized;
        while(Time.time < startTime + dashTime)
        {
            rb.AddForce(direction * dashForce, ForceMode.Force);
            yield return null;
        }
        rb.linearVelocity = Vector3.zero;
        powerUpHandler.EndPowerup();
    }


    public void Eliminated()
    {
        Debug.Log(this.name+" Eliminated");
        IsAlive = false;
    }




}
