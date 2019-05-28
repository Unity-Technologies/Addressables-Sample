using System;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace UnityEditor.AddressableAssets.Build.AnalyzeRules
{
    [Serializable]
    class AnalyzeRule
    {
        internal virtual bool CanFix { get; set; }

        [SerializeField]
        internal List<AnalyzeResult> m_Results = new List<AnalyzeResult>();

        [NonSerialized]
        protected AnalyzeResult noErrors = new AnalyzeResult("No issues found");

        public const char kDelimiter = ':';
        [Serializable]
        internal class AnalyzeResult
        {
            [SerializeField]
            string m_ResultName;

            public string resultName
            {
                get { return m_ResultName; }
                set { m_ResultName = value; }
            }

            [SerializeField]
            MessageType m_Severity;
            public MessageType severity
            {
                get { return m_Severity; }
                set { m_Severity = value; }
            }

            public AnalyzeResult(string newName, MessageType sev = MessageType.None)
            {
                resultName = newName;
                severity = sev;
            }
        }
        internal virtual string ruleName
        {
            get { return GetType().ToString(); }
        }
        internal virtual List<AnalyzeResult> RefreshAnalysis(AddressableAssetSettings settings)
        {
            return new List<AnalyzeResult>();
        }

        internal virtual void FixIssues(AddressableAssetSettings settings)
        {
        }

        internal virtual void ClearAnalysis()
        {
            m_Results.Clear();
        }

        internal virtual void Revert()
        {
        }
    }
}
