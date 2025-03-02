using UnityEngine;

public class DetectSteeringWheelButtons : MonoBehaviour
{
    void Update()
    {
        // Detect steering wheel button presses
        for (int i = 0; i <= 19; i++)
        {
            KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), "JoystickButton" + i);
            if (Input.GetKeyDown(keyCode))
            {
                Debug.Log($"Logitech Xbox button {keyCode} pressed!");
            }
        }
    }
}
