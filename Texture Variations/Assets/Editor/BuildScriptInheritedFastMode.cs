using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BuildScriptInheritedFastMode.asset", menuName = "Addressable Assets/Data Builders/Fast Mode Variations")]
public class BuildScriptInheritedFastMode : BuildScriptFastMode
{
    public override string Name
    {
        get { return "Speedy Spice"; }
    }

    protected override TResult BuildDataInternal<TResult>(IDataBuilderContext context)
    {
        var result = base.BuildDataInternal<TResult>(context);
        
        AddressableAssetSettings settings = context.GetValue<AddressableAssetSettings>(AddressablesBuildDataBuilderContext.BuildScriptContextConstants.kAddressableAssetSettings);
        DoCleanup(settings);
        return result;
    }

    protected override void ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext, ref bool needsLegacyProvider)
    {
        if (assetGroup.HasSchema<TextureVariationSchema>())
        {
            ProcessTextureScaler(assetGroup.GetSchema<TextureVariationSchema>(), assetGroup, aaContext);
        }
        base.ProcessGroup(assetGroup, aaContext, ref needsLegacyProvider);
    }

    List<AddressableAssetGroup> m_SourceGroupList = new List<AddressableAssetGroup>();

    void ProcessTextureScaler(TextureVariationSchema schema, AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
    {
       m_SourceGroupList.Add(assetGroup);
    
       var entries = new List<AddressableAssetEntry>(assetGroup.entries);
       foreach (var entry in entries)
       {
           foreach (var pair in schema.Variations)
           {
               entry.SetLabel(pair.label, true);
           }
           entry.SetLabel(schema.BaselineLabel, true);
       }
    }
    
    void DoCleanup(AddressableAssetSettings settings)
    {
       foreach (var group in m_SourceGroupList)
       {
           var schema = group.GetSchema<TextureVariationSchema>();
           if (schema == null)
               continue;
    
           foreach (var entry in group.entries)
           {
               entry.labels.Remove(schema.BaselineLabel);
               foreach (var pair in schema.Variations)
               {
                   entry.labels.Remove(pair.label);
               }
           }
       }
       
       m_SourceGroupList.Clear();
    }
}
