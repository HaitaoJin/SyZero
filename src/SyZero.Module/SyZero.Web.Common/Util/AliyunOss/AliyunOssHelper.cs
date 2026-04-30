using Aliyun.OSS;
using Aliyun.OSS.Common;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;

namespace SyZero.Web.Common
{
    public class AliyunOssHelper
    {
        private const string virtualPath = "Upload/";

        #region 非断点上传

        /// <summary>
        /// 单文件上传，非断点上传
        /// </summary>
        /// <param name="file">上传文件对象</param>
        /// <param name="bucketName">aliyunoss创建的bucketName，一个项目一个bucket</param>
        /// <returns></returns>
        public static ServerResponse<FileData> UpLoadSingleFile(IFormFile file, string bucketName, string uploadUserId = "")
        {
            string objectName = string.Empty;
            try
            {
                FileData fileData = UploadFileToOSS(file, bucketName, uploadUserId);
                if (fileData != null)
                {
                    return ResponseProvider.Success(fileData, "成功");
                }
                else
                {
                    return ResponseProvider.Error<FileData>("未找到文件!");
                }
            }
            catch (Exception ex)
            {
                //记录日志
                return ResponseProvider.Error<FileData>(ex.Message);
            }
        }

        /// <summary>
        /// 批量上传文件，非断点上传
        /// </summary>
        /// <param name="files">上传文件对象集合</param>
        /// <param name="bucketName">aliyunoss创建的bucketName，一个项目一个bucket，如果不传值会上传到默认的bucket下</param>
        /// <returns></returns>
        public static ServerResponse<List<FileData>> UpLoadMultipleFile(IFormFileCollection files, string bucketName, string uploadUserId = "")
        {
            try
            {
                List<FileData> list = new List<FileData>();
                foreach (var file in files)
                {
                    FileData fileData = UploadFileToOSS(file, bucketName, uploadUserId);
                    if (fileData != null)
                    {
                        list.Add(fileData);
                    }
                    else
                    {
                        return ResponseProvider.Error<List<FileData>>("未找到文件!");
                    }
                }

                return ResponseProvider.Success(list, "成功");
            }
            catch (Exception ex)
            {
                return ResponseProvider.Error<List<FileData>>(ex.Message);
            }
        }

        /// <summary>
        /// 私有方法  上传到本地并且上传到OSS
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bucketName">系统对应的bucketName，如果不传值会上传到默认的bucket下</param>
        /// <returns></returns>
        private static FileData UploadFileToOSS(IFormFile file, string bucketName, string uploadUserId = "")
        {
            if (file != null && !string.IsNullOrEmpty(file.FileName))
            {
                #region 上传文件到本地

                string suffix = Path.GetExtension(file.FileName);//获取文件后缀
                string beforeSuffix = string.IsNullOrEmpty(uploadUserId) ? "0_" : uploadUserId + "_";
                string tempFileName = beforeSuffix + DateTime.Now.ToString("yyyyMMddHHmmssffff") + suffix; // 修改上传文件名称
                DateTime date = DateTime.Now;
                string folderName = date.ToString("yyyy") + "/" + date.ToString("MM") + "/" + date.ToString("dd");  // 上传到本地的时间文件夹名称
                string dirPath = AppContext.BaseDirectory + virtualPath + "/" + bucketName + "/" + folderName; // 上传到本地的绝对路径
                //Console.WriteLine("dirPath:--------------------------------------" + dirPath);
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);// 创建本地上传路径
                }

                string filePath = dirPath + "/" + tempFileName; // 上传到的文件夹路径 例如: D;\项目名称\Upload\20190124\
                string filelocalPath = Path.Combine(AppContext.BaseDirectory, filePath); // 文件所在的绝对路径，例如: D;\项目名称\Upload\20190124\20190124171656123.jpg
                //Console.WriteLine("filePath:--------------------------------------" + filePath);
                using (var stream = new FileStream(filelocalPath, FileMode.Create))
                {
                    file.CopyTo(stream); // 上传到本地地址
                    stream.Flush();
                }

                #endregion

                #region 从本地上传到aliyunOSS

