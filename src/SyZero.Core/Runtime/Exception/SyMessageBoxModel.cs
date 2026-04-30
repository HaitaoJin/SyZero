namespace SyZero
{
    public class SyMessageBoxModel
    {
        /// <summary>
        /// 自定义
        /// </summary>
        /// <param name="data">数据对象 例如:{status=500,action="6666"}</param>
        /// <param name="msgStatus"></param>
        public SyMessageBoxModel(object Data, SyMessageBoxStatus _EMessageBoxStatus)
        {
            this.code = (int)_EMessageBoxStatus;
            this.msg = Data;
        }

        /// <summary>
        /// 自定义
        /// </summary>
        /// <param name="data">数据对象 例如:{status=500,action="6666"}</param>
        /// <param name="msgStatus"></param>
        public SyMessageBoxModel(object Data, int code)
        {
            this.code = code;
            this.msg = Data;
        }

        /// <summary>
        /// 自定义
        /// </summary>
        /// <param name="data">数据对象 例如:{status=500,action="6666"}</param>
        /// <param name="msgStatus"></param>
        public SyMessageBoxModel()
        {
        }

        /// <summary>
        /// 错误状态码
        /// </summary>
        public int code { set; get; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public object msg { set; get; }

    }
}
