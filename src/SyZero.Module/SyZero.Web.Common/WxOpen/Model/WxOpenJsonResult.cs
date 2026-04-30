namespace SyZero.Web.Common
{
    public class WxOpenJsonResult
    {
        public WxOpenReturnCode errcode { get; set; }


        /// <summary>
        /// 返回结果信息
        /// </summary>
        public string errmsg { get; set; }


        public override string ToString()
        {
            return string.Format("WxJsonResult：{{errcode:'{0}',errcode_name:'{1}',errmsg:'{2}'}}",
                (int)errcode, errcode.ToString(), errmsg);
        }
    }

    /// <summary>
    /// 公众号返回码（JSON）
    /// 应该更名为ReturnCode_MP，但为减少项目中的修改，此处依旧用ReturnCode命名
    /// </summary>
    public enum WxOpenReturnCode
    {
        /// <summary>
        /// SDK配置错误
        /// </summary>
        SDKSettingError = -99,
        /// <summary>
        /// 系统繁忙此时请开发者稍候再试
        /// </summary>
        SystemBusy = -1,
        /// <summary>
        /// 请求成功
        /// </summary>
        RequestSuccess = 0,
        /// <summary>
        /// 获取access_token时AppSecret错误或者access_token无效
        /// </summary>
        AccessTokenInValid = 40001,
        /// <summary>
        /// 不合法的凭证类型
        /// </summary>
        IvalidCertificateType = 40002,
        /// <summary>
        /// 不合法的OpenID
        /// </summary>
        不合法的OpenID = 40003,
        /// <summary>
        /// 不合法的媒体文件类型
        /// </summary>
        不合法的媒体文件类型 = 40004,
        /// <summary>
        /// 不合法的文件类型
        /// </summary>
        不合法的文件类型 = 40005,
        /// <summary>
        /// 不合法的文件大小
        /// </summary>
        不合法的文件大小 = 40006,
        /// <summary>
        /// 不合法的媒体文件id
        /// </summary>
        不合法的媒体文件id = 40007,
        /// <summary>
        /// 不合法的消息类型_40008
        /// </summary>
        不合法的消息类型_40008 = 40008,
        /// <summary>
        /// 不合法的图片文件大小
        /// </summary>
        不合法的图片文件大小 = 40009,
        /// <summary>
        /// 不合法的语音文件大小
        /// </summary>
        不合法的语音文件大小 = 40010,
        /// <summary>
        /// 不合法的视频文件大小
        /// </summary>
        不合法的视频文件大小 = 40011,
        /// <summary>
        /// 不合法的缩略图文件大小
        /// </summary>
        不合法的缩略图文件大小 = 40012,
        /// <summary>
        /// 不合法的APPID
        /// </summary>
        不合法的APPID = 40013,
        /// <summary>
        /// 不合法的access_token
        /// </summary>
        不合法的access_token = 40014,
        /// <summary>
        /// 不合法的菜单类型
        /// </summary>
        不合法的菜单类型 = 40015,
        /// <summary>
        /// 不合法的按钮个数1
        /// </summary>
        不合法的按钮个数1 = 40016,
        /// <summary>
        /// 不合法的按钮个数2
        /// </summary>
        不合法的按钮个数2 = 40017,
        /// <summary>
        /// 不合法的按钮名字长度
        /// </summary>
        不合法的按钮名字长度 = 40018,
        /// <summary>
        /// 不合法的按钮KEY长度
        /// </summary>
        不合法的按钮KEY长度 = 40019,
        /// <summary>
        /// 不合法的按钮URL长度
        /// </summary>
        不合法的按钮URL长度 = 40020,
        /// <summary>
        /// 不合法的菜单版本号
        /// </summary>
        不合法的菜单版本号 = 40021,
        /// <summary>
        /// 不合法的子菜单级数
        /// </summary>
        不合法的子菜单级数 = 40022,
        /// <summary>
        /// 不合法的子菜单按钮个数
        /// </summary>
        不合法的子菜单按钮个数 = 40023,
        /// <summary>
        /// 不合法的子菜单按钮类型
        /// </summary>
        不合法的子菜单按钮类型 = 40024,
        /// <summary>
        /// 不合法的子菜单按钮名字长度
        /// </summary>
        不合法的子菜单按钮名字长度 = 40025,
        /// <summary>
        /// 不合法的子菜单按钮KEY长度
        /// </summary>
        不合法的子菜单按钮KEY长度 = 40026,
        /// <summary>
        /// 不合法的子菜单按钮URL长度
        /// </summary>
        不合法的子菜单按钮URL长度 = 40027,
        /// <summary>
        /// 不合法的自定义菜单使用用户
        /// </summary>
        不合法的自定义菜单使用用户 = 40028,
        /// <summary>
        /// 不合法的oauth_code
        /// </summary>
        不合法的oauth_code = 40029,
        /// <summary>
        /// 不合法的refresh_token
        /// </summary>
        不合法的refresh_token = 40030,
        /// <summary>
        /// 不合法的openid列表
        /// </summary>
        不合法的openid列表 = 40031,
        /// <summary>
        /// 不合法的openid列表长度
        /// </summary>
        不合法的openid列表长度 = 40032,
        /// <summary>
        /// 不合法的请求字符不能包含uxxxx格式的字符
        /// </summary>
        不合法的请求字符不能包含uxxxx格式的字符 = 40033,
        /// <summary>
        /// 不合法的参数
        /// </summary>
        不合法的参数 = 40035,

        //小程序、 公众号都有
        /// <summary>
        /// template_id不正确
        /// </summary>
        template_id不正确 = 40037,

        /// <summary>
        /// 不合法的请求格式
        /// </summary>
        不合法的请求格式 = 40038,
        /// <summary>
        /// 不合法的请求格式
        /// </summary>
        不合法的URL长度 = 40039,
        /// <summary>
        /// 不合法的分组id
        /// </summary>
        不合法的分组id = 40050,
        /// <summary>
        /// 分组名字不合法
        /// </summary>
        分组名字不合法 = 40051,
        /// <summary>
        /// appsecret不正确
        /// </summary>
        appsecret不正确 = 40125,//invalid appsecret

        /// <summary>
        /// 小程序Appid不存在
        /// </summary>
        小程序Appid不存在 = 40166,

        /// <summary>
        /// 缺少access_token参数
        /// </summary>
        缺少access_token参数 = 41001,
        /// <summary>
        /// 缺少appid参数
        /// </summary>
        缺少appid参数 = 41002,
        /// <summary>
        /// 缺少refresh_token参数
        /// </summary>
        缺少refresh_token参数 = 41003,
        /// <summary>
        /// 缺少secret参数
        /// </summary>
        缺少secret参数 = 41004,
        /// <summary>
        /// 缺少多媒体文件数据
        /// </summary>
        缺少多媒体文件数据 = 41005,
        /// <summary>
        /// 缺少media_id参数
        /// </summary>
        缺少media_id参数 = 41006,
        /// <summary>
        /// 缺少子菜单数据
        /// </summary>
        缺少子菜单数据 = 41007,
        /// <summary>
        /// 缺少oauth_code
        /// </summary>
        缺少oauth_code = 41008,
        /// <summary>
        /// 缺少openid
        /// </summary>
        缺少openid = 41009,

        //小程序
        /// <summary>
        /// 
        /// </summary>
        form_id不正确_或者过期 = 41028,
        /// <summary>
        /// 
        /// </summary>
        form_id已被使用 = 41029,
        /// <summary>
        /// 
        /// </summary>
        page不正确 = 41030,

        /// <summary>
        /// 
        /// </summary>
        access_token超时 = 42001,
        /// <summary>
        /// 
        /// </summary>
        refresh_token超时 = 42002,
        /// <summary>
        /// 
        /// </summary>
        oauth_code超时 = 42003,
        /// <summary>
        /// 
        /// </summary>
        需要GET请求 = 43001,
        /// <summary>
        /// 
        /// </summary>
        需要POST请求 = 43002,
        /// <summary>
        /// 
        /// </summary>
        需要HTTPS请求 = 43003,
        /// <summary>
        /// 
        /// </summary>
        需要接收者关注 = 43004,
        /// <summary>
        /// 
        /// </summary>
        需要好友关系 = 43005,
        /// <summary>
        /// 
        /// </summary>
        多媒体文件为空 = 44001,
        /// <summary>
        /// 
        /// </summary>
        POST的数据包为空 = 44002,
        /// <summary>
        /// 
        /// </summary>
        图文消息内容为空 = 44003,
        /// <summary>
        /// 
        /// </summary>
        文本消息内容为空 = 44004,
        /// <summary>
        /// 
        /// </summary>
        多媒体文件大小超过限制 = 45001,
        /// <summary>
        /// 
        /// </summary>
        消息内容超过限制 = 45002,
        /// <summary>
        /// 
        /// </summary>
        标题字段超过限制 = 45003,
        /// <summary>
        /// 
        /// </summary>
        描述字段超过限制 = 45004,
        /// <summary>
        /// 
        /// </summary>
        链接字段超过限制 = 45005,
        /// <summary>
        /// 
        /// </summary>
        图片链接字段超过限制 = 45006,
        /// <summary>
        /// 
        /// </summary>
        语音播放时间超过限制 = 45007,
        /// <summary>
        /// 
        /// </summary>
        图文消息超过限制 = 45008,
        /// <summary>
        /// 
        /// </summary>
        接口调用超过限制 = 45009,
        /// <summary>
        /// 
        /// </summary>
        创建菜单个数超过限制 = 45010,
        /// <summary>
        /// 
        /// </summary>
        回复时间超过限制 = 45015,
        /// <summary>
        /// 
        /// </summary>
        系统分组不允许修改 = 45016,
        /// <summary>
        /// 
        /// </summary>
        分组名字过长 = 45017,
        /// <summary>
        /// 
        /// </summary>
        分组数量超过上限 = 45018,
        /// <summary>
        /// 
        /// </summary>
        不存在媒体数据 = 46001,
        /// <summary>
        /// 
        /// </summary>
        不存在的菜单版本 = 46002,
        /// <summary>
        /// 
        /// </summary>
        不存在的菜单数据 = 46003,
        /// <summary>
        /// 
        /// </summary>
        解析JSON_XML内容错误 = 47001,
        /// <summary>
        /// 
        /// </summary>
        api功能未授权 = 48001,
        /// <summary>
        /// 
        /// </summary>
        用户未授权该api = 50001,
        /// <summary>
        /// 
        /// </summary>
        参数错误invalid_parameter = 61451,
        /// <summary>
        /// 
        /// </summary>
        无效客服账号invalid_kf_account = 61452,
        /// <summary>
        /// 
        /// </summary>
        客服帐号已存在kf_account_exsited = 61453,
        /// <summary>
        /// 客服帐号名长度超过限制(仅允许10个英文字符，不包括@及@后的公众号的微信号)(invalid kf_acount length)
        /// </summary>
        客服帐号名长度超过限制 = 61454,
        /// <summary>
        /// 客服帐号名包含非法字符(仅允许英文+数字)(illegal character in kf_account)
        /// </summary>
        客服帐号名包含非法字符 = 61455,
        /// <summary>
        ///  	客服帐号个数超过限制(10个客服账号)(kf_account count exceeded)
        /// </summary>
        客服帐号个数超过限制 = 61456,
        /// <summary>
        /// 
        /// </summary>
        无效头像文件类型invalid_file_type = 61457,
        /// <summary>
        /// 
        /// </summary>
        系统错误system_error = 61450,
        /// <summary>
        /// 
        /// </summary>
        日期格式错误 = 61500,
        /// <summary>
        /// 
        /// </summary>
        日期范围错误 = 61501,

        //新加入的一些类型，以下文字根据P2P项目格式组织，非官方文字
        发送消息失败_48小时内用户未互动 = 10706,
        /// <summary>
        /// 
        /// </summary>
        发送消息失败_该用户已被加入黑名单_无法向此发送消息 = 62751,
        /// <summary>
        /// 
        /// </summary>
        发送消息失败_对方关闭了接收消息 = 10703,
        /// <summary>
        /// 
        /// </summary>
        对方不是粉丝 = 10700,
        /// <summary>
        /// 
        /// </summary>
        没有留言权限 = 88000,//without comment privilege
        /// <summary>
        /// 
        /// </summary>
        该图文不存在 = 88001,//msg_data is not exists
        /// <summary>
        /// 
        /// </summary>
        文章存在敏感信息 = 88002,//the article is limit for safety
        /// <summary>
        /// 
        /// </summary>
        精选评论数已达上限 = 88003,//elected comment upper limit
        /// <summary>
        /// 
        /// </summary>
        已被用户删除_无法精选 = 88004,//comment was deleted by user
        /// <summary>
        /// 
        /// </summary>
        已经回复过了 = 88005,//already reply
        //88006暂时留空，未找到
        /// <summary>
        /// 
        /// </summary>
        回复超过长度限制或为0 = 88007,//reply content beyond max len or content len is zero
        /// <summary>
        /// 
        /// </summary>
        该评论不存在 = 88008,//comment is not exists
        /// <summary>
        /// 
        /// </summary>
        获取评论数目不合法 = 88010,//count range error. cout <= 0 or count > 50

        //开放平台

        /// <summary>
        /// 
        /// </summary>
        该公众号_小程序已经绑定了开放平台帐号 = 89000,//account has bound open，该公众号/小程序已经绑定了开放平台帐号
        /// <summary>
        /// 
        /// </summary>
        该主体已有任务执行中_距上次任务24h后再试 = 89249,//  task running
        /// <summary>
        /// 
        /// </summary>
        内部错误 = 89247,//    inner error
        /// <summary>
        /// 
        /// </summary>
        无效微信号 = 86004,//   invalid wechat
        /// <summary>
        /// 
        /// </summary>
        法人姓名与微信号不一致 = 61070,// name, wechat name not in accordance
        /// <summary>
        /// 
        /// </summary>
        企业代码类型无效_请选择正确类型填写 = 89248,//  invalid code type
        /// <summary>
        /// 
        /// </summary>
        未找到该任务 = 89250,//  task not found
        /// <summary>
        /// 
        /// </summary>
        待法人人脸核身校验 = 89251,//   legal person checking
        /// <summary>
        /// 
        /// </summary>
        法人_企业信息一致性校验中 = 89252,//   front checking
        /// <summary>
        /// 
        /// </summary>
        缺少参数 = 89253,//    lack of some params
        /// <summary>
        /// 
        /// </summary>
        第三方权限集不全_补全权限集全网发布后生效 = 89254,//   lack of some component rights
        /// <summary>
        /// 
        /// </summary>
        已下发的模板消息法人并未确认且已超时_24h_未进行身份证校验 = 100001,
        /// <summary>
        /// 
        /// </summary>
        已下发的模板消息法人并未确认且已超时_24h_未进行人脸识别校验 = 100002,
        /// <summary>
        /// 
        /// </summary>
        已下发的模板消息法人并未确认且已超时_24h = 100003,
        /// <summary>
        /// 
        /// </summary>
        工商数据返回_企业已注销 = 101,
        /// <summary>
        /// 
        /// </summary>
        工商数据返回_企业不存在或企业信息未更新 = 102,
        /// <summary>
        /// 
        /// </summary>
        工商数据返回_企业法定代表人姓名不一致 = 103,
        /// <summary>
        /// 
        /// </summary>
        工商数据返回_企业法定代表人身份证号码不一致 = 104,
        /// <summary>
        /// 
        /// </summary>
        法定代表人身份证号码_工商数据未更新_请5_15个工作日之后尝试 = 105,
        /// <summary>
        /// 
        /// </summary>
        工商数据返回_企业信息或法定代表人信息不一致 = 1000,
        /// <summary>
        /// 
        /// </summary>
        名称格式不合法 = 53010,
        /// <summary>
        /// 
        /// </summary>
        名称检测命中频率限制 = 53011,
        /// <summary>
        /// 
        /// </summary>
        禁止使用该名称 = 53012,
        /// <summary>
        /// 
        /// </summary>
        公众号_名称与已有公众号名称重复_小程序_该名称与已有小程序名称重复 = 53013,
        /// <summary>
        /// 
        /// </summary>
        公众号_公众号已有_名称A_时_需与该帐号相同主体才可申请_名称A_小程序_小程序已有_名称A_时_需与该帐号相同主体才可申请_名称A_ = 53014,
        /// <summary>
        /// 
        /// </summary>
        公众号_该名称与已有小程序名称重复_需与该小程序帐号相同主体才可申请_小程序_该名称与已有公众号名称重复_需与该公众号帐号相同主体才可申请 = 53015,
        公众号_该名称与已有多个小程序名称重复_暂不支持申请_小程序_该名称与已有多个公众号名称重复_暂不支持申请 = 53016,
        /// <summary>
        /// 
        /// </summary>
        公众号_小程序已有_名称A_时_需与该帐号相同主体才可申请_名称A_小程序_公众号已有_名称A_时_需与该帐号相同主体才可申请_名称A = 53017,
        /// <summary>
        /// 
        /// </summary>
        名称命中微信号 = 53018,
        /// <summary>
        /// 
        /// </summary>
        名称在保护期内 = 53019,


        //小程序代码管理返回码
        /// <summary>
        /// 
        /// </summary>
        不是由第三方代小程序进行调用 = 86000,
        /// <summary>
        /// 
        /// </summary>
        不存在第三方的已经提交的代码 = 86001,
        /// <summary>
        /// 
        /// </summary>
        标签格式错误 = 85006,
        /// <summary>
        /// 
        /// </summary>
        页面路径错误 = 85007,
        /// <summary>
        /// 
        /// </summary>
        类目填写错误 = 85008,
        /// <summary>
        /// 
        /// </summary>
        已经有正在审核的版本 = 85009,
        /// <summary>
        /// 
        /// </summary>
        item_list有项目为空 = 85010,
        /// <summary>
        /// 
        /// </summary>
        标题填写错误 = 85011,
        /// <summary>
        /// 
        /// </summary>
        无效的审核id = 85012,
        /// <summary>
        /// 
        /// </summary>
        没有审核版本 = 85019,
        /// <summary>
        /// 
        /// </summary>
        审核状态未满足发布 = 85020,
        /// <summary>
        /// 
        /// </summary>
        状态不可变 = 85021,
        /// <summary>
        /// 
        /// </summary>
        action非法 = 85022,
        /// <summary>
        /// 
        /// </summary>
        审核列表填写的项目数不在1到5以内 = 85023,
        /// <summary>
        /// 
        /// </summary>
        小程序没有线上版本_不能进行灰度 = 85079,
        /// <summary>
        /// 
        /// </summary>
        小程序提交的审核未审核通过 = 85080,
        /// <summary>
        /// 
        /// </summary>
        无效的发布比例 = 85081,
        /// <summary>
        /// 
        /// </summary>
        当前的发布比例需要比之前设置的高 = 85082,
        /// <summary>
        /// 
        /// </summary>
        小程序还未设置昵称_头像_简介_请先设置完后再重新提交 = 86002,
        /// <summary>
        /// 
        /// </summary>
        现网已经在灰度发布_不能进行版本回退 = 87011,
        /// <summary>
        /// 
        /// </summary>
        该版本不能回退_可能的原因_1_无上一个线上版用于回退_2_此版本为已回退版本_不能回退_3_此版本为回退功能上线之前的版本_不能回退 = 87012,
        /// <summary>
        /// 
        /// </summary>
        版本输入错误 = 85015,

        /// <summary>
        /// 小程序为“签名错误”。对应公众号： 87009, “errmsg” : “reply is not exists” //该回复不存在
        /// </summary>
        签名错误 = 87009,
        //小程序MsgSecCheck接口
        /// <summary>
        /// 
        /// </summary>
        内容含有违法违规内容 = 87014,

        //小程序地点管理返回码
        POST参数非法 = 20002,
        /// <summary>
        /// 
        /// </summary>
        该经营资质已添加_请勿重复添加 = 92000,
        /// <summary>
        /// 
        /// </summary>
        附近地点添加数量达到上线_无法继续添加 = 92002,
        /// <summary>
        /// 
        /// </summary>
        地点已被其它小程序占用 = 92003,
        /// <summary>
        /// 
        /// </summary>
        附近功能被封禁 = 92004,
        /// <summary>
        /// 
        /// </summary>
        地点正在审核中 = 92005,
        /// <summary>
        /// 
        /// </summary>
        地点正在展示小程序 = 92006,
        /// <summary>
        /// 
        /// </summary>
        地点审核失败 = 92007,
        /// <summary>
        /// 
        /// </summary>
        程序未展示在该地点 = 92008,
        /// <summary>
        /// 
        /// </summary>
        小程序未上架或不可见 = 92009,
        地点不存在 = 93010,
        /// <summary>
        /// 
        /// </summary>
        个人类型小程序不可用 = 93011,

        //门店小程序返回码
        需要补充相应资料_填写org_code和other_files参数 = 85024,
        /// <summary>
        /// 
        /// </summary>
        管理员手机登记数量已超过上限 = 85025,
        /// <summary>
        /// 
        /// </summary>
        该微信号已绑定5个管理员 = 85026,
        /// <summary>
        /// 
        /// </summary>
        管理员身份证已登记过5次 = 85027,
        /// <summary>
        /// 
        /// </summary>
        该主体登记数量已超过上限 = 85028,
        /// <summary>
        /// 
        /// </summary>
        商家名称已被占用 = 85029,
        /// <summary>
        /// 
        /// </summary>
        不能使用该名称 = 85031,
        /// <summary>
        /// 
        /// </summary>
        该名称在侵权投诉保护期 = 85032,
        /// <summary>
        /// 
        /// </summary>
        名称包含违规内容或微信等保留字 = 85033,
        /// <summary>
        /// 
        /// </summary>
        商家名称在改名15天保护期内 = 85034,
        /// <summary>
        /// 
        /// </summary>
        需与该帐号相同主体才可申请 = 85035,
        /// <summary>
        /// 
        /// </summary>
        介绍中含有虚假混淆内容 = 85036,
        /// <summary>
        /// 
        /// </summary>
        头像或者简介修改达到每个月上限 = 85049,
        /// <summary>
        /// 
        /// </summary>
        没有权限 = 43104,
        /// <summary>
        /// 
        /// </summary>
        正在审核中_请勿重复提交 = 85050,
        /// <summary>
        /// 
        /// </summary>
        请先成功创建门店后再调用 = 85053,
        /// <summary>
        /// 
        /// </summary>
        临时mediaid无效 = 85056,

        /// <summary>
        /// 
        /// </summary>
        输入参数有误 = 40097,
        /// <summary>
        /// 
        /// </summary>
        门店不存在 = 65115,
        /// <summary>
        /// 该门店状态不允许更新
        /// </summary>
        该门店状态不允许更新 = 65118,
    }

}
