using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement
{
    class ChainOperation<TObject, TObjectDependency> : AsyncOperationBase<TObject>
    {
        AsyncOperationHandle<TObjectDependency> m_DepOp;
        AsyncOperationHandle<TObject> m_WrappedOp;
        Func<AsyncOperationHandle<TObjectDependency>, AsyncOperationHandle<TObject>> m_Callback;
        Action<AsyncOperationHandle<TObject>> m_CachedOnWrappedCompleted;
        public ChainOperation()
        {
            m_CachedOnWrappedCompleted = OnWrappedCompleted;
        }
        protected override string DebugName { get { return string.Format("{2} - Chain<{0},{1}>", typeof(TObject).Name, typeof(TObjectDependency).Name, m_DepOp.DebugName); } }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            deps.Add(m_DepOp);
        }

        public void Init(AsyncOperationHandle<TObjectDependency> dependentOp, Func<AsyncOperationHandle<TObjectDependency>, AsyncOperationHandle<TObject>> callback)
        {
            m_DepOp = dependentOp;
            m_DepOp.Acquire();
            m_Callback = callback;
        }

        protected override void Execute()
        {
            m_WrappedOp = m_Callback(m_DepOp);
            m_WrappedOp.Completed += m_CachedOnWrappedCompleted;
            m_Callback = null;
        }

        private void OnWrappedCompleted(AsyncOperationHandle<TObject> x)
        {
            string errorMsg = string.Empty;
            if (x.Status == AsyncOperationStatus.Failed)
                errorMsg = string.Format("ChainOperation of Type: {0} failed because dependent operation failed\n{1}", typeof(TObject), x.OperationException != null ? x.OperationException.Message : string.Empty);
            Complete(m_WrappedOp.Result, x.Status == AsyncOperationStatus.Succeeded, errorMsg);
        }

        protected override void Destroy()
        {
            m_WrappedOp.Release();
            m_DepOp.Release();
        }
    }

    class ChainOperationTypelessDepedency<TObject> : AsyncOperationBase<TObject>
    {
        AsyncOperationHandle m_DepOp;
        AsyncOperationHandle<TObject> m_WrappedOp;
        Func<AsyncOperationHandle, AsyncOperationHandle<TObject>> m_Callback;
        Action<AsyncOperationHandle<TObject>> m_CachedOnWrappedCompleted;
        public ChainOperationTypelessDepedency()
        {
            m_CachedOnWrappedCompleted = OnWrappedCompleted;
        }
        protected override string DebugName { get { return string.Format("{1} - Chain<{0}>", typeof(TObject).Name, m_DepOp.DebugName); } }

        protected override void GetDependencies(List<AsyncOperationHandle> deps)
        {
            deps.Add(m_DepOp);
        }

        public void Init(AsyncOperationHandle dependentOp, Func<AsyncOperationHandle, AsyncOperationHandle<TObject>> callback)
        {
            m_DepOp = dependentOp;
            m_DepOp.Acquire();
            m_Callback = callback;
        }

        protected override void Execute()
        {
            m_WrappedOp = m_Callback(m_DepOp);
            m_WrappedOp.Completed += m_CachedOnWrappedCompleted;
            m_Callback = null;
        }

        private void OnWrappedCompleted(AsyncOperationHandle<TObject> x)
        {
            string errorMsg = string.Empty;
            if (x.Status == AsyncOperationStatus.Failed)
                errorMsg = string.Format("ChainOperation of Type: {0} failed because dependent operation failed\n{1}", typeof(TObject), x.OperationException != null ? x.OperationException.Message : string.Empty);
            Complete(m_WrappedOp.Result, x.Status == AsyncOperationStatus.Succeeded, errorMsg);
        }

        protected override void Destroy()
        {
            m_WrappedOp.Release();
            m_DepOp.Release();
        }
    }
}