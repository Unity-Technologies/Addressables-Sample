using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UnityEditor.AddressableAssets.Build
{
    [Serializable]
    struct AssetState : IEquatable<AssetState>
    {
        public GUID guid;
        public Hash128 hash;

        public bool Equals(AssetState other)
        {
            return guid == other.guid && hash == other.hash;
        }
    }

    [Serializable]
    class CachedAssetState : IEquatable<CachedAssetState>
    {
        public AssetState asset;
        public AssetState[] dependencies;

        public bool Equals(CachedAssetState other)
        {
            bool result = other != null && asset.Equals(other.asset);
            result &= dependencies != null && other.dependencies != null;
            result &= dependencies.Length == other.dependencies.Length;
            var index = 0;
            while (result && index < dependencies.Length)
            {
                result &= dependencies[index].Equals(other.dependencies[index]);
                index++;
            }
            return result;
        }
    }


    /// <summary>
    /// Data stored with each build that is used to generated content updates.
    /// </summary>
    [Serializable]
    class AddressablesContentState
    {
        /// <summary>
        /// The version that the player was built with.  This is usually set to AddressableAssetSettings.PlayerBuildVersion.
        /// </summary>
        [SerializeField]
        public string playerVersion;

        /// <summary>
        /// The version of the unity editor used to build the player.
        /// </summary>
        [SerializeField]
        public string editorVersion;

        /// <summary>
        /// Dependency information for all assets in the build that have been marked StaticContent.
        /// </summary>
        [SerializeField]
        public CachedAssetState[] cachedInfos;

        /// <summary>
        /// The path of a remote catalog.  This is the only place the player knows to look for an updated catalog.
        /// </summary>
        [SerializeField]
        public string remoteCatalogLoadPath;
    }

    /// <summary>
    /// Contains methods used for the content update workflow.
    /// </summary>
    public static class ContentUpdateScript
    {
        static bool GetAssetState(GUID asset, out AssetState assetState)
        {
            assetState = new AssetState();
            if (asset.Empty())
                return false;

            var path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path))
                return false;

            var hash = AssetDatabase.GetAssetDependencyHash(path);
            if (!hash.isValid)
                return false;

            assetState.guid = asset;
            assetState.hash = hash;
            return true;
        }

        static bool GetCachedAssetStateForData(GUID asset, IEnumerable<GUID> dependencies, out CachedAssetState cachedAssetState)
        {
            cachedAssetState = null;

            AssetState assetState;
            if (!GetAssetState(asset, out assetState))
                return false;

            var visited = new HashSet<GUID>();
            visited.Add(asset);
            var dependencyStates = new List<AssetState>();
            foreach (var dependency in dependencies)
            {
                if (!visited.Add(dependency))
                    continue;

                AssetState dependencyState;
                if (!GetAssetState(dependency, out dependencyState))
                    continue;
                dependencyStates.Add(dependencyState);
            }

            cachedAssetState = new CachedAssetState();
            cachedAssetState.asset = assetState;
            cachedAssetState.dependencies = dependencyStates.ToArray();
            return true;
        }

        static bool HasAssetOrDependencyChanged(CachedAssetState cachedInfo)
        {
            CachedAssetState newCachedInfo;
            if (!GetCachedAssetStateForData(cachedInfo.asset.guid, cachedInfo.dependencies.Select(x => x.guid), out newCachedInfo))
                return true;
            return !cachedInfo.Equals(newCachedInfo);
        }

        /// <summary>
        /// Save the content update information for a set of AddressableAssetEntry objects.
        /// </summary>
        /// <param name="path">File to write content stat info to.  If file already exists, it will be deleted before the new file is created.</param>
        /// <param name="entries">The entries to save.</param>
        /// <param name="dependencyData">The raw dependency information generated from the build.</param>
        /// <param name="playerVersion">The player version to save. This is usually set to AddressableAssetSettings.PlayerBuildVersion.</param>
        /// <param name="remoteCatalogPath">The server path (if any) that contains an updateable content catalog.  If this is empty, updates cannot occur.</param>
        /// <returns>True if the file is saved, false otherwise.</returns>
        public static bool SaveContentState(string path, List<AddressableAssetEntry> entries, IDependencyData dependencyData, string playerVersion, string remoteCatalogPath)
        {
            try
            {
                IList<CachedAssetState> cachedInfos = new List<CachedAssetState>();
                foreach (var assetData in dependencyData.AssetInfo)
                {
                    CachedAssetState cachedAssetState;
                    if (GetCachedAssetStateForData(assetData.Key, assetData.Value.referencedObjects.Select(x => x.guid), out cachedAssetState))
                        cachedInfos.Add(cachedAssetState);
                }
                foreach (var sceneData in dependencyData.SceneInfo)
                {
                    CachedAssetState cachedAssetState;
                    if (GetCachedAssetStateForData(sceneData.Key, sceneData.Value.referencedObjects.Select(x => x.guid), out cachedAssetState))
                        cachedInfos.Add(cachedAssetState);
                }

                var cacheData = new AddressablesContentState
                {
                    cachedInfos = cachedInfos.ToArray(),
                    playerVersion = playerVersion,
                    editorVersion = Application.unityVersion,
                    remoteCatalogLoadPath = remoteCatalogPath
                };
                var formatter = new BinaryFormatter();
                if (File.Exists(path))
                    File.Delete(path);
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
                formatter.Serialize(stream, cacheData);
                stream.Flush();
                stream.Close();
                stream.Dispose();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        /// <summary>
        /// Gets the path of the cache data from a selected build.
        /// </summary>
        /// <param name="browse">If true, the user is allowed to browse for a specific file.</param>
        /// <returns></returns>
        public static string GetContentStateDataPath(bool browse)
        {
            var assetPath = AddressableAssetSettingsDefaultObject.kDefaultConfigFolder;
            if (AddressableAssetSettingsDefaultObject.Settings != null)
                assetPath = AddressableAssetSettingsDefaultObject.Settings.ConfigFolder;
            assetPath = Path.Combine(assetPath, PlatformMappingService.GetPlatform().ToString());

            if (browse)
            {
                if (string.IsNullOrEmpty(assetPath))
                    assetPath = Application.dataPath;

                assetPath = EditorUtility.OpenFilePanel("Build Data File", Path.GetDirectoryName(assetPath), "bin");

                if (string.IsNullOrEmpty(assetPath))
                    return null;

                return assetPath;
            }

            Directory.CreateDirectory(assetPath);
            var path = Path.Combine(assetPath, "addressables_content_state.bin");
            return path;
        }

        /// <summary>
        /// Loads cache data from a specific location
        /// </summary>
        /// <param name="contentStateDataPath"></param>
        /// <returns></returns>
        internal static AddressablesContentState LoadContentState(string contentStateDataPath)
        {
            if (string.IsNullOrEmpty(contentStateDataPath))
            {
                Debug.LogErrorFormat("Unable to load cache data from {0}.", contentStateDataPath);
                return null;
            }
            var stream = new FileStream(contentStateDataPath, FileMode.Open, FileAccess.Read);
            var formatter = new BinaryFormatter();
            var cacheData = formatter.Deserialize(stream) as AddressablesContentState;
            if (cacheData == null)
            {
                Addressables.LogError("Invalid hash data file.  This file is usually named addressables_content_state.bin and is saved in the same folder as your source AddressableAssetsSettings.asset file.");
                return null;
            }
            return cacheData;
        }

        static bool s_StreamingAssetsExists;
        static string kStreamingAssetsPath = "Assets/StreamingAssets";

        internal static void Cleanup(bool deleteStreamingAssetsFolderIfEmpty)
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                Directory.Delete(Addressables.BuildPath, true);
                if (File.Exists(Addressables.BuildPath + ".meta"))
                    File.Delete(Addressables.BuildPath + ".meta");
            }
            if (deleteStreamingAssetsFolderIfEmpty)
            {
                if (Directory.Exists(kStreamingAssetsPath))
                {
                    var files = Directory.GetFiles(kStreamingAssetsPath);
                    if (files.Length == 0)
                    {
                        Directory.Delete(kStreamingAssetsPath);
                        if (File.Exists(kStreamingAssetsPath + ".meta"))
                            File.Delete(kStreamingAssetsPath + ".meta");

                    }
                }
            }
        }

        /// <summary>
        /// Builds player content using the player content version from a specified cache file.
        /// </summary>
        /// <param name="settings">The settings object to use for the build.</param>
        /// <param name="contentStateDataPath">The path of the cache data to use.</param>
        /// <returns>The build operation.</returns>
        public static AddressablesPlayerBuildResult BuildContentUpdate(AddressableAssetSettings settings, string contentStateDataPath)
        {
            var cacheData = LoadContentState(contentStateDataPath);

            if (!IsCacheDataValid(settings, cacheData))
                return null;

            s_StreamingAssetsExists = Directory.Exists("Assets/StreamingAssets");
            var context = new AddressablesDataBuilderInput(settings, cacheData.playerVersion);

            Cleanup(!s_StreamingAssetsExists);

            SceneManagerState.Record();
            var result = settings.ActivePlayerDataBuilder.BuildData<AddressablesPlayerBuildResult>(context);
            if (!string.IsNullOrEmpty(result.Error))
                Debug.LogError(result.Error);
            SceneManagerState.Restore();
            return result;
        }

        internal static bool IsCacheDataValid(AddressableAssetSettings settings, AddressablesContentState cacheData)
        {
            if (cacheData == null)
                return false;

            if (cacheData.editorVersion != Application.unityVersion)
                Addressables.LogWarningFormat("Building content update with Unity editor version `{0}`, data was created with version `{1}`.  This may result in incompatible data.", Application.unityVersion, cacheData.editorVersion);

            if (string.IsNullOrEmpty(cacheData.remoteCatalogLoadPath))
            {
                Addressables.LogError("Previous build had 'Build Remote Catalog' disabled.  You cannot update a player that has no remote catalog specified");
                return false;
            }
            if (!settings.BuildRemoteCatalog)
            {
                Addressables.LogError("Current settings have 'Build Remote Catalog' disabled.  You cannot update a player that has no remote catalog to look to.");
                return false;
            }

            if (cacheData.remoteCatalogLoadPath != settings.RemoteCatalogLoadPath.GetValue(settings))
            {
                Addressables.LogErrorFormat("Current 'Remote Catalog Load Path' does not match load path of original player.  Player will only know to look up catalog at original location. Original: {0}  Current: {1}", cacheData.remoteCatalogLoadPath, settings.RemoteCatalogLoadPath.GetValue(settings));
                return false;
            }

            return true;
        }

        internal static List<AddressableAssetEntry> GatherModifiedEntries(AddressableAssetSettings settings, string cacheDataPath)
        {
            var cacheData = LoadContentState(cacheDataPath);
            if (cacheData == null)
            {
                return null;
            }

            var allEntries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(allEntries, g => g.HasSchema<BundledAssetGroupSchema>() && g.GetSchema<ContentUpdateGroupSchema>().StaticContent);

            var entryToCacheInfo = new Dictionary<string, CachedAssetState>();
            foreach (var cacheInfo in cacheData.cachedInfos)
                if (cacheInfo != null)
                    entryToCacheInfo[cacheInfo.asset.guid.ToString()] = cacheInfo;
            var modifiedEntries = new List<AddressableAssetEntry>();
            foreach (var entry in allEntries)
            {
                CachedAssetState cachedInfo;
                if (!entryToCacheInfo.TryGetValue(entry.guid, out cachedInfo) || HasAssetOrDependencyChanged(cachedInfo))
                    modifiedEntries.Add(entry);
            }
            return modifiedEntries;
        }

        /// <summary>
        /// Create a new AddressableAssetGroup with the items and mark it as remote.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="items">The items to move.</param>
        /// <param name="groupName">The name of the new group.</param>
        public static void CreateContentUpdateGroup(AddressableAssetSettings settings, List<AddressableAssetEntry> items, string groupName)
        {
            var contentGroup = settings.CreateGroup(settings.FindUniqueGroupName(groupName), false, false, true, null);
            var schema = contentGroup.AddSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kRemoteLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            contentGroup.AddSchema<ContentUpdateGroupSchema>().StaticContent = false;
            settings.MoveEntries(items, contentGroup);
        }

    }


}