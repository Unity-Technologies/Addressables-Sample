using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class Done_WeaponController : MonoBehaviour
{
    // ADDRESSABLES UPDATES
    public AssetReference shot;

	public Transform shotSpawn;
	public float fireRate;
	public float delay;
    float lastFireTime;
    float currentInterval;
	void Start ()
	{
        currentInterval = delay;
        lastFireTime = Time.timeSinceLevelLoad;
	}

	void Update ()
	{
        if (Time.timeSinceLevelLoad - lastFireTime >= currentInterval)
        {
            lastFireTime = Time.timeSinceLevelLoad;
            // ADDRESSABLES UPDATES
            shot.InstantiateAsync(shotSpawn.position, shotSpawn.rotation);

            GetComponent<AudioSource>().Play();
            currentInterval = fireRate;
        }
	}
}
