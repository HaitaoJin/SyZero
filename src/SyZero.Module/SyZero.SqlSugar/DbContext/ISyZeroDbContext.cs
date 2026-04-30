using SqlSugar;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyZero.SqlSugar.DbContext
{
    public interface ISyZeroDbContext: ISqlSugarClient, IDisposable, ITenant
    {
    }
}
