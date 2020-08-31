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
using UnityEngine.ResourceManagement.ResourceProviders.Simulation;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BuildScriptInheritedVirtualMode.asset", menuName = "Addressables/Custom Build/Variant Simulate Groups (advanced)")]
public class BuildScriptInheritedVirtualMode : BuildScriptVirtualMode
{
    public override string Name
    {
        get { return "Variant Simulate Groups (advanced)"; }
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

        if (assetGroup.HasSchema<PrefabTextureVariantSchema>())
            ProcessVariants(assetGroup.GetSchema<PrefabTextureVariantSchema>(), assetGroup, aaContext);

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

    void ProcessVariants(PrefabTextureVariantSchema schema,
        AddressableAssetGroup group,
        AddressableAssetsBuildContext context)
    {
        m_SourceGroupList.Add(group);

        var entries = new List<AddressableAssetEntry>(group.entries);
        foreach (var entry in entries)
        {
            entry.SetLabel(schema.DefaultLabel, true, true, false);

            foreach (var variant in schema.Variants)
                entry.SetLabel(variant.Label, true, true, false);
        }
    }

    void DoCleanup(AddressableAssetSettings settings)
    {
        foreach (var group in m_SourceGroupList)
        {
            if (group.HasSchema<TextureVariationSchema>())
            {
                var schema = group.GetSchema<TextureVariationSchema>();

                foreach (var entry in group.entries)
                {
                    entry.labels.Remove(schema.BaselineLabel);
                    foreach (var pair in schema.Variations)
                    {
                        entry.labels.Remove(pair.label);
                    }
                }
            }

            if (group.HasSchema<PrefabTextureVariantSchema>())
            {
                var schema = group.GetSchema<PrefabTextureVariantSchema>();
                foreach (var entry in group.entries)
                {
                    entry.labels.Remove(schema.DefaultLabel);
                    foreach (var variant in schema.Variants)
                        entry.labels.Remove(variant.Label);
                }
            }
        }

        m_SourceGroupList.Clear();
    }
}
