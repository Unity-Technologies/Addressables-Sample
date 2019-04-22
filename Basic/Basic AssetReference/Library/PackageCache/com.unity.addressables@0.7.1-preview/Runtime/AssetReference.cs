using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Serialization;

namespace UnityEngine.AddressableAssets
{
    /// <summary>
    /// Generic version of AssetReference class.  This should not be used directly as CustomPropertyDrawers do not support generic types.  Instead use the concrete derived classes such as AssetReferenceGameObject.
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    public class AssetReferenceT<TObject> : AssetReference
    {

        /// <summary>
        /// Construct a new AssetReference object.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        public AssetReferenceT(string guid) : base(guid)
        {
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <returns>The load operation.</returns>
        public AsyncOperationHandle<TObject> LoadAsset()
        {
            return LoadAsset<TObject>();
        }
        
        /// <inheritdoc/>
        public override bool ValidateAsset(Object obj)
        {
            var type = obj.GetType();
            return typeof(TObject).IsAssignableFrom(type);
        }
        
        /// <inheritdoc/>
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            return typeof(TObject).IsAssignableFrom(type);
#else
            return false;
#endif
        }
        
    }

    /// <summary>
    /// GameObject only asset reference.
    /// </summary>
    [Serializable]
    public class AssetReferenceGameObject : AssetReferenceT<GameObject>
    {
        public AssetReferenceGameObject(string guid) : base(guid) { }
    }
    /// <summary>
    /// Texture only asset reference.
    /// </summary>
    [Serializable]
    public class AssetReferenceTexture : AssetReferenceT<Texture>
    {
        public AssetReferenceTexture(string guid) : base(guid) { }
    }
    /// <summary>
    /// Texture2D only asset reference.
    /// </summary>
    [Serializable]
    public class AssetReferenceTexture2D : AssetReferenceT<Texture2D>
    {
        public AssetReferenceTexture2D(string guid) : base(guid) { }
    }
    /// <summary>
    /// Texture3D only asset reference
    /// </summary>
    [Serializable]
    public class AssetReferenceTexture3D : AssetReferenceT<Texture3D>
    {
        public AssetReferenceTexture3D(string guid) : base(guid) { }
    }

    /// <summary>
    /// Sprite only asset reference.
    /// </summary>
    [Serializable]
    public class AssetReferenceSprite : AssetReferenceT<Sprite>
    {
        public AssetReferenceSprite(string guid) : base(guid) { }

