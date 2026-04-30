using System.Collections.Generic;

namespace SyZero.Web.Common
{
    public class WxDailyVisitTrendResult
    {
        public List<WxDailyVisitTrendDto> list { get; set; }
    }

    public class WxDailyVisitTrendDto
    {
        /// <summary>
        /// 日期，格式为 yyyymmdd
        /// </summary>
        public string ref_date { get; set; }
        /// <summary>
        /// 打开次数
        /// </summary>
        public int session_cnt { get; set; }
        /// <summary>
        /// 访问次数
        /// </summary>
        public int visit_pv { get; set; }
        /// <summary>
        /// 访问人数
        /// </summary>
        public int visit_uv { get; set; }
        /// <summary>
        /// 新用户数
        /// </summary>
        public int visit_uv_new { get; set; }
        /// <summary>
        /// 	人均停留时长 (浮点型，单位：秒)
        /// </summary>
        public decimal stay_time_uv { get; set; }
        /// <summary>
        /// 次均停留时长 (浮点型，单位：秒)
        /// </summary>
        public decimal stay_time_session { get; set; }
        /// <summary>
        /// 平均访问深度 (浮点型)
        /// </summary>
        public decimal visit_depth { get; set; }
    }
}
