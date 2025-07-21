using UnityEngine;
using TMPro;

public class GameWinScreen : UiView
{
    public TMP_Text roundText;
    public TMP_Text scoreText;
    public TMP_Text completionTimeText; // 🕒 Add this to your GameWin UI

    public override void Initialise()
    {
        // Optional init
    }

    public void SetData(int round, int score)
    {
        roundText.text = "Round: " + round;
        scoreText.text = "Score: " + score;
    }

    public void SetCompletionTime(float seconds)
    {
        if (completionTimeText != null)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            completionTimeText.text = $"Time: {minutes:00}:{secs:00}";
        }
    }

    public void NextButton()
    {
        UiSystem.Show<TitleScreen>();
    }
}
