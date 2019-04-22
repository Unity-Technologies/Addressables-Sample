using System.IO;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class AddressablesPlayerBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder
    {
        get { return 1; }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        string addressablesStreamingAssets =
            Application.streamingAssetsPath + "/" + Addressables.StreamingAssetsSubFolder;
        if(Directory.Exists(Addressables.PlayerBuildDataPath))
            Directory.Delete(Addressables.PlayerBuildDataPath, true);
        if(Directory.GetFiles(addressablesStreamingAssets).Length == 0)
            Directory.Delete(addressablesStreamingAssets);
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        DirectoryCopy(Addressables.BuildPath, Addressables.PlayerBuildDataPath, true);
    }

    private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
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
        if (Directory.Exists(destDirName))
            Directory.Delete(destDirName);
        Directory.CreateDirectory(destDirName);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            string temppath = Path.Combine(destDirName, file.Name);
            file.CopyTo(temppath, false);
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
