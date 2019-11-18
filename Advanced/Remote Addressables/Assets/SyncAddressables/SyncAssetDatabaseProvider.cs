#if UNITY_EDITOR
using System.ComponentModel;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

[DisplayName("Sync AD Provider")]
public class SyncAssetDatabaseProvider : AssetDatabaseProvider
{
    public SyncAssetDatabaseProvider()
        : base(-1)
    {
        
    }
}
#endif

