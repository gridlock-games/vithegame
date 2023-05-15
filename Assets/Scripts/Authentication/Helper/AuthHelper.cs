using System;
using System.Security.Cryptography;
using System.Text;

public class AuthHelper 
{
    public static byte[] Encrypt(string plainText, string key)
    {
        var iv = new byte[16];
        byte[] array;

        using (var aes = Aes.Create())
        {
            var pdb = new Rfc2898DeriveBytes(key, new byte[] {
                0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 
                0x76, 0x65, 0x64, 0x65, 0x76 }, 1000);

            aes.Key = pdb.GetBytes(32);
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (var ms = new System.IO.MemoryStream())
            {
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (var sw = new System.IO.StreamWriter(cs))
                        sw.Write(plainText);
                    array = ms.ToArray();
                }
            }
        }

        return array;
    }

    
    
    public static string Decrypt(byte[] cipherText, string key)
    {
        var iv = new byte[16];

        using (var aes = Aes.Create())
        {
            var pdb = new Rfc2898DeriveBytes(key, new byte[] {
                0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 
                0x76, 0x65, 0x64, 0x65, 0x76 }, 1000);

            aes.Key = pdb.GetBytes(32);
            aes.IV = iv;

            var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (var ms = new System.IO.MemoryStream(cipherText))
            {
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                {
                    using (var sr = new System.IO.StreamReader(cs))
                        return sr.ReadToEnd();
                }
            }
        }
    }


}
