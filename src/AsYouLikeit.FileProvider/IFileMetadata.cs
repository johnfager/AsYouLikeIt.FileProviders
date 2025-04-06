using System;
using System.Collections.Generic;
using System.Text;
using Metadata = System.Collections.Generic.IDictionary<string, string>;

namespace AsYouLikeit.FileProviders
{
    public interface IFileMetadata
    {

        /// <summary>
        /// Gets or sets the relative path of the file from the base directory. This is typically used for storage or display purposes.
        /// </summary>
        string AbsoluteDirectoryPath { get; set; }

        /// <summary>
        /// Gets or sets the relative file path from the base directory. This is typically used for storage or display purposes.
        /// </summary>
        string AbsoluteFilePath { get; set; }

        /// <summary>
        /// Gets or sets the file name.
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Gets or sets the file extension (e.g., ".txt", ".jpg"). This is typically used for file type identification and handling.
        /// </summary>
        string Extension { get; set; }

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
        Metadata Metadata { get; set; }
    }
}
