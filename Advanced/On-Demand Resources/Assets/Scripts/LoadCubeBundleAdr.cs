// Single cube pack loader

using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_IOS

#endif

public class LoadCubeBundleAdr : MonoBehaviour
{
    public string prefabAddress = "CubesA";

    public GameObject ourPrefabAsset;

    public GameObject ourInstantiatedPrefab;

    void Start()
    {
        Addressables.LoadAssetAsync<GameObject>(prefabAddress).Completed += ObjectLoaded;
    }

    private void ObjectLoaded(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            ourPrefabAsset = obj.Result;
            ourInstantiatedPrefab = Instantiate(ourPrefabAsset);
        }
    }
}

// todo: use addressable assset reference instead?

