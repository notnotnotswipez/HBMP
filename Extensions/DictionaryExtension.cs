using System.Collections.Generic;

namespace HBMP.Extensions
{
    public static class DictionaryExtensions {
        public static V TryIdx<K, V>(this Dictionary<K, V> dict, K idx) {
            if (dict.ContainsKey(idx)) return dict[idx];
            return default;
        }
    }
}