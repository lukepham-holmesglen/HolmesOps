using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InputFieldError : MonoBehaviour
{

    private TMP_InputField field;
    private Sprite startImage;
    public Sprite errorImage;

    private void Awake()
    {
        field = gameObject.GetComponent<TMP_InputField>();
        startImage = field.image.sprite;
    }
    internal void Error()
    {
        field.image.sprite = errorImage;
    }

    public void Reset()
    {
        field.image.sprite = startImage;
    }
}
