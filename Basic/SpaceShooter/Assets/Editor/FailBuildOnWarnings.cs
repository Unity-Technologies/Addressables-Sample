using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditorInternal;
using UnityEngine;

public class FailBuildOnWarnings : IPreprocessBuildWithReport, IPostprocessBuildWithReport
{
    public int callbackOrder
    {
        get { return 0; }
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (InternalEditorUtility.inBatchMode)
            Application.logMessageReceived -= CheckLogForWarning;
    }

    public void OnPreprocessBuild(BuildReport report)
    {
        if (InternalEditorUtility.inBatchMode)
            Application.logMessageReceived += CheckLogForWarning;
    }

    void CheckLogForWarning(string condition, string stackTrace, LogType type)
    {
        //Unable to throw a BuildFailedException() as it doesn't give a non-zero exit code
        if(type == LogType.Warning)
            EditorApplication.Exit(1);
    }
}
