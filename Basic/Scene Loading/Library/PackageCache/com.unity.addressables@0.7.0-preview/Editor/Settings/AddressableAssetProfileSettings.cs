using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.Serialization;

// ReSharper disable DelegateSubtraction

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Contains user defined variables to control build parameters.
    /// </summary>
    [Serializable]
    public class AddressableAssetProfileSettings
    {
        internal delegate string ProfileStringEvaluationDelegate(string key);

        [NonSerialized]
        internal ProfileStringEvaluationDelegate onProfileStringEvaluation;
        
        internal void RegisterProfileStringEvaluationFunc(ProfileStringEvaluationDelegate f)
        {
            onProfileStringEvaluation -= f;
            onProfileStringEvaluation += f;
        }

        internal void UnregisterProfileStringEvaluationFunc(ProfileStringEvaluationDelegate f)
        {
            onProfileStringEvaluation -= f;
        }
        
        [Serializable]
        internal class BuildProfile
        {
            [Serializable]
            internal class ProfileEntry
            {
                [FormerlySerializedAs("m_id")]
                [SerializeField]
                string m_Id;
                public string id
                {
                    get { return m_Id; }
                    set { m_Id = value; }
                }
                [FormerlySerializedAs("m_value")]
                [SerializeField]
                string m_Value;
                public string value
                {
                    get { return m_Value; }
                    set { m_Value = value; }
                }
                internal ProfileEntry() { }
                internal ProfileEntry(string id, string v)
                {
                    m_Id = id;
                    m_Value = v;
                }
                internal ProfileEntry(ProfileEntry copy)
                {
                    m_Id = copy.m_Id;
                    m_Value = copy.m_Value;
                }
            }
            [NonSerialized]
            AddressableAssetProfileSettings m_ProfileParent;

            [FormerlySerializedAs("m_inheritedParent")]
            [SerializeField]
            string m_InheritedParent;

            public string inheritedParent
            {
                get { return m_InheritedParent; }
                set { m_InheritedParent = value; }
            }
            [FormerlySerializedAs("m_id")]
            [SerializeField]
            string m_Id;
            internal string id
            {
                get { return m_Id; }
                set { m_Id = value; }
            }
            [FormerlySerializedAs("m_profileName")]
            [SerializeField]
            string m_ProfileName;
            internal string profileName
            {
                get { return m_ProfileName; }
                set { m_ProfileName = value; }
            }
            [FormerlySerializedAs("m_values")]
            [SerializeField]
            List<ProfileEntry> m_Values = new List<ProfileEntry>();
            internal List<ProfileEntry> values
            {
                get { return m_Values; }
                set { m_Values = value; }
            }

            internal BuildProfile(string name, BuildProfile copyFrom, AddressableAssetProfileSettings ps)
            {
                m_InheritedParent = null;
                id = GUID.Generate().ToString();
                profileName = name;
                values.Clear();
                m_ProfileParent = ps;

                if (copyFrom != null)
                {
                    foreach (var v in copyFrom.values)
                        values.Add(new ProfileEntry(v));
                    m_InheritedParent = copyFrom.m_InheritedParent;
                }
            }
            internal void OnAfterDeserialize(AddressableAssetProfileSettings ps)
            {
                m_ProfileParent = ps;
            }

            int IndexOfVarId(string variableId)
            {
                if (string.IsNullOrEmpty(variableId))
                    return -1;

                for (int i = 0; i < values.Count; i++)
                    if (values[i].id == variableId)
                        return i;
                return -1;
            }

            int IndexOfVarName(string name)
            {
                if (m_ProfileParent == null)
                    return -1;

                var currId = m_ProfileParent.GetVariableId(name);
                if (string.IsNullOrEmpty(currId))
                    return -1;

                for (int i = 0; i < values.Count; i++)
                    if (values[i].id == currId)
                        return i;
                return -1;
            }

            internal string GetValueById(string variableId)
            {
                var i = IndexOfVarId(variableId);
                if (i >= 0)
                    return values[i].value;


                if (m_ProfileParent == null)
                    return null;

                return m_ProfileParent.GetValueById(m_InheritedParent, variableId);
            }

            internal void SetValueById(string variableId, string val)
            {
                var i = IndexOfVarId(variableId);
                if (i >= 0)
                    values[i].value = val;
            }

            internal void ReplaceVariableValueSubString(string searchStr, string replacementStr)
            {
                foreach (var v in values)
                    v.value = v.value.Replace(searchStr, replacementStr);
            }


            internal bool IsValueInheritedByName(string variableName)
            {
                return IndexOfVarName(variableName) >= 0;
            }

            internal bool IsValueInheritedById(string variableId)
            {
                return IndexOfVarId(variableId) >= 0;
            }
        }

        internal void OnAfterDeserialize(AddressableAssetSettings settings)
        {
            m_Settings = settings;
            foreach (var prof in m_Profiles)
            {
                prof.OnAfterDeserialize(this);
            }
        }

        [NonSerialized]
        AddressableAssetSettings m_Settings;
        [FormerlySerializedAs("m_profiles")]
        [SerializeField]
        List<BuildProfile> m_Profiles = new List<BuildProfile>();
        internal List<BuildProfile> profiles { get { return m_Profiles; } }

        [Serializable]
        internal class ProfileIdData
        {
            [FormerlySerializedAs("m_id")]
            [SerializeField]
            string m_Id;
            internal string Id { get { return m_Id; } }

            [FormerlySerializedAs("m_name")]
            [SerializeField]
            string m_Name;
            internal string ProfileName
            {
                get { return m_Name; }
            }
            internal void SetName(string newName, AddressableAssetProfileSettings ps)
            {
                if (!ps.ValidateNewVariableName(newName))
                    return;

                var currRefStr = "[" + m_Name + "]";
                var newRefStr = "[" + newName + "]";

                m_Name = newName;

                foreach (var p in ps.profiles)
                    p.ReplaceVariableValueSubString(currRefStr, newRefStr);
            }

            [FormerlySerializedAs("m_inlineUsage")]
            [SerializeField]
            bool m_InlineUsage;
            internal bool InlineUsage { get { return m_InlineUsage; } }
            internal ProfileIdData() { }
            internal ProfileIdData(string entryId, string entryName, bool inline = false)
            {
                m_Id = entryId;
                m_Name = entryName;
                m_InlineUsage = inline;
            }
            internal string Evaluate(AddressableAssetProfileSettings ps, string profileId)
            {
                if (InlineUsage)
                    return ps.EvaluateString(profileId, Id);

                return Evaluate(ps, profileId, Id);
            }
            internal static string Evaluate(AddressableAssetProfileSettings ps, string profileId, string idString)
            {
                string baseValue = ps.GetValueById(profileId, idString);
                return ps.EvaluateString(profileId, baseValue);
            }
        }
        [FormerlySerializedAs("m_profileEntryNames")]
        [SerializeField]
        List<ProfileIdData> m_ProfileEntryNames = new List<ProfileIdData>();
        [FormerlySerializedAs("m_profileVersion")]
        [SerializeField]
        int m_ProfileVersion;
        const int k_CurrentProfileVersion = 1;
        internal List<ProfileIdData> profileEntryNames
        {
            get
            {
                if (m_ProfileVersion < k_CurrentProfileVersion)
                {
                    m_ProfileVersion = k_CurrentProfileVersion;
                    //migration cleanup from old way of doing "custom" values...
                    var removeId = string.Empty;
                    foreach (var idPair in m_ProfileEntryNames)
                    {
                        if (idPair.ProfileName == customEntryString)
                        {
                            removeId = idPair.Id;
                            break;
                        }
                    }
                    if (!string.IsNullOrEmpty(removeId))
                        RemoveValue(removeId);
                }


                return m_ProfileEntryNames;
            }
        }

        public const string customEntryString = "<custom>";
        public const string undefinedEntryValue = "<undefined>";

        internal ProfileIdData GetProfileDataById(string id)
        {
            foreach (var data in profileEntryNames)
            {
                if (id == data.Id)
                    return data;
            }
            return null;
        }

        internal ProfileIdData GetProfileDataByName(string name)
        {
            foreach (var data in profileEntryNames)
            {
                if (name == data.ProfileName)
                    return data;
            }
            return null;
        }

        /// <summary>
        /// Clears out the list of profiles, then creates a new default one.
        /// </summary>
        /// <returns>Returns the ID of the newly created default profile.</returns>
        public string Reset()
        {
            m_Profiles = new List<BuildProfile>();
            return CreateDefaultProfile();
        }
        /// <summary>
        /// Evaluate a string given a profile id.
        /// </summary>
        /// <param name="profileId">The profile id to use for evaluation.</param>
        /// <param name="varString">The string to evaluate.  Any tokens surrounded by '[' and ']' will be replaced with matching variables.</param>
        /// <returns>The evaluated string.</returns>
        public string EvaluateString(string profileId, string varString)
        {
            Func<string, string> getVal = s =>
            {
                var v = GetValueByName(profileId, s);
                if (string.IsNullOrEmpty(v))
                {
                    if (onProfileStringEvaluation != null)
                    {
                        foreach (var i in onProfileStringEvaluation.GetInvocationList())
                        {
                            var del = (ProfileStringEvaluationDelegate)i;
                            v = del(s);
                            if (!string.IsNullOrEmpty(v))
                                return v;
                        }
                    }
                    v = AddressablesRuntimeProperties.EvaluateProperty(s);
                }

                return v;
            };
            
            return AddressablesRuntimeProperties.EvaluateString(varString, '[', ']', getVal);
        }

        internal void Validate(AddressableAssetSettings addressableAssetSettings)
        {
            CreateDefaultProfile();
        }

        const string k_RootProfileName = "Default";
        internal string CreateDefaultProfile()
        {
            if (!ValidateProfiles())
            {
                m_ProfileEntryNames.Clear();
                m_Profiles.Clear();

                AddProfile(k_RootProfileName, null);
                CreateValue("BuildTarget", "[UnityEditor.EditorUserBuildSettings.activeBuildTarget]");
                CreateValue(AddressableAssetSettings.kLocalBuildPath, "[UnityEngine.AddressableAssets.Addressables.BuildPath]/[BuildTarget]");
                CreateValue(AddressableAssetSettings.kLocalLoadPath, "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/[BuildTarget]");
                CreateValue(AddressableAssetSettings.kRemoteBuildPath, "ServerData/[BuildTarget]");
                CreateValue(AddressableAssetSettings.kRemoteLoadPath, "http://localhost/[BuildTarget]");
            }
            return GetDefaultProfileId();
        }

        string GetDefaultProfileId()
        {
            var def = GetDefaultProfile();
            if (def != null)
                return def.id;
            return null;
        }

        BuildProfile GetDefaultProfile()
        {
            BuildProfile profile = null;
            if (m_Profiles.Count > 0)
                profile = m_Profiles[0];
            return profile;
        }

        bool ValidateProfiles()
        {
            if (m_Profiles.Count == 0)
                return false;
            
            var root = m_Profiles[0];
            if (root == null || root.values == null)
                return false;

            foreach (var i in profileEntryNames)
                if (string.IsNullOrEmpty(i.Id) || string.IsNullOrEmpty(i.ProfileName))
                    return false;

            var rootValueCount = root.values.Count;
            for (int index = 1; index < m_Profiles.Count; index++)
            {
                var profile = m_Profiles[index];
                
                if (profile == null || string.IsNullOrEmpty(profile.profileName))
                    return false;

                if (profile.values == null || profile.values.Count != rootValueCount)
                    return false;

                for (int i = 0; i < rootValueCount; i++)
                {
                    if (root.values[i].id != profile.values[i].id)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get all available variable names
        /// </summary>
        /// <returns>The variable names, sorted alphabetically.</returns>
        public List<string> GetVariableNames()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (var entry in profileEntryNames)
                names.Add(entry.ProfileName);
            var list = names.ToList();
            list.Sort();
            return list;
        }

        /// <summary>
        /// Get all profile names.
        /// </summary>
        /// <returns>The list of profile names.</returns>
        public List<string> GetAllProfileNames()
        {
            CreateDefaultProfile();
            List<string> result = new List<string>();
            foreach (var p in m_Profiles)
                result.Add(p.profileName);
            return result;

        }

        /// <summary>
        /// Get a profile's display name.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <returns>The display name of the profile.  Returns empty string if not found.</returns>
        public string GetProfileName(string profileId)
        {
            foreach (var p in m_Profiles)
            {
                if (p.id == profileId)
                    return p.profileName;
            }
            return "";
        }

        /// <summary>
        /// Get the id of a given display name.
        /// </summary>
        /// <param name="profileName">The profile name.</param>
        /// <returns>The id of the profile.  Returns empty string if not found.</returns>
        public string GetProfileId(string profileName)
        {
            foreach (var p in m_Profiles)
            {
                if (p.profileName == profileName)
                    return p.id;
            }
            return "";
        }

        /// <summary>
        /// Gets the set of all profile ids.
        /// </summary>
        /// <returns>The set of profile ids.</returns>
        public HashSet<string> GetAllVariableIds()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (var v in profileEntryNames)
                ids.Add(v.Id);
            return ids;
        }

        /// <summary>
        /// Marks the object as modified.
        /// </summary>
        /// <param name="modificationEvent">The event type that is changed.</param>
        /// <param name="eventData">The object data that corresponds to the event.</param>
        /// <param name="postEvent">If true, the event is propagated to callbacks.</param>
        public void SetDirty(AddressableAssetSettings.ModificationEvent modificationEvent, object eventData, bool postEvent)
        {
            if (m_Settings != null)
                m_Settings.SetDirty(modificationEvent, eventData, postEvent);
        }

        internal bool ValidateNewVariableName(string name)
        {
            foreach (var idPair in profileEntryNames)
                if (idPair.ProfileName == name)
                    return false;
            return !string.IsNullOrEmpty(name) && !name.Any(c => { return c == '[' || c == ']' || c == '{' || c == '}'; });
        }

        /// <summary>
        /// Adds a new profile.
        /// </summary>
        /// <param name="name">The name of the new profile.</param>
        /// <param name="copyFromId">The id of the profile to copy values from.</param>
        /// <returns>The id of the created profile.</returns>
        public string AddProfile(string name, string copyFromId)
        {
            var existingProfile = GetProfileByName(name);
            if (existingProfile != null)
                return existingProfile.id;
            var copyRoot = GetProfile(copyFromId);
            if (copyRoot == null && m_Profiles.Count > 0)
                copyRoot = GetDefaultProfile();
            var prof = new BuildProfile(name, copyRoot, this);
            m_Profiles.Add(prof);
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileAdded, prof, true);
            return prof.id;
        }

        /// <summary>
        /// Removes a profile.
        /// </summary>
        /// <param name="profileId">The id of the profile to remove.</param>
        public void RemoveProfile(string profileId)
        {
            m_Profiles.RemoveAll(p => p.id == profileId);
            m_Profiles.ForEach(p => { if (p.inheritedParent == profileId) p.inheritedParent = null; });
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileRemoved, profileId, true);
        }

        BuildProfile GetProfileByName(string profileName)
        {
            return m_Profiles.Find(p => p.profileName == profileName);
        }

        internal string GetUniqueProfileName(string name)
        {
            var newName = name;
            int counter = 1;
            while (counter < 100)
            {
                if (GetProfileByName(newName) == null)
                    return newName;
                newName = name + counter;
                counter++;
            }
            return string.Empty;
        }


        internal BuildProfile GetProfile(string profileId)
        {
            return m_Profiles.Find(p => p.id == profileId);
        }

        string GetVariableId(string variableName)
        {
            foreach (var idPair in profileEntryNames)
            {
                if (idPair.ProfileName == variableName)
                    return idPair.Id;
            }
            return null;
        }

        /// <summary>
        /// Set the value of a variable for a specified profile.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <param name="variableName">The property name.</param>
        /// <param name="val">The value to set the property.</param>
        public void SetValue(string profileId, string variableName, string val)
        {
            var profile = GetProfile(profileId);
            if (profile == null)
            {
                Addressables.LogError("setting variable " + variableName + " failed because profile " + profileId + " does not exist.");
                return;
            }

            var id = GetVariableId(variableName);
            if (string.IsNullOrEmpty(id))
            {
                Addressables.LogError("setting variable " + variableName + " failed because variable does not yet exist. Call CreateValue() first.");
                return;
            }

            profile.SetValueById(id, val);
            SetDirty(AddressableAssetSettings.ModificationEvent.ProfileModified, profile, true);
        }

        internal string GetUniqueProfileEntryName(string name)
        {
            var newName = name;
            int counter = 1;
            while (counter < 100)
            {
                if (string.IsNullOrEmpty(GetVariableId(newName)))
                    return newName;
                newName = name + counter;
                counter++;
            }
            return string.Empty;
        }

        /// <summary>
        /// Create a new profile property.
        /// </summary>
        /// <param name="variableName">The name of the property.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The if of the created variable.</returns>
        public string CreateValue(string variableName, string defaultValue)
        {
            return CreateValue(variableName, defaultValue, false);
        }

        internal string CreateValue(string variableName, string defaultValue, bool inline)
        {
            if (m_Profiles.Count == 0)
            {
                Addressables.LogError("Attempting to add a profile variable in Addressables, but there are no profiles yet.");
            }

            var id = GetVariableId(variableName);
            if (string.IsNullOrEmpty(id))
            {
                id = GUID.Generate().ToString();
                profileEntryNames.Add(new ProfileIdData(id, variableName, inline));

                foreach (var pro in m_Profiles)
                {
                    pro.values.Add(new BuildProfile.ProfileEntry(id, defaultValue));
                }
            }
            return id;
        }

        /// <summary>
        /// Remove a profile property.
        /// </summary>
        /// <param name="variableId">The id of the property.</param>
        public void RemoveValue(string variableId)
        {
            foreach (var pro in m_Profiles)
            {
                pro.values.RemoveAll(x => x.id == variableId);
            }
            m_ProfileEntryNames.RemoveAll(x => x.Id == variableId);
        }

        /// <summary>
        /// Get the value of a property.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <param name="varId">The property id.</param>
        /// <returns></returns>
        public string GetValueById(string profileId, string varId)
        {
            BuildProfile profile = GetProfile(profileId);
            return profile == null ? varId : profile.GetValueById(varId);
        }

        /// <summary>
        /// Get the value of a property by name.
        /// </summary>
        /// <param name="profileId">The profile id.</param>
        /// <param name="varName">The variable name.</param>
        /// <returns></returns>
        public string GetValueByName(string profileId, string varName)
        {
            return GetValueById(profileId, GetVariableId(varName));
        }


        internal bool IsValueInheritedById(string profileId, string variableId)
        {
            var p = GetProfile(profileId);
            if (p == null)
                return false;
            return p.IsValueInheritedById(variableId);
        }
    }
}
