using System.Collections.Generic;

namespace SyZero.Web.Common
{
    public class SelfmenuInfoResult
    {
        public int is_menu_open { get; set; }

        public SelfmenuInfo selfmenu_info { get; set; }
    }


    public class SelfmenuInfo
    {
        public List<SelfmenuButton> button { get; set; }

    }

    public class CreateSelfmenuInfo
    {
        public List<SelfmenuSubButtonInfo> button { get; set; }

    }

    public class SelfmenuButton
    {
        public string type { get; set; }

        public string name { get; set; }

        public string url { get; set; }

        public string value { get; set; }

        public string key { get; set; }

        public string appid { get; set; }

        public string pagepath { get; set; }

        public string media_id { get; set; }

        public SelfmenuSubButton sub_button { get; set; }
    }

    public class SelfmenuSubButton
    {
        public List<SelfmenuSubButtonInfo> list { get; set; }
    }

    public class SelfmenuSubButtonInfo
    {
        public string type { get; set; }

        public string name { get; set; }

        public string url { get; set; }

        public string value { get; set; }

        public string key { get; set; }

        public string appid { get; set; }

        public string pagepath { get; set; }

        public string media_id { get; set; }

        public List<SelfmenuSubButtonInfo> sub_button { get; set; }
    }
}
