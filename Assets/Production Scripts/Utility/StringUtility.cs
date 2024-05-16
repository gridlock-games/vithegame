using System;
using System.Text.RegularExpressions;
using Unity.Netcode;
using Unity.Collections;

namespace Vi.Utility
{
    public static class StringUtility
    {
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
    }

    public struct NetworkString64Bytes : INetworkSerializeByMemcpy
    {
        private ForceNetworkSerializeByMemcpy<FixedString64Bytes> _info;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info);
        }

        public override string ToString()
        {
            return _info.Value.ToString();
        }

        public static implicit operator string(NetworkString64Bytes s) => s.ToString();
        public static implicit operator NetworkString64Bytes(string s) => new NetworkString64Bytes() { _info = new FixedString64Bytes(s) };
    }

    public struct NetworkString512Bytes : INetworkSerializeByMemcpy
    {
        private ForceNetworkSerializeByMemcpy<FixedString512Bytes> _info;
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _info);
        }

        public override string ToString()
        {
            return _info.Value.ToString();
        }

        public static implicit operator string(NetworkString512Bytes s) => s.ToString();
        public static implicit operator NetworkString512Bytes(string s) => new NetworkString512Bytes() { _info = new FixedString512Bytes(s) };
    }
}