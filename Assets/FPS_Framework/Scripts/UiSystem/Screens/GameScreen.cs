using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class GameScreen : UiView
{
    [Header("Health")]
    [SerializeField] private Image currentHealth;
    [SerializeField] private Image currentWeaponSprite;
    [SerializeField] private TMP_Text currentAmmo;
    [SerializeField] private TMP_Text maximumAmmo;

    [Header("UI Text")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text roundText;
    [SerializeField] private TMP_Text timerText; // 🕒 Add this in your UI Canvas

    public int currentScore;
    public int currentRound;

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
        // Optional init
    }

    public void Reset()
    {
        currentScore = 0;
        currentRound = 0;
        IncreaseRound(0);
        IncreaseScore(0);
        UpdateHealthBar(1);
        UpdateTimer(0f); // 🕒 Reset timer display
    }

    public void UpdateHealthBar(float currentDivideByMax)
    {
        currentHealth.fillAmount = currentDivideByMax;
    }

    public void ChangeWeapon(Sprite sprite, int currAmmo, int maxAmmo)
    {
        currentWeaponSprite.sprite = sprite;
        currentAmmo.text = currAmmo.ToString();
        maximumAmmo.text = maxAmmo.ToString();
    }

    public void UpdateAmmoCount(int newAmount)
    {
        currentAmmo.text = newAmount.ToString();
    }

    public void IncreaseScore(int amount)
    {
        currentScore += amount;
        scoreText.text = "Score: " + currentScore.ToString();
    }

    public void IncreaseRound(int amount)
    {
        currentRound += amount;
        roundText.text = "Round: " + currentRound.ToString();
    }

    public void UpdateTimer(float seconds)
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            timerText.text = $"Time: {minutes:00}:{secs:00}";
        }
    }

    void OnDisable()
    {
        Debug.LogWarning($"{gameObject.name} was disabled at runtime!", this);
    }
}
