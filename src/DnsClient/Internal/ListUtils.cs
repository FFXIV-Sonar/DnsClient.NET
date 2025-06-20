using System.Collections.Generic;

namespace DnsClient.Internal
{
    internal static class ListUtils
    {
        public static IReadOnlyList<T> GetOrCreateList<T>(IEnumerable<T> source)
        {
            if (source is IReadOnlyList<T> list) return list;
            return [.. source];
        }


    }
}
