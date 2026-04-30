
namespace SyZero.Web.Common
{
    /// <summary>
    /// 接收返回的信息
    /// </summary>
    public class FileData
    {
        public FileData(bool _isSuccess = false, string _objectName = "", string _fileUrl = "", string _localFilePath = "")
        {
            IsSuccess = _isSuccess;
            OssKey = _objectName;
            Url = _fileUrl;
            LocalFilePath = _localFilePath;
        }

        public FileData()
        {
        }

        /// <summary>
        /// 文件在Oss上的http地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 上传到Oss上的相对路径
        /// </summary>
        public string OssKey { get; set; }

        /// <summary>
        /// 本地文件存储地址
        /// </summary>
        public string LocalFilePath { get; set; }

    }
}