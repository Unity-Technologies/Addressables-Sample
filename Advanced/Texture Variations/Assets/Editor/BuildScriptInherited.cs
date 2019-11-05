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

[CreateAssetMenu(fileName = "BuildScriptInherited.asset", menuName = "Addressables/Custom Build/Packed Variations")]
public class BuildScriptInherited : BuildScriptPackedMode
{
    public override string Name
    {
        get { return "Packed Variations"; }
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
        {
            var errorString = ProcessTextureScaler(assetGroup.GetSchema<TextureVariationSchema>(), assetGroup, aaContext);
            if (!string.IsNullOrEmpty(errorString))
                return errorString;
        }

        return base.ProcessGroup(assetGroup, aaContext);
    }

    List<AddressableAssetGroup> m_SourceGroupList = new List<AddressableAssetGroup>();
    Dictionary<string, AddressableAssetGroup> m_GeneratedGroups = new Dictionary<string, AddressableAssetGroup>();

    
    AddressableAssetGroup FindOrCopyGroup(string groupName, AddressableAssetGroup baseGroup, AddressableAssetSettings settings, TextureVariationSchema schema)
    {
        AddressableAssetGroup result;
        if (!m_GeneratedGroups.TryGetValue(groupName, out result))
        {
            List<AddressableAssetGroupSchema> schemas = new List<AddressableAssetGroupSchema>(baseGroup.Schemas);
            schemas.Remove(schema);
            result = settings.CreateGroup(groupName, false, false, false, schemas);
            m_GeneratedGroups.Add(groupName, result);
        }

        return result;
    }
    
    string ProcessTextureScaler(
        TextureVariationSchema schema,
        AddressableAssetGroup assetGroup,
        AddressableAssetsBuildContext aaContext)
    {
        m_SourceGroupList.Add(assetGroup);

        var entries = new List<AddressableAssetEntry>(assetGroup.entries);
        foreach (var entry in entries)
        {
            var entryPath = entry.AssetPath;
            if (AssetDatabase.GetMainAssetTypeAtPath(entryPath) == typeof(Texture2D))
            {
                var fileName = Path.GetFileNameWithoutExtension(entryPath);
                if(string.IsNullOrEmpty(fileName))
                    return "Failed to get file name for: " + entryPath;
                if (!Directory.Exists("Assets/GeneratedTextures"))
                    Directory.CreateDirectory("Assets/GeneratedTextures");
                if (!Directory.Exists("Assets/GeneratedTextures/Texture"))
                    Directory.CreateDirectory("Assets/GeneratedTextures/Texture");
                
                var sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(entryPath); 
                var aiSource = AssetImporter.GetAtPath(entryPath) as TextureImporter;
                int maxDim = Math.Max(sourceTex.width, sourceTex.height);

                foreach (var pair in schema.Variations)
                {
                    var newGroup = FindOrCopyGroup(assetGroup.Name + "_" + pair.label, assetGroup, aaContext.settings, schema);
                    var newFile = entryPath.Replace(fileName, fileName+"_variationCopy_" + pair.label);
                    newFile = newFile.Replace("Assets/", "Assets/GeneratedTextures/");

                    AssetDatabase.CopyAsset(entryPath, newFile);
                
                    var aiDest = AssetImporter.GetAtPath(newFile) as TextureImporter;
                    if (aiDest == null)
                    {
                        var message = "failed to get TextureImporter on new texture asset: " + newFile;
                        return message;
                    }
                    
                    float scaleFactor = pair.textureScale;

                    float desiredLimiter = maxDim * scaleFactor;
                    aiDest.maxTextureSize = NearestMaxTextureSize(desiredLimiter);

                    aiDest.isReadable = true;

                    aiDest.SaveAndReimport();
                    var newEntry = aaContext.settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(newFile), newGroup);
                    newEntry.address = entry.address;
                    newEntry.SetLabel(pair.label, true);
                }
                entry.SetLabel(schema.BaselineLabel, true);
            }
        }

        return string.Empty;
    }

    static int NearestMaxTextureSize(float desiredLimiter)
    {
        float lastDiff = Math.Abs(desiredLimiter);
        int lastPow = 32;
        for (int i = 0; i < 9; i++)
        {

            int powOfTwo = lastPow << 1;
            float newDiff = Math.Abs(desiredLimiter - powOfTwo);
            if (newDiff > lastDiff)
                return lastPow;

            lastPow = powOfTwo;
            lastDiff = newDiff;

        }

        return 8192;
    }
    
    void DoCleanup(AddressableAssetSettings settings)
    {
        List<string> directories = new List<string>();
        foreach (var group in m_GeneratedGroups.Values)
        {
            List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(group.entries);
            foreach (var entry in entries)
            {
                var path = entry.AssetPath;
                AssetDatabase.DeleteAsset(path);
            }

            settings.RemoveGroup(group);
        }
        m_GeneratedGroups.Clear();

        foreach (var group in m_SourceGroupList)
        {
            var schema = group.GetSchema<TextureVariationSchema>();
            if (schema == null)
                continue;

            foreach (var entry in group.entries)
            {
                entry.labels.Remove(schema.BaselineLabel);
            }
        }
        
        m_SourceGroupList.Clear();
    }
}
