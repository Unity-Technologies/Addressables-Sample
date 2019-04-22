using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.Util;

// ReSharper disable DelegateSubtraction

namespace UnityEngine.ResourceManagement.AsyncOperations
{
    internal interface ICachable
    {
        int Hash { get; set; }
    }

    internal interface IAsyncOperation
    {
        object GetResultAsObject();
        Type ResultType { get; }
        int Version { get; }
        string DebugName { get; }
        void DecrementReferenceCount();
        void IncrementReferenceCount();
        int ReferenceCount { get; }
        float PercentComplete { get; }
        AsyncOperationStatus Status { get; }

        Exception OperationException { get; }
        bool IsDone { get; }
        Action<IAsyncOperation> OnDestroy { set; }
        void GetDependencies(List<AsyncOperationHandle> deps);

        event Action<AsyncOperationHandle> CompletedTypeless;
        event Action<AsyncOperationHandle> Destroyed;

        void InvokeCompletionEvent();
        System.Threading.Tasks.Task<object> Task { get; }
        void Start(ResourceManager rm, AsyncOperationHandle dependency, DelegateList<float> updateCallbacks);

        AsyncOperationHandle Handle { get; }

    }

    /// <summary>
    /// base class for implemented AsyncOperations, implements the needed interfaces and consolidates redundant code
    /// </summary>
    public abstract class AsyncOperationBase<TObject> : IAsyncOperation
    {
        /// <summary>
        /// This will be called by the resource manager after all dependent operation complete. This method should not be called manually.
        /// A custom operation should override this method and begin work when it is called.
        /// </summary>
        protected abstract void Execute();

        /// <summary>
        /// This will be called by the resource manager when the reference count of the operation reaches zero. This method should not be called manually.
        /// A custom operation should override this method and release any held resources
        /// </summary>
        protected virtual void Destroy() { }

        /// <summary>
        /// A custom operation should override this method to return the progress of the operation.
        /// </summary>
        /// <returns>Progress of the operation. Value should be between 0.0f and 1.0f</returns>
        protected virtual float Progress { get { return 0; } }

        /// <summary>
        /// A custom operation should override this method to provide a debug friendly name for the operation.
        /// </summary>
        protected virtual string DebugName { get { return this.ToString(); } }

        /// <summary>
        /// A custom operation should override this method to provide a list of AsyncOperationHandles that it depends on.
        /// </summary>
        /// <param name="dependencies">The list that should be populated with dependent AsyncOperationHandles.</param>
        protected virtual void GetDependencies(List<AsyncOperationHandle> dependencies) { }

        internal TObject Result { get { return m_Result; } }
        int m_referenceCount = 1;
        AsyncOperationStatus m_Status;
        Exception m_Error;
        ResourceManager m_RM;
        TObject m_Result;
        private int m_Version;
        internal int Version { get { return m_Version; } }

        DelegateList<AsyncOperationHandle> m_CompletedAction;
        DelegateList<AsyncOperationHandle> m_DestroyedAction;
        DelegateList<AsyncOperationHandle<TObject>> m_CompletedActionT;
        Action<IAsyncOperation> m_OnDestroyAction;
        internal Action<IAsyncOperation> OnDestroy { set { m_OnDestroyAction = value; } }
        internal int ReferenceCount { get { return m_referenceCount; } }
        Action<AsyncOperationHandle> m_dependencyCompleteAction;

        protected AsyncOperationBase()
        {
            m_UpdateCallback = UpdateCallback;
            m_dependencyCompleteAction = o => InvokeExecute();
        }

        internal static string ShortenPath(string p, bool keepExtension)
        {
            var slashIndex = p.LastIndexOf('/');
            if (slashIndex > 0)
                p = p.Substring(slashIndex + 1);
            if (!keepExtension)
            {
                slashIndex = p.LastIndexOf('.');
                if (slashIndex > 0)
                    p = p.Substring(0, slashIndex);
            }
            return p;
        }

        internal void IncrementReferenceCount()
        {
            if (m_referenceCount == 0)
                throw new Exception(string.Format("Cannot increment reference count on operation {0} because it has already been destroyed", this));

            m_referenceCount++;
            m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount, m_referenceCount);
        }

        internal void DecrementReferenceCount()
        {
            if (m_referenceCount <= 0)
                throw new Exception(string.Format("Cannot decrement reference count for operation {0} because it is already 0", this));
            
            m_referenceCount--;
            m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationReferenceCount, m_referenceCount);

