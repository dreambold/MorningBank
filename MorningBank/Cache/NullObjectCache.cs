using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MorningBank.Cache
{
    public class NullObjectCachee : IWebCache
    { // to allow disabling of caching
        public void Remove(string key)
        {
        }
        public void Store(string key, object obj)
        {
        }
        public T Retrieve<T>(string key)
        {
            return default(T);
        }
    }
}