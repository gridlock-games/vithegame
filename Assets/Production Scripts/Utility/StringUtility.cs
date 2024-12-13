using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Collections;
using System.Linq;
using UnityEngine;

namespace Vi.Utility
{
    public static class StringUtility
    {
        public static Color SetColorAlpha(Color baseColor, float newAlpha)
        {
            baseColor.a = newAlpha;
            return baseColor;
        }

        public static T Random<T>(this List<T> enumerable)
        {
            if (enumerable == null)
            {
                throw new System.ArgumentNullException(nameof(enumerable));
            }

            // note: creating a Random instance each call may not be correct for you,
            // consider a thread-safe static instance
            var list = enumerable as IList<T> ?? enumerable.ToList();
            return list.Count == 0 ? default(T) : list[UnityEngine.Random.Range(0, list.Count)];
        }

        public static float NormalizeValue(float value, float min, float max)
        {
            value = Mathf.Clamp(value, min, max);
            if (Mathf.Approximately(max - min, 0)) { return 0; }
            return (value - min) / (max - min);
        }

        /// <summary>
        ///     Calculate the difference between 2 strings using the Levenshtein distance algorithm
        /// </summary>
        /// <param name="source1">First string</param>
        /// <param name="source2">Second string</param>
        /// <returns></returns>
        public static int Calculate(string source1, string source2) //O(n*m)
        {
            var source1Length = source1.Length;
            var source2Length = source2.Length;

            var matrix = new int[source1Length + 1, source2Length + 1];

            // First calculation, if one entry is empty return full length
            if (source1Length == 0)
                return source2Length;

            if (source2Length == 0)
                return source1Length;

            // Initialization of matrix with row size source1Length and columns size source2Length
            for (var i = 0; i <= source1Length; matrix[i, 0] = i++) { }
            for (var j = 0; j <= source2Length; matrix[0, j] = j++) { }

            // Calculate rows and collumns distances
            for (var i = 1; i <= source1Length; i++)
            {
                for (var j = 1; j <= source2Length; j++)
                {
                    var cost = (source2[j - 1] == source1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(
                        Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                        matrix[i - 1, j - 1] + cost);
                }
            }
            // return result
            return matrix[source1Length, source2Length];
        }

        public static string FromCamelCase(string inputString)
        {
            string returnValue = inputString;

            //Strip leading "_" character
            returnValue = Regex.Replace(returnValue, "^_", "").Trim();
            //Add a space between each lower case character and upper case character
            returnValue = Regex.Replace(returnValue, "([a-z])([A-Z])", "$1 $2").Trim();
            //Add a space between 2 upper case characters when the second one is followed by a lower space character
            returnValue = Regex.Replace(returnValue, "([A-Z])([A-Z][a-z])", "$1 $2").Trim();

            if (char.IsLower(returnValue[0])) { returnValue = char.ToUpper(returnValue[0]) + returnValue[1..]; }

            return returnValue;
        }

        public static string EvaluateFixedString(FixedString64Bytes input)
        {
            if (input == "")
            {
                return null;
            }
            else
            {
                return input.ToString();
            }
        }

        public static string FormatDynamicFloatForUI(float dynamicFloat, float decimalThreshold = 10)
        {
            if (dynamicFloat < 0.1f & dynamicFloat > 0) { dynamicFloat = 0.1f; }
            return dynamicFloat < decimalThreshold & dynamicFloat > 0 ? dynamicFloat.ToString("F1") : dynamicFloat.ToString("F0");
        }
    }
}