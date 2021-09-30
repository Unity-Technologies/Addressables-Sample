using UnityEditor;
using UnityEngine;
using UnityEngine.ResourceManagement.Util;

namespace AddressablesPlayAssetDelivery.Editor
{
    /// <summary>
    /// Asset container for 
    /// </summary>
    [CreateAssetMenu(fileName = "PlayAssetDeliveryInitializationSettings.asset", menuName = "Addressables/Initialization/Play Asset Delivery Initialization Settings")]
    public class PlayAssetDeliveryInitializationSettings : ScriptableObject, IObjectInitializationDataProvider
    {  
        /// <summary>
        /// Display name used in GUI for this object.
        /// </summary>
        public string Name { get { return "Play Asset Delivery Initialization Settings"; } }
        
        [SerializeField]
        PlayAssetDeliveryInitializationData m_Data = new PlayAssetDeliveryInitializationData();
        /// <summary>
        /// The cache initialization data that will be serialized and applied during Addressables initialization.
        /// </summary>
        public PlayAssetDeliveryInitializationData Data
        {
            get
            {
                return m_Data;
            }
            set
            {
                m_Data = value;
            }
        }

        /// <summary>
        /// Create initialization data to be serialized into the Addressables runtime data.
        /// </summary>
        /// <returns>The serialized data for the initialization class and the data.</returns>
        public ObjectInitializationData CreateObjectInitializationData()
        {
            return ObjectInitializationData.CreateSerializedInitializationData<PlayAssetDeliveryInitialization>(typeof(PlayAssetDeliveryInitialization).Name, m_Data);
        }
    }

    [CustomPropertyDrawer(typeof(PlayAssetDeliveryInitializationData), true)]
    class PlayAssetDeliveryInitializationDataDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var prop = property.FindPropertyRelative("m_LogWarnings");
            prop.boolValue = EditorGUI.Toggle(position, new GUIContent("Log Warnings", "Show warnings that occur when configuring Addressables."), prop.boolValue);
            EditorGUI.EndProperty();
        }
    }
}
