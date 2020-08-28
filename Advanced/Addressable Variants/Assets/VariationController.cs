using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VariationController : MonoBehaviour
{
    Material m_Mat;
    AsyncOperationHandle m_PrefabHandle;
    GameObject m_Instance;

    public void SwitchToHighDef()
    {
        LoadTexture("tree", "HD" );
    }

    public void SwitchToLowDef()
    {
        LoadTexture("tree", "SD");
    }

    public void LoadDefaultPrefab()
    {
        LoadPrefab("Prefab", "default");

    }

    public void LoadMediumResPrefab()
    {
        LoadPrefab("Prefab", "MediumRes");
    }

    public void LoadLowResPrefab()
    {
        LoadPrefab("Prefab", "LowRes");
    }

    public void Clear()
    {
        if (m_Instance != null)
        {
            Destroy(m_Instance);
            m_Instance = null;
        }

        if (m_PrefabHandle.IsValid())
            Addressables.Release(m_PrefabHandle);
    }

    void LoadPrefab(string key, string label)
    {
        Addressables.LoadAssetsAsync<GameObject>((IEnumerable) new List<object> { key, label }, null,
            Addressables.MergeMode.Intersection).Completed += PrefabLoaded;

    }

    void LoadTexture(string key, string label)
    {
        m_Mat = GetComponent<MeshRenderer>().material;

        Addressables.LoadAssetsAsync<Texture2D>((IEnumerable) new List<object> { key, label }, null, 
            Addressables.MergeMode.Intersection).Completed += TextureLoaded;
    }
    
    void PrefabLoaded(AsyncOperationHandle<IList<GameObject>> obj)
    {
        Clear();

        m_PrefabHandle = obj;
        m_Instance = Instantiate(obj.Result[0]);
    }

    void TextureLoaded(AsyncOperationHandle<IList<Texture2D>> obj)
    {
        m_Mat.mainTexture = obj.Result[0];
    }
}
