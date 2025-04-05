using System;
using System.Collections.Generic;
using System.Text;

namespace AsYouLikeit.FileProviders
{
    public partial class FileMetdataBase : IFileMetadata
    {
        public string AbsoluteDirectoryPath { get; set; }

        public string AbsoluteFilePath { get; set; }

        public string FileName { get; set; }

        public string Extension { get; set; }

        public long Size { get; set; }
   
        public DateTimeOffset LastModified { get; set; }
 
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
