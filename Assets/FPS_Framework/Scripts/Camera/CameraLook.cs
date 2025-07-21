using System;
using UnityEngine;

public class CameraLook : MonoBehaviour
{
    [SerializeField]
    private Vector2 sensitivity = new Vector2(1, 1);
    [Tooltip("Min and Max up/down rotation angle the camera can have")]
    [SerializeField]
    private Vector2 yClamp = new Vector2(-60, 60);
    [SerializeField]
    private bool smooth;
    [SerializeField]
    private float interpolationSpeed = 25.0f;

    [SerializeField]
    private CharacterBehaviour playerCharacter;
    [SerializeField]
    private Rigidbody playerCharacterRigidbody;
    private Quaternion rotationCharacter;
    private Quaternion rotationCamera;

    private void Start()
    {
        rotationCharacter = playerCharacter.transform.localRotation;

        rotationCamera = transform.localRotation;
    }

    private void LateUpdate()
    {
        Vector2 frameInput = playerCharacter.IsCursorLocked() ? playerCharacter.GetInputLook() : default;

        frameInput *= sensitivity;

        //Yaw
        Quaternion rotationYaw = Quaternion.Euler(0.0f, frameInput.x, 0.0f);
        //Pitch
        Quaternion rotationPitch = Quaternion.Euler(-frameInput.y, 0.0f, 0.0f);

        rotationCamera *= rotationPitch;
        rotationCharacter *= rotationYaw;

        //local rotation
        Quaternion localRotation = transform.localRotation;

        //smooth
        if(smooth)
        {
            localRotation = Quaternion.Slerp(localRotation, rotationCamera, Time.deltaTime * interpolationSpeed);

            playerCharacterRigidbody.MoveRotation(Quaternion.Slerp(playerCharacterRigidbody.rotation, rotationCharacter, Time.deltaTime * interpolationSpeed));
        }
        else
        {
            localRotation *= rotationPitch;
            localRotation = Clamp(localRotation);

            playerCharacterRigidbody.MoveRotation(playerCharacterRigidbody.rotation * rotationYaw);
        }

        transform.localRotation = localRotation;
    }

    private Quaternion Clamp(Quaternion rotation)
    {
        rotation.x /= rotation.w;
        rotation.y /= rotation.w;
        rotation.z /= rotation.w;
        rotation.w = 1.0f;

        //pitch
        float pitch = 2.0f * Mathf.Rad2Deg * Mathf.Atan(rotation.x);

        //clamp
        pitch = Mathf.Clamp(pitch, yClamp.x, yClamp.y);
        rotation.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * pitch);

        return rotation;
    }
}
