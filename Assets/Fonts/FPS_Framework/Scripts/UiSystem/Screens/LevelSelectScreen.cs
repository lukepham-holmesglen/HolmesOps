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
        var lightingProfile = levelPrefab.GetComponent<LevelLightingProfile>();

        FadeController.Instance.FadeToWhiteThen(levelPrefab, () =>
        {
            // Apply lighting before showing the game
            if (lightingProfile != null)
                EnvironmentManager.Instance.ApplyLightingProfile(lightingProfile.profile);

            GameMan.Instance.StartGame(levelPrefab);
            UiSystem.Show<GameScreen>();
        });
    }
}
