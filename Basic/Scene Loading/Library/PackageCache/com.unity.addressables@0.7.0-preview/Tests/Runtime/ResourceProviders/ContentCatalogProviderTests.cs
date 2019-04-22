using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.AddressableAssets.ResourceProviders.Tests
{
	[TestFixture]
	public class ContentCatalogProviderTests
	{
		const string k_LocationName = "TestLocation";
		const string k_LocationId = "TestLocationID";
		const string k_CacheLocationId = "CacheLocationID";
		const string k_RemoteLocationId = "RemoteLocationID";
		
		ResourceLocationBase m_SimpleLocation = new ResourceLocationBase(k_LocationName, k_LocationId, typeof(ContentCatalogProvider).FullName);
		
		[Test]
		public void DetermineIdToLoad_IfNoDependencies_ReturnsMainId()
		{
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			var loadedId = contentCatalogOp.DetermineIdToLoad(m_SimpleLocation, null);
			
			Assert.AreEqual(k_LocationId, loadedId);
		}
		
		[Test]
		public void DetermineIdToLoad_IfTooFewDependencies_ReturnsMainId()
		{
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			var loadedId = contentCatalogOp.DetermineIdToLoad(m_SimpleLocation, new List<object>{1});
			
			Assert.AreEqual(k_LocationId, loadedId);
		}
		[Test]
		public void DetermineIdToLoad_IfTooManyDependencies_ReturnsMainId()
		{
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			var loadedId = contentCatalogOp.DetermineIdToLoad(m_SimpleLocation, new List<object>{1,2,3});
			
			Assert.AreEqual(k_LocationId, loadedId);
		}
		
		[Test]
		public void DetermineIdToLoad_IfOfflineAndNoCache_ReturnsMainId()
		{
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			var loadedId = contentCatalogOp.DetermineIdToLoad(m_SimpleLocation, new List<object>{string.Empty, string.Empty});
			
			Assert.AreEqual(k_LocationId, loadedId);
		}

		[Test]
		public void DetermineIdToLoad_IfOfflineAndHasCache_ReturnsCacheId()
		{
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			IResourceLocation[] dependencies = new IResourceLocation[(int)ContentCatalogProvider.DependencyHashIndex.Count];

			dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Remote] = new ResourceLocationBase(string.Empty, k_RemoteLocationId, typeof(ContentCatalogProvider).FullName);
			dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Cache] = new ResourceLocationBase(string.Empty, k_CacheLocationId, typeof(ContentCatalogProvider).FullName);

			var location = new ResourceLocationBase(k_LocationName, k_LocationId, typeof(ContentCatalogProvider).FullName, dependencies);
			var loadedId = contentCatalogOp.DetermineIdToLoad(location, new List<object>{string.Empty, "hash"});
			
			Assert.AreEqual(k_CacheLocationId, loadedId);
		}

		[Test]
		public void DetermineIdToLoad_IfOnlineMatchesCache_ReturnsCacheId()
		{
			
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			IResourceLocation[] dependencies = new IResourceLocation[(int)ContentCatalogProvider.DependencyHashIndex.Count];

			dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Remote] = new ResourceLocationBase(string.Empty, k_RemoteLocationId, typeof(ContentCatalogProvider).FullName);
			dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Cache] = new ResourceLocationBase(string.Empty, k_CacheLocationId, typeof(ContentCatalogProvider).FullName);

			var location = new ResourceLocationBase(k_LocationName, k_LocationId, typeof(ContentCatalogProvider).FullName, dependencies);
			var loadedId = contentCatalogOp.DetermineIdToLoad(location, new List<object>{"hash", "hash"});
			
			Assert.AreEqual(k_CacheLocationId, loadedId);
		}

		[Test]
		public void DetermineIdToLoad_IfOnlineMismatchesCache_ReturnsRemoteId()
		{
			var contentCatalogOp = new ContentCatalogProvider.InternalOp();

			IResourceLocation[] dependencies = new IResourceLocation[(int)ContentCatalogProvider.DependencyHashIndex.Count];

			dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Remote] = new ResourceLocationBase(string.Empty, k_RemoteLocationId, typeof(ContentCatalogProvider).FullName);
			dependencies[(int)ContentCatalogProvider.DependencyHashIndex.Cache] = new ResourceLocationBase(string.Empty, k_CacheLocationId, typeof(ContentCatalogProvider).FullName);

			var location = new ResourceLocationBase(k_LocationName, k_LocationId, typeof(ContentCatalogProvider).FullName, dependencies);
			
			
			var loadedId = contentCatalogOp.DetermineIdToLoad(location, new List<object>{"newHash", "hash"});
			Assert.AreEqual(k_RemoteLocationId, loadedId);
			
			loadedId = contentCatalogOp.DetermineIdToLoad(location, new List<object>{"newHash", string.Empty});
			Assert.AreEqual(k_RemoteLocationId, loadedId);
		}
	}
}