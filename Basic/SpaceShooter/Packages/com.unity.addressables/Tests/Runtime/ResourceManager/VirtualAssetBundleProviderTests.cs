using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.TestTools;
using System.Collections;

#if UNITY_EDITOR
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
#endif

#if UNITY_EDITOR
namespace UnityEngine.ResourceManagement.Tests
{
    [TestFixture]
    public class VirtualAssetBundleProviderTests
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

        private ResourceLocationBase AddNewBundle(VirtualAssetBundleRuntimeData data, string bundleName, bool local, int dataSize, int headerSize)
        {
            VirtualAssetBundle bundle = new VirtualAssetBundle(bundleName, local, 0, "");
            bundle.SetSize(dataSize, headerSize);
            data.AssetBundles.Add(bundle);
            ResourceLocationBase bundleLocation = new ResourceLocationBase(bundleName, bundleName, typeof(AssetBundleProvider).FullName);
            bundle.SetSize(dataSize, headerSize);
            return bundleLocation;
        }

        private List<ResourceLocationBase> CreateBundleSet(VirtualAssetBundleRuntimeData data, int count, bool local, int dataSize, int headerSize)
        {
            List<ResourceLocationBase> locations = new List<ResourceLocationBase>();
            for (int i = 0; i < count; i++)
                locations.Add(AddNewBundle(data, "bundle" + i.ToString(), local, dataSize, headerSize));
            return locations;
        }

        [Test]
        public void WhenMultipleBundlesLoading_BandwidthIsAmortizedAcrossAllBundles([Values(true, false)]bool localBundles)
        {
            const int kBundleCount = 4;
            const int kLocalBW = 800;
            const int kRemoteBW = 2000;
            const int kHeaderSize = 2000;
            const int kDataSize = 4000;
            const float kTimeSlize = 0.5f;

            int kUpdatesForRemoteDownload = (int)Math.Ceiling((((kDataSize * kBundleCount) / kRemoteBW) / kTimeSlize));
            int kUpdatesLocalLoad = (int)Math.Ceiling((((kHeaderSize * kBundleCount) / kLocalBW) / kTimeSlize));

            ResourceManager rm = new ResourceManager();
            rm.CallbackHooksEnabled = false;

            VirtualAssetBundleRuntimeData data = new VirtualAssetBundleRuntimeData(kLocalBW, kRemoteBW);
            List<ResourceLocationBase> locations = CreateBundleSet(data, kBundleCount, localBundles, kDataSize, kHeaderSize);
            VirtualAssetBundleProvider provider = new VirtualAssetBundleProvider(data);
            rm.ResourceProviders.Add(provider);
            var ops = new List<AsyncOperationHandle<VirtualAssetBundle>>();
            foreach(IResourceLocation loc in locations )
                ops.Add(rm.ProvideResource<VirtualAssetBundle>(loc));


            int totalUpdatesNeeded = kUpdatesLocalLoad + (localBundles ? 0 : kUpdatesForRemoteDownload);
            for (int i = 0; i < totalUpdatesNeeded; i++)
            {
                foreach(AsyncOperationHandle<VirtualAssetBundle> op in ops)
                {
                    Assert.IsFalse(op.IsDone);
                    Assert.Less(op.PercentComplete, 1.0f);
                }
                provider.Update(kTimeSlize);
            }

            foreach(var op in ops)
            {
                Assert.IsTrue(op.IsDone);
                Assert.AreEqual(1.0f, op.PercentComplete);
                Assert.AreEqual(AsyncOperationStatus.Succeeded, op.Status);
                op.Release();
            }

            Assert.Zero(rm.OperationCacheCount);
            rm.Dispose();
        }
        
        [UnityTest]
        public IEnumerator WhenLoadingUnknownBundle_OperationFailsWithMessage()
        {
            ResourceManager rm = new ResourceManager();
            rm.CallbackHooksEnabled = false;

            VirtualAssetBundleRuntimeData data = new VirtualAssetBundleRuntimeData();
            VirtualAssetBundleProvider provider = new VirtualAssetBundleProvider(data);
            rm.ResourceProviders.Add(provider);
            ResourceLocationBase unknownLocation = new ResourceLocationBase("unknown", "unknown", typeof(AssetBundleProvider).FullName);
            var op = rm.ProvideResource<VirtualAssetBundle>(unknownLocation);
            // wait for delayed action manager. 
            // TODO: refactor delayed action manager so we can pump it instead of waiting a frame
            yield return null; 
            Assert.AreEqual(AsyncOperationStatus.Failed, op.Status);
            StringAssert.Contains("Unable to unload virtual bundle", op.OperationException.Message);
            op.Release();

            Assert.Zero(rm.OperationCacheCount);
            rm.Dispose();
        }

        [Test]
        public void WhenInBundleLoadCompleteCallback_CanLoadAnotherBundle()
        {
            // TODO
        }

        [Test]
        public void WhenInBundleLoadCompleteCallback_CanUnloadAnotherBundle()
        {
            // TODO
        }
    }
}
#endif

