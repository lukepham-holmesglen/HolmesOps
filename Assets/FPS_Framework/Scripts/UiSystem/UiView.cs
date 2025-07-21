using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class UiView : MonoBehaviour
{

    public abstract void Initialise();
    public virtual void Show() => gameObject.SetActive(true);
    public virtual void Hide() => gameObject.SetActive(false);
}
