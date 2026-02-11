using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelSelectScreen : UiView
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

    public void SelectedLevel(GameObject levelPrefab)
    {
        // Start fade sequence on separate controller
        FadeController.Instance.FadeToWhiteThen(levelPrefab, () =>
        {
            // Load level and show game screen after fade-in completes
            GameMan.Instance.StartGame(levelPrefab);
            UiSystem.Show<GameScreen>();
        });
    }
}
