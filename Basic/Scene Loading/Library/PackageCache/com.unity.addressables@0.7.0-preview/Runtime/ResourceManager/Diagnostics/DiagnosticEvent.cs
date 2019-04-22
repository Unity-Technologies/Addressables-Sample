using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Serialization;

namespace UnityEngine.ResourceManagement.Diagnostics
{
    /// <summary>
    /// Diagnostic event data.
    /// </summary>
    [Serializable]
    public struct DiagnosticEvent
    {
        [SerializeField]
        string m_Graph;  //id of graph definition to use
        [SerializeField]
        int[] m_Dependencies; //used to nest datagraphs
        [SerializeField]
        int m_ObjectId;     //id of a set of data streams
        [SerializeField]
        string m_DisplayName;
        [SerializeField]
        int m_Stream;    //data stream
        [SerializeField]
        int m_Frame;     //frame of the event
        [SerializeField]
        int m_Value;      //data value of event

        /// <summary>
        /// Gets the graph id that this event is intended for
        /// </summary>
        /// <value>The graph Id</value>
        public string Graph { get { return m_Graph; } }
        /// <summary>
        /// Unique object identifier for this event
        /// </summary>
        public int ObjectId { get { return m_ObjectId; } }
        /// <summary>
        /// Display name for event
        /// </summary>
        public string DisplayName { get { return m_DisplayName; } }
        /// <summary>
        /// Array of object identifiers that are dependencies for this event
        /// </summary>
        public int[] Dependencies { get { return m_Dependencies; } }
        /// <summary>
        /// The stream id for the event.  Each graph may display multiple streams of data for the same event Id
        /// </summary>
        /// <value>Stream Id</value>
        public int Stream { get { return m_Stream; } }
        /// <summary>
        /// The frame that the event occurred 
        /// </summary>
        /// <value>Frame number</value>
        public int Frame { get { return m_Frame; } }
        /// <summary>
        /// The value of the event. This value depends on the event type
        /// </summary>
        /// <value>Event value</value>
        public int Value { get { return m_Value; } }

        /// <summary>
        /// DiagnosticEvent constructor
        /// </summary>
        /// <param name="graph">Graph id.</param>
        /// <param name="name">Event name.</param>
        /// <param name="id">Event id.</param>
        /// <param name="stream">Stream index.</param>
        /// <param name="frame">Frame number.</param>
        /// <param name="value">Event value.</param>
        /// <param name="deps">Array of dependency event ids.</param>
        public DiagnosticEvent(string graph, string name, int id, int stream, int frame, int value, int[] deps)
        {
            m_Graph = graph;
            m_DisplayName = name;
            m_ObjectId = id;
            m_Stream = stream;
            m_Frame = frame;
            m_Value = value;
            m_Dependencies = deps;
        }

        /// <summary>
        /// Serializes the event into JSON and then encodes with System.Text.Encoding.ASCII.GetBytes
        /// </summary>
        /// <returns>Byte array containing serialized version of the event</returns>
        internal byte[] Serialize()
        {
            return Encoding.ASCII.GetBytes(JsonUtility.ToJson(this));
        }

        /// <summary>
        /// Deserializes event from a byte array created by the <see cref="Serialize"/> method
        /// </summary>
        /// <returns>Deserialized DiagnosticEvent struct</returns>
        /// <param name="data">Serialized data</param>
        public static DiagnosticEvent Deserialize(byte[] data)
        {
            return JsonUtility.FromJson<DiagnosticEvent>(Encoding.ASCII.GetString(data));
        }
    }
}