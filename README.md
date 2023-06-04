# AsYouLikeIt.FileProvider
## The As You Like It File Provider
Abstractions on locations for files and file systems that allow for cloud and local physical file access.
- Configures via DI to work with files on the physical file system for CRUD operations.

### IFileService
An interface to handle file sytem CRUD operations.

### FileSystemService
An implementation for physical file systems

### AzureBlobFileService
An implementation for Azure Blob Storage file systems.

***

## Version History

### 1.2.0.1
- Added support for listing directories.

### 1.2.1.0
- Added support for lower case conversion in Azure paths and more blob name safety checking
- Added support for different EndpointSuffix to StorageAccountConfig
- Clean up project folder structure