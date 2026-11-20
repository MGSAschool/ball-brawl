using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UI_InputReader : MonoBehaviour
{
    public event Action OnPausePress;

    public void OnPause(InputValue inputValue)
    {
        if(inputValue.isPressed)
        {
            OnPausePress?.Invoke();
        }
    }
}
