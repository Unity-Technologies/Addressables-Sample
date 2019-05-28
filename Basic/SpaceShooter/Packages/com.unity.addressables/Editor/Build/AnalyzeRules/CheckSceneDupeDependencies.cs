using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Pipeline;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    class CheckSceneDupeDependencies : CheckDupeDependenciesBase
    {
        internal override bool CanFix
        {
            get { return false; }
        }

        internal Dictionary<GUID, List<string>> m_AssetToDependenciesMapping = new Dictionary<GUID, List<string>>();
        internal List<GUID> m_AddressableAssets = new List<GUID>();
        internal Dictionary<EditorBuildSettingsScene, List<GUID>> m_SceneToDependencies = new Dictionary<EditorBuildSettingsScene, List<GUID>>();

        internal override string ruleName
        {
            get { return "Check Scene to Addressable Duplicate Dependencies"; }
        }

        internal override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("Cannot run Analyze with unsaved scenes");
                m_Results.Add(new AnalyzeResult(ruleName + "Cannot run Analyze with unsaved scenes"));
                return m_Results;
            }


            //Get all our Addressable Assets
            m_AddressableAssets = (from aaGroup in settings.groups
                                   from entry in aaGroup.entries
                                   select new GUID(entry.guid)).ToList();

            
            BuildSceneToDependenciesMap(EditorBuildSettings.scenes);
            CalculateInputDefinitions(settings);

            var context = GetBuildContext(settings);
            ReturnCode exitCode = RefreshBuild(context);
            if (exitCode < ReturnCode.Success)
            {
                Debug.LogError("Analyze build failed. " + exitCode);
                m_Results.Add(new AnalyzeResult(ruleName + "Analyze build failed. " + exitCode));
                return m_Results;
            }

            var explicietGuids = m_ExtractData.WriteData.AssetToFiles.Keys;
            var implicitGuids = GetImplicitGuidToFilesMap().Keys;
            var allBundleGuids = explicietGuids.Union(implicitGuids);

            IntersectSceneDependenciesWithBundleDependencies(allBundleGuids.ToList());

            m_Results = (from scene in m_SceneToDependencies.Keys
                         from dependency in m_SceneToDependencies[scene]
                         where m_ExtractData.WriteData.AssetToFiles.ContainsKey(dependency)

                         let assetPath = AssetDatabase.GUIDToAssetPath(dependency.ToString())
                         let files = m_ExtractData.WriteData.AssetToFiles[dependency]

                         from file in files
                         let bundle = m_ExtractData.WriteData.FileToBundle[file]

                         select new AnalyzeResult(
                             "Scene Assets Duplicated in Addressables" + kDelimiter +
                             scene.path + kDelimiter +
                             bundle + kDelimiter +
                             assetPath,
                             MessageType.Warning)).ToList();

            if (m_Results.Count == 0)
                m_Results.Add(noErrors);

            return m_Results;
        }

        internal void BuildSceneToDependenciesMap(EditorBuildSettingsScene[] scenes)
        { 
            for (int i = 0; i < scenes.Length; i++)
            {
                var scene = scenes[i];
                if (!m_SceneToDependencies.ContainsKey(scene))
                    m_SceneToDependencies.Add(scene, new List<GUID>());

                var dependencyGuids = (from dependencyPath in AssetDatabase.GetDependencies(scene.path)
                    select new GUID(AssetDatabase.AssetPathToGUID(dependencyPath)));

                m_SceneToDependencies[scene].AddRange(dependencyGuids);
            }
        }

        internal void IntersectSceneDependenciesWithBundleDependencies(List<GUID> allBundleGuids)
        {
            //Go through our scene dependencies and only keep ones that are bundle dependencies as well
            foreach (var key in m_SceneToDependencies.Keys)
            {
                var bundleDependencies = allBundleGuids.Intersect(m_SceneToDependencies[key]).ToList();

                m_SceneToDependencies[key].Clear();
                m_SceneToDependencies[key].AddRange(bundleDependencies);
            }
        }

        internal override void FixIssues(AddressableAssetSettings settings)
        {
            //Do nothing.  There's nothing to fix.
        }

        internal override void ClearAnalysis()
        {
            m_SceneToDependencies.Clear();
            m_AssetToDependenciesMapping.Clear();
            m_AddressableAssets.Clear();
            base.ClearAnalysis();
        }
    }
}
