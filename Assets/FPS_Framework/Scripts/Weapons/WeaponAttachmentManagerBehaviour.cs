using UnityEngine;

public abstract class WeaponAttachmentManagerBehaviour : MonoBehaviour
{
    #region Virtual Unity Functions
    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {

    }

    protected virtual void Update()
    {

    }

    protected virtual void LateUpdate()
    {

    }
    #endregion

    #region GETTERS
    public abstract MagazineBehaviour GetEquippedMagazine();

    public abstract Transform GetEquippedMuzzlePos();
    #endregion
}
