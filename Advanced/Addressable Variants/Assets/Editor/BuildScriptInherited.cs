using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BuildScriptInherited.asset", menuName = "Addressables/Custom Build/Packed Variations")]
public class BuildScriptInherited : BuildScriptPackedMode, ISerializationCallbackReceiver
{
    public override string Name
    {
        get { return "Packed Variations"; }
    }

    Dictionary<string, Hash128> m_AssetPathToHashCode = new Dictionary<string, Hash128>();
    HashSet<string> m_DirectoriesInUse = new HashSet<string>();
    string m_BaseDirectory = "Assets/AddressablesGenerated";

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

        if (assetGroup.HasSchema<PrefabTextureVariantSchema>())
        {
            var errorString = ProcessVariants(assetGroup.GetSchema<PrefabTextureVariantSchema>(), assetGroup, aaContext);
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

    string ProcessVariants(PrefabTextureVariantSchema schema, 
        AddressableAssetGroup group,
        AddressableAssetsBuildContext context)
    {
        var settings = context.Settings;
        Directory.CreateDirectory(m_BaseDirectory);

        var entries = new List<AddressableAssetEntry>(group.entries);
        foreach (var mainEntry in entries)
        {
            if (AssetDatabase.GetMainAssetTypeAtPath(mainEntry.AssetPath) != typeof(GameObject))
                continue;

            string fileName = Path.GetFileNameWithoutExtension(mainEntry.AssetPath);
            mainEntry.SetLabel(schema.DefaultLabel, true, true);
            
            string mainAssetPath = AssetDatabase.GUIDToAssetPath(mainEntry.guid);
            Hash128 assetHash = AssetDatabase.GetAssetDependencyHash(mainAssetPath);

            bool assetHashChanged = false;
            if (!m_AssetPathToHashCode.ContainsKey(mainAssetPath))
                m_AssetPathToHashCode.Add(mainAssetPath, assetHash);
            else if (m_AssetPathToHashCode[mainAssetPath] != assetHash)
            {
                assetHashChanged = true;
                m_AssetPathToHashCode[mainAssetPath] = assetHash;
            }

            foreach (var variant in schema.Variants)
            {
                string groupDirectory = Path.Combine(m_BaseDirectory, $"{group.Name}-{Path.GetFileNameWithoutExtension(mainEntry.address)}").Replace('\\', '/');
                string variantDirectory = Path.Combine(groupDirectory, variant.Label).Replace('\\', '/');
                m_DirectoriesInUse.Add(groupDirectory);
                m_DirectoriesInUse.Add(variantDirectory);
                Directory.CreateDirectory(variantDirectory);

                var variantGroup = CreateTemporaryGroupCopy($"{group.Name}_VariantGroup_{variant.Label}", group.Schemas, settings);

                string newPrefabPath = mainAssetPath.Replace("Assets/", variantDirectory + '/').Replace(fileName, $"{fileName}_variant_{variant.Label}");
                if (assetHashChanged || !File.Exists(newPrefabPath))
                    AssetDatabase.CopyAsset(mainAssetPath, newPrefabPath);

                var dependencies = AssetDatabase.GetDependencies(newPrefabPath);
                foreach (var dependency in dependencies)
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(dependency) == typeof(GameObject))
                    {
                        var gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(dependency);
                        foreach (var childRender in gameObject.GetComponentsInChildren<MeshRenderer>())
                            ConvertToVariant(childRender, variantDirectory, variant, assetHashChanged);
                    }
                }

                var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(newPrefabPath), variantGroup, false, false);
                entry.address = mainEntry.address;
                entry.SetLabel(variant.Label, true, true, false);
            }
        }

        return string.Empty;
    }

    void ConvertToVariant(MeshRenderer meshRenderer, string variantDirectory, PrefabTextureVariantSchema.VariantLabelPair variant, bool assetHashChanged)
    {
        if (meshRenderer != null && meshRenderer.sharedMaterial != null)
        {
            var mat = CreateOrGetVariantMaterial(meshRenderer.sharedMaterial, variantDirectory, variant.Label, assetHashChanged);
            var texture = CreateOrGetVariantTexture(meshRenderer.sharedMaterial.mainTexture,
                variantDirectory, variant.Label, variant.TextureScale, assetHashChanged);

            mat.mainTexture = texture;
            meshRenderer.sharedMaterial = mat;
            AssetDatabase.SaveAssets();
        }
    }

    AddressableAssetGroup CreateTemporaryGroupCopy(string groupName, List<AddressableAssetGroupSchema> schemas, AddressableAssetSettings settings)
    {
        var variantGroup = settings.CreateGroup(groupName, false, false, false, schemas);
        if(variantGroup.HasSchema<PrefabTextureVariantSchema>())
            variantGroup.RemoveSchema<PrefabTextureVariantSchema>();
        if (!m_GeneratedGroups.ContainsKey(variantGroup.Name))
            m_GeneratedGroups.Add(variantGroup.Name, variantGroup);
        return variantGroup;
    }

    Material CreateOrGetVariantMaterial(Material baseMaterial, string variantDirectory, string label, bool assetHashChanged)
    {
        string assetPath = AssetDatabase.GetAssetPath(baseMaterial);
        if(assetPath.StartsWith(variantDirectory))
            return AssetDatabase.LoadAssetAtPath<Material>(assetPath);

        string matFileName = Path.GetFileNameWithoutExtension(assetPath);
        string path = assetPath.Replace("Assets/", variantDirectory + '/').Replace(matFileName, $"{matFileName}_{label}");
        if (assetHashChanged || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CopyAsset(assetPath, path);
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        m_DirectoriesInUse.Add(Path.GetDirectoryName(path).Replace('\\', '/'));
        return mat;
    }

    Texture2D CreateOrGetVariantTexture(Texture baseTexture, string variantDirectory, string label, float scale, bool assetHashChanged)
    {
        string assetPath = AssetDatabase.GetAssetPath(baseTexture);
        if (assetPath.StartsWith(variantDirectory))
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

        string textureFileName = Path.GetFileNameWithoutExtension(assetPath);
        string path = assetPath.Replace("Assets/", variantDirectory + '/').Replace(textureFileName, $"{textureFileName}_{label}");

        if (assetHashChanged || !File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CopyAsset(assetPath, path);

            var srcTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            int maxDim = Math.Max(srcTexture.width, srcTexture.height);
            var aiDest = AssetImporter.GetAtPath(path) as TextureImporter;
            if (aiDest == null)
                return null;

            float scaleFactor = scale;
            float desiredLimiter = maxDim * scaleFactor;
            aiDest.maxTextureSize = NearestMaxTextureSize(desiredLimiter);
            aiDest.isReadable = true;
            aiDest.SaveAndReimport();
        }
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        m_DirectoriesInUse.Add(Path.GetDirectoryName(path).Replace('\\', '/'));
        return texture;

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
                    var newGroup = FindOrCopyGroup(assetGroup.Name + "_" + pair.label, assetGroup, aaContext.Settings, schema);
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
                    var newEntry = aaContext.Settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(newFile), newGroup);
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
            if (group.HasSchema<TextureVariationSchema>())
            {
                List<AddressableAssetEntry> entries = new List<AddressableAssetEntry>(group.entries);
                foreach (var entry in entries)
                {
                    var path = entry.AssetPath;
                    AssetDatabase.DeleteAsset(path); 
                }
            }

            settings.RemoveGroup(group);
            if (Directory.Exists(m_BaseDirectory) && group.HasSchema<PrefabTextureVariantSchema>())
            {
                foreach (var directory in Directory.EnumerateDirectories(m_BaseDirectory, "*", SearchOption.AllDirectories))
                {
                    string formattedDirectory = directory.Replace('\\', '/');
                    if (m_DirectoriesInUse.Contains(formattedDirectory))
                        continue;
                    Directory.Delete(formattedDirectory, true);
                }

            }

        }

        m_DirectoriesInUse.Clear();
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

    [SerializeField]
    List<string> m_AssetPathToHashKey = new List<string>();
    [SerializeField]
    List<Hash128> m_AssetPathToHashValue = new List<Hash128>();

    public void OnBeforeSerialize()
    {
        foreach (var key in m_AssetPathToHashCode.Keys)
        {
            m_AssetPathToHashKey.Add(key);
            m_AssetPathToHashValue.Add(m_AssetPathToHashCode[key]);
        }
    }

    public void OnAfterDeserialize()
    {
        for (int i = 0; i < Math.Min(m_AssetPathToHashKey.Count, m_AssetPathToHashValue.Count); i++)
        {
            if(!m_AssetPathToHashCode.ContainsKey(m_AssetPathToHashKey[i]))
                m_AssetPathToHashCode.Add(m_AssetPathToHashKey[i], m_AssetPathToHashValue[i]);
        }
    }
}
