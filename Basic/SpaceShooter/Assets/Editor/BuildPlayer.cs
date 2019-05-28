using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Pipeline;
using UnityEngine;

public class BuildPlayer : MonoBehaviour
{
    [MenuItem("Build/Player")]
    public static void Build()
    {
        BuildPipeline.BuildPlayer(new string[] { "Assets/SpaceShooter.unity" }, Application.dataPath + "/../../UnityBuild/build",
            EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
    }

}
