using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    internal interface IGenericProviderOperation
    {
        void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp);
        int ProvideHandleVersion { get; }
        IResourceLocation Location { get; }
        int DependencyCount { get; }
        void GetDependencies(IList<object> dstList);
        TDepObject GetDependency<TDepObject>(int index);
        void SetProgressCallback(Func<float> callback);
        void ProviderCompleted<T>(T result, bool status, Exception e);
        Type RequestedType { get; }
    }

    internal class ProviderOperation<TObject> : AsyncOperationBase<TObject>, IGenericProviderOperation, ICachable
    {
        private Action<int, object, bool, Exception> m_CompletionCallback;
        private Action<int, IList<object>> m_GetDepCallback;
        private Func<float> m_GetProgressCallback;
        private IResourceProvider m_Provider;
        private AsyncOperationHandle<IList<AsyncOperationHandle>> m_DepOp;
        private IResourceLocation m_Location;
        private int m_ProvideHandleVersion;
        private TObject m_Result;
        private bool m_NeedsRelease;
        int ICachable.Hash { get; set; }
        private ResourceManager m_ResourceManager;

        public int ProvideHandleVersion { get { return m_ProvideHandleVersion; } }
        public IResourceLocation Location { get { return m_Location; } }

        public ProviderOperation()
        {
        }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            if(m_DepOp.IsValid())
                deps.Add(m_DepOp);
        }

        protected override string DebugName
        {
            get
            {
                return string.Format("Resource<{0}>({1})", typeof(TObject).Name, m_Location == null ? "Invalid" : ShortenPath(m_Location.InternalId, true));
            }
        }

        internal const string kInvalidHandleMsg = "The ProvideHandle is invalid. After the handle has been completed, it can no longer be used";

        public void GetDependencies(IList<object> dstList)
        {
            dstList.Clear();

            if (!m_DepOp.IsValid())
                return;

            if (m_DepOp.Result == null)
                return;
            
            for (int i = 0; i < m_DepOp.Result.Count; i++)
                dstList.Add(m_DepOp.Result[i].Result);
        }

        public Type RequestedType { get { return typeof(TObject); } }

        public int DependencyCount {
            get {
                return (!m_DepOp.IsValid() || m_DepOp.Result == null) ? 0 : m_DepOp.Result.Count;
            }
        }

        public TDepObject GetDependency<TDepObject>(int index)
        {
            if (!m_DepOp.IsValid() || m_DepOp.Result == null)
                throw new Exception("Cannot get dependency because no dependencies were available");

            return (TDepObject)(m_DepOp.Result[index].Result);
        }

        public void SetProgressCallback(Func<float> callback)
        {
            m_GetProgressCallback = callback;
        }

        public void ProviderCompleted<T>(T result, bool status, Exception e)
        {
            m_ProvideHandleVersion++;
            m_GetProgressCallback = null;

            m_NeedsRelease = status;

            ProviderOperation<T> top = this as ProviderOperation<T>;
            if (top != null)
            {
                top.m_Result = result;
            }
            else if (result == null && !typeof(TObject).IsValueType)
            {
                m_Result = (TObject)(object)null;
            }
            else if(result != null && typeof(TObject).IsAssignableFrom(result.GetType()))
            {
                m_Result = (TObject)(object)result;
            }
            else
            {
                string errorMsg = string.Format("Provider of type {0} with id {1} has provided a result of type {2} which cannot be converted to requested type {3}. The operation will be marked as failed.", m_Provider.GetType().ToString(), m_Provider.ProviderId, typeof(T), typeof(TObject));
                Complete(m_Result, false, errorMsg);
                throw new Exception(errorMsg);
            }

            Complete(m_Result, status, e != null ? e.Message : string.Empty);
        }
        protected override float Progress
        {
            get
            {
                if (m_GetProgressCallback == null)
                    return 0.5f;
                try
                {
                    return m_GetProgressCallback();
                }
                catch
                {
                    return 0.0f;
                }
            }
        }

        protected override void Execute()
        {
            Debug.Assert(m_DepOp.IsDone);

            if (m_DepOp.IsValid() && m_DepOp.Status == AsyncOperationStatus.Failed && (m_Provider.BehaviourFlags & ProviderBehaviourFlags.CanProvideWithFailedDependencies) == 0)
            {
                ProviderCompleted(default(TObject), false, new Exception("Dependency Exception", m_DepOp.OperationException));
            }
            else
            {
                try
                {
                    m_Provider.Provide(new ProvideHandle(m_ResourceManager, this));
                }
                catch (Exception e)
                {
                    ProviderCompleted(default(TObject), false, e);
                }
            }
        }

        public void Init(ResourceManager rm, IResourceProvider provider, IResourceLocation location, AsyncOperationHandle<IList<AsyncOperationHandle>> depOp)
        {
            m_ResourceManager = rm;
            m_DepOp = depOp;
            if (m_DepOp.IsValid())
                m_DepOp.Acquire();
            m_Provider = provider;
            m_Location = location;
        }

        protected override void Destroy()
        {
            if (m_NeedsRelease)
                m_Provider.Release(m_Location, m_Result);
            if (m_DepOp.IsValid())
                m_DepOp.Release();
        }
    }
}