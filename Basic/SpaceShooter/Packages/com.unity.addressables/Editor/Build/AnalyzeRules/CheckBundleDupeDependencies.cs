using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build.Pipeline;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    class CheckBundleDupeDependencies : CheckDupeDependenciesBase
    {
        internal struct CheckDupeResult
        {
            public AddressableAssetGroup Group;
            public List<string> DuplicatedFiles;
            public string AssetPath;
            public GUID DuplicatedGroupGuid;
        }

        internal override bool CanFix
        {
            get { return true; }
        }
        
        internal override string ruleName
        { get { return "Check Duplicate Bundle Dependencies"; } }

        [NonSerialized]
        internal readonly Dictionary<string, Dictionary<string, List<string>>> m_AllIssues = new Dictionary<string, Dictionary<string, List<string>>>();
        [SerializeField]
        internal HashSet<GUID> m_ImplicitAssets;

        internal override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            ClearAnalysis();
            return CheckForDuplicateDependencies(settings);
        }

        List<AnalyzeResult> CheckForDuplicateDependencies(AddressableAssetSettings settings)
        {

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogError("Cannot run Analyze with unsaved scenes");
                m_Results.Add(new AnalyzeResult(ruleName + "Cannot run Analyze with unsaved scenes"));
                return m_Results;
            }

            CalculateInputDefinitions(settings);

            if (m_AllBundleInputDefs.Count > 0)
            {
                var context = GetBuildContext(settings);
                ReturnCode exitCode = RefreshBuild(context);
                if (exitCode < ReturnCode.Success)
                {
                    Debug.LogError("Analyze build failed. " + exitCode);
                    m_Results.Add(new AnalyzeResult(ruleName + "Analyze build failed. " + exitCode));
                    return m_Results;
                }

                var implicitGuids = GetImplicitGuidToFilesMap();
                var checkDupeResults = CalculateDuplicates(implicitGuids, context);
                BuildImplicitDuplicatedAssetsSet(checkDupeResults);

                m_Results = (from issueGroup in m_AllIssues
                            from bundle in issueGroup.Value
                            from item in bundle.Value
                            select new AnalyzeResult(ruleName + kDelimiter +
                                                     issueGroup.Key + kDelimiter +
                                                     bundle.Key + kDelimiter +
                                                     item, MessageType.Warning)).ToList();
            }

            if (m_Results.Count == 0)
                m_Results.Add(noErrors);

            return m_Results;
        }

        internal IEnumerable<CheckDupeResult> CalculateDuplicates(Dictionary<GUID, List<string>> implicitGuids, AddressableAssetsBuildContext aaContext)
        {
            //Get all guids that have more than one bundle referencing them
            IEnumerable<KeyValuePair<GUID, List<string>>> validGuids =
                from dupeGuid in implicitGuids
                where dupeGuid.Value.Count > 1
                where IsValidPath(AssetDatabase.GUIDToAssetPath(dupeGuid.Key.ToString()))
                select dupeGuid;

            return
                from guidToFile in validGuids
                from file in guidToFile.Value

                    //Get the files that belong to those guids
                let fileToBundle = m_ExtractData.WriteData.FileToBundle[file]

                //Get the bundles that belong to those files
                let bundleToGroup = aaContext.bundleToAssetGroup[fileToBundle]

                //Get the asset groups that belong to those bundles
                let selectedGroup = aaContext.settings.FindGroup(findGroup => findGroup.Guid == bundleToGroup)

                select new CheckDupeResult
                {
                    Group = selectedGroup,
                    DuplicatedFiles = guidToFile.Value,
                    AssetPath = AssetDatabase.GUIDToAssetPath(guidToFile.Key.ToString()),
                    DuplicatedGroupGuid = guidToFile.Key
                };
        }

        internal void BuildImplicitDuplicatedAssetsSet(IEnumerable<CheckDupeResult> checkDupeResults)
        {
            m_ImplicitAssets = new HashSet<GUID>();
            foreach (var checkDupeResult in checkDupeResults)
            {
                Dictionary<string, List<string>> groupData;
                if (!m_AllIssues.TryGetValue(checkDupeResult.Group.Name, out groupData))
                {
                    groupData = new Dictionary<string, List<string>>();
                    m_AllIssues.Add(checkDupeResult.Group.Name, groupData);
                }

                foreach (string file in checkDupeResult.DuplicatedFiles)
                {
                    List<string> assets;
                    if (!groupData.TryGetValue(m_ExtractData.WriteData.FileToBundle[file], out assets))
                    {
                        assets = new List<string>();
                        groupData.Add(m_ExtractData.WriteData.FileToBundle[file], assets);
                    }

                    assets.Add(checkDupeResult.AssetPath);
                }

                m_ImplicitAssets.Add(checkDupeResult.DuplicatedGroupGuid);
            }
        }

        bool IsValidPath(string path)
        {
            return AddressableAssetUtility.IsPathValidForEntry(path) &&
             !path.ToLower().Contains("/resources/") &&
             !path.ToLower().StartsWith("resources/");
        }

        internal override void FixIssues(AddressableAssetSettings settings)
        {
            if (m_ImplicitAssets == null)
                CheckForDuplicateDependencies(settings);

            if (m_ImplicitAssets.Count == 0)
                return;

            var group = settings.CreateGroup("Duplicate Asset Isolation", false, false, false, null, typeof(BundledAssetGroupSchema));
            foreach (var asset in m_ImplicitAssets)
                settings.CreateOrMoveEntry(asset.ToString(), group, false, false);
        }

        internal override void ClearAnalysis()
        {
            m_AllIssues.Clear();
            m_ImplicitAssets = null;
            base.ClearAnalysis();
        }
    }
}