using System;
using System.Text;
using System.Collections;

namespace Catneep.Utils
{
    public static class StringUtils
    {

        private static readonly StringBuilder stringBuilder = new StringBuilder();

        public static string ToBinaryText(this BitArray bitArray)
        {
            return ToBinaryText<bool>(bitArray, b => b);
        }
        public static string ToBinaryText<T>(this IEnumerable enumerable, Func<T, bool> boolSelector)
        {
            stringBuilder.Length = 0;
            foreach (T item in enumerable)
            {
                stringBuilder.Append(boolSelector(item) ? '1' : '0');
            }
            return stringBuilder.ToString();
        }

    }
}