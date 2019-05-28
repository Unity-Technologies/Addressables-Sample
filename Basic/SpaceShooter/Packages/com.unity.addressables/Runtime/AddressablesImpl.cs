using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.Addressables.Editor.Tests")]
#endif
[assembly: InternalsVisibleTo("Unity.Addressables.Tests")]
namespace UnityEngine.AddressableAssets
{

    internal class AddressablesImpl
    {
        ResourceManager m_ResourceManager;
        IInstanceProvider m_InstanceProvider;
        public IInstanceProvider InstanceProvider
        {
            get
            {
                return m_InstanceProvider;
            }
            set
            {
                m_InstanceProvider = value;
                var rec = m_InstanceProvider as IUpdateReceiver;
                if (rec != null)
                    m_ResourceManager.AddUpdateReceiver(rec);
            }
        }
        public ISceneProvider SceneProvider;
        public ResourceManager ResourceManager
        {
            get
            {
                if (m_ResourceManager == null)
                    m_ResourceManager = new ResourceManager(new DefaultAllocationStrategy());
                return m_ResourceManager;
            }
        }

        List<IResourceLocator> m_ResourceLocators = new List<IResourceLocator>();
        AsyncOperationHandle<IResourceLocator> m_InitializationOperation;

        Action<AsyncOperationHandle> m_OnHandleCompleteAction;
        Action<AsyncOperationHandle<SceneInstance>> m_OnSceneHandleCompleteAction;
        Action<AsyncOperationHandle> m_OnHandleDestroyedAction;
        Dictionary<object, AsyncOperationHandle> m_resultToHandle = new Dictionary<object, AsyncOperationHandle>();
        HashSet<AsyncOperationHandle<SceneInstance>> m_SceneInstances = new HashSet<AsyncOperationHandle<SceneInstance>>();

        internal int SceneOperationCount { get { return m_SceneInstances.Count; } }
        internal int TrackedHandleCount { get { return m_resultToHandle.Count; } }


        public AddressablesImpl(IAllocationStrategy alloc)
        {
            m_ResourceManager = new ResourceManager(alloc);
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            foreach (var s in m_SceneInstances)
            {
                if (!s.IsValid())
                {
                    m_SceneInstances.Remove(s);
                    break;
                }
                if (s.Result.Scene == scene)
                {
                    m_SceneInstances.Remove(s);
                    m_ResourceManager.ReleaseScene(SceneProvider, s);
                    break;
                }
            }
            m_ResourceManager.CleanupSceneInstances();
        }

        public string StreamingAssetsSubFolder
        {
            get
            {
                return "aa";
            }
        }

        public string BuildPath
        {
            get { return "Library/com.unity.addressables/StreamingAssetsCopy/" + StreamingAssetsSubFolder + "/" + PlatformMappingService.GetPlatform(); }
        }

        public string PlayerBuildDataPath
        {
            get
            {
                return Application.streamingAssetsPath + "/" + StreamingAssetsSubFolder + "/" +
                       PlatformMappingService.GetPlatform();
            }
        }

        public string RuntimePath
        {
            get
            {
#if UNITY_EDITOR
                return BuildPath;
#else
                return PlayerBuildDataPath;
#endif
            }
        }

        public void Log(string msg)
        {
            Debug.Log(msg);
        }

        public void LogFormat(string format, params object[] args)
        {
            Debug.LogFormat(format, args);
        }

        public void LogWarning(string msg)
        {
            Debug.LogWarning(msg);
        }

        public void LogWarningFormat(string format, params object[] args)
        {
            Debug.LogWarningFormat(format, args);
        }

        public void LogError(string msg)
        {
            Debug.LogError(msg);
        }

        public void LogException(AsyncOperationHandle op, Exception ex)
        {
            Debug.LogErrorFormat("{0} encountered in operation {1}: {2}", ex.GetType().Name, op.DebugName, ex.Message);
        }

        public void LogErrorFormat(string format, params object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }

        public string ResolveInternalId(string id)
        {
            var path = AddressablesRuntimeProperties.EvaluateString(id);
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_XBOXONE
            if (path.Length >= 260 && path.StartsWith(Application.dataPath))
                path = path.Substring(Application.dataPath.Length + 1);
#endif
            return path;
        } 

