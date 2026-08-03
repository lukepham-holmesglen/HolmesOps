using TMPro;
using UnityEngine;

namespace Mirza.VFXToolKit
{
    public class MFXTK_SetRenderScaleDropdownUI : MonoBehaviour
    {
        TMP_Dropdown dropdown;
        public MFXTK_SetRenderScale setRenderScale;

        void Start()
        {
            dropdown = GetComponent<TMP_Dropdown>();
            dropdown.onValueChanged.AddListener(SetRenderScale);
        }

        public void SetRenderScale(int value)
        {
            setRenderScale.renderScale = 0.25f + (value * 0.25f);
        }
    }
}