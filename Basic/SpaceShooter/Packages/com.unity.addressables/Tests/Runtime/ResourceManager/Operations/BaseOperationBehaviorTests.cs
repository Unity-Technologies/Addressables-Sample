using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.TestTools;
using System.Linq;
using UnityEngine.TestTools.Constraints;

namespace UnityEngine.ResourceManagement.Tests
{
    public class BaseOperationBehaviorTests
    {
        Action<AsyncOperationHandle, Exception> m_PrevHandler;
        ResourceManager m_RM;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            m_PrevHandler = ResourceManager.ExceptionHandler;
            ResourceManager.ExceptionHandler = null;
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            ResourceManager.ExceptionHandler = m_PrevHandler;
        }

        [SetUp]
        public void Setup()
        {
            m_RM = new ResourceManager();
            m_RM.CallbackHooksEnabled = false; // default for tests. disabled callback hooks. we will call update manually
        }

        [TearDown]
        public void TearDown()
        {
            Assert.Zero(m_RM.OperationCacheCount);
            m_RM.Dispose();
        }

        [Test]
        public void WhenReferenceCountReachesZero_DestroyCallbackInvoked()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            int resultInDestroyCallback = 0;
            op.Destroyed += (x) => resultInDestroyCallback = x.Convert<int>().Result;
            op.Release();
            Assert.AreEqual(1, resultInDestroyCallback);
        }

        [Test]
        public void WhileCompletedCallbackIsDeferredOnCompletedOperation_ReferenceCountIsHeld()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            int refCount = op.ReferenceCount;
            bool completedCalled = false;
            op.Completed += (x) => completedCalled = true; // callback is deferred to next update
            Assert.AreEqual(refCount + 1, op.ReferenceCount);
            m_RM.Update(0.0f);
            Assert.AreEqual(refCount, op.ReferenceCount);
            Assert.AreEqual(true, completedCalled);
            op.Release();
        }

        [Test]
        public void WhenInDestroyCallback_IncrementAndDecrementReferenceCount_Throws()
        {
            var op = m_RM.CreateCompletedOperation<int>(1, string.Empty);
            int resultInDestroyCallback = 0;
            Exception onInc = null;
            Exception onDec = null;
            op.Destroyed += (x) =>
            {
                try { x.Acquire(); }
                catch (Exception e) { onInc = e; }
                try { x.Release(); }
                catch (Exception e) { onDec = e; }
                resultInDestroyCallback = x.Convert<int>().Result;
            };
            op.Release();
            Assert.NotNull(onInc);
            Assert.NotNull(onDec);
        }

        class MockOperation<T> : AsyncOperationBase<T>
        {
            public Action ExecuteCallback = () => { };
            protected override void Execute()
            {
                ExecuteCallback();
            }
        }

        [Test]
        public void WhenOperationHasDependency_ExecuteNotCalledUntilDependencyCompletes()
        {
            var op1 = new MockOperation<int>();
            var op2 = new MockOperation<int>();
            var handle1 = m_RM.StartOperation(op1, default(AsyncOperationHandle));
            op2.ExecuteCallback = () => { op2.Complete(0, true, string.Empty); };
            var handle2 = m_RM.StartOperation(op2, handle1);
            m_RM.Update(0.0f);
            Assert.AreEqual(false, handle2.IsDone);
            op1.Complete(0, true, null);
            Assert.AreEqual(true, handle2.IsDone);
            handle1.Release();
            handle2.Release();
        }

        // TODO:
        // public void WhenOperationHasDependency_AndDependencyFails_DependentOpStillExecutes()

        // Bad derived class behavior
        // public void CustomOperation_WhenCompleteCalledBeforeStartOperation_ThrowsOperationDoesNotComplete
        // public void CustomOperation_WhenCompleteCalledMultipleTimes_Throws
        // public void CustomOperation_WhenProgressCallbackThrowsException_ErrorLoggedAndHandleReturnsZero
        // public void CustomOperation_WhenDestroyThrowsException_ErrorLogged
        // public void CustomOperation_WhenExecuteThrows_ErrorLoggedAndOperationSetAsFailed

        // TEST: Per operation update behavior

        // public void AsyncOperationHandle_WhenReleaseOnInvalidHandle_Throws
        // public void AsyncOperationHandle_WhenConvertToIncompatibleHandleType_Throws
        //
        
    }
}
