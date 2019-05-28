using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.HostingServices
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    /// <summary>
    /// HTTP implemenation of hosting service.
    /// </summary>
    public class HttpHostingService : BaseHostingService
    {
        /// <summary>
        /// Enum helper for standard Http result codes
        /// </summary>
        protected enum ResultCode
        {
            Ok = 200,
            NotFound = 404
        }

        const string k_HostingServicePortKey = "HostingServicePort";
        const int k_FileReadBufferSize = 64 * 1024;

        static readonly IPEndPoint k_DefaultLoopbackEndpoint = new IPEndPoint(IPAddress.Loopback, 0);
        int m_ServicePort;
        readonly List<string> m_ContentRoots;
        readonly Dictionary<string, string> m_ProfileVariables;
        
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// The actual Http listener used by this service
        /// </summary>
        protected HttpListener MyHttpListener { get; set; }

        /// <summary>
        /// The port number on which the service is listening
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public int HostingServicePort
        {
            get
            {
                return m_ServicePort;
            }
            protected set
            {
                if (value > 0 && IsPortAvailable(value))
                    m_ServicePort = value;
            }
        }

        /// <inheritdoc/>
        public override bool IsHostingServiceRunning
        {
            get { return MyHttpListener != null && MyHttpListener.IsListening; }
        }

        /// <inheritdoc/>
        public override List<string> HostingServiceContentRoots
        {
            get { return m_ContentRoots; }
        }

        /// <inheritdoc/>
        public override Dictionary<string, string> ProfileVariables
        {
            get
            {
                m_ProfileVariables[k_HostingServicePortKey] = HostingServicePort.ToString();
                m_ProfileVariables[DisambiguateProfileVar(k_HostingServicePortKey)] = HostingServicePort.ToString();
                return m_ProfileVariables;
            }
        }

        /// <summary>
        /// Create a new <see cref="HttpHostingService"/>
        /// </summary>
        public HttpHostingService()
        {
            m_ProfileVariables = new Dictionary<string, string>();
            m_ContentRoots = new List<string>();
            MyHttpListener = new HttpListener();
        }

        /// <inheritdoc/>
        public override void StartHostingService()
        {
            if (IsHostingServiceRunning)
                return;

            if (HostingServicePort <= 0)
            {
                HostingServicePort = GetAvailablePort();
            }
            else if (!IsPortAvailable(HostingServicePort))
            {
                LogError("Port {0} is in use, cannot start service!", HostingServicePort);
                return;
            }

            if (HostingServiceContentRoots.Count == 0)
            {
                throw new Exception(
                    "ContentRoot is not configured; cannot start service. This can usually be fixed by modifying the BuildPath for any new groups and/or building content.");
            }

            ConfigureHttpListener();
            MyHttpListener.Start();
            MyHttpListener.BeginGetContext(HandleRequest, null);
            Log("Started. Listening on port {0}", HostingServicePort);
        }

        /// <inheritdoc/>
        public override void StopHostingService()
        {
            if (!IsHostingServiceRunning) return;
            Log("Stopping");
            MyHttpListener.Stop();
        }

        /// <inheritdoc/>
        public override void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            {
                var newPort = EditorGUILayout.DelayedIntField("Port", HostingServicePort);
                if (newPort != HostingServicePort)
                {
                    if (IsPortAvailable(newPort))
                        ResetListenPort(newPort);
                    else
                        LogError("Cannot listen on port {0}; port is in use", newPort);
                }

                if (GUILayout.Button("Reset", GUILayout.MaxWidth(150)))
                    ResetListenPort();

                //GUILayout.Space(rect.width / 2f);
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <inheritdoc/>
        public override void OnBeforeSerialize(KeyDataStore dataStore)
        {
            dataStore.SetData(k_HostingServicePortKey, HostingServicePort);
            base.OnBeforeSerialize(dataStore);
        }

        /// <inheritdoc/>
        public override void OnAfterDeserialize(KeyDataStore dataStore)
        {
            HostingServicePort = dataStore.GetData(k_HostingServicePortKey, 0);
            base.OnAfterDeserialize(dataStore);
        }

        /// <summary>
        /// Listen on a new port then next time the server starts. If the server is already running, it will be stopped
        /// and restarted automatically.
        /// </summary>
        /// <param name="port">Specify a port to listen on. Default is 0 to choose any open port</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public void ResetListenPort(int port = 0)
        {
            var isRunning = IsHostingServiceRunning;
            StopHostingService();
            HostingServicePort = port;

            if (isRunning)
                StartHostingService();
        }

        /// <summary>
        /// Handles any configuration necessary for <see cref="MyHttpListener"/> before listening for connections. 
        /// </summary>
        protected virtual void ConfigureHttpListener()
        {
            try
            {
                MyHttpListener.Prefixes.Clear();
                MyHttpListener.Prefixes.Add("http://+:" + HostingServicePort + "/");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// Asynchronous callback to handle a client connection request on <see cref="MyHttpListener"/>. This method is
        /// recursive in that it will call itself immediately after receiving a new incoming request to listen for the
        /// next connection.
        /// </summary>
        /// <param name="ar">Asynchronous result from previous request. Pass null to listen for an initial request</param>
        /// <exception cref="ArgumentOutOfRangeException">thrown when the request result code is unknown</exception>
        protected virtual void HandleRequest(IAsyncResult ar)
        {
            if (!IsHostingServiceRunning)
                return;

            var c = MyHttpListener.EndGetContext(ar);
            MyHttpListener.BeginGetContext(HandleRequest, null);

            var relativePath = c.Request.RawUrl.Substring(1);

            var fullPath = FindFileInContentRoots(relativePath);
            var result = fullPath != null ? ResultCode.Ok : ResultCode.NotFound;
            var info = fullPath != null ? new FileInfo(fullPath) : null;
            var size = info != null ? info.Length.ToString() : "-";
            var remoteAddress = c.Request.RemoteEndPoint != null ? c.Request.RemoteEndPoint.Address : null;
            var timestamp = DateTime.Now.ToString("o");

            Log("{0} - - [{1}] \"{2}\" {3} {4}", remoteAddress, timestamp, fullPath, (int) result, size);

            switch (result)
            {
                case ResultCode.Ok:
                    ReturnFile(c, fullPath);
                    break;
                case ResultCode.NotFound:
                    Return404(c);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Searches for the given relative path within the configured content root directores.
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns>The full system path to the file if found, or null if file could not be found</returns>
        protected virtual string FindFileInContentRoots(string relativePath)
        {
            foreach (var root in HostingServiceContentRoots)
            {
                var fullPath = Path.Combine(root, relativePath);
                if (File.Exists(fullPath))
                    return fullPath;
            }

            return null;
        }

        /// <summary>
        /// Sends a file to the connected HTTP client
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filePath"></param>
        /// <param name="readBufferSize"></param>
        protected virtual void ReturnFile(HttpListenerContext context, string filePath, int readBufferSize = k_FileReadBufferSize)
        {
            context.Response.ContentType = "application/octet-stream";

            var buffer = new byte[readBufferSize];
            using (var fs = File.OpenRead(filePath))
            {
                context.Response.ContentLength64 = fs.Length;
                int read;
                while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
                    context.Response.OutputStream.Write(buffer, 0, read);
            }

            context.Response.OutputStream.Close();
        }

        /// <summary>
        /// Sets the status code to 404 on the given <see cref="HttpListenerContext"/> object
        /// </summary>
        /// <param name="context"></param>
        protected virtual void Return404(HttpListenerContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.Close();
        }

        /// <summary>
        /// Tests to see if the given port # is already in use 
        /// </summary>
        /// <param name="port">port number to test</param>
        /// <returns>true if there is not a listener on the port</returns>
        protected static bool IsPortAvailable(int port)
        {
            try
            {
                if (port <= 0)
                    return false;

                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect(IPAddress.Loopback, port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(500));
                    if (!success)
                        return true;

                    client.EndConnect(result);
                }
            }
            catch
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Find an open network listen port on the local system
        /// </summary>
        /// <returns>a system assigned port, or 0 if none are available</returns>
        protected static int GetAvailablePort()
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Bind(k_DefaultLoopbackEndpoint);

                var endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint != null ? endPoint.Port : 0;
            }
        }
    }
}