namespace ApiSearchConsole.Services
{
    public class CacheService
    {
        private readonly string _cacheDirectory;

        public CacheService(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;
            Directory.CreateDirectory(_cacheDirectory);
        }

        public string GetCacheFilePath(string url)
        {
            var fileName = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(url)) + ".html";
            return Path.Combine(_cacheDirectory, fileName);
        }

        public bool IsCached(string url)
        {
            return File.Exists(GetCacheFilePath(url));
        }

        public void SaveToCache(string url, string content)
        {
            File.WriteAllText(GetCacheFilePath(url), content);
        }

        public string LoadFromCache(string url)
        {
            return File.ReadAllText(GetCacheFilePath(url));
        }
    }
}