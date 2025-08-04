using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class DamageZone : MonoBehaviour
{
    [Header("Damage Settings")]
    public int damagePerTick = 10;
    public float damageInterval = 1.0f;

    [Header("Audio Feedback")]
    public AudioClip damageSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    [Header("Visual Feedback")]
    [Tooltip("UI script that controls the damage overlay")]
    public DamageZoneUI damageUI;

    public Renderer flashingObject;
    public Color flashColor = Color.red;
    public float flashSpeed = 2.0f;

    private Coroutine damageCoroutine;
    private Coroutine flashCoroutine;
    private Material flashingMaterial;
    private Color originalColor;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null && damageCoroutine == null)
        {
            damageCoroutine = StartCoroutine(DamageOverTime(character));
            if (flashingObject != null)
                flashCoroutine = StartCoroutine(FlashVisual());

            if (damageUI)
                damageUI.ShowOverlay(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null)
        {
            if (damageCoroutine != null)
            {
                StopCoroutine(damageCoroutine);
                damageCoroutine = null;
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
                ResetFlashColor();
            }

            if (damageUI)
                damageUI.ShowOverlay(false);
        }
    }

    private IEnumerator DamageOverTime(Character character)
    {
        while (true)
        {
            if (damageSound)
                AudioSource.PlayClipAtPoint(damageSound, transform.position, soundVolume);

            character.ChangeCurrentHealth(-damagePerTick);

            yield return new WaitForSeconds(damageInterval);
        }
    }

    private IEnumerator FlashVisual()
    {
        if (flashingObject == null || flashingObject.material == null)
            yield break;

        flashingMaterial = flashingObject.material;
        originalColor = flashingMaterial.color;

        while (true)
        {
            float t = Mathf.PingPong(Time.time * flashSpeed, 1f);
            flashingMaterial.color = Color.Lerp(originalColor, flashColor, t);
            yield return null;
        }
    }

    private void ResetFlashColor()
    {
        if (flashingMaterial != null)
            flashingMaterial.color = originalColor;
    }

    public void SetDamageUI(DamageZoneUI uiScript)
    {
        damageUI = uiScript;
    }
}
