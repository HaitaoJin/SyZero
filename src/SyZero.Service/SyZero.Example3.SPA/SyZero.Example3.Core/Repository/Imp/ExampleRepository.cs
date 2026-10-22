using SyZero.Example3.Core.Entities;
using SyZero.SqlSugar.Repositories;
using SyZero.SqlSugar.DbContext;

namespace SyZero.Example3.Core.Repository.Imp
{
    public class ExampleRepository : SqlSugarRepository<Example>, IExampleRepository
    {
        public ExampleRepository(ISyZeroDbContext dbContext)
            : base(dbContext)
        {
        }
    }
}
