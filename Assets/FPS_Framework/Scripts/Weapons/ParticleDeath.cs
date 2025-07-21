using UnityEngine;
using System.Collections;

public class ParticleDeath : MonoBehaviour
{
    private void Start()
    {
        Destroy(gameObject, 5.0f);
    }
}
