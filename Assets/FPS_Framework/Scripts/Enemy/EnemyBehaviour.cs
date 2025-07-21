using UnityEngine;

public abstract class EnemyBehaviour : MonoBehaviour
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

    public abstract void GetShot();
    


    #endregion
}
