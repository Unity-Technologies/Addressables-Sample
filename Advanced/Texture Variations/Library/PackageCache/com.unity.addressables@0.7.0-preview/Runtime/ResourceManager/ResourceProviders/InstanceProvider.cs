using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Basic implementation of IInstanceProvider.
    /// </summary>
    public class InstanceProvider : IInstanceProvider
    {
        Dictionary<GameObject, AsyncOperationHandle<GameObject>> m_Instantiations = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();

        /// <inheritdoc/>
        public GameObject ProvideInstance(AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiateParameters)
        {
            GameObject result = instantiateParameters.Instantiate(prefabHandle.Result);
            prefabHandle.Acquire();
            m_Instantiations.Add(result, prefabHandle);
            return result;
        }

        /// <inheritdoc/>
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                Debug.LogWarningFormat("Releasing null GameObject to InstanceProvider.");
            }
            else
            {
                AsyncOperationHandle<GameObject> resource;
                if (!m_Instantiations.TryGetValue(instance, out resource))
                {
                    Debug.LogWarningFormat("Releasing unknown GameObject {0} to InstanceProvider.", instance);
                }
                else
                {
                    resource.Release();
                    m_Instantiations.Remove(instance);
                }
            }
            if (Application.isPlaying)
                Object.Destroy(instance);
            else
                Object.DestroyImmediate(instance);
        }
    }
}