        /// <inheritdoc/>
        public override bool ValidateAsset(string path)
        {
#if UNITY_EDITOR
            var type = AssetDatabase.GetMainAssetTypeAtPath(path);
            bool isTexture = typeof(Texture2D).IsAssignableFrom(type);
            if (isTexture)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                return (importer != null) && (importer.spriteImportMode != SpriteImportMode.None);
            }
#endif
            return false;
        }
    }

    /// <summary>
    /// Reference to an addressable asset.  This can be used in script to provide fields that can be easily set in the editor and loaded dynamically at runtime.
    /// To determine if the reference is set, use RuntimeKey.isValid.  
    /// </summary>
    [Serializable]
    public class AssetReference : IKeyEvaluator
    {
        [FormerlySerializedAs("m_assetGUID")]
        [SerializeField]
        string m_AssetGUID = "";
        AsyncOperationHandle m_operation;
        /// <summary>
        /// The actual key used to request the asset at runtime. RuntimeKey.isValid can be used to determine if this reference was set.
        /// </summary>
        public object RuntimeKey
        {
            get { return m_AssetGUID; }
        }

        /// <summary>
        /// Construct a new AssetReference object.
        /// </summary>
        public AssetReference()
        {
        }

        /// <summary>
        /// Construct a new AssetReference object.
        /// </summary>
        /// <param name="guid">The guid of the asset.</param>
        public AssetReference(string guid)
        {
            m_AssetGUID = guid;
        }

        /// <summary>
        /// The loaded asset.  This value is only set after the AsyncOperationHandle returned from LoadAsset completes.  It will not be set if only Instantiate is called.  It will be set to null if release is called.
        /// </summary>
        public Object Asset
        {
            get
            {
                return m_operation.Result as Object;
            }
        }

#if UNITY_EDITOR
        Object m_CachedAsset;
#endif
        /// <summary>
        /// String representation of asset reference.
        /// </summary>
        /// <returns>The asset guid as a string.</returns>
        public override string ToString()
        {
#if UNITY_EDITOR
            return "[" + m_AssetGUID + "]" + m_CachedAsset;
#else
            return "[" + m_AssetGUID + "]";
#endif
        }

        /// <summary>
        /// Load the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <returns>The load operation.</returns>
        public AsyncOperationHandle<TObject> LoadAsset<TObject>()
        {
            AsyncOperationHandle<TObject> result = Addressables.LoadAsset<TObject>(RuntimeKey);
            m_operation = result;
            return result;
        }

        /// <summary>
        /// Loads the reference as a scene.
        /// </summary>
        /// <returns>The operation handle for the scene load.</returns>
        public AsyncOperationHandle<SceneInstance> LoadScene()
        {
            var result = Addressables.LoadScene(RuntimeKey);
            m_operation = result;
            return result;
        }
        /// <summary>
        /// Instantiate the referenced asset as type TObject.
        /// </summary>
        /// <param name="position">Position of the instantiated object.</param>
        /// <param name="rotation">Rotation of the instantiated object.</param>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <returns></returns>
        public AsyncOperationHandle<GameObject> Instantiate(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Addressables.Instantiate(RuntimeKey, position, rotation, parent, false);
        }

        /// <summary>
        /// Instantiate the referenced asset as type TObject.
        /// </summary>
        /// <typeparam name="TObject">The object type.</typeparam>
        /// <param name="parent">The parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Option to retain world space when instantiated with a parent.</param>
        /// <returns></returns>
        public AsyncOperationHandle<GameObject> Instantiate(Transform parent = null, bool instantiateInWorldSpace = false)
        {
            return Addressables.Instantiate(RuntimeKey, parent, instantiateInWorldSpace, true);
        }

        /// <summary>
        /// Release the internal operation handle.
        /// </summary>
        public void ReleaseAsset()
        {
            if (!m_operation.IsValid())
            {
                Debug.LogWarning("Cannot release a null or unloaded asset.");
                return;
            }
            Addressables.Release(m_operation);
            m_operation = default(AsyncOperationHandle);
        }


        /// <summary>
        /// Release an instantiated object.
        /// </summary>
        /// <param name="obj">The object to release.</param>
        public void ReleaseInstance(GameObject obj)
        {
            Addressables.Release(obj);
        }

        /// <summary>
        /// Validates that the referenced asset allowable for this asset reference.
        /// </summary>
        /// <param name="obj">The Object to validate.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(Object obj)
        {
            return true;
        }
        
        /// <summary>
        /// Validates that the referenced asset allowable for this asset reference.
        /// </summary>
        /// <param name="path">The path to the asset in question.</param>
        /// <returns>Whether the referenced asset is valid.</returns>
        public virtual bool ValidateAsset(string path)
        {
            return true;
        }

#if UNITY_EDITOR

        /// <summary>
        /// Used by the editor to represent the asset referenced.
        /// </summary>
        public Object editorAsset
        {
            get
            {
                if (m_CachedAsset != null || string.IsNullOrEmpty(m_AssetGUID))
                    return m_CachedAsset;
                var assetPath = AssetDatabase.GUIDToAssetPath(m_AssetGUID);
                var mainType = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                return (m_CachedAsset = AssetDatabase.LoadAssetAtPath(assetPath, mainType));
            }
        }
        /// <summary>
        /// Sets the asset on the AssetReference.  Only valid in the editor, this sets both the editorAsset attribute,
        ///   and the internal asset GUID, which drives the RuntimeKey attribute.
        /// <param name="value">Object to reference</param>
        /// </summary>
        public bool SetEditorAsset(Object value)
        {
            if(value == null)
            {
                m_CachedAsset = null;
                m_AssetGUID = string.Empty;
                return true;
            }

            if (m_CachedAsset != value)
            {
                var path = AssetDatabase.GetAssetOrScenePath(value);
                if (string.IsNullOrEmpty(path))
                {
                    Addressables.LogWarningFormat("Invalid object for AssetReference {0}.", value);
                    return false;
                }
                if (!ValidateAsset(path))
                {
                    Addressables.LogWarningFormat("Invalid asset for AssetReference path = '{0}'.", path);
                    return false;
                }
                else
                {
                    m_AssetGUID = AssetDatabase.AssetPathToGUID(path);
                    m_CachedAsset = value;
                }
            }

            return true;
        }
#endif
    }
}
