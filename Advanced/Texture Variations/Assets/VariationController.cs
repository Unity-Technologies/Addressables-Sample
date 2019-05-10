using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class VariationController : MonoBehaviour
{
    Material m_Mat;
    void Start()
    {
        m_Mat = GetComponent<MeshRenderer>().material;
    }

    public void SwitchToHighDef()
    {
        LoadTexture("tree", "HD" );
    }

    public void SwitchToLowDef()
    {
        LoadTexture("tree", "SD");
    }

    void LoadTexture(string key, string label)
    {
        Addressables.LoadAssetsAsync<Texture2D>(new List<object> { key, label }, null, Addressables.MergeMode.Intersection).Completed
            += TextureLoaded;
    }
    
    void TextureLoaded(AsyncOperationHandle<IList<Texture2D>> obj)
    {
        m_Mat.mainTexture = obj.Result[0];
    }
}
