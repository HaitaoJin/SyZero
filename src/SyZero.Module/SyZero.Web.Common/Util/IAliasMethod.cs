using System;
using System.Collections.Generic;

namespace SyZero.Web.Common
{
    public interface IAliasMethod
    {
        /// <summary>
        /// 获取随机采样
        /// </summary>
        /// <returns></returns>
        int Next();

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        void Initialization(List<Double> probabilities);
    }
}
