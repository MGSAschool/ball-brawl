using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class RumbleControls : MonoBehaviour
{
    public static RumbleControls instance;
    private Coroutine rumbleCoroutine;
    private Gamepad gamepad;

    void Awake()
    {
        gamepad = Gamepad.current;
    }

    public void SetMotorSpeed(float low, float high, float duration)
    {
        if(gamepad == null) return;

        gamepad.SetMotorSpeeds(low, high);
        rumbleCoroutine = StartCoroutine(Rumble(duration));
    }


    IEnumerator Rumble(float duration)
    {
        yield return new WaitForSeconds(duration);
        ResetMotorSpeed();
    }
    public void ResetMotorSpeed()
    {
        if (gamepad == null) return;
        gamepad.ResetHaptics();
        rumbleCoroutine = null;
    }
}
