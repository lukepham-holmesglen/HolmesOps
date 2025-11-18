using UnityEngine;

public class Magazine : MagazineBehaviour
{
    [SerializeField]
    private int ammunitionTotal = 10;
    [SerializeField]
    private int reserveAmmoTotal = 50;

    public override int GetAmmunitionTotal() => ammunitionTotal;
    public override int GetReserveTotal() => reserveAmmoTotal;
}
