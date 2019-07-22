#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

public class SyncAssetDatabaseProvider : AssetDatabaseProvider
{
    public SyncAssetDatabaseProvider()
        : base(-1)
    {
        
    }
}
#endif

