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

[CreateAssetMenu(fileName = "BuildScriptInheritedFastMode.asset", menuName = "Addressables/Custom Build/Fast Mode Variations")]
public class BuildScriptInheritedFastMode : BuildScriptFastMode
{
    public override string Name
    {
        get { return "Vary Fast"; }
    }

    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context)
    {
        var result = base.BuildDataImplementation<TResult>(context);

        AddressableAssetSettings settings = context.AddressableSettings;
        DoCleanup(settings);
        return result;
    }

    protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext)
    {
        if (assetGroup.HasSchema<TextureVariationSchema>())
            ProcessTextureScaler(assetGroup.GetSchema<TextureVariationSchema>(), assetGroup, aaContext);
        
        return base.ProcessGroup(assetGroup, aaContext);
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
