using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Tests;
using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.TestTools;

public class PlatformSwitchingTests : AddressableAssetTestBase
{
    [TestCase(BuildTarget.Switch)]
    [TestCase(BuildTarget.PS4)]
    public void WhenBuildingForPlatform_BuildFilesAreGenerated(BuildTarget target)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
            Assert.Ignore();
        else
        {
            Init();

            string buildPath = "Assets/WhenBuildingForPlatform_BuildFilesAreGenerated";
            string savedBuildPath = m_Settings.buildSettings.bundleBuildPath;

            m_Settings.CreateOrMoveEntry(m_AssetGUID, m_Settings.DefaultGroup, false, false);
            m_Settings.buildSettings.bundleBuildPath = buildPath;

            BuildTarget backupTarget = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup backupTargetGroup = BuildPipeline.GetBuildTargetGroup(backupTarget);
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildPipeline.GetBuildTargetGroup(target), target);

            AddressableAssetBuildResult result = null;
            BuildScript.buildCompleted += r =>  result = r;

            Assert.DoesNotThrow(() => { m_Settings.BuildPlayerContentImpl(); });
            Assert.IsTrue(string.IsNullOrEmpty(result.Error));
            Assert.IsTrue(Directory.Exists(m_Settings.buildSettings.bundleBuildPath));
            Assert.IsTrue(Directory.GetFiles(buildPath).Length > 0);

            //Cleanup
            AssetDatabase.DeleteAsset(buildPath);
            m_Settings.buildSettings.bundleBuildPath = savedBuildPath;
            EditorUserBuildSettings.SwitchActiveBuildTarget(backupTargetGroup, backupTarget);
        }
    }
}
