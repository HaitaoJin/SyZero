using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SyZero.Example3.Web.Core.Models;

namespace SyZero.Example3.Web.Core.Filter
{
    /// <summary>
    /// 结果过滤器
    /// </summary>
    public class AppResultFilter : IResultFilter, IOrderedFilter
    {
        public int Order { get; set; } = int.MaxValue - 10;

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result.GetType() == typeof(ObjectResult))
            {
                context.Result = new JsonResult(new ResultModel((context.Result as ObjectResult)?.Value));
            }
        }
    }
}
