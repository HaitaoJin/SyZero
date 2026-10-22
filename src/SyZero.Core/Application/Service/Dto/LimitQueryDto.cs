namespace SyZero.Application.Service.Dto
{
    /// <summary>
    /// ILimitQuery实现
    /// </summary>
    public class LimitQueryDto : ILimitQuery
    {
        public int Limit { get; set; } = 10;
    }
}
