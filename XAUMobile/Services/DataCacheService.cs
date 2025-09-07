using Newtonsoft.Json.Linq;

namespace XAUMobile
{
    // USER DATA CACHE SERVICE. USED TO CACHE USER & GAME DATA TO AVOID REPITITVE API CALLS TO XBOX.
    // see xaml.cs pages for expiration time (10 mins for profile; 30 mins for games

    public class UserDataCacheService
    {
        private Dictionary<string, JObject> _cache = new Dictionary<string, JObject>();

        public void CacheData(string key, JObject data)
        {
            _cache[key] = data;
        }

        public JObject? GetCachedData(string key)
        {
            if (_cache.ContainsKey(key))
            {
                return _cache[key];
            }
            return null;
        }
    }
}