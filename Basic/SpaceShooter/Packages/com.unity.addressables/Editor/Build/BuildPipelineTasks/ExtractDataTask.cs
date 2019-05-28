using System;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.AddressableAssets.Build.BuildPipelineTasks
{
    class ExtractDataTask : IBuildTask
    {
        public int Version { get { return 1; } }

        public IDependencyData DependencyData { get { return m_DependencyData; } }

        public IBundleWriteData WriteData { get { return m_WriteData; } }

        public IBuildCache BuildCache { get { return m_BuildCache; } }

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IDependencyData m_DependencyData;

        [InjectContext(ContextUsage.In)]
        IBundleWriteData m_WriteData;

        [InjectContext(ContextUsage.In)]
        IBuildCache m_BuildCache;
#pragma warning restore 649

        public ReturnCode Run()
        {
            return ReturnCode.Success;
        }
    }
}