using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;

public class BasicReference : MonoBehaviour
{

	public AssetReference baseCube;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void SpawnThing()
	{
		baseCube.InstantiateAsync();
	}
}
