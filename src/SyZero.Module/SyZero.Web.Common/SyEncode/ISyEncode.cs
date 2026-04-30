using SyZero.Dependency;

namespace SyZero.Web.Common
{
    public interface ISyEncode : ISingletonDependency
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="Text">内容</param>
        /// <param name="sKey">密匙</param>
        /// <returns></returns>
        string Encrypt(string Text, string sKey, EncryptType type);
        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="Text">内容</param>
        /// <param name="sKey">密匙</param>
        /// <returns></returns>
        string Decrypt(string Text, string sKey, EncryptType type);

        /// <summary>
        /// MD5加密 32位
        /// </summary>
        /// <param name="Text">内容</param>
        /// <returns></returns>
        string Get32MD5One(string Text);

        /// <summary>
        /// MD5加密 16位
        /// </summary>
        /// <param name="Text">内容</param>
        /// <returns></returns>
        string Get16MD5One(string Text);

        /// <summary>
        /// 微信小程序 开放数据解密
        /// AES解密（Base64）
        /// </summary>
        /// <param name="encryptedData">已加密的数据</param>
        /// <param name="sessionKey">解密密钥</param>
        /// <param name="iv">IV偏移量</param>
        /// <returns></returns>
        string DecryptForWeChatApplet(string encryptedData, string sessionKey, string iv);
    }
}
