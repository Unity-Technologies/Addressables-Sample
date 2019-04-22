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
            protected override string DebugName { get { return string.Format("Scene({0})", ShortenPath(m_Location.InternalId, false)); } }

            protected override void Execute()
            {
                m_Inst = SceneProvider.InternalLoadScene(m_Location, m_LoadMode, m_ActivateOnLoad, m_Priority);
                ((IUpdateReceiver)this).Update(0.0f);
            }

            protected override void Destroy()
            {
                SceneManager.UnloadSceneAsync(Result.Scene);
                base.Destroy();
            }

            void IUpdateReceiver.Update(float unscaledDeltaTime)
            {
                if (m_Inst.m_Operation.isDone || !m_ActivateOnLoad && m_Inst.m_Operation.progress == .9f)
                    Complete(m_Inst, true, null);
            }
        }

        class UnloadSceneOp : AsyncOperationBase<SceneInstance>
        {
            SceneInstance m_SceneInstance;
            public void Init(SceneInstance sceneInstance)
            {
                m_SceneInstance = sceneInstance;
            }

            protected override void Execute()
            {
                SceneManager.UnloadSceneAsync(m_SceneInstance.Scene).completed += UnloadSceneOp_completed;
            }

            private void UnloadSceneOp_completed(AsyncOperation op)
            {
                Complete(m_SceneInstance, true, string.Empty);
            }
        }

        /// <inheritdoc/>
        public AsyncOperationHandle<SceneInstance> ProvideScene(ResourceManager resourceManager, IResourceLocation location, LoadSceneMode loadMode, bool activateOnLoad, int priority)
        {
            AsyncOperationHandle<IList<AsyncOperationHandle>> depOp = default(AsyncOperationHandle<IList<AsyncOperationHandle>>);
            if (location.HasDependencies)
                depOp = resourceManager.ProvideResourceGroupCached(location.Dependencies, location.DependencyHashCode, typeof(AssetBundleResource), null);

            SceneOp op = new SceneOp(resourceManager);
            op.Init(location, loadMode, activateOnLoad, priority, depOp);
            return resourceManager.StartOperation<SceneInstance>(op, depOp);
        }

        internal static SceneInstance InternalLoadScene(IResourceLocation location, LoadSceneMode loadMode, bool activateOnLoad, int priority)
        {
            var op = InternalLoad(location.InternalId, location.HasDependencies, loadMode);
            op.allowSceneActivation = activateOnLoad;
            op.priority = priority;
            return new SceneInstance() { m_Operation = op, Scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1) };
        }


        /// <inheritdoc/>
        public AsyncOperationHandle<SceneInstance> ReleaseScene(ResourceManager resourceManager, AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            return InternalReleaseScene(resourceManager, sceneLoadHandle);
        }

        static AsyncOperationHandle<SceneInstance> InternalReleaseScene(ResourceManager resourceManager, AsyncOperationHandle<SceneInstance> sceneLoadHandle)
        {
            var unloadOp = new UnloadSceneOp();
            unloadOp.Init(sceneLoadHandle.Result);
            return resourceManager.StartOperation<SceneInstance>(unloadOp, sceneLoadHandle);
        }


        static AsyncOperation InternalLoad(string path, bool loadingFromBundle, LoadSceneMode mode)
        {
#if !UNITY_EDITOR
            return InternalPlayerLoad(path, mode);
#else
            return loadingFromBundle ? InternalPlayerLoad(path, mode) : UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters() { loadSceneMode = mode });
#endif
        }

        static AsyncOperation InternalPlayerLoad(string path, LoadSceneMode mode)
        {
            return SceneManager.LoadSceneAsync(path, new LoadSceneParameters() { loadSceneMode = mode });
        }

    }
}
