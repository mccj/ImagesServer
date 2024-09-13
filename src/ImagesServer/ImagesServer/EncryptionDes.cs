using System.Security.Cryptography;
using System.Text;

using Microsoft.IdentityModel.Tokens;
namespace RoadFlow.Utility
{
    /// <summary>
    ///EncryptionDes 的摘要说明
    /// </summary>
    public static class EncryptionDes
    {
        private static string defaultKey = "123456";//加密密钥必须为8位
        /// <summary>
        /// 加密算法
        /// </summary>
        /// <param name="pToEncrypt"></param>
        /// <returns></returns>
        public static string Encrypt(string pToEncrypt, string? key = null)
        {
            key = string.IsNullOrWhiteSpace(key) ? defaultKey : key;

            if (string.IsNullOrWhiteSpace(pToEncrypt))
                return string.Empty;
            try
            {
                var inputByteArray = Encoding.UTF8.GetBytes(pToEncrypt);
                var provider = DES.Create();
                provider.Key = Encoding.UTF8.GetBytes(key);
                provider.IV = Encoding.UTF8.GetBytes(key);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, provider.CreateEncryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return Base64UrlEncoder.Encode(ms.ToArray());
            }
            catch { return ""; }
        }
        /// <summary>
        /// 解密算法
        /// </summary>
        /// <param name="pToDecrypt"></param>
        /// <returns></returns>
        public static string Decrypt(string pToDecrypt, string? key = null)
        {
            key = string.IsNullOrWhiteSpace(key) ? defaultKey : key;

            if (string.IsNullOrWhiteSpace(pToDecrypt))
                return string.Empty;
            try
            {
                var des = DES.Create();
                var inputByteArray = Base64UrlEncoder.DecodeBytes(pToDecrypt);
                des.Key = Encoding.UTF8.GetBytes(key);
                des.IV = Encoding.UTF8.GetBytes(key);
                using var ms = new MemoryStream();
                using var cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();

                return Encoding.UTF8.GetString(ms.ToArray());
            }
            catch { return ""; }
        }
    }
}