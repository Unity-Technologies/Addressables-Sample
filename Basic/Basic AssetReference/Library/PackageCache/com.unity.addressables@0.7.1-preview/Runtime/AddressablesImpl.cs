using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Diagnostics;
using UnityEngine.ResourceManagement.Exceptions;
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
        public IInstanceProvider InstanceProvider;
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
        Action<AsyncOperationHandle> m_OnHandleDestroyedAction;
        Action<AsyncOperationHandle<GameObject>> m_OnInstanceHandleCompleteAction;
        Action<AsyncOperationHandle> m_OnInstanceHandleDestroyedAction;

        Dictionary<object, AsyncOperationHandle> m_resultToHandle = new Dictionary<object, AsyncOperationHandle>();

        public AddressablesImpl(IAllocationStrategy alloc)
        {
            m_ResourceManager = new ResourceManager(alloc);
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

        public AsyncOperationHandle<IResourceLocator> Initialize()
        {
            if (!m_InitializationOperation.IsValid())
            {
                //these need to be referenced in order to prevent stripping on IL2CPP platforms.
                if (string.IsNullOrEmpty(Application.streamingAssetsPath))
                    Debug.LogWarning("Application.streamingAssetsPath has been stripped!");
#if !UNITY_SWITCH
                if (string.IsNullOrEmpty(Application.persistentDataPath))
                    Debug.LogWarning("Application.persistentDataPath has been stripped!");
#endif
                var runtimeDataPath = ResolveInternalId(PlayerPrefs.GetString(Addressables.kAddressablesRuntimeDataPath, RuntimePath + "/settings.json"));

                if (string.IsNullOrEmpty(runtimeDataPath))
                    return ResourceManager.CreateCompletedOperation<IResourceLocator>(null, string.Format("Invalid Key: {0}", runtimeDataPath));

                m_OnHandleCompleteAction = OnHandleCompleted;
                m_OnHandleDestroyedAction = OnHandleDestroyed;
                m_OnInstanceHandleCompleteAction = OnInstanceHandleCompleted;
                m_OnInstanceHandleDestroyedAction = OnInstanceHandleDestroyed;
                m_InitializationOperation = Initialization.InitializationOperation.CreateInitializationOperation(this, runtimeDataPath, null);
                m_InitializationOperation.Completed += (x) => ResourceManager.ExceptionHandler = LogException;
                SceneManager.sceneUnloaded += OnSceneUnloaded;
            }
            return m_InitializationOperation;
        }
        public AsyncOperationHandle<IResourceLocator> LoadContentCatalog(string catalogPath, string providerSuffix = null)
        {
            var catalogLoc = new ResourceLocationBase(catalogPath, catalogPath, typeof(JsonAssetProvider).FullName);
            if (!InitializationOperation.IsDone)
                return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadContentCatalog(catalogPath, providerSuffix));
            return Initialization.InitializationOperation.LoadContentCatalog(this, catalogLoc, providerSuffix);
        }

        public AsyncOperationHandle<IResourceLocator> InitializationOperation
        {
            get
            {
                if (!m_InitializationOperation.IsValid())
                    Initialize();
                return m_InitializationOperation;
            }
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

        internal int TrackedHandleCount { get { return m_resultToHandle.Count; } }
        internal void ClearTrackHandles()
        {
            m_resultToHandle.Clear();
        }

        public AsyncOperationHandle<TObject> LoadAsset<TObject>(IResourceLocation location)
        {
            return TrackHandle(ResourceManager.ProvideResource<TObject>(location));
        }

        AsyncOperationHandle<TObject> LoadAssetWithChain<TObject>(object key)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadAsset<TObject>(key));
        }

        public AsyncOperationHandle<TObject> LoadAsset<TObject>(object key)
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

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocations(IList<object> keys, Addressables.MergeMode mode)
        {
            var op = new LoadResourceLocationKeysOp();
            op.Init(this, keys, mode);
            return TrackHandle(ResourceManager.StartOperation(op, InitializationOperation));
        }

        public AsyncOperationHandle<IList<IResourceLocation>> LoadResourceLocations(object key)
        {
            var op = new LoadResourceLocationKeyOp();
            op.Init(this, key);
            return TrackHandle(ResourceManager.StartOperation(op, InitializationOperation));
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssets<TObject>(IList<IResourceLocation> locations, Action<TObject> callback)
        {
            return TrackHandle(ResourceManager.ProvideResources(locations, callback));
        }

        AsyncOperationHandle<IList<TObject>> LoadAssetsWithChain<TObject>(IList<object> keys, Action<TObject> callback, Addressables.MergeMode mode)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadAssets(keys, callback, mode));
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssets<TObject>(IList<object> keys, Action<TObject> callback, Addressables.MergeMode mode)
        {
            if (!InitializationOperation.IsDone)
                return LoadAssetsWithChain(keys, callback, mode);
            
            IList<IResourceLocation> locations;
            if (!GetResourceLocations(keys, mode, out locations))
                return ResourceManager.CreateCompletedOperation<IList<TObject>>(null, new InvalidKeyException(keys).Message);

            return LoadAssets(locations, callback);
        }

        AsyncOperationHandle<IList<TObject>> LoadAssetsWithChain<TObject>(object key, Action<TObject> callback)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op2 => LoadAssets(key, callback));
        }

        public AsyncOperationHandle<IList<TObject>> LoadAssets<TObject>(object key, Action<TObject> callback)
        {
            if (!InitializationOperation.IsDone)
                return LoadAssetsWithChain(key, callback);

            IList<IResourceLocation> locations;
            if (!GetResourceLocations(key, out locations))
                ResourceManager.CreateCompletedOperation<IList<AsyncOperationHandle<TObject>>>(null, new InvalidKeyException(key).Message);

            return LoadAssets(locations, callback);
        }

         void OnHandleDestroyed(AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                m_resultToHandle.Remove(handle.Result);
            }
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
            return ResourceManager.CreateChainOperation(InitializationOperation, op => GetDownloadSize(key));
        }
        public AsyncOperationHandle<long> GetDownloadSize(object key)
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

        AsyncOperationHandle<IList<object>> DownloadDependenciesWithChain(object key)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => DownloadDependencies(key));
        }

        public AsyncOperationHandle<IList<object>> DownloadDependencies(object key)
        {
            if (!InitializationOperation.IsDone)
                return DownloadDependenciesWithChain(key);

            IList<IResourceLocation> locations;
            if (!GetResourceLocations(key, out locations))
                return ResourceManager.CreateCompletedOperation<IList<object>>(null, new InvalidKeyException(key).Message);


            var locHash = new HashSet<IResourceLocation>();
            foreach (var loc in locations)
            {
                if (loc.HasDependencies)
                {
                    foreach (var dep in loc.Dependencies)
                        locHash.Add(dep);
                }
            }
            return LoadAssets<object>(new List<IResourceLocation>(locHash), null);
        }

        public  AsyncOperationHandle<GameObject> Instantiate(IResourceLocation location, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return Instantiate(location, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
        }
        public  AsyncOperationHandle<GameObject> Instantiate(IResourceLocation location, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return Instantiate(location, new InstantiationParameters(position, rotation, parent), trackHandle);
        }
        public  AsyncOperationHandle<GameObject> Instantiate(object key, Transform parent = null, bool instantiateInWorldSpace = false, bool trackHandle = true)
        {
            return Instantiate(key, new InstantiationParameters(parent, instantiateInWorldSpace), trackHandle);
        }
      
        public  AsyncOperationHandle<GameObject> Instantiate(object key, Vector3 position, Quaternion rotation, Transform parent = null, bool trackHandle = true)
        {
            return Instantiate(key, new InstantiationParameters(position, rotation, parent), trackHandle);
        }

        AsyncOperationHandle<GameObject> InstantiateWithChain(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => Instantiate(key, instantiateParameters, trackHandle));
        }

        public AsyncOperationHandle<GameObject> Instantiate(object key, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            if (!InitializationOperation.IsDone)
                return InstantiateWithChain(key, instantiateParameters, trackHandle);

            key = EvaluateKey(key);
            IList<IResourceLocation> locs;
            for (int i = 0; i < m_ResourceLocators.Count; i++)
            {
                if (m_ResourceLocators[i].Locate(key, out locs))
                    return Instantiate(locs[0], instantiateParameters, trackHandle);
            }
            return ResourceManager.CreateCompletedOperation<GameObject>(null, new InvalidKeyException(key).Message);
        }

        public AsyncOperationHandle<GameObject> Instantiate(IResourceLocation location, InstantiationParameters instantiateParameters, bool trackHandle = true)
        {
            var opHandle = ResourceManager.ProvideInstance(InstanceProvider, location, instantiateParameters);
            if (!trackHandle)
                return opHandle;
            opHandle.Completed += m_OnInstanceHandleCompleteAction;
            return opHandle;
        }
        
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null)
            {
                LogWarning("Addressables.ReleaseInstance() - trying to release null object.");
                return;
            }

            AsyncOperationHandle<GameObject> handle;
            if (s_InstanceToHandle.TryGetValue(instance, out handle))
                Release(handle);
            else
            {
                LogError("Addressables.ReleaseInstance was called on a GameObject that Addressables was not previously aware of.  We will Destroy the object for now, but this functionality is Deprecated and will be removed.  If this was created through Addressables, check that you set 'trackHandle=true' when instantiating.");
                Object.Destroy(instance);
            }
        }

        void OnInstanceHandleDestroyed(AsyncOperationHandle handle)
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var instance = handle.Result as GameObject;
                if (s_CurrentFrame != Time.frameCount)
                {
                    s_CurrentFrame = Time.frameCount;
                    s_InstancesReleasedInCurrentFrame.Clear();
                }

                //silently ignore multiple releases that occur in the same frame
                if (s_InstancesReleasedInCurrentFrame.Contains(instance))
                    return;

                s_InstancesReleasedInCurrentFrame.Add(instance);

                if (!s_SceneToInstances[instance.scene].Remove(instance))
                    LogWarningFormat("Instance {0} was not found in scene {1}.", instance.GetInstanceID(), instance.scene);
                if (!s_InstanceToScene.Remove(instance))
                    LogWarningFormat("Instance {0} was not found instance->scene map.", instance.GetInstanceID());
                s_InstanceToHandle.Remove(instance);
            }
        }

        void OnInstanceHandleCompleted(AsyncOperationHandle<GameObject> op)
        {
            if (op.Status == AsyncOperationStatus.Succeeded)
            {
                var gameObject = op.Result;
                if (!s_InstanceToHandle.ContainsKey(gameObject))
                {
                    op.Destroyed += m_OnInstanceHandleDestroyedAction;
                    s_InstanceToHandle.Add(gameObject, op);
                    s_InstanceToScene.Add(gameObject, gameObject.scene);
                    HashSet<GameObject> instances;
                    if (!s_SceneToInstances.TryGetValue(gameObject.scene, out instances))
                        s_SceneToInstances.Add(gameObject.scene, instances = new HashSet<GameObject>());
                    instances.Add(gameObject);
                }
            }
        }

        AsyncOperationHandle<SceneInstance> LoadSceneWithChain(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            return ResourceManager.CreateChainOperation(InitializationOperation, op => LoadScene(key, loadMode, activateOnLoad, priority));
        }

        public AsyncOperationHandle<SceneInstance> LoadScene(object key, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            if (!InitializationOperation.IsDone)
                return LoadSceneWithChain(key, loadMode, activateOnLoad, priority);

            IList<IResourceLocation> locations;
            if (!GetResourceLocations(key, out locations))
                return ResourceManager.CreateCompletedOperation<SceneInstance>(default(SceneInstance), new InvalidKeyException(key).Message);

            return LoadScene(locations[0], loadMode, activateOnLoad, priority);
        }

        public  AsyncOperationHandle<SceneInstance> LoadScene(IResourceLocation location, LoadSceneMode loadMode = LoadSceneMode.Single, bool activateOnLoad = true, int priority = 100)
        {
            if (loadMode == LoadSceneMode.Single)
                ReleaseLoadedAddressablesScenes();

            return TrackHandle(ResourceManager.ProvideScene(SceneProvider, location, loadMode, activateOnLoad, priority));
        }

        public AsyncOperationHandle<SceneInstance> UnloadScene(SceneInstance scene, bool autoReleaseHandle = true)
        {
            AsyncOperationHandle handle;
            if (!m_resultToHandle.TryGetValue(scene, out handle))
            {
                var msg = string.Format("Addressables.UnloadScene() - Cannot find handle for scene {0}", scene);
                LogWarning(msg);
                return ResourceManager.CreateCompletedOperation<SceneInstance>(scene, msg);
            }

            return UnloadScene(handle, autoReleaseHandle);
        }

        public AsyncOperationHandle<SceneInstance> UnloadScene(AsyncOperationHandle handle, bool autoReleaseHandle = true)
        {
            return UnloadScene(handle.Convert<SceneInstance>(), autoReleaseHandle);
        }
        
        public AsyncOperationHandle<SceneInstance> UnloadScene(AsyncOperationHandle<SceneInstance> handle, bool autoReleaseHandle = true)
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

        private void ReleaseLoadedAddressablesScenes()
        {
            List<AsyncOperationHandle> releaseHandles = new List<AsyncOperationHandle>();
            foreach (AsyncOperationHandle handle in m_resultToHandle.Values)
            {
                if (handle.Result is SceneInstance)
                    releaseHandles.Add(handle);
            }

            foreach (AsyncOperationHandle handle in releaseHandles)
                Release(handle);

            ValidateSceneInstances();
        }








        Dictionary<GameObject, AsyncOperationHandle<GameObject>> s_InstanceToHandle = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();
        Dictionary<GameObject, Scene> s_InstanceToScene = new Dictionary<GameObject, Scene>();
        Dictionary<Scene, HashSet<GameObject>> s_SceneToInstances = new Dictionary<Scene, HashSet<GameObject>>();
        int s_CurrentFrame;
        HashSet<Object> s_InstancesReleasedInCurrentFrame = new HashSet<Object>();

        /// <summary>
        /// Notify the ResourceManager that a tracked instance has changed scenes so that it can be released properly when the scene is unloaded.
        /// </summary>
        /// <param name="gameObject">The gameobject that is being moved to a new scene.</param>
        /// <param name="previousScene">Previous scene for gameobject.</param>
        /// <param name="currentScene">Current scene for gameobject.</param>
        public void RecordInstanceSceneChange(GameObject gameObject, Scene previousScene, Scene currentScene)
        {
            if (gameObject == null)
                return;
            HashSet<GameObject> instanceIds;
            if (!s_SceneToInstances.TryGetValue(previousScene, out instanceIds))
                LogFormat("Unable to find instance table for instance {0}.", gameObject.GetInstanceID());
            else
                instanceIds.Remove(gameObject);
            if (!s_SceneToInstances.TryGetValue(currentScene, out instanceIds))
                s_SceneToInstances.Add(currentScene, instanceIds = new HashSet<GameObject>());
            instanceIds.Add(gameObject);

            s_InstanceToScene[gameObject] = currentScene;
        }

        void OnSceneUnloaded(Scene scene)
        {
            if (!Application.isPlaying)
                return;

            if (s_CurrentFrame != Time.frameCount)
            {
                s_CurrentFrame = Time.frameCount;
                s_InstancesReleasedInCurrentFrame.Clear();
            }

            HashSet<GameObject> instances;
            if (s_SceneToInstances.TryGetValue(scene, out instances))
            {
                foreach (var go in instances)
                {
                    if (IsDontDestroyOnLoad(go))
                        continue;

                    if (s_InstancesReleasedInCurrentFrame.Contains(go))
                        continue;

                    AsyncOperationHandle<GameObject> handle;
                    if (s_InstanceToHandle.TryGetValue(go, out handle))
                    {
                        Release(handle);
                        s_InstanceToHandle.Remove(go);
                    }
                }

                s_SceneToInstances.Remove(scene);
            }
        }

        private static string m_DontDestroyOnLoadSceneName = "DontDestroyOnLoad";
        bool IsDontDestroyOnLoad(GameObject go)
        {
            if (go != null && go.scene.name == m_DontDestroyOnLoadSceneName)
            {
                Scene temp;
                if (!s_InstanceToScene.TryGetValue(go, out temp))
                    s_InstanceToScene.Add(go, go.scene);
                else
                    s_InstanceToScene[go] = go.scene;

                HashSet<GameObject> newInstances;
                if (!s_SceneToInstances.TryGetValue(go.scene, out newInstances))
                    s_SceneToInstances.Add(go.scene, newInstances = new HashSet<GameObject>());

                if (!newInstances.Contains(go))
                    newInstances.Add(go);

                return true;
            }

            return false;
        }

        void ValidateSceneInstances()
        {
            var objectsThatNeedToBeFixed = new List<KeyValuePair<Scene, GameObject>>();
            foreach (var kvp in s_SceneToInstances)
            {
                foreach (var go in kvp.Value)
                {
                    if (go == null)
                    {
                        LogWarningFormat("GameObject instance has been destroyed, use Addressables.ResourceManager.ReleaseInstance to ensure proper reference counts.");
                    }
                    else
                    {
                        if (go.scene != kvp.Key)
                        {
                            LogWarningFormat("GameObject instance {0} has been moved to from scene {1} to scene {2}.  When moving tracked instances, use Addressables.ResourceManager.RecordInstanceSceneChange to ensure that reference counts are accurate.", go, kvp.Key, go.scene.GetHashCode());
                            objectsThatNeedToBeFixed.Add(new KeyValuePair<Scene, GameObject>(kvp.Key, go));
                        }
                    }
                }
            }

            foreach (var go in objectsThatNeedToBeFixed)
                RecordInstanceSceneChange(go.Value, go.Key, go.Value.scene);
        }


    }
}

