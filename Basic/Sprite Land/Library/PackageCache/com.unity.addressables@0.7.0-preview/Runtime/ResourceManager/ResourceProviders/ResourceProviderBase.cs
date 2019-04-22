using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Base class for IResourceProvider.
    /// </summary>
    public abstract class ResourceProviderBase : IResourceProvider, IInitializableObject
    {
        protected string m_ProviderId;
        protected ProviderBehaviourFlags m_BehaviourFlags = ProviderBehaviourFlags.None;

        /// <inheritdoc/>
        public virtual string ProviderId
        {
            get
            {
                if(string.IsNullOrEmpty(m_ProviderId))
                    m_ProviderId = GetType().FullName;

                return m_ProviderId;
            }
        }

        /// <inheritdoc/>
        public virtual bool Initialize(string id, string data)
        {
            m_ProviderId = id;
            return !string.IsNullOrEmpty(m_ProviderId);
        }

        /// <inheritdoc/>
        public virtual bool CanProvide(Type t, IResourceLocation location)
        {
            return GetDefaultType(location).IsAssignableFrom(t);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return ProviderId;
        }

        /// <summary>
        /// Release the specified object that was created from the specified location.
        /// </summary>
        /// <param name="location">The location of the object</param>
        /// <param name="obj">The object to release.</param>
        /// <returns></returns>
        public virtual void Release(IResourceLocation location, object obj)
        {
        }

        /// <summary>
        /// Get the default type of object that this provider can provide.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public virtual Type GetDefaultType(IResourceLocation location)
        {
            return typeof(object);
        }

        /// <summary>
        /// Provide the object specified in the provideHandle.
        /// </summary>
        /// <param name="provideHandle">Contains all data needed to provide the requested object.</param>
        public abstract void Provide(ProvideHandle provideHandle);

        ProviderBehaviourFlags IResourceProvider.BehaviourFlags { get { return m_BehaviourFlags; } }
    }
}
