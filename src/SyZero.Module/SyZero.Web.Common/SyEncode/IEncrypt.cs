namespace SyZero.Web.Common
{
    interface IEncrypt
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="Text">内容</param>
        /// <param name="sKey">密匙</param>
        /// <returns></returns>
        string Encrypt(string Text, string sKey);
        /// <summary>
        /// 解密
        /// </summary>
        /// <param name="Text">内容</param>
        /// <param name="sKey">密匙</param>
        /// <returns></returns>
        string Decrypt(string Text, string sKey);
    }
}
