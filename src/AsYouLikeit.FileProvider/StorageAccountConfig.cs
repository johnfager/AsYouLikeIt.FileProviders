
namespace AsYouLikeit.FileProviders
{
    public class StorageAccountConfig
    {
        public string StorageAccountName { get; set; }

        public string AccessKey { get; set; }

        public bool UseLowerCase { get; set; }

        public string EndpointSuffix { get; set; }
    }
}
