using System.Collections.Generic;

namespace UnityEditor.AddressableAssets.Build
{
    public class FileRegistry
    {
        private readonly HashSet<string> m_FilePaths;

        public FileRegistry()
        {
            m_FilePaths = new HashSet<string>();
        }

        public IEnumerable<string> GetFilePaths()
        {
            return new HashSet<string>(m_FilePaths);
        }

        public void AddFile(string path)
        {
            m_FilePaths.Add(path);
        }

        public void RemoveFile(string path)
        {
            m_FilePaths.Remove(path);
        }
    }
}
