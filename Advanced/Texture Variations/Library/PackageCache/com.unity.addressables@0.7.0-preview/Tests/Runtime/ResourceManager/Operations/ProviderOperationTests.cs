using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace UnityEngine.ResourceManagement.Tests
{
    public class ProviderOperationTests
    {
        MockProvider m_Provider;
        MockProvider m_Provider2;
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

        private void ProvideBasicCallback(ProvideHandle provideHandle)
        {
            provideHandle.Complete(provideHandle.Location.InternalId, true, null);
        }

        [SetUp]
        public void Setup()
        {
            m_RM = new ResourceManager();
            m_RM.CallbackHooksEnabled = false;

            m_Provider = new MockProvider();
            m_Provider.ProvideCallback = ProvideBasicCallback;
            m_Provider2 = new MockProvider();
            m_Provider2._ProviderId = "MockId2";
            m_Provider2.ProvideCallback = ProvideBasicCallback;

            m_RM.ResourceProviders.Add(m_Provider);
            m_RM.ResourceProviders.Add(m_Provider2);
        }

        [TearDown]
        public void TearDown()
        {
            m_RM.ResourceProviders.Remove(m_Provider);
            m_RM.ResourceProviders.Remove(m_Provider2);
            Assert.Zero(m_RM.OpCacheCount);
            m_RM.Dispose();
            m_RM = null;
        }

        [Test]
        public void WhenDependency_ProvideCalledAfterDependencyFinishes()
        {
            ProvideHandle provideHandle = default(ProvideHandle);
            m_Provider2.ProvideCallback = x => { provideHandle = x; };
            ResourceLocationBase depLoc = new ResourceLocationBase("dep", "dep", m_Provider2.ProviderId);
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc);
            var op = m_RM.ProvideResource<object>(loc);
            m_RM.Update(0.0f);
            Assert.False(op.IsDone);
            provideHandle.Complete(2, true, null);
            m_RM.Update(0.0f);
            Assert.True(op.IsDone);
            op.Release();
        }

        [Test]
        public void OnDestroyed_DepOpReleasedAndProviderReleased()
        {
            ResourceLocationBase depLoc = new ResourceLocationBase("dep", "dep", m_Provider2.ProviderId);
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc);
            ResourceLocationBase loc2 = new ResourceLocationBase("2", "2", m_Provider.ProviderId, depLoc);
            var op1 = m_RM.ProvideResource<object>(loc);
            m_RM.Update(0.0f);
            Assert.AreEqual(1, m_Provider2.ProvideLog.Count);
            var op2 = m_RM.ProvideResource<object>(loc2);
            m_RM.Update(0.0f);
            Assert.AreEqual(1, m_Provider2.ProvideLog.Count);

            Assert.AreEqual(2, m_Provider.ProvideLog.Count);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op1.Status);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op2.Status);

            // decrement the first op. the second op should still be holding the dependency
            op1.Release(); 
            Assert.AreEqual(0, m_Provider2.ReleaseLog.Count);

            // decrement the second op. the dependency should now have been released
            op2.Release();
            Assert.AreEqual(1, m_Provider2.ReleaseLog.Count);

            Assert.AreEqual(2, m_Provider.ReleaseLog.Count);
            Assert.AreEqual(1, m_Provider2.ReleaseLog.Count);
        }

        [Test]
        public void WhenDependentOpFails_ProvideIsNotCalled()
        {
            ResourceLocationBase depLoc = new ResourceLocationBase("dep", "dep", "unknown provider");
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc);
            var op = m_RM.ProvideResource<object>(loc);
            m_RM.Update(0.0f);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            op.Release();
            Assert.AreEqual(0, m_Provider.ProvideLog.Count);
            Assert.AreEqual(0, m_Provider.ReleaseLog.Count);
        }

        [Test]
        public void WhenDependentOpFails_AndProviderSupportsFailedDependencies_OperationContinues()
        {
            m_Provider._BehaviourFlags = ProviderBehaviourFlags.CanProvideWithFailedDependencies;
            ResourceLocationBase depLoc = new ResourceLocationBase("dep", "dep", "unknown provider");
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc);
            var op = m_RM.ProvideResource<object>(loc);
            m_RM.Update(0.0f);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.AreEqual("1", op.Result);
            op.Release();
            Assert.AreEqual(1, m_Provider.ProvideLog.Count);
            Assert.AreEqual(1, m_Provider.ReleaseLog.Count);
        }

        [Test]
        public void WhenProviderCompletesInsideProvideCall_CallbackIsNotDeferred()
        {
            ResourceLocationBase depLoc = new ResourceLocationBase("dep", "dep", m_Provider2.ProviderId);
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc);
            ProvideHandle depHandle = new ProvideHandle();
            m_Provider2.ProvideCallback = (x) => { depHandle = x; };
            var op = m_RM.ProvideResource<object>(loc);

            bool callbackCalled = false;
            Assert.AreEqual(AsyncOperationStatus.None, op.Status);
            op.Completed += x => callbackCalled = true;
            
            // mark dependency complete
            depHandle.Complete(1, true, null);
            Assert.IsTrue(callbackCalled);

            op.Release();
        }

        [Test]
        public void WhenProviderCompletesOutsideProvideCall_CallbackIsImmediate()
        {
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId);
            ProvideHandle depHandle = new ProvideHandle();
            m_Provider.ProvideCallback = (x) => { depHandle = x; };
            var op = m_RM.ProvideResource<object>(loc);

            bool callbackCalled = false;
            Assert.AreEqual(AsyncOperationStatus.None, op.Status);
            op.Completed += x => callbackCalled = true;

            // mark dependency complete
            Assert.IsFalse(callbackCalled);
            depHandle.Complete(1, true, null);
            Assert.IsTrue(callbackCalled);
            op.Release();
        }

        [Test]
        public void UsingProviderHandleWithInvalidVersion_ThrowsException()
        {
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId);
            ProvideHandle handle = new ProvideHandle();
            m_Provider.ProvideCallback = (x) => { handle = x; };
            var op = m_RM.ProvideResource<object>(loc);
            handle.Complete<object>(null, true, null);
            Assert.Catch(() => handle.Complete<object>(null, true, null), ProviderOperation<object>.kInvalidHandleMsg);
            Assert.Catch(() => handle.GetDependencies(new List<object>()), ProviderOperation<object>.kInvalidHandleMsg);
            op.Release();
        }

        [Test]
        public void GetDependency_InsertsDependenciesIntoList()
        {
            List<object> deps = new List<object>();
            ResourceLocationBase depLoc = new ResourceLocationBase("dep1", "dep1", m_Provider2.ProviderId);
            ResourceLocationBase depLoc2 = new ResourceLocationBase("dep2", "dep2", m_Provider2.ProviderId);
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc, depLoc2);
            m_Provider.ProvideCallback = (handle) => { handle.GetDependencies(deps); handle.Complete(0, true, null); };
            var op = m_RM.ProvideResource<object>(loc);
            m_RM.Update(0.0f);

            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.AreEqual(deps[0], "dep1");
            Assert.AreEqual(deps[1], "dep2");
            op.Release();
        }

        class Type1 { }
        class Type2 { }

        [Test]
        public void WhenProviderCallsComplete_AndTypeIsIncorrect_Throws()
        {
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId);
            Exception testException = null;
            m_Provider.ProvideCallback = (x) => 
            {
                try
                {
                    x.Complete(new Type2(), true, null);
                }
                catch(Exception e)
                {
                    testException = e;
                }
            };
            var op = m_RM.ProvideResource<Type1>(loc);
            m_RM.Update(0.0f);
            Assert.IsNotNull(testException);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            Assert.IsNull(op.Result);
            Assert.IsTrue(op.OperationException.Message.Contains("Provider of type"));
            op.Release();
        }

        [Test]
        public void WhenProviderThrowsExceptionOnProvide_OperationFails()
        {
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId);
            m_Provider.ProvideCallback = (x) => { throw new Exception("I have failed"); };
            var op = m_RM.ProvideResource<Type1>(loc);
            m_RM.Update(0.0f);

            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            op.Release();
        }

        [Test]
        public void WhenProviderThrowsExceptionInProgressCallback_ProgressReturnsZero()
        {
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId);
            ProvideHandle handle = new ProvideHandle();
            bool didThrow = false;
            m_Provider.ProvideCallback = (x) => 
            {
                handle = x;
                handle.SetProgressCallback(() => { didThrow = true; throw new Exception("I have failed"); });
            };
            var op = m_RM.ProvideResource<Type1>(loc);
            Assert.AreEqual(0.0f, op.PercentComplete);
            Assert.True(didThrow);
            handle.Complete<object>(null, true, null);
            op.Release();
        }

        public void ProvideWithoutSpecifiedType_UsesDefaultProviderType()
        {
            ResourceLocationBase depLoc = new ResourceLocationBase("dep", "dep", m_Provider2.ProviderId);
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId, depLoc);
            ProvideHandle handle = default(ProvideHandle);
            m_Provider2.ProvideCallback = (x) => { handle = x; };
            m_Provider2.DefaultType = typeof(Type2);
            var op = m_RM.ProvideResource<object>(loc);

            Assert.AreEqual(typeof(Type2), handle.Type);
            handle.Complete(new Type2(), true, null);
            op.Release();
        }

        void ProviderCompleteTypeTest<TRequestType, TResultType>(TResultType result, string exceptionMessage)
        {
            ResourceLocationBase loc = new ResourceLocationBase("1", "1", m_Provider.ProviderId);
            ProvideHandle handle = default(ProvideHandle);
            m_Provider.ProvideCallback = (x) =>
            {
                handle = x;
            };
            var op = m_RM.ProvideResource<TRequestType>(loc);

            if (string.IsNullOrEmpty(exceptionMessage))
            {
                handle.Complete(result, true, null);
            }
            else
            {
                // TODO: assert contents of exception
                Assert.Catch(() => handle.Complete(result, true, null));
            }

            Assert.True(op.IsDone);
            op.Release();
        }
        class Type3 : Type2 { }

        [Test]
        public void ProvideHandle_CompleteWithExactType_Succeeds()
        {
            ProviderCompleteTypeTest<Type2, Type2>(new Type2(), null);
        }

        [Test]
        public void ProvideHandle_CompleteWithDerivedTypeAsResult_Succeeds()
        {
            ProviderCompleteTypeTest<Type2, Type3>(new Type3(), null);
        }

        [Test]
        public void ProvideHandle_CompleteWithBaseTypeAsResult_Succeeds()
        {
            ProviderCompleteTypeTest<Type3, Type2>(new Type3(), null);
        }

        [Test]
        public void ProvideHandle_CompleteWithNullForReferencedType_Succeeds()
        {
            ProviderCompleteTypeTest<Type1, Type1>(null, null);
        }

        [Test]
        public void ProvideHandle_CompleteWithNonAssignableType_Throws()
        {
            ProviderCompleteTypeTest<Type2, Type1>(new Type1(), "Failed");
        }


        [Test]
        public void ProvideResource_WhenDependencyFailsToLoad_AndProviderCannotLoadWithFailedDependencies_ProvideNotCalled()
        {
            m_Provider.ProvideCallback = (pi) => { throw new Exception("This Should Not Have Been Called"); };
            m_RM.ResourceProviders.Add(m_Provider);
            ResourceLocationBase locDep = new ResourceLocationBase("depasset", "depasset", "unkonwn");
            ResourceLocationBase locRoot = new ResourceLocationBase("rootasset", "rootasset", m_Provider.ProviderId, locDep);
            AsyncOperationHandle<object> op = m_RM.ProvideResource<object>(locRoot);
            m_RM.Update(0.0f);
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            op.Release();
        }

        [Test]
        public void ProvideResource_WhenDependencyFailsToLoad_AndProviderCanLoadWithFailedDependencies_ProviderStillProvides()
        {

            m_Provider._BehaviourFlags = ProviderBehaviourFlags.CanProvideWithFailedDependencies;
            m_Provider.ProvideCallback = (pi) =>
            {
                pi.Complete(5, true, null);
            };
            m_RM.ResourceProviders.Add(m_Provider);
            ResourceLocationBase locDep = new ResourceLocationBase("depasset", "depasset", "unkonwn");
            ResourceLocationBase locRoot = new ResourceLocationBase("rootasset", "rootasset", m_Provider.ProviderId, locDep);
            AsyncOperationHandle<object> op = m_RM.ProvideResource<object>(locRoot);
            m_RM.Update(0.0f);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
            Assert.AreEqual(5, op.Result);
            op.Release();
        }

        [Test]
        public void ProvideResources_CanLoadAndUnloadMultipleResources()
        {

            m_Provider.ProvideCallback = (pi) =>
            {
                pi.Complete(int.Parse(pi.Location.InternalId), true, null);
            };
            m_RM.ResourceProviders.Add(m_Provider);
            var locations = new List<IResourceLocation>() {
                new ResourceLocationBase("0", "0", m_Provider.ProviderId),
                new ResourceLocationBase("1", "1", m_Provider.ProviderId),
            };
            AsyncOperationHandle<IList<object>> op = m_RM.ProvideResources<object>(locations);
            m_RM.Update(0.0f);
            for (int i = 0; i < locations.Count; i++)
            {
                Assert.AreEqual((int)op.Result[i], i);
            }
            op.Release();
        }

        [Test]
        public void ProvideResource_CanLoadNestedDepdendencies()
        {
            m_Provider._BehaviourFlags = ProviderBehaviourFlags.CanProvideWithFailedDependencies;
            List<IResourceLocation> loadOrder = new List<IResourceLocation>();
            m_Provider.ProvideCallback = (pi) =>
            {
                loadOrder.Add(pi.Location);
                pi.Complete(0, true, null);
            };
            IResourceLocation i3 = new ResourceLocationBase("3", "3", m_Provider.ProviderId);
            IResourceLocation i2 = new ResourceLocationBase("2", "2", m_Provider.ProviderId, i3);
            IResourceLocation i1 = new ResourceLocationBase("1", "1", m_Provider.ProviderId, i2);
            var op = m_RM.ProvideResource<object>(i1);
            m_RM.Update(0.0f);
            Assert.AreEqual(5, m_RM.OpCacheCount);
            Assert.AreSame(i3, loadOrder[0]);
            Assert.AreSame(i2, loadOrder[1]);
            Assert.AreSame(i1, loadOrder[2]);
            op.Release();
        }
    }
}