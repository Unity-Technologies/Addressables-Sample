using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace PlayAssetDelivery
{
    public class LoadObject : MonoBehaviour
    {
        public InitializeAddressables script;
        public AssetReference reference;
        public Transform parent;

        bool isLoading = false;
        GameObject obj = null;

        public void Load()
        {
            if (!script.hasInitialized)
                Debug.LogError("Not finished initializing.");
            else if (isLoading)
                Debug.LogError("Loading operation currently in progress.");
            else if(!isLoading)
            {
                if (obj == null)
                {
                    // Load the object
                    StartCoroutine(Instantiate());
                }
                else
                {
                    // Destroy the object
                    Addressables.ReleaseInstance(obj);
                    obj = null;
                }
            }
        }

        IEnumerator Instantiate()
        {
            isLoading = true;
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(reference, parent);
            yield return handle;
            obj = handle.Result;
            isLoading = false;
        }
    }
}
