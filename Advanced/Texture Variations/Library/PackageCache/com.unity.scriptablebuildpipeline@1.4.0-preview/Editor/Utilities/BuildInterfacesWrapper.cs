using System;
using System.Reflection;

namespace UnityEditor.Build.Pipeline.Utilities
{
    public class BuildInterfacesWrapper : IDisposable
    {
        Type m_Type = null;

        bool m_Disposed = false;

        public BuildInterfacesWrapper()
        {
            // BuildPipelineInterfaces is internal, and needs a little cleaning before it can be made public,
            // but we need to use this for IProcessScene, IProcessSceneWithReport, & IPreprocessShaders callbacks.
            // So use reflection for now, pay no attention to the man behind the curtain
            m_Type = Type.GetType("UnityEditor.Build.BuildPipelineInterfaces, UnityEditor");
            var init = m_Type.GetMethod("InitializeBuildCallbacks", BindingFlags.NonPublic | BindingFlags.Static);
            init.Invoke(null, new object[] { 18 }); // 18 = BuildCallbacks.SceneProcessors | BuildCallbacks.ShaderProcessors
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            var clean = m_Type.GetMethod("CleanupBuildCallbacks", BindingFlags.NonPublic | BindingFlags.Static);
            clean.Invoke(null, null);

            m_Disposed = true;
        }
    }
}
