using System;
using System.Collections.Generic;
using System.Linq;

namespace SyZero.Application.DynamicApi
{
    /// <summary>
    /// 动态 API Provider 共用的名称归一化工具：去除指定后缀、应用自定义委托。
    /// </summary>
    public static class DynamicApiNameNormalizer
    {
        /// <summary>
        /// 按长度倒序匹配并移除首个命中的后缀（忽略大小写）。
        /// </summary>
        /// <param name="name">原始名称。</param>
        /// <param name="postfixes">候选后缀集合，可包含空白项（自动忽略）。</param>
        public static string RemovePostfix(string name, IEnumerable<string> postfixes)
        {
            if (string.IsNullOrEmpty(name) || postfixes == null)
            {
                return name;
            }

            foreach (var postfix in OrderByLengthDesc(postfixes))
            {
                if (name.EndsWith(postfix, StringComparison.OrdinalIgnoreCase))
                {
                    return name.Substring(0, name.Length - postfix.Length);
                }
            }

            return name;
        }

        /// <summary>
        /// 同 <see cref="RemovePostfix"/>，但若提供 <paramref name="customNormalizer"/>，
        /// 则优先使用该委托结果。
        /// </summary>
        public static string Normalize(string name, IEnumerable<string> postfixes, Func<string, string> customNormalizer)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            if (customNormalizer != null)
            {
                return customNormalizer(name);
            }

            return RemovePostfix(name, postfixes);
        }

        private static IEnumerable<string> OrderByLengthDesc(IEnumerable<string> postfixes)
        {
            return postfixes
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(p => p.Length);
        }
    }
}
