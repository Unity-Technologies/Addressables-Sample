using System;
using UnityEngine.Serialization;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Reference to an asset label.  This class can be used in scripts as a field and will use a CustomPropertyDrawer to provide a DropDown UI of available labels.
    /// </summary>
    [Serializable]
    public class AssetLabelReference : IKeyEvaluator
    {
        [FormerlySerializedAs("m_labelString")]
        [SerializeField]
        string m_LabelString;
        /// <summary>
        /// The label string.
        /// </summary>
        public string labelString
        {
            get { return m_LabelString; }
            set { m_LabelString = value; }
        }
        /// <summary>
        /// The runtime key used for indexing values in the Addressables system.
        /// </summary>
        public object RuntimeKey
        {
            get
            {
                if (labelString == null)
                    labelString = string.Empty;
                return labelString;
            }
        }

        /// <inheritdoc/>
        public bool RuntimeKeyIsValid()
        {
            return !string.IsNullOrEmpty(RuntimeKey.ToString());
        }

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        /// <returns>The hash code of the label string.</returns>
        public override int GetHashCode()
        {
            return labelString.GetHashCode();
        }
    }
}
