#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

public class CheckBundleDupeDependenciesMultiIsolatedGroups : CheckBundleDupeDependencies
{
    protected internal struct GroupComparator : IEqualityComparer<List<AddressableAssetGroup>>
    {
        public bool Equals(List<AddressableAssetGroup> x, List<AddressableAssetGroup> y)
        {
            foreach (AddressableAssetGroup group in x)
            {
                if (y.Find(i => i.Guid == group.Guid) == null)
                    return false;
            }
            return true;
        }

        public int GetHashCode(List<AddressableAssetGroup> obj)
        {
            int hashCode = obj.Count > 0 ? 17 : 0;
            foreach (AddressableAssetGroup group in obj)
                hashCode = hashCode * 31 + group.Guid.GetHashCode();
            return hashCode;
        }
    }

    /// <inheritdoc />
    public override string ruleName { get { return "Check Duplicate Bundle Dependencies Multi-Isolated Groups"; } }

    /// <summary>
    /// Fix duplicates by moving them to new groups.
    /// </summary>
    /// <param name="settings">The current Addressables settings object</param>
    /// <remarks>Duplicates referenced by the same groups will be moved to the same new group.</remarks>
    public override void FixIssues(AddressableAssetSettings settings)
    {
        if (CheckDupeResults == null)
            CheckForDuplicateDependencies(settings);

        Dictionary<GUID, List<AddressableAssetGroup>> implicitAssetsToGroup = GetImplicitAssetsToGroup(CheckDupeResults);

        var groupsToAssets = new Dictionary<List<AddressableAssetGroup>, List<GUID>>(new GroupComparator());
        foreach (KeyValuePair<GUID, List<AddressableAssetGroup>> pair in implicitAssetsToGroup)
        {
            if (!groupsToAssets.TryGetValue(pair.Value, out List<GUID> assets))
            {
                assets = new List<GUID>();
                groupsToAssets.Add(pair.Value, assets);
            }
            groupsToAssets[pair.Value].Add(pair.Key);
        }

        foreach (KeyValuePair<List<AddressableAssetGroup>, List<GUID>> pair in groupsToAssets)
        {
            var group = settings.CreateGroup("Duplicate Asset Isolation", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            group.GetSchema<ContentUpdateGroupSchema>().StaticContent = true;
            foreach (GUID asset in pair.Value)
                settings.CreateOrMoveEntry(asset.ToString(), group, false, false);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
    }

    protected Dictionary<GUID, List<AddressableAssetGroup>> GetImplicitAssetsToGroup(IEnumerable<CheckDupeResult> checkDupeResults)
    {
        var implicitAssetsToGroup = new Dictionary<GUID, List<AddressableAssetGroup>>();
        foreach (var checkDupeResult in checkDupeResults)
        {
            GUID assetGuid = checkDupeResult.DuplicatedGroupGuid;
            if (!implicitAssetsToGroup.TryGetValue(assetGuid, out List<AddressableAssetGroup> groups))
            {
                groups = new List<AddressableAssetGroup>();
                implicitAssetsToGroup.Add(assetGuid, groups);
            }
            implicitAssetsToGroup[assetGuid].Add(checkDupeResult.Group);
        }
        return implicitAssetsToGroup;
    }
}

[InitializeOnLoad]
class RegisterCheckBundleDupeDependenciesMultiIsolatedGroups
{
    static RegisterCheckBundleDupeDependenciesMultiIsolatedGroups()
    {
        AnalyzeSystem.RegisterNewRule<CheckBundleDupeDependenciesMultiIsolatedGroups>();
    }
}
#endif
