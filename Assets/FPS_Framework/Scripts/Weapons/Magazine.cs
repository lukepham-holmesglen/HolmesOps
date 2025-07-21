using UnityEngine;

public class Magazine : MagazineBehaviour
{
    [SerializeField]
    private int ammunitionTotal = 10;

    public override int GetAmmunitionTotal() => ammunitionTotal;
}
