using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace UnityEngine.ResourceManagement.ResourceProviders
{
    /// <summary>
    /// Class that contains properties to apply to instantiated objects.
    /// </summary>
    public struct InstantiationParameters
    {
        Vector3 m_Position;
        Quaternion m_Rotation;
        Transform m_Parent;
        bool m_InstantiateInWorldPosition;
        bool m_SetPositionRotation;

        /// <summary>
        /// Position in world space to instantiate object.
        /// </summary>
        public Vector3 Position { get { return m_Position; } }
        /// <summary>
        /// Rotation in world space to instantiate object.
        /// </summary>
        public Quaternion Rotation { get { return m_Rotation; } }
        /// <summary>
        /// Transform to set as the parent of the instantiated object.
        /// </summary>
        public Transform Parent { get { return m_Parent; } }
        /// <summary>
        /// When setting the parent Transform, this sets whether to preserve instance transform relative to world space or relative to the parent.
        /// </summary>
        public bool InstantiateInWorldPosition { get { return m_InstantiateInWorldPosition; } }
        /// <summary>
        /// Flag to tell the IInstanceProvider whether to set the position and rotation on new instances.
        /// </summary>
        public bool SetPositionRotation { get { return m_SetPositionRotation; } }
        /// <summary>
        /// Create a new InstantationParameters class that will set the parent transform and use the prefab transform.
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// <param name="instantiateInWorldSpace">Flag to tell the IInstanceProvider whether to set the position and rotation on new instances.</param>
        /// </summary>
        public InstantiationParameters(Transform parent, bool instantiateInWorldSpace)
        {
            m_Position = Vector3.zero;
            m_Rotation = Quaternion.identity;
            m_Parent = parent;
            m_InstantiateInWorldPosition = instantiateInWorldSpace;
            m_SetPositionRotation = false;
        }
        /// <summary>
        /// Create a new InstantationParameters class that will set the position, rotation, and Transform parent of the instance.
        /// <param name="position">Position relative to the parent to set on the instance.</param>
        /// <param name="rotation">Rotation relative to the parent to set on the instance.</param>
        /// <param name="parent">Transform to set as the parent of the instantiated object.</param>
        /// </summary>
        public InstantiationParameters(Vector3 position, Quaternion rotation, Transform parent)
        {
            m_Position = position;
            m_Rotation = rotation;
            m_Parent = parent;
            m_InstantiateInWorldPosition = false;
            m_SetPositionRotation = true;
        }

        /// <summary>
        /// Instantiate an object with the parameters of this object.
        /// <param name="source">Object to instantiate.</param>
        /// <returns>Instantiated object.</returns>
        /// <typeparam name="TObject">Object type. This type must be of type UnityEngine.Object.</typeparam>
        /// </summary>
        public TObject Instantiate<TObject>(TObject source) where TObject : Object
        {
            TObject result;
            if (m_Parent == null)
            {
                if (m_SetPositionRotation)
                    result = Object.Instantiate(source, m_Position, m_Rotation);
                else
                    result = Object.Instantiate(source);
            }
            else
            {
                if (m_SetPositionRotation)
                    result = Object.Instantiate(source, m_Position, m_Rotation, m_Parent);
                else
                    result = Object.Instantiate(source, m_Parent, m_InstantiateInWorldPosition);
            }
            return result;
        }
    }
    
    /// <summary>
    /// Interface that provides instances of objects.  This is used in ResourceManager.Instantiate* calls.
    /// </summary>
    public interface IInstanceProvider
    {
        /// <summary>
        /// Provide an instance of the gameobject contained in the prefabHandle.
        /// </summary>
        /// <param name="prefabHandle">The operation handle for the prefab to instantiate.</param>
        /// <param name="instantiateParameters">The parameters to use for instantation.</param>
        /// <returns>The instantiated object.</returns>
        GameObject ProvideInstance(ResourceManager resourceManager, AsyncOperationHandle<GameObject> prefabHandle, InstantiationParameters instantiateParameters);
        /// <summary>
        /// Release an instance.
        /// </summary>
        /// <param name="instance">The instance to release.</param>
        void ReleaseInstance(ResourceManager resourceManager, GameObject instance);
    }
}
