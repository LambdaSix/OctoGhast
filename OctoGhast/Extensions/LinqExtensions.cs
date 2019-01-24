using System;
using System.Collections.Generic;

namespace OctoGhast.Extensions {
    public static class LinqExtensions {
        public static IEnumerable<TOut> Pair<TOut, T>(this IEnumerable<T> source, Func<T, T, TOut> map) {
            using (var iterator = source.GetEnumerator()) {
                while (iterator.MoveNext()) {
                    var first = iterator.Current;
                    var second = iterator.MoveNext() ? iterator.Current : default(T);
                    yield return map(first, second);
                }
            }
        }
    }
}