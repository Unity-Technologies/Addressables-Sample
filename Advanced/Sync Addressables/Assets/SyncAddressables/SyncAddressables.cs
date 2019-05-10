using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class SyncAddressables
{
    static bool s_Initialized = false;
    
    public static bool Ready
    {
        get { return s_Initialized; }
    }

    [RuntimeInitializeOnLoadMethod]
    static void Init()
    {
        Addressables.InitializeAsync().Completed += InitDone;
    }

    static void InitDone(AsyncOperationHandle<IResourceLocator> obj)
    {
        s_Initialized = true;
    }

    public static TObject LoadAsset<TObject>(object key)
    {
        if(!s_Initialized)
            throw new Exception("Whoa there friend!  We haven't init'd yet!");

        var op = Addressables.LoadAssetAsync<TObject>(key);        
        
        if(!op.IsDone)
            throw new Exception("Sync LoadAsset failed to load in a sync way! " + key);

        if (op.Result == null)
        {
            var message = "Sync LoadAsset has null result " + key;
            if (op.OperationException != null)
                message += " Exception: " + op.OperationException;

            throw new Exception(message);
        }

        return op.Result;
    }

    public static GameObject Instantiate(object key, Transform parent = null, bool instantiateInWorldSpace = false)
    {
        if(!s_Initialized)
            throw new Exception("Whoa there friend!  We haven't init'd yet!");
        
        var op = Addressables.InstantiateAsync(key, parent, instantiateInWorldSpace); 
        
        if(!op.IsDone)
            throw new Exception("Sync Instantiate failed to finish! " + key);

        if (op.Result == null)
        {
            var message = "Sync Instantiate has null result " + key;
            if (op.OperationException != null)
                message += " Exception: " + op.OperationException;

            throw new Exception(message);
        }
        
        return op.Result;
    }
}
