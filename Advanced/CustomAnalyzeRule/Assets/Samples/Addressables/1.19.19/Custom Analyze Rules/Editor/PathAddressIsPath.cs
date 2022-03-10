#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// * This is a fixable rule.  Running fix on it will change addresses to comply with the rule.
/// * When run, it first identifies all addresses that seem to be paths.  Of those, it makes sure that the address actually matches the path of the asset.
/// * This would be useful if you primarily left the addresses of your assets as the path (which is the default when marking an asset addressable).  If the asset is moved within the project, then the address no longer maps to where it is. This rule could fix that.
/// </summary>
public class PathAddressIsPath : UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule
{
    public override bool CanFix
    {
        get { return true;}
        set { }
    }

    public override string ruleName
    {
        get { return "Addresses that are paths, match path"; }
    }

    [SerializeField]
    List<AddressableAssetEntry> m_MisnamedEntries = new List<AddressableAssetEntry>();

    public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
    {
        List<AnalyzeResult> results = new List<AnalyzeResult>();
        foreach (var group in settings.groups)
        {
            if (group.HasSchema<PlayerDataGroupSchema>())
                continue;

            foreach (var e in group.entries)
            {
                if (e.address.Contains("Assets") && e.address.Contains("/") && e.address != e.AssetPath)
                {
                    m_MisnamedEntries.Add(e);
                    results.Add(new AnalyzeResult { resultName = group.Name + kDelimiter + e.address, severity = MessageType.Error });
                }
            }
        }

        if (results.Count == 0)
            results.Add(new AnalyzeResult{resultName = "No issues found."});

    return results;
    }

    public override void FixIssues(AddressableAssetSettings settings)
    {
        foreach (var e in m_MisnamedEntries)
        {
            e.address = e.AssetPath;
        }
        m_MisnamedEntries = new List<AddressableAssetEntry>();
    }

    public override void ClearAnalysis()
    {
        m_MisnamedEntries = new List<AddressableAssetEntry>();
    }
}


[InitializeOnLoad]
class RegisterPathAddressIsPath
{
    static RegisterPathAddressIsPath()
    {
        AnalyzeSystem.RegisterNewRule<PathAddressIsPath>();
    }
}
#endif