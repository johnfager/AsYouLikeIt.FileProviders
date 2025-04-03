using System;
using System.Collections.Generic;
using System.Text;
using Metadata = System.Collections.Generic.IDictionary<string, string>;

namespace AsYouLikeit.FileProviders
{
    public interface IFileMetadata
    {
        /// <summary>
        /// Gets or sets the full path of the file, including the file name.
        /// </summary>
        string FullPath { get; set; }

        /// <summary>
        /// Gets or sets the directory path where the file is located.
        /// </summary>
        string DirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Gets or sets the size of the file in bytes.
        /// </summary>
        long Size { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the file was last modified.
        /// </summary>
        DateTimeOffset LastModified { get; set; }

        /// <summary>
        /// Gets or sets the metadata associated with the file.
        /// </summary>
        Metadata MetaData { get; set; }
    }
}
