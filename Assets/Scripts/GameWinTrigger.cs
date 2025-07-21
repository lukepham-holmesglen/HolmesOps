using UnityEngine;

public class GameWinTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameMan.Instance.GameWin();
        }
    }
}