            if (m_referenceCount == 0)
            {
                m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationDestroy);

                Destroy();
                if (m_DestroyedAction != null)
                {
                    m_DestroyedAction.Invoke(new AsyncOperationHandle<TObject>(this));
                    m_DestroyedAction.Clear();
                }

                if (m_OnDestroyAction != null)
                {
                    m_OnDestroyAction(this);
                    m_OnDestroyAction = null;
                }

                m_Result = default(TObject);
                m_referenceCount = 1;
                m_Status = AsyncOperationStatus.None;
                m_Error = null;
                m_Version++;
                m_RM = null;
            }
        }

        System.Threading.EventWaitHandle m_waitHandle;
        internal System.Threading.WaitHandle WaitHandle
        {
            get
            {
                if (m_waitHandle == null)
                    m_waitHandle = new System.Threading.EventWaitHandle(false, System.Threading.EventResetMode.ManualReset);
                m_waitHandle.Reset();
                return m_waitHandle;
            }
        }

        internal System.Threading.Tasks.Task<TObject> Task
        {
            get
            {
                return System.Threading.Tasks.Task.Factory.StartNew(o =>
                {
                    var asyncOperation = o as AsyncOperationBase<TObject>;
                    asyncOperation.WaitHandle.WaitOne();
                    return asyncOperation.Result;
                }, this);
            }
        }

        System.Threading.Tasks.Task<object> IAsyncOperation.Task
        {
            get
            {
                return Task as System.Threading.Tasks.Task<object>;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var instId = "";
            var or = m_Result as Object;
            if (or != null)
                instId = "(" + or.GetInstanceID() + ")";
            return string.Format("{0}, result='{1}', status='{2}'", base.ToString(), (or + instId), m_Status);
        }

        bool m_InDeferredCallbackQueue;
        void RegisterForDeferredCallbackEvent()
        {
            if (IsDone && !m_InDeferredCallbackQueue)
            {
                m_InDeferredCallbackQueue = true;
                m_RM.RegisterForDeferredCallback(this);
            }
        }

        internal event Action<AsyncOperationHandle<TObject>> Completed
        {
            add
            {
                if (m_CompletedActionT == null)
                    m_CompletedActionT = DelegateList<AsyncOperationHandle<TObject>>.CreateWithGlobalCache();
                m_CompletedActionT.Add(value);
                RegisterForDeferredCallbackEvent();
            }
            remove
            {
                m_CompletedActionT.Remove(value);
            }
        }

        internal event Action<AsyncOperationHandle> Destroyed
        {
            add
            {
                if (m_DestroyedAction == null)
                    m_DestroyedAction = DelegateList<AsyncOperationHandle>.CreateWithGlobalCache();
                m_DestroyedAction.Add(value);
            }
            remove
            {
                m_DestroyedAction.Remove(value);
            }
        }

        internal event Action<AsyncOperationHandle> CompletedTypeless
        {
            add
            {
                if (m_CompletedAction == null)
                    m_CompletedAction = DelegateList<AsyncOperationHandle>.CreateWithGlobalCache();
                m_CompletedAction.Add(value);
                RegisterForDeferredCallbackEvent();
            }
            remove
            {
                m_CompletedAction.Remove(value);
            }
        }

        /// <inheritdoc />
        internal AsyncOperationStatus Status { get { return m_Status; } }
        /// <inheritdoc />
        internal Exception OperationException
        {
            get { return m_Error; }
            private set
            {
                m_Error = value;
                if (m_Error != null && ResourceManager.ExceptionHandler != null)
                    ResourceManager.ExceptionHandler(new AsyncOperationHandle(this), value);
            }
        }
        internal bool MoveNext() { return !IsDone; }
        internal void Reset() { }
        internal object Current { get { return null; } } // should throw exception?
        internal bool IsDone { get { return Status == AsyncOperationStatus.Failed || Status == AsyncOperationStatus.Succeeded; } }
        internal float PercentComplete
        {
            get
            {
                if (m_Status == AsyncOperationStatus.None)
                {
                    try
                    {
                        return Progress;
                    }
                    catch
                    {
                        return 0.0f;
                    }
                }
                return 1.0f;
            }
        }

        internal void InvokeCompletionEvent()
        {
            if (m_CompletedAction != null)
            {
                m_CompletedAction.Invoke(new AsyncOperationHandle(this));
                m_CompletedAction.Clear();
            }

            if (m_CompletedActionT != null)
            {
                m_CompletedActionT.Invoke(new AsyncOperationHandle<TObject>(this));
                m_CompletedActionT.Clear();
            }
            if (m_waitHandle != null)
                m_waitHandle.Set();

            m_InDeferredCallbackQueue = false;
        }

        internal AsyncOperationHandle<TObject> Handle { get { return new AsyncOperationHandle<TObject>(this); } }

        DelegateList<float> m_UpdateCallbacks;
        Action<float> m_UpdateCallback;

        //bool m_IsExecuting;

        private void UpdateCallback(float unscaledDeltaTime)
        {
            IUpdateReceiver updateOp = this as IUpdateReceiver;
            updateOp.Update(unscaledDeltaTime);
        }

        /// <summary>
        /// Complete the operation and invoke events.
        /// </summary>
        /// <param name="result">The result object for the operation.</param>
        /// <param name="success">True if successful.</param>
        /// <param name="errorMsg">The error message if the operation has failed.</param>
        public void Complete(TObject result, bool success, string errorMsg)
        {
            IUpdateReceiver upOp = this as IUpdateReceiver;
            if (m_UpdateCallbacks != null && upOp != null)
                m_UpdateCallbacks.Remove(m_UpdateCallback);

            m_Result = result;
            m_Status = success ? AsyncOperationStatus.Succeeded : AsyncOperationStatus.Failed;

            if (m_Status == AsyncOperationStatus.Failed)
            {
                m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationFail, 0, errorMsg);
                OperationException = new Exception(errorMsg);
            }

            // Why defer the callback?
            //if (m_IsExecuting)
            //    RegisterForDeferredCallbackEvent();
            //else
            m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationPercentComplete, 1);
            m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationComplete);
            InvokeCompletionEvent();

            DecrementReferenceCount();
        }


        internal void Start(ResourceManager rm, AsyncOperationHandle dependency, DelegateList<float> updateCallbacks)
        {
            m_RM = rm;
            m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationCreate);
            m_RM.PostDiagnosticEvent(new AsyncOperationHandle(this), ResourceManager.DiagnosticEventType.AsyncOperationPercentComplete, 0);
            IncrementReferenceCount(); // keep a reference until the operation completes
            m_UpdateCallbacks = updateCallbacks;
            if (dependency.IsValid() && !dependency.IsDone)
                dependency.Completed += m_dependencyCompleteAction;
            else
                InvokeExecute();
        }

        private void InvokeExecute()
        {
       //     m_IsExecuting = true;
            Execute();
            IUpdateReceiver upOp = this as IUpdateReceiver;
            if (upOp != null)
                m_UpdateCallbacks.Add(m_UpdateCallback);

         //   m_IsExecuting = false;
        }

        event Action<AsyncOperationHandle> IAsyncOperation.CompletedTypeless
        {
            add { CompletedTypeless += value; }
            remove { CompletedTypeless -= value; }
        }

        event Action<AsyncOperationHandle> IAsyncOperation.Destroyed
        {
            add
            {
                Destroyed += value;
            }

            remove
            {
                Destroyed -= value;
            }
        }

        int IAsyncOperation.Version { get { return Version; } }

        int IAsyncOperation.ReferenceCount { get { return ReferenceCount; } }

        float IAsyncOperation.PercentComplete { get { return PercentComplete; } }

        AsyncOperationStatus IAsyncOperation.Status { get { return Status; } }

        Exception IAsyncOperation.OperationException { get { return OperationException; } }

        bool IAsyncOperation.IsDone { get { return IsDone; } }

        AsyncOperationHandle IAsyncOperation.Handle { get { return Handle; } }

        Action<IAsyncOperation> IAsyncOperation.OnDestroy { set { OnDestroy = value; } }

        string IAsyncOperation.DebugName { get { return DebugName; } }


        object IAsyncOperation.GetResultAsObject()
        {
            return m_Result;
        }

        Type IAsyncOperation.ResultType { get { return typeof(TObject); } }
        void IAsyncOperation.GetDependencies(List<AsyncOperationHandle> deps) { GetDependencies(deps); }

        void IAsyncOperation.DecrementReferenceCount() { DecrementReferenceCount(); }

        void IAsyncOperation.IncrementReferenceCount() { IncrementReferenceCount(); }

        void IAsyncOperation.InvokeCompletionEvent() { InvokeCompletionEvent(); }

        void IAsyncOperation.Start(ResourceManager rm, AsyncOperationHandle dependency, DelegateList<float> updateCallbacks)
        {
            Start(rm, dependency, updateCallbacks);
        }

    }
}
