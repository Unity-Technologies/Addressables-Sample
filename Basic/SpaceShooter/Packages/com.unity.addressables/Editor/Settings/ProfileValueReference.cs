using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace UnityEditor.AddressableAssets.Settings
{
    /// <summary>
    /// Used to store references to profile variables.  This class is intended to be used for fields in user scripts, specifically ones that subclass AddressableAssetGroupSchema.
    /// </summary>
    [Serializable]
    public class ProfileValueReference
    {
        [FormerlySerializedAs("m_id")]
        [SerializeField]
        string m_Id;

        /// <summary>
        /// This delegate will be invoked when the reference profile id changes.  This will NOT be invoked when the actual profile value itself changes.
        /// </summary>
        public Action<ProfileValueReference> OnValueChanged;

        /// <summary>
        /// Get the profile variable id.
        /// </summary>
        public string Id 
        { 
            get { return m_Id; }
            internal set { m_Id = value; }
        }

        /// <summary>
        /// Evaluate the profile value using the provided settings object.
        /// </summary>
        /// <param name="settings">The settings object to evaluate with.  The activeProfileId will be used.</param>
        /// <returns>The evaluated string stored in the referenced profile variable.</returns>
        public string GetValue(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("ProfileValueReference: AddressableAssetSettings object is null.");
                return null;
            }
            return GetValue(settings.profileSettings, settings.activeProfileId);
        }

        /// <summary>
        /// Evaluate the profile value using the provided profile settings object and a profile id.
        /// </summary>
        /// <param name="profileSettings">The profile settings object.</param>
        /// <param name="profileId">The profile id.</param>
        /// <returns>The evaluated string stored in the referenced profile variable.</returns>
        public string GetValue(AddressableAssetProfileSettings profileSettings, string profileId)
        {
            if (profileSettings == null)
            {
                Debug.LogWarning("ProfileValueReference: AddressableAssetProfileSettings object is null.");
                return null;
            }
            if (string.IsNullOrEmpty(profileId))
            {
                Debug.LogWarning("ProfileValueReference: GetValue called with invalid profileId.");
                return null;
            }
            return profileSettings.EvaluateString(profileId, profileSettings.GetValueById(profileId, m_Id));
        }

        /// <summary>
        /// Get the profile variable name that is referenced.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <returns>The name of the profile variable name.</returns>
        public string GetName(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                Debug.LogWarning("ProfileValueReference: GetName() - AddressableAssetSettings object is null.");
                return null;
            }
            var pid = settings.profileSettings.GetProfileDataById(m_Id);
            if(pid == null)
                Debug.LogWarningFormat("ProfileValueReference: GetName() - Unable to find variable id {0} in settings object.", m_Id);

            return pid == null ? string.Empty : pid.ProfileName;
        }

        /// <summary>
        /// Set the profile variable id using the id of the variable.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="id">The id of the profile variable to set.</param>
        /// <returns>True if the profile data is found and set, false otherwise.</returns>
        public bool SetVariableById(AddressableAssetSettings settings, string id)
        {
            if (settings == null)
            {
                Debug.LogWarning("ProfileValueReference: SetVariableById() - AddressableAssetSettings object is null.");
                return false;
            }
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning("ProfileValueReference: SetVariableById() - invalid parameter id.");
                return false;
            }
            if (settings.profileSettings.GetProfileDataById(id) == null)
            {
                Debug.LogWarningFormat("ProfileValueReference: SetVariableById() - Unable to find variable id {0} in settings object.", id);
                return false;
            }
            m_Id = id;
            if (OnValueChanged != null)
                OnValueChanged(this);
            return true;
        }

        /// <summary>
        /// Set the profile variable id using the name of the variable.
        /// </summary>
        /// <param name="settings">The settings object.</param>
        /// <param name="name">The name of the profile variable to set.</param>
        /// <returns>True if the profile data is found and set, false otherwise.</returns>
        public bool SetVariableByName(AddressableAssetSettings settings, string name)
        {
            if (settings == null)
            {
                Debug.LogWarning("ProfileValueReference: SetVariableByName() - AddressableAssetSettings object is null.");
                return false;
            }
            if (string.IsNullOrEmpty(name))
            {
                Debug.LogWarning("ProfileValueReference: SetVariableByName() - invalid parameter name.");
                return false;
            }

            var idData = settings.profileSettings.GetProfileDataByName(name);
            if (idData == null)
            {
                Debug.LogWarningFormat("ProfileValueReference: SetVariableByName() - Unable to find variable name {0} in settings object.", name);
                return false;
            }
            m_Id = idData.Id;
            if (OnValueChanged != null)
                OnValueChanged(this);
            return true;
        }
    }
}