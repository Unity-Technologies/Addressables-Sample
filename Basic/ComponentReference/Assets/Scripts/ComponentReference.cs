
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

    //Ideally Load and Instantiate would return the component type.  we plan to add that to this demo at some point.
    public AsyncOperationHandle<GameObject> LoadAssetAsync()
    {
        return LoadAssetAsync<GameObject>();
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
