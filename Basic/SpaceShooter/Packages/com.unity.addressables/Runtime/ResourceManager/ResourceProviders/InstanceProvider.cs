using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Basic implementation of IInstanceProvider.
    /// </summary>
    public class InstanceProvider : IInstanceProvider
    {
        Dictionary<GameObject, AsyncOperationHandle<GameObject>> m_InstanceObjectToPrefabHandle = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();
  
        /// <inheritdoc/>
        public GameObject ProvideInstance(ResourceManager resourceManager, AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiateParameters)
        {
            GameObject result = instantiateParameters.Instantiate(prefabHandle.Result);
            m_InstanceObjectToPrefabHandle.Add(result, prefabHandle);
            return result;
        }

        /// <inheritdoc/>
        public void ReleaseInstance(ResourceManager resourceManager, GameObject instance)
        {
            AsyncOperationHandle<GameObject> resource;
            if (!m_InstanceObjectToPrefabHandle.TryGetValue(instance, out resource))
            {
                Debug.LogWarningFormat("Releasing unknown GameObject {0} to InstanceProvider.", instance);
            }
            else
            {
                resourceManager.Release(resource);
                m_InstanceObjectToPrefabHandle.Remove(instance);
            }

            if (instance != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(instance);
                else
                    Object.DestroyImmediate(instance);
            }
        }
    }
}
