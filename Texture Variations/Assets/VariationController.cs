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
        Addressables.LoadAssets<Texture2D>(new List<object> { "tree", "HD" }, null, Addressables.MergeMode.Intersection).Completed += TextureLoaded;
    }

    public void SwitchToLowDef()
    {
        Addressables.LoadAssets<Texture2D>(new List<object> { "tree", "SD" }, null, Addressables.MergeMode.Intersection).Completed += TextureLoaded;
    }

    void TextureLoaded(AsyncOperationHandle<IList<Texture2D>> obj)
    {
        m_Mat.mainTexture = obj.Result[0];
    }
}
