namespace PackageManagement
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;

    /// <summary>
    ///     Extension method class.
    /// </summary>
    internal static class ExtensionMethods
    {
        /// <summary>
        /// Reads all of the lines from a <see cref="Stream"/>. Stream is closed when finished.
        /// </summary>
        /// <param name="stream">This stream.</param>
        /// <returns>The lines.</returns>
        public static IEnumerable<string> ReadLines(this Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        /// <summary>
        /// Checks if a string contains the search string, using the invariant culture to ignore case.
        /// </summary>
        /// <param name="s">This string.</param>
        /// <param name="search">The string to search for.</param>
        /// <returns>True if this string contains the search string, otherwise false.</returns>
        public static bool ContainsIgnoreCase(this string s, string search)
        {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(s, search, CompareOptions.IgnoreCase) >= 0;
        }

        public static bool EqualsIgnoreCase(this string str1, string str2)
        {
            return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
