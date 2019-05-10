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
		//note, that in some samples, this throws a warning about Addressables not being aware of the object.  This
		// is because in some samples, the objects were not created via Addressables.InstantiateAsync, but were instead
		// created by loading an addressable asset, then instantiating that through Unity's built in instantiation.
		// As of 0.8, this is still functional code (but with a warning of upcoming change).  In a coming soon release, this
		// will need to become:
		//		if(!Addressables.ReleaseInstance(gameObject))
		//			Destroy(gameObject);
		Addressables.ReleaseInstance(gameObject);
	}
}