        public IList<IResourceLocator> ResourceLocators
        {
            get
            {
                return m_ResourceLocators;
            }
        }

        internal bool GetResourceLocations(object key, out IList<IResourceLocation> locations)
        {
            key = EvaluateKey(key);

            locations = null;
            HashSet<IResourceLocation> current = null;
            foreach (var l in m_ResourceLocators)
            {
                IList<IResourceLocation> locs;
                if (l.Locate(key, out locs))
                {
                    if (locations == null)
                    {
                        //simple, common case, no allocations
                        locations = locs;
                    }
                    else
                    {
                        //less common, need to merge...
                        if (current == null)
                        {
                            current = new HashSet<IResourceLocation>();
                            foreach (var loc in locations)
                                current.Add(loc);
                        }

                        current.UnionWith(locs);
                    }
                }
            }

            if (current == null)
                return locations != null;

            locations = new List<IResourceLocation>(current);
            return true;
        }

        internal bool GetResourceLocations(IEnumerable<object> keys, Addressables.MergeMode merge, out IList<IResourceLocation> locations)
        {
            locations = null;
            HashSet<IResourceLocation> current = null;
            foreach (var key in keys)
            {
                IList<IResourceLocation> locs;
                if (GetResourceLocations(key, out locs))
                {
                    if (locations == null)
                    {
                        locations = locs;
                        if (merge == Addressables.MergeMode.UseFirst)
                            return true;
                    }
                    else
                    {
                        if (current == null)
                        {
                            current = new HashSet<IResourceLocation>();
                            foreach (var loc in locations)
                                current.Add(loc);
                        }

                        if (merge == Addressables.MergeMode.Intersection)
                            current.IntersectWith(locs);
                        else if (merge == Addressables.MergeMode.Union)
                            current.UnionWith(locs);
                    }
                }
                else
                {
                    //if entries for a key are not found, the intersection is empty
                    if (merge == Addressables.MergeMode.Intersection)
                    {
                        locations = null;
                        return false;
                    }
                }
            }

            if (current == null)
                return locations != null;
            if (current.Count == 0)
            {
                locations = null;
                return false;
            }
            locations = new List<IResourceLocation>(current);
            return true;
        }

        public AsyncOperationHandle<IResourceLocator> InitializeAsync(string runtimeDataPath, string providerSuffix = null)
        {
            //these need to be referenced in order to prevent stripping on IL2CPP platforms.
            if (string.IsNullOrEmpty(Application.streamingAssetsPath))
                Debug.LogWarning("Application.streamingAssetsPath has been stripped!");
#if !UNITY_SWITCH
            if (string.IsNullOrEmpty(Application.persistentDataPath))
                Debug.LogWarning("Application.persistentDataPath has been stripped!");
#endif
            if (string.IsNullOrEmpty(runtimeDataPath))
                return ResourceManager.CreateCompletedOperation<IResourceLocator>(null, string.Format("Invalid Key: {0}", runtimeDataPath));

            m_OnHandleCompleteAction = OnHandleCompleted;
            m_OnSceneHandleCompleteAction = OnSceneHandleCompleted;
            m_OnHandleDestroyedAction = OnHandleDestroyed;
            m_InitializationOperation = Initialization.InitializationOperation.CreateInitializationOperation(this, runtimeDataPath, providerSuffix);
            m_InitializationOperation.Completed += (x) => ResourceManager.ExceptionHandler = LogException;
            
            return m_InitializationOperation;
        }

