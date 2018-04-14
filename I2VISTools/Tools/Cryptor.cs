using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace I2VISTools.Tools
{
    class Cryptor
    {
        /// /// Шифрует строку value
        /// 
        /// Строка которую необходимо зашифровать
        /// Ключ шифрования
        public static string Encrypt(string str, string keyCrypt)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(str), keyCrypt));
        }

        /// /// Расшифроывает данные из строки value
        /// 
        /// Зашифрованая строка
        /// Ключ шифрования
        /// Возвращает null, если прочесть данные не удалось
        [DebuggerNonUserCode]
        public static string Decrypt(string str, string keyCrypt)
        {
            string result;
            try
            {
                CryptoStream Cs = InternalDecrypt(Convert.FromBase64String(str), keyCrypt);
                var sr = new StreamReader(Cs);

                result = sr.ReadToEnd();

                Cs.Close();
                Cs.Dispose();

                sr.Close();
                sr.Dispose();
            }
            catch (CryptographicException)
            {
                return null;
            }

            return result;
        }

        private static byte[] Encrypt(byte[] key, string value)
        {
            SymmetricAlgorithm Sa = Rijndael.Create();
            var ct = Sa.CreateEncryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);

            var ms = new MemoryStream();
            var cs = new CryptoStream(ms, ct, CryptoStreamMode.Write);

            cs.Write(key, 0, key.Length);
            cs.FlushFinalBlock();

            byte[] result = ms.ToArray();

            ms.Close();
            ms.Dispose();

            cs.Close();
            cs.Dispose();

            ct.Dispose();

            return result;
        }

        private static CryptoStream InternalDecrypt(byte[] key, string value)
        {
            SymmetricAlgorithm sa = Rijndael.Create();
            ICryptoTransform ct = sa.CreateDecryptor((new PasswordDeriveBytes(value, null)).GetBytes(16), new byte[16]);

            var ms = new MemoryStream(key);
            return new CryptoStream(ms, ct, CryptoStreamMode.Read);
        }
    }
}
