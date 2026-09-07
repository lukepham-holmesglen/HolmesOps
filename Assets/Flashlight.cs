using UnityEngine;

public class Flashlight : MonoBehaviour
{
    public Light flashlightObject;
    private bool isOn = false;

    void Update()
    {
        // Press 'F' to toggle the flashlight
        if (Input.GetKeyDown(KeyCode.F))
        {
            isOn = !isOn;
            flashlightObject.enabled = isOn;
        }
    }
}
