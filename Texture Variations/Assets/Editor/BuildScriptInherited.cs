using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

[CreateAssetMenu(fileName = "BuildScriptInherited.asset", menuName = "Addressable Assets/Data Builders/Variations")]
public class BuildScriptInherited : BuildScriptPackedMode
{
    public override string Name
    {
        get { return "Spice Of Life"; }
    }

    protected override void ProcessGroup(
        AddressableAssetGroup assetGroup, 
        AddressableAssetsBuildContext aaContext, 
        List<ObjectInitializationData> resourceProviderData, 
        List<AssetBundleBuild> allBundleInputDefs, 
        HashSet<string> createdProviderIds)
    {
        if (assetGroup.HasSchema<TextureVariationSchema>())
            ProcessTextureScaler(assetGroup.GetSchema<TextureVariationSchema>(), assetGroup, aaContext);
        
        
        base.ProcessGroup(assetGroup, aaContext, resourceProviderData, allBundleInputDefs, createdProviderIds);
    }

    void ProcessTextureScaler(
        TextureVariationSchema schema,
        AddressableAssetGroup assetGroup,
        AddressableAssetsBuildContext aaContext)
    {
            var scaler = 0.5f;// schema.TextureScale;
            List<string> texturePaths = new List<string>();

            var entries = new List<AddressableAssetEntry>(assetGroup.entries);
            foreach (var entry in entries)
            {
                var entryPath = entry.AssetPath;
                if (AssetDatabase.GetMainAssetTypeAtPath(entryPath) == typeof(Texture2D))
                {
                    var fileName = Path.GetFileNameWithoutExtension(entryPath);
                    var newFile = entryPath.Replace(fileName, fileName+"_variationCopy");
                    newFile = newFile.Replace("Assets/", "Assets/GeneratedTextures/");
                    texturePaths.Add(newFile);

                    if (!Directory.Exists("Assets/GeneratedTextures"))
                        Directory.CreateDirectory("Assets/GeneratedTextures");
                    if (!Directory.Exists("Assets/GeneratedTextures/Texture"))
                        Directory.CreateDirectory("Assets/GeneratedTextures/Texture");

                   
                    AssetDatabase.CopyAsset(entryPath, newFile);
                    
                    var aiSource = AssetImporter.GetAtPath(entryPath) as TextureImporter;
                    var sourceTex = AssetDatabase.LoadAssetAtPath<Texture2D>(entryPath); 
                    var aiDest = AssetImporter.GetAtPath(newFile) as TextureImporter;

                    float scaleFactor = 0.05f;
               
                    int maxDim = Math.Max(sourceTex.width, sourceTex.height);

                    float desiredLimiter = maxDim * scaleFactor;
                    aiDest.maxTextureSize = NearestMaxTextureSize(desiredLimiter);
                    
                    
                    aiDest.isReadable = true;

                    aiDest.SaveAndReimport();
                    var newEntry = aaContext.settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(newFile), assetGroup);
                    newEntry.address = entry.address;
                    entry.SetLabel("HD", true);
                    newEntry.SetLabel("SD", true);
                }
            }  
    }
    
    int NearestMaxTextureSize(float desiredLimiter)
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
}
