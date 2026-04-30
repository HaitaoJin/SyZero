using System;

namespace SyZero
{
    /// <summary>
    /// MVC 异常拦截 该对象 返回客户端
    /// </summary>
    [Serializable]
    public class SyMessageException : Exception
    {
        /// <summary>
        /// 异常模型
        /// </summary>
        public SyMessageBoxModel Model { set; get; }

        /// <summary>
        /// 成功消息
        /// </summary>
        public SyMessageException()
            : base("操作成功!")
        {
            this.Model = new SyMessageBoxModel(this.Message, SyMessageBoxStatus.Success);
        }

        /// <summary>
        /// 失败并返回
        /// </summary>
        /// <param name="Messager"></param>
        public SyMessageException(string Message)
            : base(Message)
        {
            this.Model = new SyMessageBoxModel(Message, SyMessageBoxStatus.Fail);
        }

        /// <summary>
        /// 状态返回
        /// </summary>
        /// <param name="Messager"></param>
        public SyMessageException(SyMessageBoxStatus syMessageBoxStatus)
            : base(syMessageBoxStatus.ToDescription())
        {
            this.Model = new SyMessageBoxModel(syMessageBoxStatus.ToDescription(), syMessageBoxStatus);
        }

        /// <summary>
        /// 自定义异常
        /// </summary>
        /// <param name="Data">客户端接收对象 例如: {status=1,data="这是消息!"}</param>
        public SyMessageException(object message)
            : base($"自定义异常 请忽略!{SyMessageBoxStatus.Custom.ToString()}")
        {
            this.Model = new SyMessageBoxModel(message, SyMessageBoxStatus.Custom);
        }

        /// <summary>
        /// 自定义返回
        /// </summary>
        /// <param name="Messager"></param>
        public SyMessageException(string message, SyMessageBoxStatus syMessageBoxStatus)
            : base(message)
        {
            this.Model = new SyMessageBoxModel(message, syMessageBoxStatus);
        }

        /// <summary>
        /// 自定义返回
        /// </summary>
        /// <param name="Messager"></param>
        public SyMessageException(string message, int code)
            : base(message)
        {
            this.Model = new SyMessageBoxModel(message, code);
        }

        /// <summary>
        /// 自定义返回
        /// </summary>
        /// <param name="Messager"></param>
        public SyMessageException(SyMessageBoxModel model)
            : base(model.msg.ToString())
        {
            this.Model = model;
        }
    }
}
