using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UiCharacter : MonoBehaviour
{

    public TMP_Text Field;
    public Image Background;
    public Sprite DeselectedBg;
    public Sprite SelectedBg;

    public void Reset()
    {
        Background.sprite = DeselectedBg;
        Field.text = "";
    }
    public void Focus()
    {
        Background.sprite = SelectedBg;
        Field.text = "";
    }
    public void SetText(string c)
    {
        Background.sprite = DeselectedBg;
        Field.text = c.ToUpper();
    }

    public string GetText()
    {
        return Field.text;
    }

}
