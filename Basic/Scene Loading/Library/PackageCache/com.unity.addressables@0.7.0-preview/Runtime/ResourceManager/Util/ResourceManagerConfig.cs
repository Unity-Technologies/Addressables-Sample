using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement.Util
{
    /// <summary>
    /// Interface for objects that support post construction initialization via an id and byte array.
    /// </summary>
    public interface IInitializableObject
    {
        /// <summary>
        /// Initialize a constructed object.
        /// </summary>
        /// <param name="id">The id of the object.</param>
        /// <param name="data">Serialized data for the object.</param>
        /// <returns>The result of the initialization.</returns>
        bool Initialize(string id, string data);
    }


    /// <summary>
    /// Interface for objects that can create object initialization data.
    /// </summary>
    public interface IObjectInitializationDataProvider
    {
        /// <summary>
        /// The name used in the GUI for this provider
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Construct initialization data for runtime.
        /// </summary>
        /// <returns>Init data that will be deserialized at runtime.</returns>
        ObjectInitializationData CreateObjectInitializationData();
    }

    /// <summary>
    /// Allocation strategy for managing heap allocations
    /// </summary>
    public interface IAllocationStrategy
    {
        /// <summary>
        /// Create a new object of type t.
        /// </summary>
        /// <param name="type">The type to return.</param>
        /// <param name="typeHash">The hash code of the type.</param>
        /// <returns>The new object.</returns>
        object New(Type type, int typeHash);
        /// <summary>
        /// Release an object.
        /// </summary>
        /// <param name="obj">The object to release.</param>
        void Release(int typeHash, object obj);
    }
    /// <summary>
    /// Default allocator that relies in garbace collection
    /// </summary>
    public class DefaultAllocationStrategy : IAllocationStrategy
    {
        /// <inheritdoc/>
        public object New(Type type, int typeHash)
        {
            return Activator.CreateInstance(type);
        }
        /// <inheritdoc/>
        public void Release(int typeHash, object obj)
        {
            
        }
    }

    /// <summary>
    /// Allocation strategy that uses internal pools of objects to avoid allocations that can trigger GC calls.
    /// </summary>
    public class LRUCacheAllocationStrategy : IAllocationStrategy
    {
        int m_poolMaxSize;
        int m_poolInitialCapacity;
        int m_poolCacheMaxSize;
        List<List<object>> m_poolCache = new List<List<object>>();
        Dictionary<int, List<object>> m_cache = new Dictionary<int, List<object>>();
        /// <summary>
        /// Create a new LRUAllocationStrategy objct.
        /// </summary>
        /// <param name="poolMaxSize">The max size of each pool.</param>
        /// <param name="poolCapacity">The initial capacity to create each pool list with.</param>
        /// <param name="poolCacheMaxSize">The max size of the internal pool cache.</param>
        /// <param name="initialPoolCacheCapacity">The initial number of pools to create.</param>
        public LRUCacheAllocationStrategy(int poolMaxSize, int poolCapacity, int poolCacheMaxSize, int initialPoolCacheCapacity)
        {
            m_poolMaxSize = poolMaxSize;
            m_poolInitialCapacity = poolCapacity;
            m_poolCacheMaxSize = poolCacheMaxSize;
            for (int i = 0; i < initialPoolCacheCapacity; i++)
                m_poolCache.Add(new List<object>(m_poolInitialCapacity));
        }

        List<object> GetPool()
        {
            int count = m_poolCache.Count;
            if (count == 0)
                return new List<object>(m_poolInitialCapacity);
            var pool = m_poolCache[count - 1];
            m_poolCache.RemoveAt(count - 1);
            return pool;
        }

        void ReleasePool(List<object> pool)
        {
            if (m_poolCache.Count < m_poolCacheMaxSize)
                m_poolCache.Add(pool);
        }

        /// <inheritdoc/>
        public object New(Type type, int typeHash)
        {
            List<object> pool;
            if (m_cache.TryGetValue(typeHash, out pool))
            {
                var count = pool.Count;
                var v = pool[count - 1];
                pool.RemoveAt(count - 1);
                if (count == 1)
                {
                    m_cache.Remove(typeHash);
                    ReleasePool(pool);
                }
                return v;
            }
            return Activator.CreateInstance(type);
        }

        /// <inheritdoc/>
        public void Release(int typeHash, object obj)
        {
            List<object> pool;
            if (!m_cache.TryGetValue(typeHash, out pool))
                m_cache.Add(typeHash, pool = GetPool());
            if (pool.Count < m_poolMaxSize)
                pool.Add(obj);
        }
    }

    /// <summary>
    /// Attribute for restricting which types can be assigned to a SerializedType
    /// </summary>
    public class SerializedTypeRestrictionAttribute : Attribute
    {
        /// <summary>
        /// The type to restrict a serialized type to.
        /// </summary>
        public Type type;
    }

    /// <summary>
    /// Cache for nodes of LinkedLists.  This can be used to eliminate GC allocations.
    /// </summary>
    /// <typeparam name="T">The type of node.</typeparam>
    public class LinkedListNodeCache<T>
    {
        int m_NodesCreated = 0;
        LinkedList<T> m_NodeCache;
        /// <summary>
        /// Creates or returns a LinkedListNode of the requested type and set the value.
        /// </summary>
        /// <param name="val">The value to set to returned node to.</param>
        /// <returns>A LinkedListNode with the value set to val.</returns>
        public LinkedListNode<T> Acquire(T val)
        {
            if (m_NodeCache != null)
            {
                var n = m_NodeCache.First;
                if (n != null)
                {
                    m_NodeCache.RemoveFirst();
                    n.Value = val;
                    return n;
                }
            }
            m_NodesCreated++;
            return new LinkedListNode<T>(val);
        }

        /// <summary>
        /// Release the linked list node for later use.
        /// </summary>
        /// <param name="node"></param>
        public void Release(LinkedListNode<T> node)
        {
            if (m_NodeCache == null)
                m_NodeCache = new LinkedList<T>();

            node.Value = default(T);
            m_NodeCache.AddLast(node);
        }
        internal int CreatedNodeCount { get { return m_NodesCreated; } }
        internal int CachedNodeCount { get { return m_NodeCache == null ? 0 : m_NodeCache.Count; } }
    }

    internal static class GlobalLinkedListNodeCache<T>
    {
        static LinkedListNodeCache<T> m_globalCache;
        public static LinkedListNode<T> Acquire(T val)
        {
            if (m_globalCache == null)
                m_globalCache = new LinkedListNodeCache<T>();
            return m_globalCache.Acquire(val);
        }
        public static void Release(LinkedListNode<T> node)
        {
            if (m_globalCache == null)
                m_globalCache = new LinkedListNodeCache<T>();
            m_globalCache.Release(node);
        }
    }

    /// <summary>
    /// Wrapper for serializing types for runtime.
    /// </summary>
    [Serializable]
    public struct SerializedType
    {
        [FormerlySerializedAs("m_assemblyName")]
        [SerializeField]
        string m_AssemblyName;
        /// <summary>
        /// The assembly name of the type.
        /// </summary>
        public string AssemblyName { get { return m_AssemblyName; } }

        [FormerlySerializedAs("m_className")]
        [SerializeField]
        string m_ClassName;
        /// <summary>
        /// The name of the type.
        /// </summary>
        public string ClassName { get { return m_ClassName; } }

        Type m_CachedType;

        /// <inheritdoc/>
        public override string ToString()
        {
            return Value == null ? "" : Value.Name;
        }

        /// <summary>
        /// Get and set the serialized type.
        /// </summary>
        public Type Value
        {
            get
            {
                if (string.IsNullOrEmpty(m_AssemblyName) || string.IsNullOrEmpty(m_ClassName))
                    return null;

                if (m_CachedType == null)
                {
                    var assembly = Assembly.Load(m_AssemblyName);
                    if (assembly != null)
                        m_CachedType = assembly.GetType(m_ClassName);
                }
                return m_CachedType;
            }
            set
            {
                if (value != null)
                {
                    m_AssemblyName = value.Assembly.FullName;
                    m_ClassName = value.FullName;
                }
                else
                {
                    m_AssemblyName = m_ClassName = null;
                }
            }
        }
    }

    /// <summary>
    /// Contains data used to construct and initialize objects at runtime.
    /// </summary>
    [Serializable]
    public struct ObjectInitializationData
    {
#pragma warning disable 0649
        [FormerlySerializedAs("m_id")]
        [SerializeField]
        string m_Id;
        /// <summary>
        /// The object id.
        /// </summary>
        public string Id { get { return m_Id; } }

        [FormerlySerializedAs("m_objectType")]
        [SerializeField]
        SerializedType m_ObjectType;
        /// <summary>
        /// The object type that will be created.
        /// </summary>
        public SerializedType ObjectType { get { return m_ObjectType; } }

        [FormerlySerializedAs("m_data")]
        [SerializeField]
        string m_Data;
        /// <summary>
        /// String representation of the data that will be passed to the IInitializableObject.Initialize method of the created object.  This is usually a JSON string of the serialized data object.
        /// </summary>
        public string Data { get { return m_Data; } }
#pragma warning restore 0649 

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("ObjectInitializationData: id={0}, type={1}", m_Id, m_ObjectType);
        }

        /// <summary>
        /// Create an instance of the defined object.  Initialize will be called on it with the id and data if it implements the IInitializableObject interface.
        /// </summary>
        /// <param name="idOverride">Optional id to assign to the created object.  This only applies to objects that inherit from IInitializableObject.</param>
        /// <returns>Constructed object.  This object will already be initialized with its serialized data and id.</returns>
        public TObject CreateInstance<TObject>(string idOverride = null)
        {
            try
            {
                var objType = m_ObjectType.Value;
                if (objType == null)
                    return default(TObject);
                var obj = Activator.CreateInstance(objType, true);
                var serObj = obj as IInitializableObject;
                if (serObj != null)
                {
                    if (!serObj.Initialize(idOverride == null ? m_Id : idOverride, m_Data))
                        return default(TObject);
                }
                return (TObject)obj;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return default(TObject);
            }
        }

#if UNITY_EDITOR
        Type[] m_RuntimeTypes;
        /// <summary>
        /// Construct a serialized data for the object.
        /// </summary>
        /// <param name="objectType">The type of object to create.</param>
        /// <param name="id">The object id.</param>
        /// <param name="dataObject">The serializable object that will be saved into the Data string via the JSONUtility.ToJson method.</param>
        /// <returns>Contains data used to construct and initialize an object at runtime.</returns>
        public static ObjectInitializationData CreateSerializedInitializationData(Type objectType, string id = null, object dataObject = null)
        {
            return new ObjectInitializationData
            {
                m_ObjectType = new SerializedType { Value = objectType },
                m_Id = string.IsNullOrEmpty(id) ? objectType.FullName : id,
                m_Data = dataObject == null ? null : JsonUtility.ToJson(dataObject),
                m_RuntimeTypes = dataObject == null ? null : new[] { dataObject.GetType() }
            };
        }

        /// <summary>
        /// Construct a serialized data for the object.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="id">The ID for the object</param>
        /// <param name="dataObject">The serializable object that will be saved into the Data string via the JSONUtility.ToJson method.</param>
        /// <returns>Contains data used to construct and initialize an object at runtime.</returns>
        public static ObjectInitializationData CreateSerializedInitializationData<T>(string id = null, object dataObject = null)
        {
            return CreateSerializedInitializationData(typeof(T), id, dataObject);
        }

        /// <summary>
        /// Get the set of runtime types need to deserialized this object.  This is used to ensure that types are not stripped from player builds.
        /// </summary>
        /// <returns></returns>
        public Type[] GetRuntimeTypes()
        {
            return m_RuntimeTypes;
        }
#endif
    }

    static class ResourceManagerConfig
    {
        public static Array CreateArrayResult(Type type, Object[] allAssets)
        {
            var elementType = type.GetElementType();
            if (elementType == null)
                return null;
            int length = 0;
            foreach (var asset in allAssets)
            {
                if (asset.GetType() == elementType)
                    length++;
            }
            var array = Array.CreateInstance(elementType, length);
            int index = 0;

            foreach (var asset in allAssets)
            {
                if(elementType.IsAssignableFrom(asset.GetType()))
                    array.SetValue(asset, index++);
            }

            return array;
        }

        public static TObject CreateArrayResult<TObject>(Object[] allAssets) where TObject : class
        {
            return CreateArrayResult(typeof(TObject), allAssets) as TObject;
        }

        public static IList CreateListResult(Type type, Object[] allAssets)
        {
            var genArgs = type.GetGenericArguments();
            var listType = typeof(List<>).MakeGenericType(genArgs);
            var list = Activator.CreateInstance(listType) as IList;
            var elementType = genArgs[0];
            if (list == null)
                return null;
            foreach (var a in allAssets)
            {
                if(elementType.IsAssignableFrom(a.GetType()))
                    list.Add(a);
            }
            return list;
        }

        public static TObject CreateListResult<TObject>(Object[] allAssets)
        {
            return (TObject)CreateListResult(typeof(TObject), allAssets);
        }

        public static bool IsInstance<T1, T2>()
        {
            var tA = typeof(T1);
            var tB = typeof(T2);
#if !UNITY_EDITOR && UNITY_WSA_10_0 && ENABLE_DOTNET
            return tB.GetTypeInfo().IsAssignableFrom(tA.GetTypeInfo());
#else
            return tB.IsAssignableFrom(tA);
#endif
        }

    }
}
