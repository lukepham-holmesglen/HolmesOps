using UnityEngine;

public class Magazine : MagazineBehaviour
{
    [SerializeField]
    private int ammunitionTotal = 100;
    [SerializeField]
    private int reserveAmmoTotal = 500;

    public override int GetAmmunitionTotal() => ammunitionTotal;
    public override int GetReserveTotal() => reserveAmmoTotal;
}
