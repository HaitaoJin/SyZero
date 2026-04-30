using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SyZero.Application.Routing
{
    public class AppFileStreamResult
    {
        private readonly Stream _stream;
        private readonly string _fileName;
        private readonly string _contentType;

        public AppFileStreamResult(Stream stream, string fileName, string contentType)
        {
            _stream = stream;
            _fileName = fileName;
            _contentType = contentType;
        }

        public string FileName => _fileName;

        public string ContentType => _contentType;

        public long Length => _stream.Length;

        public string ContentDisposition => $"attachment; filename=\"{Uri.EscapeDataString(FileName)}\"";

        public Stream OpenReadStream()
        {
            return _stream;
        }

        public void CopyTo(Stream target)
        {
             _stream.CopyTo(target);
            _stream.Position = 0; // 复位流的位置（可选）
        }

        public async Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
        {
            await _stream.CopyToAsync(target);
            _stream.Position = 0; // 复位流的位置（可选）
        }
    }
}
