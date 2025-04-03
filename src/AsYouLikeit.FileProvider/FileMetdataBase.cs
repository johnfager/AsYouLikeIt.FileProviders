using System;
using System.Collections.Generic;
using System.Text;

namespace AsYouLikeit.FileProviders
{
    public partial class FileMetdataBase : IFileMetadata
    {
        public string FullPath { get; set; }
      
        public string DirectoryPath { get; set; }
  
        public string FileName { get; set; }
   
        public long Size { get; set; }
   
        public DateTimeOffset LastModified { get; set; }
 
        public IDictionary<string, string> MetaData { get; set; } = new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
