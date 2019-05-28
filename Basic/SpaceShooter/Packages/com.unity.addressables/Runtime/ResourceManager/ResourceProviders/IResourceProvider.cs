using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Flags for resource providers.
    /// </summary>
    public enum ProviderBehaviourFlags
    {
        None = 0,
        CanProvideWithFailedDependencies = 1
    }

    /// <summary>
    /// Container for all data need by providers to fulfill requests.
    /// </summary>
    public struct ProvideHandle
    {
        int m_Version;
        IGenericProviderOperation m_InternalOp;
        ResourceManager m_ResourceManager;
        internal ProvideHandle(ResourceManager rm, IGenericProviderOperation op)
        {
            m_ResourceManager = rm;
            m_InternalOp = op;
            m_Version = op.ProvideHandleVersion;
        }

        IGenericProviderOperation InternalOp
        {
            get
            {
                if (m_InternalOp.ProvideHandleVersion != m_Version)
                {
                    throw new Exception(ProviderOperation<object>.kInvalidHandleMsg);
                }
                return m_InternalOp;
            }
        }

        /// <summary>
        /// The ResourceManager used to create the operation.
        /// </summary>
        public ResourceManager ResourceManager
        {
            get
            {
                return m_ResourceManager;
            }
        }

        /// <summary>
        /// The requested object type.
        /// </summary>
        public Type Type { get { return InternalOp.RequestedType; } }

        /// <summary>
        /// The location for the request.
        /// </summary>
        public IResourceLocation Location { get { return InternalOp.Location; } }

        /// <summary>
        /// Number of dependencies.
        /// </summary>
        public int DependencyCount { get { return InternalOp.DependencyCount; } }

        /// <summary>
        /// Get a specific dependency object.
        /// </summary>
        /// <typeparam name="TDepObject">The dependency type.</typeparam>
        /// <param name="index">The index of the dependency.</param>
        /// <returns>The dependency object.</returns>
        public TDepObject GetDependency<TDepObject>(int index) { return InternalOp.GetDependency<TDepObject>(index); }

        /// <summary>
        /// Get the depedency objects.
        /// </summary>
        /// <param name="list">The list of dependecies to fill in.</param>
        public void GetDependencies(IList<object> list)
        {
            InternalOp.GetDependencies(list);
        }

        /// <summary>
        /// Set the func for handling progress requests.
        /// </summary>
        /// <param name="callback">The callback function.</param>
        public void SetProgressCallback(Func<float> callback)
        {
            InternalOp.SetProgressCallback(callback);
        }
        
        /// <summary>
        /// Called to complete the operation.
        /// </summary>
        /// <typeparam name="T">The type of object requested.</typeparam>
        /// <param name="result">The result object.</param>
        /// <param name="status">True if the operation was successful, false otherwise.</param>
        /// <param name="exception">The exception if the operation failed.</param>
        public void Complete<T>(T result, bool status, Exception exception)
        {
            InternalOp.ProviderCompleted<T>(result, status, exception);
        }
    }


    /// <summary>
    /// Resoure Providers handle loading (Provide) and unloading (Release) of objects
    /// </summary>
    public interface IResourceProvider
    {
        /// <summary>
        /// Unique identifier for this provider, used by Resource Locations to find a suitable Provider
        /// </summary>
        /// <value>The provider identifier.</value>
        string ProviderId { get; }

        /// <summary>
        /// The default type of object that this provider can provide.
        /// </summary>
        /// <param name="location">The location that can be used to determine the type.</param>
        /// <returns>The type of object that can be provided.</returns>
        Type GetDefaultType(IResourceLocation location);

        /// <summary>
        /// Determine if this provider can provide the specified object type from the specified location.
        /// </summary>
        /// <param name="type">The type of object.</param>
        /// <param name="location">The resource location of the object.</param>
        /// <returns>True if this provider can create the specified object.</returns>
        bool CanProvide(Type type, IResourceLocation location);

        /// <summary>
        /// Tells the provide that it needs to provide a resource and report the results through the passed provideHandle. When this is called, all dependencies have completed and are available through the provideHandle.
        /// </summary>
        /// <param name="provideHandle">A handle used to update the operation.</param>
        void Provide(ProvideHandle provideHandle);

        /// <summary>
        /// Release and/or unload the given resource location and asset
        /// </summary>
        /// <returns><c>true</c>, if release was successful. <c>false</c> otherwise.</returns>
        /// <param name="location">Location to release.</param>
        /// <param name="asset">Asset to unload.</param>
        void Release(IResourceLocation location, object asset);

        /// <summary>
        /// Custom flags for the provider.
        /// </summary>
        ProviderBehaviourFlags BehaviourFlags { get; }
    }
}
