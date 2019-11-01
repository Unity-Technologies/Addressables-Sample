using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Done_DestroyByBoundary : MonoBehaviour
{
	void OnTriggerExit (Collider other) 
	{
        // ADDRESSABLES UPDATES
        Addressables.ReleaseInstance(other.gameObject);
	}
}