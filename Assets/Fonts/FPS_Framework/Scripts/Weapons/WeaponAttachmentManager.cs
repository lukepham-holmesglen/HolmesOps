using UnityEngine;

public class WeaponAttachmentManager : WeaponAttachmentManagerBehaviour
{
    [Header("Muzzle")]

    [Tooltip("Selected Muzzle Index.")]
    [SerializeField]
    private int muzzleIndex = 0;

    [Tooltip("All possible Muzzle Attachments that this Weapon can use!")]
    [SerializeField]
    private Transform[] muzzleArray;

    [Header("Magazine")]

    [Tooltip("Selected Magazine Index.")]
    [SerializeField]
    private int magazineIndex;

    [Tooltip("All possible Magazine Attachments that this Weapon can use!")]
    [SerializeField]
    private Magazine[] magazineArray;

    private MagazineBehaviour magazineBehaviour;

    protected override void Awake()
    {
        magazineBehaviour = magazineArray[0];
    }


    #region GETTERS
    public override MagazineBehaviour GetEquippedMagazine() => magazineBehaviour;

    public override Transform GetEquippedMuzzlePos() => muzzleArray[muzzleIndex];

    #endregion
}
