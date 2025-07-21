using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public abstract class UiPopup : MonoBehaviour
{

    public TMP_Text TitleField;
    public TMP_Text MessageField;
    public Button DismissButton;
    public Button ConfirmButton;

    public VerticalLayoutGroup contentLayout;

    public abstract void Initialise();

    private void Start()
    {
        contentLayout = gameObject.GetComponentInChildren<VerticalLayoutGroup>();
    }

    public virtual void Show(string title, string message, Action callback, string dismissText = "No", string confirmText = "Yes")
    {

        if (TitleField != null)
        {
            if (string.IsNullOrEmpty(message))
            {
                TitleField.gameObject.SetActive(false);
            }
            else
            {
                TitleField.gameObject.SetActive(true);
                TitleField.text = title;
            }
        }

        if (MessageField != null)
        {
            if (string.IsNullOrEmpty(message))
            {
                MessageField.gameObject.SetActive(false);
            }
            else
            {
                MessageField.gameObject.SetActive(true);
                MessageField.text = message;
            }
        }

        if(DismissButton != null)
        {
            DismissButton.gameObject.GetComponentInChildren<TMP_Text>().text = dismissText;
            DismissButton.onClick.RemoveAllListeners();
            DismissButton.onClick.AddListener(() => Hide());
        }

        if(ConfirmButton != null)
        {
            ConfirmButton.gameObject.GetComponentInChildren<TMP_Text>().text = confirmText;
            ConfirmButton.onClick.RemoveAllListeners();
            ConfirmButton.onClick.AddListener(() => Confirm(callback));
        }

        gameObject.SetActive(true);
        resize();
    }

    private void resize()
    {

        if (contentLayout != null)
        {
            contentLayout.enabled = false;
            contentLayout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)contentLayout.transform);
        }
        
    }

    public virtual void Hide() {
        gameObject.SetActive(false);
    } 

    public virtual void Confirm(Action callback)
    {
        callback?.Invoke();
        Hide();
    }

}
