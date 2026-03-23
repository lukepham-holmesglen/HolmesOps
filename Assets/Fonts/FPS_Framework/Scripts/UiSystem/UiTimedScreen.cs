using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UiTimedScreen : UiView
{
    [Header("Timed Screen Properties")]
    public float ScreenTime = 2f;
    private float startTime;

    public override void Initialise()
    {
    }

    public override void Show()
    {
        base.Show();
        startTime = Time.time;
        StartCoroutine(_waitForTime());
    }

    public virtual void OnTimeOut()
    {
        Debug.Log(gameObject.name + " timed out.");
    }

    IEnumerator _waitForTime()
    {
        yield return new WaitForSeconds(ScreenTime);
        OnTimeOut();
    }
}
