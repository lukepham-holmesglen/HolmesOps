using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : Component
{

	public static T Instance { get; private set; }
	public bool dontDestroyOnLoad = true;

	public virtual void Awake()
	{
		if (Instance == null)
		{
			Instance = this as T;
            if (dontDestroyOnLoad) 
			{
				DontDestroyOnLoad(this);
			}
		}
		else
		{
			Destroy(gameObject);
		}
	}

}
