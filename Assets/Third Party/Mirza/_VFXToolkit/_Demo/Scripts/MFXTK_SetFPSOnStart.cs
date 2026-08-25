using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    public class MVFXTK_SetFPS : MonoBehaviour
    {
        public int targetFPS = 60;

        [Space]

        public bool forceSet = true;
        public bool unlockOnDisable = true;

        void OnEnable()
        {
            Application.targetFrameRate = targetFPS;
        }

        void Update()
        {
            if (forceSet)
            {
                Application.targetFrameRate = targetFPS;
            }
        }

        void OnDisable()
        {
            if (unlockOnDisable)
            {
                Application.targetFrameRate = -1;
            }
        }
    }
}