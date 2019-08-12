
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ComponentReference<TComponent> : AssetReference
{
    public ComponentReference(string guid) : base(guid)
    {
    }

    //these are not named InstantiateAsync/LoadAssetAsync because in addressables 1.1.x the base methods not virtual.  They will be in 1.2+.  So this demo needs updating post 1.2.x
    public AsyncOperationHandle<TComponent> InstantiateComponentAsync(Transform parent = null, bool instantiateInWorldSpace = false)
    {
        return Addressables.ResourceManager.CreateChainOperation<TComponent, GameObject>(this.InstantiateAsync(), GameObjectReady);
    }
    public AsyncOperationHandle<TComponent> LoadComponentAsync()
    {
        return Addressables.ResourceManager.CreateChainOperation<TComponent, GameObject>(this.LoadAssetAsync<GameObject>(), GameObjectReady);
    }

    AsyncOperationHandle<TComponent> GameObjectReady(AsyncOperationHandle<GameObject> arg)
    {
        var comp = arg.Result.GetComponent<TComponent>();
        return Addressables.ResourceManager.CreateCompletedOperation<TComponent>(comp, string.Empty);
    }

    public override bool ValidateAsset(Object obj)
    {
        var go = obj as GameObject;
        return go != null && go.GetComponent<TComponent>() != null;
    }
    
    public override bool ValidateAsset(string path)
    {
#if UNITY_EDITOR
        //this load can be expensive...
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return go != null && go.GetComponent<TComponent>() != null;
#else
            return false;
#endif
    }
    
}

