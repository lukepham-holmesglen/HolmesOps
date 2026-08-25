using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Mirza.VFXToolKit
{
    [ExecuteAlways]
    public class MFXTK_RenderScaleSliderUI : MonoBehaviour
    {
        Slider slider;

        public MFXTK_SetRenderScale setRenderScale;
        public TextMeshProUGUI label;

        void Start()
        {
            slider = GetComponent<Slider>();
            slider.onValueChanged.AddListener(SetRenderScale);
        }

        public void SetRenderScale(float value)
        {
            setRenderScale.renderScale = value * 0.25f;
            label.text = setRenderScale.renderScale.ToString("0.00x");
        }
    }
}