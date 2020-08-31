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

[InitializeOnLoad]
[CreateAssetMenu(fileName = "BuildScriptInheritedFastMode.asset", menuName = "Addressables/Custom Build/Variant Use Asset Database (fastest)")]
public class BuildScriptInheritedFastMode : BuildScriptFastMode
{
    BuildScriptInheritedFastMode()
    {
        EditorApplication.playModeStateChanged -= PlayModeStateChanged;
        EditorApplication.playModeStateChanged += PlayModeStateChanged;
    }

    public override string Name
    {
        get { return "Variant Use Asset Database (fastest)"; }
    }

    private bool m_UsingVariantFastMode = false;
    private AddressableAssetSettings m_Settings;
    protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context)
    {
        m_Settings = context.AddressableSettings;
        foreach (var group in m_Settings.groups)
            ProcessGroup(group, null);

        var result = base.BuildDataImplementation<TResult>(context);
        m_UsingVariantFastMode = true;
        return result;
    }

    void PlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && m_UsingVariantFastMode)
        {
            DoCleanup(m_Settings);
            m_UsingVariantFastMode = false;
        }
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
