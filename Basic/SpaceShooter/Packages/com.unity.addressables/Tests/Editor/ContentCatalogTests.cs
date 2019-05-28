using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace UnityEditor.AddressableAssets.Tests {
    public class ContentCatalogTests
    {
        List<object> m_Keys;
        List<Type> m_Providers;

        [Serializable]
        public class SerializableKey
        {
            public int index;
            public string path;
        }

        [OneTimeSetUp]
        public void Init()
        {
            m_Keys = new List<object>();
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 1000; i++)
            {
                var r = Random.Range(0, 100);
                if (r < 20)
                {
                    int len = Random.Range(1, 5);
                    for (int j = 0; j < len; j++)
                        sb.Append(GUID.Generate().ToString());
                    m_Keys.Add(sb.ToString());
                    sb.Length = 0;
                }
                else if (r < 40)
                {
                    m_Keys.Add((ushort)(i * 13));
                }
                else if (r < 50)
                {
                    m_Keys.Add(i * 13);
                }
                else if (r < 60)
                {
                    m_Keys.Add((uint)(i * 13));
                }
                else if (r < 80)
                {
                    m_Keys.Add(new SerializableKey { index = i, path = GUID.Generate().ToString() });
                }
                else
                {
                    m_Keys.Add(Hash128.Parse(GUID.Generate().ToString()));
                }
            }
            m_Providers = new List<Type>();
            m_Providers.Add(typeof(BundledAssetProvider));
            m_Providers.Add(typeof(AssetBundleProvider));
            m_Providers.Add(typeof(AssetDatabaseProvider));
            m_Providers.Add(typeof(LegacyResourcesProvider));
            m_Providers.Add(typeof(JsonAssetProvider));
            m_Providers.Add(typeof(TextDataProvider));
            m_Providers.Add(typeof(TextDataProvider));
        }

        List<T> GetRandomSubset<T>(List<T> keys, int count)
        {
            if (keys.Count == 0 || count == 0)
                return new List<T>();
            var entryKeys = new HashSet<T>();
            for (int k = 0; k < count; k++)
                entryKeys.Add(keys[Random.Range(0, keys.Count)]);
            return entryKeys.ToList();
        }

        [Serializable]
        public class EvenData
        {
            public int index;
            public string path;
        }

        [Serializable]
        public class OddData
        {
            public int index;
            public string path;
        }

        [Test]
        public void AssetBundleRequestOptionsTest()
        {
            var options = new AssetBundleRequestOptions
            {
                ChunkedTransfer = true,
                Crc = 123,
                Hash = new Hash128(1, 2, 3, 4).ToString(),
                RedirectLimit = 4,
                RetryCount = 7,
                Timeout = 12
            };
            var dataEntry = new ContentCatalogDataEntry("internalId", "provider", new object[] { 1 }, null, options);
            var entries = new List<ContentCatalogDataEntry>();
            entries.Add(dataEntry);
            var ccData = new ContentCatalogData(entries);
            var locator = ccData.CreateLocator();
            IList<IResourceLocation> locations;
            if (!locator.Locate(1, out locations))
                Assert.Fail("Unable to locate resource location");
            var loc = locations[0];
            var locOptions = loc.Data as AssetBundleRequestOptions;
            Assert.IsNotNull(locOptions);
            Assert.AreEqual(locOptions.ChunkedTransfer, options.ChunkedTransfer);
            Assert.AreEqual(locOptions.Crc, options.Crc);
            Assert.AreEqual(locOptions.Hash, options.Hash);
            Assert.AreEqual(locOptions.RedirectLimit, options.RedirectLimit);
            Assert.AreEqual(locOptions.RetryCount, options.RetryCount);
            Assert.AreEqual(locOptions.Timeout, options.Timeout);
        }

        [Test]
        public void VerifySerialization()
        {
            var sw = Stopwatch.StartNew();
            sw.Start();
            var catalog = new ContentCatalogData();
            var entries = new List<ContentCatalogDataEntry>();
            var availableKeys = new List<object>();

            for (int i = 0; i < 1000; i++)
            {
                var internalId = "Assets/TestPath/" + GUID.Generate() + ".asset";
                var eKeys = GetRandomSubset(m_Keys, Random.Range(1, 5));
                object data;
                if (i % 2 == 0)
                    data = new EvenData { index = i, path = internalId };
                else
                    data = new OddData { index = i, path = internalId };

                var e = new ContentCatalogDataEntry(internalId, m_Providers[Random.Range(0, m_Providers.Count)].FullName, eKeys, GetRandomSubset(availableKeys, Random.Range(0, 1)), data);
                availableKeys.Add(eKeys[0]);
                entries.Add(e);
            }

            catalog.SetData(entries);
            sw.Stop();
            var t = sw.Elapsed.TotalMilliseconds;
            sw.Reset();
            sw.Start();
            var locMap = catalog.CreateLocator();
            sw.Stop();
            Debug.LogFormat("Create: {0}ms, Load: {1}ms", t, sw.Elapsed.TotalMilliseconds);

            foreach (var k in locMap.Locations)
            {
                foreach (var loc in k.Value)
                {
                    var entry = entries.Find(e => e.InternalId == loc.InternalId);
                    Assert.AreEqual(entry.Provider, loc.ProviderId);

                    var deps = loc.Dependencies;
                    if (deps != null)
                    {
                        foreach (var ed in entry.Dependencies)
                        {
                            IList<IResourceLocation> depList;
                            Assert.IsTrue(locMap.Locate(ed, out depList));
                            for (int i = 0; i < depList.Count; i++)
                                Assert.AreEqual(depList[i].InternalId, deps[i].InternalId);
                        }
                    }
                }
            }
        }
    }
}
