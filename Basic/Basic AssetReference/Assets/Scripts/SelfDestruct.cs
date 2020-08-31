using UnityEngine;
using UnityEngine.AddressableAssets;

public class SelfDestruct : MonoBehaviour {

	public float lifetime = 2f;

	void Start()
	{
		Invoke("Release", lifetime);
	}

	void Release()
	{
        if (!Addressables.ReleaseInstance(gameObject))
            Destroy(gameObject);
	}
}
