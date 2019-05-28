using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Implementation if ISceneProvider
    /// </summary>
    public class SceneProvider : ISceneProvider
    {
        class SceneOp : AsyncOperationBase<SceneInstance>, IUpdateReceiver
        {
            bool m_ActivateOnLoad;
            SceneInstance m_Inst;
            IResourceLocation m_Location;
            LoadSceneMode m_LoadMode;
            int m_Priority;
            private AsyncOperationHandle<IList<AsyncOperationHandle>> m_DepOp;
            ResourceManager m_ResourceManager;
            public SceneOp(ResourceManager rm)
            {
                m_ResourceManager = rm;
            }
            public void Init(IResourceLocation location, LoadSceneMode loadMode, bool activateOnLoad, int priority, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp)
            {
                m_DepOp = depOp;
                if (m_DepOp.IsValid())
                    m_DepOp.Acquire();

                m_Location = location;
                m_LoadMode = loadMode;
                m_ActivateOnLoad = activateOnLoad;
                m_Priority = priority;
            }

            protected override void GetDependencies(List<AsyncOperationHandle> deps)
            {
                if (m_DepOp.IsValid())
                    deps.Add(m_DepOp);
            }
            protected override string DebugName { get { return string.Format("Scene({0})", m_Location == null ? "Invalid" : ShortenPath(m_Location.InternalId, false)); } }

            protected override void Execute()
            {
                var loadingFromBundle = false;
                if (m_DepOp.IsValid())
                {
                    foreach (var d in m_DepOp.Result)
                    {
                        var abResource = d.Result as IAssetBundleResource;
                        if (abResource != null && abResource.GetAssetBundle() != null)
                            loadingFromBundle = true;
                    }
                }
                m_Inst = InternalLoadScene(m_Location, loadingFromBundle, m_LoadMode, m_ActivateOnLoad, m_Priority);
                ((IUpdateReceiver)this).Update(0.0f);
            }

            internal SceneInstance InternalLoadScene(IResourceLocation location, bool loadingFromBundle, LoadSceneMode loadMode, bool activateOnLoad, int priority)
            {
                var op = InternalLoad(location.InternalId, loadingFromBundle, loadMode);
                op.allowSceneActivation = activateOnLoad;
                op.priority = priority;
                return new SceneInstance() { m_Operation = op, Scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1) };
            }

            AsyncOperation InternalLoad(string path, bool loadingFromBundle, LoadSceneMode mode)
            {
#if !UNITY_EDITOR
                return SceneManager.LoadSceneAsync(path, new LoadSceneParameters() { loadSceneMode = mode });
#else
                if (loadingFromBundle)
                    return SceneManager.LoadSceneAsync(path, new LoadSceneParameters() { loadSceneMode = mode });
                else
                    return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters() { loadSceneMode = mode });
#endif
            }

            protected override void Destroy()
            {
                SceneManager.UnloadSceneAsync(Result.Scene);
                if (m_DepOp.IsValid())
                    m_DepOp.Release();
                base.Destroy();
            }

            void IUpdateReceiver.Update(float unscaledDeltaTime)
            {
                if (m_Inst.m_Operation.isDone || (!m_ActivateOnLoad && m_Inst.m_Operation.progress == .9f))
                    Complete(m_Inst, true, null);
            }
        }
        
        class UnloadSceneOp : AsyncOperationBase<SceneInstance>
        {
            SceneInstance m_Instance;
            AsyncOperationHandle<SceneInstance> m_sceneLoadHandle;
            public void Init(AsyncOperationHandle<SceneInstance> sceneLoadHandle)
            {
                if (sceneLoadHandle.ReferenceCount > 0)
                {
                    m_sceneLoadHandle = sceneLoadHandle;
                    m_Instance = m_sceneLoadHandle.Result;
                    m_sceneLoadHandle.Destroyed += h => Complete(m_Instance, true, "");
                }
            }
            protected override void Execute()
            {
                if (m_sceneLoadHandle.IsValid())
                    m_sceneLoadHandle.Release();
                else
                    Complete(m_Instance, true, "");
            }
        }

        /// <inheritdoc/>
        public AsyncOperationHandle<SceneInstance> ProvideScene(ResourceManager resourceManager, IResourceLocation location, LoadSceneMode loadMode, bool activateOnLoad, int priority)
        {
            AsyncOperationHandle<IList<AsyncOperationHandle>> depOp = default(AsyncOperationHandle<IList<AsyncOperationHandle>>);
            if (location.HasDependencies)
                depOp = resourceManager.ProvideResourceGroupCached(location.Dependencies, location.DependencyHashCode, typeof(IAssetBundleResource), null);

            SceneOp op = new SceneOp(resourceManager);
            op.Init(location, loadMode, activateOnLoad, priority, depOp);

            var handle = resourceManager.StartOperation<SceneInstance>(op, depOp);

            if (depOp.IsValid())
                depOp.Release();

            return handle;
        }

        /// <inheritdoc/>
        public AsyncOperationHandle<SceneInstance> ReleaseScene(ResourceManager resourceManager, AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            var unloadOp = new UnloadSceneOp();
            unloadOp.Init(sceneLoadHandle);
            return resourceManager.StartOperation(unloadOp, sceneLoadHandle);
        }
    }
}
