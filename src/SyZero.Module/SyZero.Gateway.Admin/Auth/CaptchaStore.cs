using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace SyZero.Gateway.Admin.Auth
{
    /// <summary>
    /// 验证码存储抽象。颁发后保留答案；消费 (<see cref="TryConsume"/>) 一次后即被销毁。
    /// 默认实现为 <see cref="MemoryCaptchaStore"/>，宿主可替换为分布式版本。
    /// </summary>
    public interface ICaptchaStore
    {
        /// <summary>颁发新验证码。返回挑战标题（如 "7 + 4 = ?"）。</summary>
        CaptchaChallenge Issue();

        /// <summary>校验并消费一次（成功后立即失效）。</summary>
        bool TryConsume(string id, string answer);
    }

    /// <summary>验证码挑战。</summary>
    public sealed class CaptchaChallenge
    {
        public string Id { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
    }

    /// <summary>基于 <see cref="ConcurrentDictionary{TKey,TValue}"/> 的进程内验证码存储；5 分钟过期。</summary>
    public sealed class MemoryCaptchaStore : ICaptchaStore
    {
        private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);
        private readonly ConcurrentDictionary<string, Entry> _store = new();

        /// <inheritdoc />
        public CaptchaChallenge Issue()
        {
            // 简单算术：a + b 或 a - b（保证结果非负）。
            var a = RandomNumberGenerator.GetInt32(2, 10);
            var b = RandomNumberGenerator.GetInt32(1, a); // b <= a，保证减法非负
            var op = RandomNumberGenerator.GetInt32(0, 2); // 0=+, 1=-
            int answer = op == 0 ? a + b : a - b;
            string question = op == 0 ? $"{a} + {b} = ?" : $"{a} - {b} = ?";

            var id = Guid.NewGuid().ToString("N");
            _store[id] = new Entry { Answer = answer.ToString(), ExpiresUtc = DateTime.UtcNow.Add(Ttl) };

            // 顺手清理过期项，避免无限增长。
            CleanupExpired();
            return new CaptchaChallenge { Id = id, Question = question };
        }

        /// <inheritdoc />
        public bool TryConsume(string id, string answer)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(answer)) return false;
            if (!_store.TryRemove(id, out var entry)) return false;
            if (entry.ExpiresUtc < DateTime.UtcNow) return false;
            return string.Equals(entry.Answer.Trim(), answer.Trim(), StringComparison.Ordinal);
        }

        private void CleanupExpired()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _store)
            {
                if (kv.Value.ExpiresUtc < now)
                {
                    _store.TryRemove(kv.Key, out _);
                }
            }
        }

        private sealed class Entry
        {
            public string Answer { get; set; } = string.Empty;
            public DateTime ExpiresUtc { get; set; }
        }
    }
}
