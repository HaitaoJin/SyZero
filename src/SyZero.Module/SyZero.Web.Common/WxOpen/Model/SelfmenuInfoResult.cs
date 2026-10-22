using System.Collections.Generic;

namespace SyZero.Web.Common
{
    public class SelfmenuInfoResult
    {
        public int is_menu_open { get; set; }

        public SelfmenuInfo? selfmenu_info { get; set; }
    }


    public class SelfmenuInfo
    {
        public List<SelfmenuButton>? button { get; set; }

    }

    public class CreateSelfmenuInfo
    {
        public List<SelfmenuSubButtonInfo>? button { get; set; }

    }

    public class SelfmenuButton
    {
        public string type { get; set; } = string.Empty;

        public string name { get; set; } = string.Empty;

        public string url { get; set; } = string.Empty;

        public string value { get; set; } = string.Empty;

        public string key { get; set; } = string.Empty;

        public string appid { get; set; } = string.Empty;

        public string pagepath { get; set; } = string.Empty;

        public string media_id { get; set; } = string.Empty;

        public SelfmenuSubButton? sub_button { get; set; }
    }

    public class SelfmenuSubButton
    {
        public List<SelfmenuSubButtonInfo>? list { get; set; }
    }

    public class SelfmenuSubButtonInfo
    {
        public string type { get; set; } = string.Empty;

        public string name { get; set; } = string.Empty;

        public string url { get; set; } = string.Empty;

        public string value { get; set; } = string.Empty;

        public string key { get; set; } = string.Empty;

        public string appid { get; set; } = string.Empty;

        public string pagepath { get; set; } = string.Empty;

        public string media_id { get; set; } = string.Empty;

        public List<SelfmenuSubButtonInfo>? sub_button { get; set; }
    }
}
