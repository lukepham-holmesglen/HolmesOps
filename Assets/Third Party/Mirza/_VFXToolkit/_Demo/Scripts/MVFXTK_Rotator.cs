using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    public class MVFXTK_Rotator : MonoBehaviour
    {
        public Vector3 rotation;
        public Space space = Space.Self;

        // Internal rotation state to accumulate cleanly over time.

        Quaternion currentRotation;

        //void Start()
        //{
        //    // Store initial rotation.

        //    currentRotation = transform.localRotation;
        //}

        void OnEnable()
        {
            currentRotation = transform.localRotation;
        }

        // Putting this in Update allows the user to modify the rotation in the editor,
        // while the script continues to run. The continous rotation is applied in LateUpdate.

        void Update()
        {
            // Store the current rotation for the next frame.

            if (space == Space.Self)
            {
                currentRotation = transform.localRotation;
            }
            else
            {
                currentRotation = transform.rotation;
            }
        }

        void LateUpdate()
        {
            // Compute delta rotation this frame.

            Quaternion deltaRotation = Quaternion.Euler(rotation * Time.deltaTime);

            // Apply depending on world vs. local space.

            if (space == Space.Self)
            {
                currentRotation *= deltaRotation;

                // Apply the accumulated quaternion.

                transform.localRotation = currentRotation;
            }
            else
            {
                // World space: multiply from the *outside*.

                currentRotation = deltaRotation * currentRotation;

                transform.rotation = currentRotation;
            }
        }
    }
}
