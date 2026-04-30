using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SyZero
{
    public static class FileExtensions
    {
        public static string GetFileHash(this FileInfo fileInfo)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var stream = File.OpenRead(fileInfo.FullName))
                {
                    byte[] hash = sha256.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static string GetFileHash(this Stream stream)
        {
            using (var sha256 = SHA256.Create()) // 这里可以使用其他哈希算法，例如 MD5.Create()
            {
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant(); // 转为小写并去掉连字符
            }
        }

        public static byte[] ToByteArray(this Stream stream)
        {
            // 确保流的位置在开始
            stream.Position = 0;

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream); // 将流内容复制到 MemoryStream
                return memoryStream.ToArray(); // 转换为 byte[]
            }
        }
    }
}
