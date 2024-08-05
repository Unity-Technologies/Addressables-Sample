// Multi cube pack loader. ( Serial )

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[Serializable]
public struct PackLoadDefAdr
{
    public string prefabAddress; 
}

public class PackLoadOpAdr
{
    public PackLoadDefAdr def;
    public GameObject ourPrefabAsset;
    public GameObject ourInstantiatedPrefab;
}

public class AdvancedCubeBundleLoaderAdr : MonoBehaviour
{
    public List<PackLoadDefAdr> loadQueue = null;
    public List<PackLoadOpAdr> loadQueueInternal = null;

    private int maxProgress = 0;
    private int progress = 0;

    public UnityEngine.UI.Slider slider = null;
    
    void BumpProgress()
    {
        ++progress;
        if (slider != null)
        {
            slider.value = progress;
        }
    }
    
    public IEnumerator ExecuteOnePack( PackLoadOpAdr plo )
    {
        plo.ourPrefabAsset = null;
        plo.ourInstantiatedPrefab = null;
        
        Debug.Log( $"Attempting to locate prefab '{plo.def.prefabAddress}' " );

        var op = Addressables.LoadAssetAsync<GameObject>(plo.def.prefabAddress);
        yield return op;
        
        if (op.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log( $"Loaded asset '{plo.def.prefabAddress}' ok" );

            plo.ourPrefabAsset = op.Result;
            plo.ourInstantiatedPrefab = Instantiate(plo.ourPrefabAsset);
        }
        else
        {
            Debug.Log( $"Failed to load asset '{plo.def.prefabAddress}' - {op.ToString()}" );
        }
        BumpProgress();
                
        yield return null;
    }
    
    public IEnumerator ExecuteQueue()
    {
        loadQueueInternal = new List<PackLoadOpAdr>();

        Debug.Log( $"*** Working with Queue of '{loadQueue.Count}' items ***" ); 

        maxProgress = loadQueue.Count;
        progress = 0;
        if (slider != null)
        {
            slider.gameObject.SetActive( true );
            slider.enabled = true;

            slider.minValue = 0;
            slider.maxValue = maxProgress;
            slider.value = 0;
        }
        
        foreach (var pld in loadQueue)
        {
            var plo = new PackLoadOpAdr();
            plo.def = pld;
            loadQueueInternal.Add( plo );

            Debug.Log( $"*** Queue item ${pld.prefabAddress} ***" ); 

            yield return ExecuteOnePack( plo );
        }

        Debug.Log( $"*** Queue Loaded ***" ); 
        
        yield return null;
        if (slider != null)
        {
            slider.gameObject.SetActive(false);
            slider.enabled = false;
        }
    }    

    void Start()
    {
        StartCoroutine(ExecuteQueue());
    }
}
