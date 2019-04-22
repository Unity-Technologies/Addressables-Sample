using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;

[assembly: InternalsVisibleTo("Unity.Addressables.Tests")]

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Exception to encapsulate invalid key errors.
    /// </summary>
    public class InvalidKeyException : Exception
    {
        /// <summary>
        /// The key used to generate the exception.
        /// </summary>
        public object Key { get; private set; }
        /// <summary>
        /// Construct a new InvalidKeyException.
        /// </summary>
        /// <param name="key">The key that caused the exception.</param>
        public InvalidKeyException(object key)
        {
            Key = key;
        }

        ///<inheritdoc/>
        public InvalidKeyException() { }
        ///<inheritdoc/>
        public InvalidKeyException(string message) : base(message) { }
        ///<inheritdoc/>
        public InvalidKeyException(string message, Exception innerException) : base(message, innerException) { }
        ///<inheritdoc/>
        protected InvalidKeyException(SerializationInfo message, StreamingContext context) : base(message, context) { }
        ///<inheritdoc/>
        public override string Message
        {
            get
            {
                return base.Message + ", Key=" + Key;
            }
        }
    }

    /// <summary>
    /// Entry point for Addressable API, this provides a simpler interface than using ResourceManager directly as it assumes string address type.
    /// </summary>
    public static class Addressables
    {
        static AddressablesImpl m_Addressables = new AddressablesImpl(new LRUCacheAllocationStrategy(1000, 1000, 100, 10));
        internal static IInstanceProvider InstanceProvider;
        public static ResourceManager ResourceManager { get { return m_Addressables.ResourceManager; } }
        internal static AddressablesImpl Instance { get { return m_Addressables; } }

        /// <summary>
        /// Used to resolve a string using addressables config values
        /// </summary>
        public static string ResolveInternalId(string id)
        {
            return m_Addressables.ResolveInternalId(id);
        }

        /// <summary>
        /// Enumerates the supported modes of merging the results of requests.
        /// If keys (A, B) mapped to results ([1,2,4],[3,4,5])...
        ///  - UseFirst (or None) takes the results from the first key 
        ///  -- [1,2,4]
        ///  - Union takes results of each key and collects items that matched any key.
        ///  -- [1,2,3,4,5]
        ///  - Intersection takes results of each key, and collects items that matched every key.
        ///  -- [4]
        /// </summary>
        public enum MergeMode
        {
            None = 0,
            UseFirst = 0,
            Union,
            Intersection
        }

        /// <summary>
        /// The name of the PlayerPrefs value used to set the path to load the addressables runtime data file. 
        /// </summary>
        public const string kAddressablesRuntimeDataPath = "AddressablesRuntimeDataPath";
        const string k_AddressablesLogConditional = "ADDRESSABLES_LOG_ALL";

        /// <summary>
        /// The subfolder used by the Addressables system for its initialization data.
        /// </summary>
        public static string StreamingAssetsSubFolder { get { return m_Addressables.StreamingAssetsSubFolder; } }

        /// <summary>
        /// The path used by the Addressables system for its initialization data.
        /// </summary>
        public static string BuildPath { get { return m_Addressables.BuildPath; } }

        /// <summary>
        /// The path that addressables player data gets copied to during a player build.
        /// </summary>
        public static string PlayerBuildDataPath { get { return m_Addressables.PlayerBuildDataPath; } }

        /// <summary>
        /// The path used by the Addressables system to load initialization data.
        /// </summary>
        public static string RuntimePath { get { return m_Addressables.RuntimePath; } }


        /// <summary>
        /// Gets the list of configured <see cref="IResourceLocator"/> objects. Resource Locators are used to find <see cref="IResourceLocation"/> objects from user-defined typed keys.
        /// </summary>
        /// <value>The resource locators list.</value>
        public static IList<IResourceLocator> ResourceLocators { get { return m_Addressables.ResourceLocators; } }

        /// <summary>
        /// Debug.Log wrapper method that is contional on the LOG_ADDRESSABLES symbol definition.  This can be set in the Player preferences in the 'Scripting Define Symbols'.
        /// </summary>
        /// <param name="msg">The msg to log</param>
        [Conditional(k_AddressablesLogConditional)]
        public static void Log(string msg)
        {
            m_Addressables.Log(msg);
        }

        /// <summary>
        /// Debug.LogFormat wrapper method that is contional on the LOG_ADDRESSABLES symbol definition.  This can be set in the Player preferences in the 'Scripting Define Symbols'.
        /// </summary>
        /// <param name="format">The string with format tags.</param>
        /// <param name="args">The args used to fill in the format tags.</param>
        [Conditional(k_AddressablesLogConditional)]
        public static void LogFormat(string format, params object[] args)
        {
            m_Addressables.LogFormat(format, args);
        }

        /// <summary>
        /// Debug.LogWarning wrapper method.
        /// </summary>
        /// <param name="msg">The msg to log</param>
        public static void LogWarning(string msg)
        {
            m_Addressables.LogWarning(msg);
        }

        /// <summary>
        /// Debug.LogWarningFormat wrapper method.
        /// </summary>
        /// <param name="format">The string with format tags.</param>
        /// <param name="args">The args used to fill in the format tags.</param>
        public static void LogWarningFormat(string format, params object[] args)
        {
            m_Addressables.LogWarningFormat(format, args);
        }

        /// <summary>
        /// Debug.LogError wrapper method.
        /// </summary>
        /// <param name="msg">The msg to log</param>
        public static void LogError(string msg)
        {
            m_Addressables.LogError(msg);
        }

        /// <summary>
        /// Debug.LogException wrapper method.
        /// </summary>
        /// <param name="msg">The msg to log</param>
        public static void LogException(AsyncOperationHandle op, Exception ex)
        {
            m_Addressables.LogException(op, ex);
        }

        /// <summary>
        /// Debug.LogErrorFormat wrapper method.
        /// </summary>
        /// <param name="format">The string with format tags.</param>
        /// <param name="args">The args used to fill in the format tags.</param>
        public static void LogErrorFormat(string format, params object[] args)
        {
            m_Addressables.LogErrorFormat(format, args);
        }

        /// <summary>
        /// Initialize Addressables system.  Addressables will be initialized on the first API call if this is not called explicitly.
        /// </summary>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IResourceLocator> Initialize()
        {
            return m_Addressables.Initialize();
        }

        /// <summary>
        /// Additively load catalogs from runtime data.  The settings are not used.
        /// </summary>
        /// <param name="catalogPath">The path to the runtime data.</param>
        /// <param name="providerSuffix">This value, if not null or empty, will be appended to all provider ids loaded from this data.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IResourceLocator> LoadContentCatalog(string catalogPath, string providerSuffix = null)
        {
            return m_Addressables.LoadContentCatalog(catalogPath, providerSuffix);
        }

        /// <summary>
        /// Initialization operation.  You can register a callback with this if you need to run code after Addressables is ready.  Any requests made before this operaton completes will automatically cahin to its result.
        /// </summary>
        public static AsyncOperationHandle<IResourceLocator> InitializationOperation
        {
            get
            {
                return m_Addressables.InitializationOperation;
            }
        }

        /// <summary>
        /// Load a single asset
        /// </summary>
        /// <param name="location">The location of the asset.</param>
        public static AsyncOperationHandle<TObject> LoadAsset<TObject>(IResourceLocation location)
        {
            return m_Addressables.LoadAsset<TObject>(location);
        }

        /// <summary>
        /// Load a single asset
        /// </summary>
        /// <param name="key">The key of the location of the asset.</param>
        public static AsyncOperationHandle<TObject> LoadAsset<TObject>(object key)
        {
            return m_Addressables.LoadAsset<TObject>(key);
        }
 
        /// <summary>
        /// Loads the resource locations specified by the keys.
        /// </summary>
        /// <param name="keys">The set of keys to use.</param>
        /// <param name="mode">The mode for merging the results of the found locations.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocations(IList<object> keys, MergeMode mode)
        {
            return m_Addressables.LoadResourceLocations(keys, mode);
        }

        /// <summary>
        /// Request the locations for a given key.
        /// </summary>
        /// <param name="key">The key for the locations.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocations(object key)
        {
            return m_Addressables.LoadResourceLocations(key);
        }

        /// <summary>
        /// Load multiple assets
        /// </summary>
        /// <param name="locations">The locations of the assets.</param>
        /// <param name="callback">Callback Action that is called per load operation.</param>        
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IList<TObject>> LoadAssets<TObject>(IList<IResourceLocation> locations, Action<TObject> callback)
        {
            return m_Addressables.LoadAssets(locations, callback);
        }

        /// <summary>
        /// Load mutliple assets
        /// </summary>
        /// <param name="keys">List of keys for the locations.</param>
        /// <param name="callback">Callback Action that is called per load operation.</param>
        /// <param name="mode">Method for merging the results of key matches.  See <see cref="MergeMode"/> for specifics</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IList<TObject>> LoadAssets<TObject>(IList<object> keys, Action<TObject> callback, MergeMode mode)
        {
            return m_Addressables.LoadAssets(keys, callback, mode);
        }

        /// <summary>
        /// Load mutliple assets
        /// </summary>
        /// <param name="key">Key for the locations.</param>
        /// <param name="callback">Callback Action that is called per load operation.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<IList<TObject>> LoadAssets<TObject>(object key, Action<TObject> callback)
        {
            return m_Addressables.LoadAssets(key, callback);
        }

        /// <summary>
        /// Release asset.
        /// </summary>
        /// <typeparam name="TObject">The type of the object being released</typeparam>
        /// <param name="obj">The asset to release.</param>
        public static void Release<TObject>(TObject obj)
        {
            m_Addressables.Release(obj);
        }

        /// <summary>
        /// Release the operation and its associated resources.
        /// </summary>
        /// <typeparam name="TObject">The type of the AsyncOperationHandle being released</typeparam>
        /// <param name="handle">The operation handle to release.</param>
        public static void Release<TObject>(AsyncOperationHandle<TObject> handle)
        {
            m_Addressables.Release(handle);
        }

        /// <summary>
        /// Release the operation and its associated resources.
        /// </summary>
        /// <param name="handle">The operation handle to release.</param>
        public static void Release(AsyncOperationHandle handle)
        {
            m_Addressables.Release(handle);
        }

        /// <summary>
        /// Asynchronously loads only the dependencies for the specified <paramref name="key"/>.
        /// </summary>
        /// <returns>The operation handle for the request.</returns>
        /// <param name="key">key for which to load dependencies.</param>
        public static AsyncOperationHandle<long> GetDownloadSize(object key)
        {
            return m_Addressables.GetDownloadSize(key);
        }

        /// <summary>
        /// Downloads dependencies of assets marked with the specified label or address.  
        /// </summary>
        /// <param name="key">The key of the asset(s) to load dependencies for.</param>
        /// <returns>The AsyncOperationHandle for the dependency load.</returns>
        public static AsyncOperationHandle DownloadDependencies(object key)
        {
            return m_Addressables.DownloadDependencies(key);
        }


        /// <summary>
        /// Instantiate single object.
        /// </summary>
        /// <param name="location">The location of the Object to instantiate.</param>
        /// <param name="parent">Parent transform for instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to retain world space when instantiated with a parent.</param>
        /// <param name="trackHandle">If true, Addressables will track this request to allow it to be released via the result object.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<GameObject> Instantiate(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return m_Addressables.Instantiate(location, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
        }

        /// <summary>
        /// Instantiate single object.
        /// </summary>
        /// <param name="location">The location of the Object to instantiate.</param>
        /// <param name="position">The position of the instantiated object.</param>
        /// <param name="rotation">The rotation of the instantiated object.</param>
        /// <param name="parent">Parent transform for instantiated object.</param>
        /// <param name="trackHandle">If true, Addressables will track this request to allow it to be released via the result object.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<GameObject> Instantiate(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return m_Addressables.Instantiate(location, position, rotation, parent, trackHandle);
        }

        /// <summary>
        /// Instantiate single object.
        /// </summary>
        /// <param name="key">The key of the location of the Object to instantiate.</param>
        /// <param name="parent">Parent transform for instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to retain world space when instantiated with a parent.</param>
        /// <param name="trackHandle">If true, Addressables will track this request to allow it to be released via the result object.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<GameObject> Instantiate(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return m_Addressables.Instantiate(key, parent, instantiateInWorldSpace, trackHandle);
        }

        /// <summary>
        /// Instantiate single object.
        /// </summary>
        /// <param name="key">The key of the location of the Object to instantiate.</param>
        /// <param name="position">The position of the instantiated object.</param>
        /// <param name="rotation">The rotation of the instantiated object.</param>
        /// <param name="parent">Parent transform for instantiated object.</param>
        /// <param name="trackHandle">If true, Addressables will track this request to allow it to be released via the result object.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<GameObject> Instantiate(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return m_Addressables.Instantiate(key, position, rotation, parent, trackHandle);
        }


        /// <summary>
        /// Instantiate single object.
        /// </summary>
        /// <param name="key">The key of the location of the Object to instantiate.</param>
        /// <param name="instantiateParameters">Parameters for instantiation.</param>
        /// <param name="trackHandle">If true, Addressables will track this request to allow it to be released via the result object.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<GameObject> Instantiate(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return m_Addressables.Instantiate(key, instantiateParameters, trackHandle);
        }

        /// <summary>
        /// Instantiate object.
        /// </summary>
        /// <param name="location">The location of the Object to instantiate.</param>
        /// <param name="instantiateParameters">Parameters for instantiation.</param>
        /// <param name="trackHandle">If true, Addressables will track this request to allow it to be released via the result object.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<GameObject> Instantiate(IResourceLocation location, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return m_Addressables.Instantiate(location, instantiateParameters, trackHandle);
        }

        /// <summary>
        /// Load scene.
        /// </summary>
        /// <param name="key">The key of the location of the scene to load.</param>
        /// <param name="loadMode">Scene load mode.</param>
        /// <param name="activateOnLoad">If false, the scene will load but not activate (for background loading).  The SceneInstance returned has an Activate() method that can be called to do this at a later point.</param>
        /// <param name="priority">Async operation priority for scene loading.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<SceneInstance> LoadScene(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return m_Addressables.LoadScene(key, loadMode, activateOnLoad, priority);
        }

        /// <summary>
        /// Load scene.
        /// </summary>
        /// <param name="location">The location of the scene to load.</param>
        /// <param name="loadMode">Scene load mode.</param>
        /// <param name="activateOnLoad">If false, the scene will load but not activate (for background loading).  The SceneInstance returned has an Activate() method that can be called to do this at a later point.</param>
        /// <param name="priority">Async operation priority for scene loading.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<SceneInstance> LoadScene(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return m_Addressables.LoadScene(location, loadMode, activateOnLoad, priority);
        }

        /// <summary>
        /// Release scene
        /// </summary>
        /// <param name="scene">The SceneInstance to release.</param>
        /// <param name="autoReleaseHandle">If true, the handle will be released automatically when complete.</param>
        /// <returns>The operation handle for the request.</returns>
        public static AsyncOperationHandle<SceneInstance> UnloadScene(SceneInstance scene, bool autoReleaseHandle = true)
        {
            return m_Addressables.UnloadScene(scene, autoReleaseHandle);
        }
     }

}

