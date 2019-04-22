using System;
using System.IO;
using UnityEngine;

namespace Unity.CacheServer
{
    /// <inheritdoc />
    /// <summary>
    /// IDownloadItem implementation for downloading to a file specified by path
    /// </summary>
    public class FileDownloadItem : IDownloadItem
    {
        private Stream m_writeStream;
        private string m_tmpPath;

        public FileId Id { get; private set; }
        public FileType Type { get; private set; }
        
        /// <summary>
        /// File path where downloaded file data is saved. Data is first written to a temporary file location, and moved
        /// into place when the Finish() method is called by the Cache Server Client.
        /// </summary>
        public string FilePath { get; private set; }

        public Stream GetWriteStream(long size)
        {
            if (m_writeStream == null)
            {
                m_tmpPath = Path.GetTempFileName();
                m_writeStream = new FileStream(m_tmpPath, FileMode.Create, FileAccess.Write);
            }

            return m_writeStream;
        }

        public FileDownloadItem(FileId fileId, FileType fileType, string path)
        {
            Id = fileId;
            Type = fileType;
            FilePath = path;
        }
        
        public void Finish()
        {
            if (m_writeStream == null)
                return;
            
            m_writeStream.Dispose();
            try
            {
                if(File.Exists(FilePath))
                    File.Delete(FilePath);
                
                File.Move(m_tmpPath, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
    }
}