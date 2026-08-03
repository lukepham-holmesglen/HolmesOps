using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    public class MFXTK_CameraController : MonoBehaviour
    {
        public bool dragEnabled = true;

        [Space]

        public float speed = 2.0f;
        public float lerpSpeed = 10.0f;

        [Space]

        public Vector2 rotateAroundYRange = new(25.0f, -25.0f);
        public Vector2 rotateAroundXRange = new(12.5f, -12.5f);

        [Space]

        public Vector2 zoomRange = new(-4.0f, -2.5f);

        [Space]

        public float zoomLerpSpeed = 20.0f;
        public float zoomStep = 0.5f;

        float currentZoom;

        [Space]

        public Transform zoomTransform;

        Vector3 currentEulerAngles;
        Quaternion targetRotation;

        bool focusEnabledThisFrame;
        bool previouslyFocused;

        void Start()
        {
            targetRotation = transform.localRotation;
            currentEulerAngles = targetRotation.eulerAngles;

            currentZoom = zoomTransform.localPosition.z;
        }

        // ...

        public void OnBeginDrag()
        {
            dragEnabled = true;
        }
        public void OnEndDrag()
        {
            dragEnabled = false;
        }

        // ...

        void Update()
        {
            bool isFocused = Application.isFocused;

            focusEnabledThisFrame = isFocused && !previouslyFocused;

            // Early out if legacy input manager is disabled.
            // -- (required, else there will be an error on play).

#if !ENABLE_LEGACY_INPUT_MANAGER
            return;
#endif
            // Rotate.

            if (dragEnabled && Input.GetMouseButton(0))
            {
                Vector2 mouse = Vector2.zero;

                mouse.x = Input.GetAxisRaw("Mouse X");
                mouse.y = Input.GetAxisRaw("Mouse Y");

                // Prevent annoying sudden and large mouse values (especially in the editor).
                // > From returning to game view/window after clicking around the editor.

                if (focusEnabledThisFrame)
                {
                    mouse = Vector2.zero;
                }

                mouse *= speed;

                currentEulerAngles.x -= mouse.y;
                currentEulerAngles.y += mouse.x;

                //currentEulerAngles.x = Mathf.Clamp(currentEulerAngles.x, rotateAroundXRange.x, rotateAroundXRange.y);
                //currentEulerAngles.y = Mathf.Clamp(currentEulerAngles.y, rotateAroundYRange.x, rotateAroundYRange.y);

                targetRotation = Quaternion.Euler(currentEulerAngles);
            }

            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * lerpSpeed);

            // Zoom. Only if mouse in viewport of main camera.
            // -- (fixes annoying issue of mouse-wheel'ing outside game window changing zoom...).

            // Check if mouse is in normalized viewport range [0.0, 1.0].

            Vector2 mousePositionInViewport = Camera.main.ScreenToViewportPoint(Input.mousePosition);

            bool isMouseInsideViewPort =

                (mousePositionInViewport.x > 0.0f && mousePositionInViewport.x < 1.0f) &&
                (mousePositionInViewport.y > 0.0f && mousePositionInViewport.y < 1.0f);

            if (isMouseInsideViewPort)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                // Sign to make either -1.0 or 1.0 (else, it's like 0.1).

                if (scroll != 0.0f)
                {
                    scroll = Mathf.Sign(scroll);
                }

                scroll *= zoomStep;

                currentZoom += scroll;
                currentZoom = Mathf.Clamp(currentZoom, zoomRange.x, zoomRange.y);

                Vector3 localPosition = zoomTransform.localPosition;
                localPosition.z = Mathf.Lerp(localPosition.z, currentZoom, Time.deltaTime * zoomLerpSpeed);

                zoomTransform.localPosition = localPosition;
            }

            previouslyFocused = isFocused;
        }
    }
}