using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    public Vector2 moveInput {get; private set;}

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }
}