        public AsyncOperationHandle<IResourceLocator> InitializeAsync()
        {
            if (!m_InitializationOperation.IsValid())
                return InitializeAsync(ResolveInternalId(PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath, RuntimePath + "/settings.json")));
            return m_InitializationOperation;
        }
        public AsyncOperationHandle<IResourceLocator> LoadContentCatalogAsync(string catalogPath, string providerSuffix = null)
        {
            var catalogLoc = new ResourceLocationBase(catalogPath, catalogPath, typeof(JsonAssetProvider).FullName);
            if (!InitializationOperation.IsDone)
                return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadContentCatalogAsync(catalogPath, providerSuffix));
            return Initialization.InitializationOperation.LoadContentCatalog(this, catalogLoc, providerSuffix);
        }

        public AsyncOperationHandle<IResourceLocator> InitializationOperation
        {
            get
            {
                if (!m_InitializationOperation.IsValid())
                    InitializeAsync();
                return m_InitializationOperation;
            }
        }
        AsyncOperationHandle<SceneInstance> TrackHandle(AsyncOperationHandle<SceneInstance> handle)
        {
            handle.Completed += m_OnSceneHandleCompleteAction;
            return handle;
        }

        AsyncOperationHandle<TObject> TrackHandle<TObject>(AsyncOperationHandle<TObject> handle)
        {
            handle.CompletedTypeless += m_OnHandleCompleteAction;
            return handle;
        }

        AsyncOperationHandle TrackHandle(AsyncOperationHandle handle)
        {
            handle.Completed += m_OnHandleCompleteAction;
            return handle;
        }
        internal void ClearTrackHandles()
        {
            m_resultToHandle.Clear();
        }

        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(IResourceLocation location)
        {
            return TrackHandle(ResourceManager.ProvideResource<TObject>(location));
        }

        AsyncOperationHandle<TObject> LoadAssetWithChain<TObject>(object key)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadAssetAsync<TObject>(key));
        }

        public AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(object key)
        {
            if (!InitializationOperation.IsDone)
                return LoadAssetWithChain<TObject>(key);

            key = EvaluateKey(key);

            IList<IResourceLocation> locs;
            for (int i = 0; i < m_ResourceLocators.Count; i++)
            {
                if (m_ResourceLocators[i].Locate(key, out locs))
                {
                    foreach (var loc in locs)
                    {
                        var provider = ResourceManager.GetResourceProvider(typeof(TObject), loc);
                        if (provider != null)
                            return TrackHandle(ResourceManager.ProvideResource<TObject>(loc));
                    }
                }
            }
            return ResourceManager.CreateCompletedOperation<TObject>(default(TObject), new InvalidKeyException(key).Message);
        }

        class LoadResourceLocationKeyOp : AsyncOperationBase<IList<IResourceLocation>>
        {
            object m_Key;
            IList<IResourceLocation> m_locations;
            AddressablesImpl m_Addressables;
            protected override string DebugName { get { return m_Key.ToString(); } }

            public void Init(AddressablesImpl aa, object key)
            {
                m_Key = key;
                m_Addressables = aa;
            }
            protected override void Execute()
            {
                bool result = m_Addressables.GetResourceLocations(m_Key, out m_locations);
                Complete(m_locations, result, string.Empty);
            }
        }

        class LoadResourceLocationKeysOp : AsyncOperationBase<IList<IResourceLocation>>
        {
            IList<object> m_Key;
            Addressables.MergeMode m_MergeMode;
            IList<IResourceLocation> m_locations;
            AddressablesImpl m_Addressables;

            protected override string DebugName { get { return "LoadResourceLocationKeysOp"; } }
            public void Init(AddressablesImpl aa, IList<object> key, Addressables.MergeMode mergeMode)
            {
                m_Key = key;
                m_MergeMode = mergeMode;
                m_Addressables = aa;
            }
            protected override void Execute()
            {
                bool result = m_Addressables.GetResourceLocations(m_Key, m_MergeMode, out m_locations);
                Complete(m_locations, result, string.Empty);
            }
        }

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(IList<object> keys, Addressables.MergeMode mode)
        {
            var op = new LoadResourceLocationKeysOp();
            op.Init(this, keys, mode);
            return TrackHandle(ResourceManager.StartOperation(op, InitializationOperation));
        }

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocationsAsync(object key)
        {
            var op = new LoadResourceLocationKeyOp();
            op.Init(this, key);
            return TrackHandle(ResourceManager.StartOperation(op, InitializationOperation));
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IList<IResourceLocation> locations, Action<TObject> callback)
        {
            return TrackHandle(ResourceManager.ProvideResources(locations, callback));
        }

        AsyncOperationHandle<IList<TObject>> LoadAssetsWithChain<TObject>(IList<object> keys, Action<TObject> callback, Addressables.MergeMode mode)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadAssetsAsync(keys, callback, mode));
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(IList<object> keys, Action<TObject> callback, Addressables.MergeMode mode)
        {
            if (!InitializationOperation.IsDone)
                return LoadAssetsWithChain(keys, callback, mode);
            
            IList<IResourceLocation> locations;
            if (!GetResourceLocations(keys, mode, out locations))
                return ResourceManager.CreateCompletedOperation<IList<TObject>>(null, new InvalidKeyException(keys).Message);

            return LoadAssetsAsync(locations, callback);
        }

        AsyncOperationHandle<IList<TObject>> LoadAssetsWithChain<TObject>(object key, Action<TObject> callback)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op2 => LoadAssetsAsync(key, callback));
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssetsAsync<TObject>(object key, Action<TObject> callback)
        {
            if (!InitializationOperation.IsDone)
                return LoadAssetsWithChain(key, callback);

            IList<IResourceLocation> locations;
            if (!GetResourceLocations(key, out locations))
                return ResourceManager.CreateCompletedOperation<IList<TObject>>(null, new InvalidKeyException(key).Message);

            return LoadAssetsAsync(locations, callback);
        }

        void OnHandleDestroyed(AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                m_resultToHandle.Remove(handle.Result);
            }
        }

        void OnSceneHandleCompleted(AsyncOperationHandle<SceneInstance> handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                m_SceneInstances.Add(handle);
        }

        void OnHandleCompleted(AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            { 
                if (!m_resultToHandle.ContainsKey(handle.Result))
                {
                    handle.Destroyed += m_OnHandleDestroyedAction;
                    m_resultToHandle.Add(handle.Result, handle);
                }
            }
        }

        public  void Release<TObject>(TObject obj)
        {
            if (obj == null)
            {
                LogWarning("Addressables.Release() - trying to release null object.");
                return;
            }

            AsyncOperationHandle handle;
            if (m_resultToHandle.TryGetValue(obj, out handle))
                Release(handle);
            else
            {
                LogError("Addressables.Release was called on an object that Addressables was not previously aware of.  Thus nothing is being released");
            }
        }

        public void Release<TObject>(AsyncOperationHandle<TObject> handle)
        {
            m_ResourceManager.Release(handle);
        }
        
        public void Release(AsyncOperationHandle handle)
        {
            m_ResourceManager.Release(handle);
        }

        AsyncOperationHandle<long> GetDownloadSizeWithChain(object key)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => GetDownloadSizeAsync(key));
        }
        public AsyncOperationHandle<long> GetDownloadSizeAsync(object key)
        {
            if (!InitializationOperation.IsDone)
                return GetDownloadSizeWithChain(key);

            IList<IResourceLocation> locations;
            if (typeof(IList<IResourceLocation>).IsAssignableFrom(key.GetType()))
                locations = key as IList<IResourceLocation>;
            else if (typeof(IResourceLocation).IsAssignableFrom(key.GetType()))
            {
                locations = new List<IResourceLocation>(1);
                locations.Add(key as IResourceLocation);
            }
            else
            {
                if (!GetResourceLocations(key, out locations))
                    return ResourceManager.CreateCompletedOperation<long>(0, new InvalidKeyException(key).Message);
            }

            var locHash = new HashSet<IResourceLocation>();
            foreach (var loc in locations)
            {
                if (loc.HasDependencies)
                {
                    foreach (var dep in loc.Dependencies)
                        locHash.Add(dep);
                }
            }

            long size = 0;
            foreach (var d in locHash)
            {
                var sizeData = d.Data as ILocationSizeData;
                if (sizeData != null)
                    size += sizeData.ComputeSize(d);
            }
            return ResourceManager.CreateCompletedOperation<long>(size, string.Empty);
        }

        AsyncOperationHandle<IList<IAssetBundleResource>> DownloadDependenciesWithChain(object key)
        {
            return ResourceManager.CreateChainOperation<IList<IAssetBundleResource>>(InitializationOperation, op => DownloadDependenciesAsync(key).Convert<IList<IAssetBundleResource>>());
        }

        public AsyncOperationHandle DownloadDependenciesAsync(object key)
        {
            if (!InitializationOperation.IsDone)
                return DownloadDependenciesWithChain(key);

            IList<IResourceLocation> locations;
            if (!GetResourceLocations(key, out locations))
                return ResourceManager.CreateCompletedOperation<IList<IAssetBundleResource>>(null, new InvalidKeyException(key).Message);


            var locHash = new HashSet<IResourceLocation>();
            foreach (var loc in locations)
            {
                if (loc.HasDependencies)
                {
                    foreach (var dep in loc.Dependencies)
                        locHash.Add(dep);
                }
            }
            return LoadAssetsAsync<IAssetBundleResource>(new List<IResourceLocation>(locHash), null);
        }

        public  AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return InstantiateAsync(location, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
        }
        public  AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return InstantiateAsync(location, new InstantiationParameters(position, rotation, parent), trackHandle);
        }
        public  AsyncOperationHandle<GameObject> InstantiateAsync(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return InstantiateAsync(key, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
        }
      
        public  AsyncOperationHandle<GameObject> InstantiateAsync(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return InstantiateAsync(key, new InstantiationParameters(position, rotation, parent), trackHandle);
        }

        AsyncOperationHandle<GameObject> InstantiateWithChain(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => InstantiateAsync(key, instantiateParameters, trackHandle));
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            if (!InitializationOperation.IsDone)
                return InstantiateWithChain(key, instantiateParameters, trackHandle);

            key = EvaluateKey(key);
            IList<IResourceLocation> locs;
            for (int i = 0; i < m_ResourceLocators.Count; i++)
            {
                if (m_ResourceLocators[i].Locate(key, out locs))
                    return InstantiateAsync(locs[0], instantiateParameters, trackHandle);
            }
            return ResourceManager.CreateCompletedOperation<GameObject>(null, new InvalidKeyException(key).Message);
        }

        public AsyncOperationHandle<GameObject> InstantiateAsync(IResourceLocation location, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            var opHandle = ResourceManager.ProvideInstance(InstanceProvider, location, instantiateParameters);
            if (!trackHandle)
                return opHandle;
            opHandle.CompletedTypeless += m_OnHandleCompleteAction;
            return opHandle;
        }
        
        public bool ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                LogWarning("Addressables.ReleaseInstance() - trying to release null object.");
                return false;
            }

            AsyncOperationHandle handle;
            if (m_resultToHandle.TryGetValue(instance, out handle))
                Release(handle);
            else
                return false;

            return true;
        }


        AsyncOperationHandle<SceneInstance> LoadSceneWithChain(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadSceneAsync(key, loadMode, activateOnLoad, priority));
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            if (!InitializationOperation.IsDone)
                return LoadSceneWithChain(key, loadMode, activateOnLoad, priority);

            IList<IResourceLocation> locations;
            if (!GetResourceLocations(key, out locations))
                return ResourceManager.CreateCompletedOperation<SceneInstance>(default(SceneInstance), new InvalidKeyException(key).Message);

            return LoadSceneAsync(locations[0], loadMode, activateOnLoad, priority);
        }

        public  AsyncOperationHandle<SceneInstance> LoadSceneAsync(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return TrackHandle(ResourceManager.ProvideScene(SceneProvider, location, loadMode, activateOnLoad, priority));
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true)
        {
            AsyncOperationHandle handle;
            if (!m_resultToHandle.TryGetValue(scene, out handle))
            {
                var msg = string.Format("Addressables.UnloadSceneAsync() - Cannot find handle for scene {0}", scene);
                LogWarning(msg);
                return ResourceManager.CreateCompletedOperation<SceneInstance>(scene, msg);
            }

            return UnloadSceneAsync(handle, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle handle, bool autoReleaseHandle = true)
        {
            return UnloadSceneAsync(handle.Convert<SceneInstance>(), autoReleaseHandle);
        }
        
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true)
        {   
            var relOp = ResourceManager.ReleaseScene(SceneProvider, handle);
            if (autoReleaseHandle)
                relOp.Completed += op => Release(op);
            return relOp;
        }

        private object EvaluateKey(object obj)
        {
            if (obj is IKeyEvaluator)
                return (obj as IKeyEvaluator).RuntimeKey;
            return obj;
        }
    }
}

