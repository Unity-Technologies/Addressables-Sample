using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Provides assets loaded via Resources.LoadAsync API.
    /// </summary>
    public class LegacyResourcesProvider : ResourceProviderBase
    {
        internal class InternalOp
        {
            AsyncOperation m_RequestOperation;
            ProvideHandle m_PI;
            
            public void Start(ProvideHandle provideHandle)
            {
                m_PI = provideHandle;
                
                m_RequestOperation = Resources.LoadAsync<Object>(m_PI.Location.InternalId);
                m_RequestOperation.completed += AsyncOperationCompleted;
                provideHandle.SetProgressCallback(PercentComplete);
            }

            private void AsyncOperationCompleted(AsyncOperation op)
            {
                var request = op as ResourceRequest;
                object result = request != null ? request.asset : null;
                result = result != null && m_PI.Type.IsAssignableFrom(result.GetType()) ? result : null;
                m_PI.Complete(result, result != null, null);
            }

            public float PercentComplete() { return m_RequestOperation != null ? m_RequestOperation.progress : 0.0f; }
        }

        public override void Provide(ProvideHandle pi)
        {
            Type t = pi.Type;
            bool isList = t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition();

            if (t.IsArray || isList)
            {
                object result = null;
                if (t.IsArray)
                    result = ResourceManagerConfig.CreateArrayResult(t, Resources.LoadAll(pi.Location.InternalId, t.GetElementType()));
                else
                    result = ResourceManagerConfig.CreateListResult(t, Resources.LoadAll(pi.Location.InternalId, t.GetGenericArguments()[0]));

                pi.Complete(result, result != null, null);
            }
            else
            {
                new InternalOp().Start(pi);
            }
        }

        /// <inheritdoc/>
        public override void Release(IResourceLocation location, object asset)
        {
            if (location == null)
                throw new ArgumentNullException("location");
            var obj = asset as Object;
            //GameObjects cannot be resleased via Object.Destroy because they are considered an asset
            //but they can't be unloaded via Resources.UnloadAsset since they are NOT an asset?
            if (obj != null && !(obj is GameObject))
                Resources.UnloadAsset(obj);
        }
    }
}
