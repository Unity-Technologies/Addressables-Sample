#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.GUI;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// * This is a non-fixable rule (meaning it will not fix itself).
/// * When run, it checks that all addresses have a capital C in them.  Any that do not are flagged as errors.
/// * A rule like this would be useful if your studio enforced some sort of naming convention on addresses. (though it would probably be best if it could fix itself)
/// </summary>
public class AddressHasC : UnityEditor.AddressableAssets.Build.AnalyzeRules.AnalyzeRule
{
    public override bool CanFix
    {
        get { return false;}
        set { }
    }

    public override string ruleName
    {
        get { return "Ensure all addresses have a 'C'"; }
    }

    public override List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
    {
        List<AnalyzeResult> results = new List<AnalyzeResult>();
        foreach (var group in settings.groups)
        {
            if (group.HasSchema<PlayerDataGroupSchema>())
                continue;
            
            foreach (var e in group.entries)
            {
                if(!e.address.Contains("C"))
                    results.Add(new AnalyzeResult{resultName = group.Name + kDelimiter + e.address, severity = MessageType.Error});
            }
        }
        
        if (results.Count == 0)
            results.Add(new AnalyzeResult{resultName = ruleName + " - No issues found."});

        return results;
    }
}


[InitializeOnLoad]
class RegisterAddressHasC
{
    static RegisterAddressHasC()
    {
        AnalyzeSystem.RegisterNewRule<AddressHasC>();
    }
}
#endif