                string objectName = folderName + "/" + tempFileName; // 上传到AliyunOSS上的地址
                OssClient client = new OssClient(AppConfig.GetSection("AliyunOSS:Endpoint"), AppConfig.GetSection("AliyunOSS:AccessKeyId"), AppConfig.GetSection("AliyunOSS:AccessKeySecret"));
                //上传到AliyunOSS
                client.PutObject(bucketName, objectName, filePath);
                FileData fielData = new FileData
                (
                    true,
                    objectName,
                    AppConfig.GetSection("AliyunOSS:Endpoint").Insert(8, bucketName + ".") + "/" + objectName,
                    filePath
                );

                #endregion

                return fielData;
            }
            else
            {
                return new FileData(false);
            }
        }

        #endregion

        #region 断点续传

        /// <summary>
        /// 私有方法，实际实现断点续传方法
        /// </summary>
        /// <param name="objectName"></param>
        /// <param name="localFilename"></param>
        /// <param name="checkpointDir"></param>
        /// <param name="bucketName"></param>
        private static FileData FilePonit(string objectName, string localFilename, string checkpointDir, string bucketName)
        {
            FileData file = new FileData();
            file.OssKey = objectName;
            file.LocalFilePath = localFilename;
            try
            {
                // 通过UploadFileRequest设置多个参数。
                UploadObjectRequest request = new UploadObjectRequest(bucketName, objectName, localFilename)
                {
                    // 指定上传的分片大小。
                    PartSize = 1024, //byte  8 * 1024 * 1024,
                    // 指定并发线程数。                 
                    ParallelThreadCount = 3,
                    // checkpointDir保存断点续传的中间状态，用于失败后继续上传。如果checkpointDir为null，断点续传功能不会生效，每次失败后都会重新上传。
                    CheckpointDir = checkpointDir,

                    StreamTransferProgress = streamProgressCallback
                };

                OssClient client = new OssClient(AppConfig.GetSection("AliyunOSS:Endpoint"), AppConfig.GetSection("AliyunOSS:AccessKeyId"), AppConfig.GetSection("AliyunOSS:AccessKeySecret"));

                // 断点续传上传。
                client.ResumableUploadObject(request);
                //返回上传到oss的地址
                file.Url = AppConfig.GetSection("AliyunOSS:Endpoint").Insert(8, bucketName + ".") + "/" + objectName;
                file.IsSuccess = true;
            }
            catch (OssException ex)
            {
                file.IsSuccess = false;
                //记录日志
            }
            catch (Exception ex)
            {
                //记录日志
                file.IsSuccess = false;
            }

            return file;
        }

        /// <summary>
        /// 单个文件断点续传，上传到aliyun Oss
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bucketName">所对应系统的bucketName</param>
        /// <returns></returns>
        public static ServerResponse<FileData> UploadFilePonit(IFormFile file, string bucketName, string uploadUserId = "")
        {
            try
            {
                string objectName = string.Empty;
                FileData fileData = null;
                if (file != null && !string.IsNullOrEmpty(file.FileName))
                {
                    fileData = UploadFilePonitToOSS(file, bucketName, uploadUserId);
                    return ResponseProvider.Success(fileData, "成功");
                }
                else
                {
                    return ResponseProvider.Error<FileData>("未找到文件!");
                }
            }
            catch (Exception ex)
            {
                return ResponseProvider.Error<FileData>(ex.Message);
            }
        }

        /// <summary>
        /// 私有方法，断点续传实际实现方法
        /// </summary>
        /// <param name="file"></param>
        /// <param name="bucketName"></param>
        /// <returns></returns>
        private static FileData UploadFilePonitToOSS(IFormFile file, string bucketName, string uploadUserId = "")
        {
            #region 上传文件到本地

            string objectName = string.Empty;
            string suffix = Path.GetExtension(file.FileName);//获取文件后缀
            string beforeSuffix = string.IsNullOrEmpty(uploadUserId) ? "0_" : uploadUserId + "_";
            string tempFileName = beforeSuffix + DateTime.Now.ToString("yyyyMMddHHmmssffff") + suffix; // 修改上传文件名称
            DateTime date = DateTime.Now;
            string folderName = date.ToString("yyyy") + "/" + date.ToString("MM") + "/" + date.ToString("dd");  // 上传到本地的时间文件夹名称
            string dirPath = AppContext.BaseDirectory + virtualPath + "/" + bucketName + "/" + folderName; // 上传到本地的绝对路径
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);// 创建本地上传路径
            }

            string filePath = dirPath + "/" + tempFileName; // 上传到的文件夹路径 例如: D;\项目名称\Upload\20190124\
            string filelocalPath = Path.Combine(AppContext.BaseDirectory, filePath); // 文件所在的绝对路径，例如: D;\项目名称\Upload\20190124\20190124171656123.jpg
            using (var stream = new FileStream(filelocalPath, FileMode.Create))
            {
                file.CopyTo(stream); // 上传到本地地址
                stream.Flush();
            }

            #endregion

            #region 上传Oss

            string checkpointDir = $@"{AppContext.BaseDirectory}\CheckpointDir\";
            objectName = folderName + "/" + tempFileName; // Uploads/20181229/20181229155632123.png+
            if (!Directory.Exists(checkpointDir))
            {
                Directory.CreateDirectory(checkpointDir);
            }

            return FilePonit(objectName, filePath, checkpointDir, bucketName);

            #endregion
        }

        /// <summary>
        /// 批量断点续传，上传到aliyun Oss
        /// </summary>
        /// <param name="files">上传的文件集合</param>
        /// <param name="bucketName">所对应系统的bucketName</param>
        /// <returns></returns>
        public static ServerResponse<List<FileData>> UploadMultipleFilePonit(IFormFileCollection files, string bucketName, string uploadUserId = "")
        {
            try
            {
                List<FileData> list = new List<FileData>();
                foreach (var file in files)
                {
                    if (file != null && !string.IsNullOrEmpty(file.FileName))
                    {
                        FileData fileData = UploadFilePonitToOSS(file, bucketName, uploadUserId);
                        list.Add(fileData);
                    }
                    else
                    {
                        return ResponseProvider.Error<List<FileData>>("未找到文件!");
                    }
                }

                return ResponseProvider.Success(list, "成功");
            }
            catch (Exception ex)
            {
                return ResponseProvider.Error<List<FileData>>(ex.Message);
            }
        }

        private static void streamProgressCallback(object sender, StreamTransferProgressArgs args)
        {
            //Console.WriteLine("ProgressCallback - TotalBytes:{0}, TransferredBytes:{1}, IncrementTransferred:{2}", args.TotalBytes, args.TransferredBytes, args.IncrementTransferred);
        }

        #endregion

        #region 其它方法

        /// <summary>
        /// 删除系统对应的bucketName所对应的OSS文件
        /// </summary>
        /// <param name="ossPath">存储到oss的文件地址，如： 20181229/20181229155625123.png</param>
        /// <param name="bucketName">系统所对应的bucketName，如果不传值会上传到默认的bucket下</param>
        /// <returns></returns>
        public static ServerResponse DeleteAlyunOSSFile(string ossPath, string bucketName = "")
        {
            try
            {
                OssClient client = new OssClient(AppConfig.GetSection("AliyunOSS:Endpoint"), AppConfig.GetSection("AliyunOSS:AccessKeyId"), AppConfig.GetSection("AliyunOSS:AccessKeySecret"));
                client.DeleteObject(bucketName, ossPath);
                return ResponseProvider.Success("删除成功");
            }
            catch (OssException ex)
            {
                return ResponseProvider.Error("OssException异常：" + ex.Message);
            }
            catch (Exception ex)
            {
                return ResponseProvider.Error("系统异常失败" + ex.Message);
            }
        }

        /// <summary>
        ///   获取文件下载地址
        /// </summary>
        /// <param name="ossPath">存储到oss的文件地址，如： 20181229/20181229155625123.png</param>
        /// <param name="bucketName">系统所对应的bucketName</param>
        public static Uri GetFileUrl(string ossPath, string bucketName = "")
        {
            var req = new GeneratePresignedUriRequest(bucketName, ossPath, SignHttpMethod.Get)
            {
                Expiration = DateTime.Now.AddHours(1)
            };
            OssClient client = new OssClient(AppConfig.GetSection("AliyunOSS:Endpoint"), AppConfig.GetSection("AliyunOSS:AccessKeyId"), AppConfig.GetSection("AliyunOSS:AccessKeySecret"));
            return client.GeneratePresignedUri(req);
        }


        #endregion
    }

}
