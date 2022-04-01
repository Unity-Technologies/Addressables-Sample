using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

namespace AddressablesPlayAssetDelivery.Editor
{
#if UNITY_2021_2_OR_NEWER
    /// <summary>
    /// Moves custom asset pack data from their default build location <see cref="BuildScriptPlayAssetDelivery"/> to their correct player build data location.
    /// For an Android App Bundle, bundles assigned to a custom asset pack must be located in their {asset pack name}.androidpack directory in the Assets folder.
    /// The 'CustomAssetPacksData.json' file is also added to the built player's StreamingAssets file location.
    ///
    /// This script executes before the <see cref="AddressablesPlayerBuildProcessor"/> which moves all Addressables data to StreamingAssets.
    public class PlayAssetDeliveryBuildProcessor : BuildPlayerProcessor
    {
        /// <summary>
        /// Returns the player build processor callback order.
        /// </summary>
        public override int callbackOrder
        {
            get { return 0; }
        }

        /// <summary>
        /// Invoked before performing a Player build. Moves AssetBundles to their correct data location based on the build target platform.
        /// </summary>
        /// <param name="buildPlayerContext">Contains data related to the player.</param>
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android && EditorUserBuildSettings.buildAppBundle)
                MoveDataForAppBundleBuild(buildPlayerContext);
            else
                MoveDataToDefaultLocation();
        }

        /// <summary>
        /// Move custom asset pack data from their build location to their App Bundle data location.
        /// </summary>
        /// <param name="buildPlayerContext">Contains data related to the player.</param>
        public static void MoveDataForAppBundleBuild(BuildPlayerContext buildPlayerContext)
        {
            try
            {
                buildPlayerContext.AddAdditionalPathToStreamingAssets(CustomAssetPackUtility.CustomAssetPacksDataEditorPath, CustomAssetPackUtility.kCustomAssetPackDataFilename);

                AssetDatabase.StartAssetEditing();
                if (File.Exists(CustomAssetPackUtility.BuildProcessorDataPath))
                {
                    string contents = File.ReadAllText(CustomAssetPackUtility.BuildProcessorDataPath);
                    var data =  JsonUtility.FromJson<BuildProcessorData>(contents);

                    foreach (BuildProcessorDataEntry entry in data.Entries)
                    {
                        string assetsFolderPath = Path.Combine(CustomAssetPackUtility.PackContentRootDirectory, entry.AssetsSubfolderPath);
                        if (File.Exists(entry.BundleBuildPath))
                        {
                            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(entry.BundleBuildPath);
                            File.Move(entry.BundleBuildPath, assetsFolderPath);
                            File.Delete(metaFilePath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception occured when moving data for an app bundle build: {e.Message}.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Move custom asset pack data from their App Bundle data location to to their build location.
        /// </summary>
        public static void MoveDataToDefaultLocation()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                if (File.Exists(CustomAssetPackUtility.BuildProcessorDataPath))
                {
                    string contents = File.ReadAllText(CustomAssetPackUtility.BuildProcessorDataPath);
                    var data =  JsonUtility.FromJson<BuildProcessorData>(contents);

                    foreach (BuildProcessorDataEntry entry in data.Entries)
                    {
                        string assetsFolderPath = Path.Combine(CustomAssetPackUtility.PackContentRootDirectory, entry.AssetsSubfolderPath);
                        if (File.Exists(assetsFolderPath))
                        {
                            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetsFolderPath);
                            File.Move(assetsFolderPath, entry.BundleBuildPath);
                            File.Delete(metaFilePath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception occured when moving data for a player build: {e.Message}.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
#else
    /// <summary>
    /// Moves custom asset pack data from their default build location <see cref="BuildScriptPlayAssetDelivery"/> to their correct player build data location.
    /// For an Android App Bundle, bundles assigned to a custom asset pack must be located in their {asset pack name}.androidpack directory in the Assets folder.
    /// The 'CustomAssetPacksData.json' file is also moved to StreamingAssets.
    ///
    /// This script executes before the <see cref="AddressablesPlayerBuildProcessor"/> which moves all Addressables data to StreamingAssets.
    /// </summary>
    public class PlayAssetDeliveryBuildProcessor : IPreprocessBuildWithReport
    {
        /// <summary>
        /// Returns the player build processor callback order.
        /// </summary>
        public int callbackOrder
        {
            get { return 0; }
        }

        ///<summary>
        /// Invoked before performing a Player build. Moves AssetBundles to their correct data location based on the build target platform.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android && EditorUserBuildSettings.buildAppBundle)
                MoveDataForAppBundleBuild();
            else
                MoveDataToDefaultLocation();
        }

        /// <summary>
        /// Move custom asset pack data from their build location to their App Bundle data location.
        /// </summary>
        public static void MoveDataForAppBundleBuild()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                if (File.Exists(CustomAssetPackUtility.CustomAssetPacksDataEditorPath))
                {
                    if (!Directory.Exists(Application.streamingAssetsPath))
                        Directory.CreateDirectory(Application.streamingAssetsPath);
                    string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(CustomAssetPackUtility.CustomAssetPacksDataEditorPath);
                    File.Move(CustomAssetPackUtility.CustomAssetPacksDataEditorPath, CustomAssetPackUtility.CustomAssetPacksDataRuntimePath);
                    File.Delete(metaFilePath);
                }
                if (File.Exists(CustomAssetPackUtility.BuildProcessorDataPath))
                {
                    string contents = File.ReadAllText(CustomAssetPackUtility.BuildProcessorDataPath);
                    var data =  JsonUtility.FromJson<BuildProcessorData>(contents);

                    foreach (BuildProcessorDataEntry entry in data.Entries)
                    {
                        string assetsFolderPath = Path.Combine(CustomAssetPackUtility.PackContentRootDirectory, entry.AssetsSubfolderPath);
                        if (File.Exists(entry.BundleBuildPath))
                        {
                            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(entry.BundleBuildPath);
                            File.Move(entry.BundleBuildPath, assetsFolderPath);
                            File.Delete(metaFilePath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception occured when moving data for an app bundle build: {e.Message}.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Move custom asset pack data from their App Bundle data location to to their build location.
        /// </summary>
        public static void MoveDataToDefaultLocation()
        {
            try
            {
                AssetDatabase.StartAssetEditing();

                if (File.Exists(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath))
                {
                    string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath);
                    File.Move(CustomAssetPackUtility.CustomAssetPacksDataRuntimePath, CustomAssetPackUtility.CustomAssetPacksDataEditorPath);
                    File.Delete(metaFilePath);
                    CustomAssetPackUtility.DeleteDirectory(Application.streamingAssetsPath, true);
                }
                if (File.Exists(CustomAssetPackUtility.BuildProcessorDataPath))
                {
                    string contents = File.ReadAllText(CustomAssetPackUtility.BuildProcessorDataPath);
                    var data =  JsonUtility.FromJson<BuildProcessorData>(contents);

                    foreach (BuildProcessorDataEntry entry in data.Entries)
                    {
                        string assetsFolderPath = Path.Combine(CustomAssetPackUtility.PackContentRootDirectory, entry.AssetsSubfolderPath);
                        if (File.Exists(assetsFolderPath))
                        {
                            string metaFilePath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetsFolderPath);
                            File.Move(assetsFolderPath, entry.BundleBuildPath);
                            File.Delete(metaFilePath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception occured when moving data for a player build: {e.Message}.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }
    }
#endif
}
