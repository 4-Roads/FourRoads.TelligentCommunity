using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FourRoads.TelligentCommunity.GoogleMfa.Logic
{
    public static class MfaCryptoExtension
    {
        /// <summary>
        /// Calculate hash, not going to decrypt it anyway
        /// </summary>
        /// <param name="code"></param>
        /// <param name="key"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static string Hash(this string code, string key, int userId)
        {
            string input = $"{key}{code}{userId:D8}".Replace(" ", string.Empty);
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            //if later we decide to change hashing algo,
            //we will be able to tell one from another
            //by looking at the prefix
            sb.Append("md5::");
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static string RandomAlphanumeric(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            StringBuilder res = new StringBuilder();

            byte[] uintBuffer = new byte[sizeof(uint)];

            while (length-- > 0)
            {
                Rand.GetBytes(uintBuffer);
                uint num = BitConverter.ToUInt32(uintBuffer, 0);
                res.Append(chars[(int)(num % (uint)chars.Length)]);
            }

            return res.ToString();
        }

        /// <summary>
        /// Return a random integer between a min and max value.
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static int RandomInteger(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                Rand.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (int)(min + (max - min) * (scale / (double)uint.MaxValue));
        }

        private static readonly RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();
    }
}
