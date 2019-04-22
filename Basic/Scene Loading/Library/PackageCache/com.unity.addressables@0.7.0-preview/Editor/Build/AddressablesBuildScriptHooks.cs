using System;
using System.IO;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build
{
    /// <summary>
    /// Entry point to set callbacks for builds.
    /// </summary>
    public static class BuildScript
    {
        /// <summary>
        /// Global delegate for handling the result of AddressableAssets builds.  This will get called for player builds and when entering play mode.
        /// </summary>
        public static Action<AddressableAssetBuildResult> buildCompleted;
    }

    static class AddressablesBuildScriptHooks
    {
        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.playModeStateChanged += OnEditorPlayModeChanged;
        }
        
        static void OnEditorPlayModeChanged(PlayModeStateChange state)
        {  
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                var settings = AddressableAssetSettingsDefaultObject.Settings;
                if (settings == null)
                    return;
                if (settings.ActivePlayModeDataBuilder == null)
                {
                    var err = "Active play mode build script is null.";
                    Debug.LogError(err);

                    if (BuildScript.buildCompleted != null)
                    {
                        var result = AddressableAssetBuildResult.CreateResult<AddressableAssetBuildResult>(null, 0, err);
                        BuildScript.buildCompleted(result);
                    }
                    return;
                }

                if (!settings.ActivePlayModeDataBuilder.CanBuildData<AddressablesPlayModeBuildResult>())
                {
                    var err = string.Format("Active build script {0} cannot build AddressablesPlayModeBuildResult.", settings.ActivePlayModeDataBuilder);
                    Debug.LogError(err);
                    if (BuildScript.buildCompleted != null)
                    {
                        
                        var result = AddressableAssetBuildResult.CreateResult<AddressableAssetBuildResult>(null, 0, err);
                        BuildScript.buildCompleted(result);
                    }

                    return;
                }

                var res = settings.ActivePlayModeDataBuilder.BuildData<AddressablesPlayModeBuildResult>(new AddressablesDataBuilderInput(settings));
                if (!string.IsNullOrEmpty(res.Error))
                {
                    Debug.LogError(res.Error);
                    EditorApplication.isPlaying = false;
                }
                else
                {
                    if (BuildScript.buildCompleted != null)
                        BuildScript.buildCompleted(res);
                    settings.DataBuilderCompleted(settings.ActivePlayModeDataBuilder, res);
                }

                EditorUtility.SetDirty(settings);
            }
        }
    }
}
