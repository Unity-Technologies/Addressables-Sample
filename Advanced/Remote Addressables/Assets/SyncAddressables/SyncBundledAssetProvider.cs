using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;

[DisplayName("Sync Asset Provider")]
public class SyncBundledAssetProvider : BundledAssetProvider
{
    
        internal class InternalOp
        {
            internal static AssetBundle LoadBundleFromDependecies(IList<object> results)
            {
                if (results == null || results.Count == 0)
                    return null;

                AssetBundle bundle = null;
                bool firstBundleWrapper = true;
                for (int i = 0; i < results.Count; i++)
                {
                    var abWrapper = results[i] as IAssetBundleResource;
                    if (abWrapper != null)
                    {
                        //only use the first asset bundle, even if it is invalid
                        var b = abWrapper.GetAssetBundle();
                        if (firstBundleWrapper)
                            bundle = b;
                        firstBundleWrapper = false;
                    }
                }
                return bundle;
            }
            
            public void Start(ProvideHandle provideHandle)
            {
                Type t = provideHandle.Type;
                List<object> deps = new List<object>();
                provideHandle.GetDependencies(deps);
                AssetBundle bundle = LoadBundleFromDependecies(deps);
                if (bundle == null)
                {
                    provideHandle.Complete<AssetBundle>(null, false, new Exception("Unable to load dependent bundle from location " + provideHandle.Location));
                    return;
                }
                
                object result = null;
                if (t.IsArray)
                    result = bundle.LoadAssetWithSubAssets(provideHandle.Location.InternalId, t.GetElementType());
                else if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
                    result = bundle.LoadAssetWithSubAssets(provideHandle.Location.InternalId, t.GetElementType()).ToList();
                else
                    result = bundle.LoadAsset(provideHandle.Location.InternalId, t);

                provideHandle.Complete(result, result != null, null);
            }

        }
    
    public override void Provide(ProvideHandle provideHandle)
    {
        new InternalOp().Start(provideHandle);   
    }
}
