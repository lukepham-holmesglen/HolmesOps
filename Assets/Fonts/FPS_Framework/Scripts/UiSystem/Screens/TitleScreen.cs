using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleScreen : UiView
{
    public override void Show()
    {
        base.Show();
    }
    public override void Hide()
    {
        base.Hide();
    }
    public override void Initialise()
    {
    }

    public void Started()
    {
        UiSystem.Show<LevelSelectScreen>();
    }
}
