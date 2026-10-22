using Microsoft.Extensions.Logging;
using SqlSugar;
using SyZero.SqlSugar.DbContext;

namespace SyZero.Example3.Core.DbContext
{
    public class Example3DbContext : SyZeroDbContext
    {
        public Example3DbContext(ConnectionConfig config, ILoggerFactory loggerFactory)
            : base(config, loggerFactory)
        {
        }
    }
}
