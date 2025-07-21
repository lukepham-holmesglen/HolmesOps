using UnityEngine;

public class EnemyAnimatorListener : MonoBehaviour
{
    public Enemy enemy;

    private void OnDeathComplete()
    {
        enemy.Die();
    }
}
