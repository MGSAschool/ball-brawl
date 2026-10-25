using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 moveInput {get; private set;}
    public event Action DashPressed;
    public event Action PowerupPressed;

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnDash(InputValue value)
    {
        if(value.isPressed) DashPressed?.Invoke();
    }

    public void OnUsePowerUp(InputValue value)
    {
        if(value.isPressed)PowerupPressed?.Invoke();
    }
    

}
