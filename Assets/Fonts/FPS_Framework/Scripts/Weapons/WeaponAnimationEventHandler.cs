using UnityEngine;

public class WeaponAnimationEventHandler : MonoBehaviour
{
    private WeaponBehaviour weapon;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        weapon = GetComponent<WeaponBehaviour>();
    }

    private void OnEjectCasing()
    {
        Debug.Log("Eject Casing");
    }
}
