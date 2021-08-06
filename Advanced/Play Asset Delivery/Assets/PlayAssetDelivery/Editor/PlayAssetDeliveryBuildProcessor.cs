#if UNITY_2021_2_OR_NEWER && USE_CUSTOM_PREPROCESSOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace AddressablesPlayAssetDelivery.Editor
{
    /// <summary>
    /// Copies Addresssables build data to the Assets/StreamingAssets folder when processing a player build.
    ///
    /// If using Unity 2021.2+ and Addressables 1.19.0+, the AddressablesPlayerBuildProcessor class no longer copies data to the 'Assets/StreamingAssets' folder
    /// in favor of a performance optimization. We will need to rely on this custom build processor instead.
    /// </summary>
    public class PlayAssetDeliveryBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        /// <summary>
        /// Returns the player build processor callback order.
        /// </summary>
        public int callbackOrder
        {
            get { return 1; }
        }

        /// <summary>
        /// Restores temporary data created as part of a build.
        /// </summary>
        public void OnPostprocessBuild(BuildReport report)
        {
            CleanTemporaryPlayerBuildData();
        }

        [InitializeOnLoadMethod]
        internal static void CleanTemporaryPlayerBuildData()
        {
            if (Directory.Exists(Addressables.PlayerBuildDataPath))
            {
                DirectoryUtility.DirectoryMove(Addressables.PlayerBuildDataPath, Addressables.BuildPath);
                DirectoryUtility.DeleteDirectory(Application.streamingAssetsPath, onlyIfEmpty: true);
            }
        }

        ///<summary>
        /// Initializes temporary build data.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            CopyTemporaryPlayerBuildData();
        }

        internal static void CopyTemporaryPlayerBuildData()
        {
            if (Directory.Exists(Addressables.BuildPath))
            {
                if (Directory.Exists(Addressables.PlayerBuildDataPath))
                {
                    Debug.LogWarning($"Found and deleting directory \"{Addressables.PlayerBuildDataPath}\", directory is managed through Addressables.");
                    DirectoryUtility.DeleteDirectory(Addressables.PlayerBuildDataPath, false);
                }

                string parentDir = Path.GetDirectoryName(Addressables.PlayerBuildDataPath);
                if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
                    Directory.CreateDirectory(parentDir);
                Directory.Move(Addressables.BuildPath, Addressables.PlayerBuildDataPath);
            }
        }
    }

    internal static class DirectoryUtility
    {
        internal static void DeleteDirectory(string directoryPath, bool onlyIfEmpty = true, bool recursiveDelete = true)
        {
            if (!Directory.Exists(directoryPath))
                return;

            bool isEmpty = !Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories).Any()
                && !Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories).Any();
            if (!onlyIfEmpty || isEmpty)
            {
                // check if the folder is valid in the AssetDatabase before deleting through standard file system
                string relativePath = directoryPath.Replace("\\", "/").Replace(Application.dataPath, "Assets");
                if (AssetDatabase.IsValidFolder(relativePath))
                    AssetDatabase.DeleteAsset(relativePath);
                else
                    Directory.Delete(directoryPath, recursiveDelete);
            }
        }

        internal static void DirectoryMove(string sourceDirName, string destDirName)
        {
            if (!Directory.Exists(sourceDirName))
            {
                Debug.LogError($"Could not Move directory {sourceDirName}, directory not found.");
                return;
            }
            if (Directory.Exists(destDirName))
            {
                Debug.LogError($"Could not Move to directory {destDirName}, directory arlready exists.");
                return;
            }

            Directory.Move(sourceDirName, destDirName);
            // check if the folder is valid in the AssetDatabase before deleting through standard file system
            string relativePath = sourceDirName.Replace("\\", "/").Replace(Application.dataPath, "Assets");
            if (AssetDatabase.IsValidFolder(relativePath))
            {
                // recreate the root folder so that it can be removed via adb
                Directory.CreateDirectory(sourceDirName);
                AssetDatabase.DeleteAsset(relativePath);
            }
            else if (File.Exists(sourceDirName + ".meta"))
                File.Delete(sourceDirName + ".meta");
        }

        internal static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
                Directory.CreateDirectory(destDirName);

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, true);
                }
            }
        }
    }
}
#endif
