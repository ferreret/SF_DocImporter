using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LibUtil
{
    public static class Common
    {
        public static string GenerateUniqueIdentifier(int length = 8)
        {
            // Get current timestamp
            string datePart = DateTime.Now.ToString("yyyyMMddHHmmssfff"); // Format: YYYYMMDDHHMMSSMMM

            // Combine date with a random number (ensuring it doesn't exceed the length limit)
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(datePart));
                string hash = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, length);
                return hash;
            }
        }

        // Method to calculate the Levenshtein distance
        public static int LevenshteinDistance(string s1, string s2)
        {
            // Convert both strings to uppercase and remove spaces
            s1 = s1.Replace(" ", "").ToUpper();
            s2 = s2.Replace(" ", "").ToUpper();

            int[,] dp = new int[s1.Length + 1, s2.Length + 1];

            for (int i = 0; i <= s1.Length; i++)
                dp[i, 0] = i;

            for (int j = 0; j <= s2.Length; j++)
                dp[0, j] = j;

            for (int i = 1; i <= s1.Length; i++)
            {
                for (int j = 1; j <= s2.Length; j++)
                {
                    int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;

                    dp[i, j] = Math.Min(
                        Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                        dp[i - 1, j - 1] + cost);
                }
            }

            return dp[s1.Length, s2.Length];
        }

        // Method to read file, compute distances, and return the result
        public static (string?, int) FindUniqueMinLevenshtein(string filePath, string inputString)
        {
            List<(string, int)> distances = new List<(string, int)>();

            // Read file and calculate Levenshtein distance for each line
            foreach (var line in File.ReadLines(filePath))
            {
                string value = line.Trim();
                int distance = LevenshteinDistance(value, inputString);
                distances.Add((value, distance));
            }

            // Sort distances by ascending order
            distances = distances.OrderBy(x => x.Item2).ToList();

            // Check if the minimum distance is unique
            int minDistance = distances.First().Item2;
            var minDistanceValues = distances.Where(x => x.Item2 == minDistance).ToList();

            // Return the value with the minimum distance and the distance if it's unique, otherwise return null and -1
            if (minDistanceValues.Count == 1)
            {
                return (minDistanceValues.First().Item1, minDistance); // Return the unique minimum distance value and the distance
            }
            else
            {
                return (null, -1); // Return null and -1 if the minimum distance is not unique
            }
        }
    }
}
