using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Security.Cryptography;


/*
-------------------
-example:
    string text = "user=admin;pass=123";
    string password = "My$ecretKey";

    string encrypted = CryptoUtils.Encrypt(text, password);
    // Send via NetMQ...

    string decrypted = CryptoUtils.Decrypt(encrypted, password);
    Console.WriteLine(decrypted);  // ➜ user=admin;pass=123

-------------------

*/
namespace ProfitWin.Common
{
    public static class CryptoUtils
    {
        // Derives a 256-bit key from the given string key using SHA-256
        private static byte[] GetKey(string key)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(key));
            }
        }

        public static string Encrypt(string text, string key)
        {
            byte[] keyBytes = GetKey(key);
            using Aes aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();
            byte[] iv = aes.IV;

            using var encryptor = aes.CreateEncryptor();
            byte[] plainBytes = Encoding.UTF8.GetBytes(text);
            byte[] cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Prepend IV to ciphertext
            byte[] result = new byte[iv.Length + cipherBytes.Length];
            Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, iv.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }

        public static string Decrypt(string cipherBase64, string key)
        {
            byte[] keyBytes = GetKey(key);
            byte[] allBytes = Convert.FromBase64String(cipherBase64);

            using Aes aes = Aes.Create();
            aes.Key = keyBytes;

            // Extract IV
            byte[] iv = new byte[aes.BlockSize / 8];
            byte[] cipherBytes = new byte[allBytes.Length - iv.Length];
            Buffer.BlockCopy(allBytes, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(allBytes, iv.Length, cipherBytes, 0, cipherBytes.Length);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            byte[] plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }
}
