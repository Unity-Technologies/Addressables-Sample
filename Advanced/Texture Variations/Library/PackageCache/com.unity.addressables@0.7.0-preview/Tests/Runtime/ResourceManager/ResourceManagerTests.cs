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
    public class ResourceManagerTests
    {
        Action<AsyncOperationHandle, Exception> m_PrevHandler;
       
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

        ResourceManager m_ResourceManager;
        [SetUp]
        public void Setup()
        {
            m_ResourceManager = new ResourceManager();
            m_ResourceManager.CallbackHooksEnabled = false; // default for tests. disabled callback hooks. we will call update manually
        }

        [TearDown]
        public void TearDown()
        {
            Assert.Zero(m_ResourceManager.OpCacheCount);
            m_ResourceManager.Dispose();
        }

        class IntOperation : AsyncOperationBase<int>
        {
            string msg = "msg";
            protected override void Execute()
            {
                Complete(0, true, msg);
            }
        }

        [Test]
        public void WhenOperationReturnsValueType_NoGCAllocs()
        {
            var op = new IntOperation();
            Assert.That(() =>
            {
                var handle = m_ResourceManager.StartOperation(op, default);
                handle.Release();
            }, TestTools.Constraints.Is.Not.AllocatingGCMemory(), "GC Allocation detected");
        }

        [Test]
        public void WhenProviderImplementsIReceiverUpdate_UpdateIsCalledWhileInProviderList()
        {
            MockProvider provider = new MockProvider();
            m_ResourceManager.ResourceProviders.Add(provider);
            m_ResourceManager.Update(0.0f);
            Assert.AreEqual(1, provider.UpdateCount);

            // Update isn't called after removing provider
            m_ResourceManager.ResourceProviders.Remove(provider);
            m_ResourceManager.Update(0.0f);
            Assert.AreEqual(1, provider.UpdateCount);
        }

        [UnityTest]
        public IEnumerator WhenResourceManagerCallbackHooksAreEnabled_ResourceManagerUpdatesProvidersAndCleansUp()
        {
            int beforeGOCount = GameObject.FindObjectsOfType<MonoBehaviourCallbackHooks>().Length;
            MockProvider provider = new MockProvider();
            m_ResourceManager.CallbackHooksEnabled = true;
            m_ResourceManager.ResourceProviders.Add(provider);
            yield return null;
            Assert.AreEqual(1, provider.UpdateCount);
            Assert.AreEqual(beforeGOCount+1, GameObject.FindObjectsOfType<MonoBehaviourCallbackHooks>().Length);
            m_ResourceManager.Dispose();
            yield return null;
            Assert.AreEqual(beforeGOCount, GameObject.FindObjectsOfType<MonoBehaviourCallbackHooks>().Length);
        }


        class MockInstanceProvider : IInstanceProvider
        {
            public Func<AsyncOperationHandle<GameObject>, InstantiationParameters, GameObject> ProvideInstanceCallback;
            public Action<GameObject> ReleaseInstanceCallback;
            public GameObject ProvideInstance(AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiateParameters)
            {
                return ProvideInstanceCallback(prefabHandle, instantiateParameters);
            }

            public void ReleaseInstance(GameObject instance)
            {
                ReleaseInstanceCallback(instance);
            }
        }

        class GameObjectProvider : IResourceProvider
        {
            public string ProviderId { get { return "GOPRovider"; } }

            public ProviderBehaviourFlags BehaviourFlags { get { return ProviderBehaviourFlags.None; } }

            public bool CanProvide(Type t, IResourceLocation location)
            {
                return t == typeof(GameObject);
            }

            public Type GetDefaultType(IResourceLocation location)
            {
                return typeof(GameObject);
            }

            public bool Initialize(string id, string data) { return true; }

            public void Provide(ProvideHandle provideHandle)
            {
                var result = new GameObject(provideHandle.Location.InternalId);
                provideHandle.Complete(result, true, null);
            }

            public void Release(IResourceLocation location, object asset)
            {
                GameObject.Destroy((GameObject)asset);
            }
        }

        // TODO:
        // To test: release via operation, 
        // Edge cases: game object fails to load, callback throws exception, Release called on handle before operation completes
        //
        [Test]
        public void ProvideInstance_CanProvide()
        {
            m_ResourceManager.ResourceProviders.Add(new GameObjectProvider());
            ResourceLocationBase locDep = new ResourceLocationBase("prefab", "prefab1", "GOPRovider");

            MockInstanceProvider iProvider = new MockInstanceProvider();
            InstantiationParameters instantiationParameters = new InstantiationParameters(null, true);
            AsyncOperationHandle<GameObject> []refResource = new AsyncOperationHandle<GameObject>[1];
            iProvider.ProvideInstanceCallback = (prefabHandle, iParam) =>
            {
                refResource[0] = prefabHandle;
                prefabHandle.Acquire();
                Assert.AreEqual("prefab1", prefabHandle.Result.name);
                return new GameObject("instance1");
            };
            iProvider.ReleaseInstanceCallback = (go) => { refResource[0].Release(); GameObject.Destroy(go); };

            AsyncOperationHandle<GameObject> obj = m_ResourceManager.ProvideInstance(iProvider, locDep, instantiationParameters);

            m_ResourceManager.Update(0.0f);
            Assert.AreEqual(AsyncOperationStatus.Succeeded, obj.Status);
            Assert.AreEqual("instance1", obj.Result.name);
            Assert.AreEqual(1, m_ResourceManager.OpCacheCount);
            obj.Release();
        }
        

    }
}