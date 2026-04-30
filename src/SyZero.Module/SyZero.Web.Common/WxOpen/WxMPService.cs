using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace SyZero.Web.Common
{
    /// <summary>
    /// 小程序微信服务
    /// </summary>
    public class WxMPService
    {
        public string access_token { get; set; }
        public string appId { get; set; }
        public string appSecret { get; set; }

        public WxMPService()
        {

        }
        public WxMPService(string AppId, string AppSecret)
        {
            this.appId = AppId;
            this.appSecret = AppSecret;
        }

        /// <summary>
        /// 使用app的client_id 和 client_secret登陆并获取授权token
        /// </summary>
        /// <returns></returns>
        public async Task<JsCode2JsonResult> Login(string code)
        {
            try
            {
                string urlFormat = "https://api.weixin.qq.com/sns/jscode2session?appid={0}&secret={1}&js_code={2}&grant_type={3}";
                var url = string.Format(urlFormat, appId, appSecret, code, "authorization_code");
                var jsonResult = await RestHelper.WechatGet<JsCode2JsonResult>(url, "");
                return jsonResult;
            }
            catch (Exception ex)
            {
                return new JsCode2JsonResult() { errmsg = ex.Message, errcode = WxOpenReturnCode.系统错误system_error };
            }
        }

        /// <summary>
        /// 获取小程序AccessToken
        /// </summary>
        /// <returns></returns>
        public async Task<WxAccessToken> GetWxToken()
        {
            WxAccessToken accessToken = new WxAccessToken();
            string grant_type = "client_credential";
            string wxacodeUrl = string.Format("https://api.weixin.qq.com/cgi-bin/token?grant_type={0}&appid={1}&secret={2}", grant_type, appId, appSecret);
            RestRequest request = new RestRequest(wxacodeUrl, Method.Get);
            request.AddHeader("Content-Type", "application/json");
            //获取微信凭证
            var result = await RestHelper.ExecuteAsync(request);
            if (result == null) return accessToken;
            accessToken.access_token = result["access_token"].ToString();
            accessToken.expires_in = result["expires_in"].ToString();
            access_token = accessToken.access_token;
            Console.WriteLine("[获取小程序AccessToken]accessToken:" + accessToken.expires_in);
            return accessToken;
        }

        /// <summary>
        /// 发送小程序模板消息（需登录）
        /// </summary>
        /// <returns></returns>
        public async Task<WxacodeReturn> SubscribeMessage(string openid, string templateId, object data)
        {
            string wxacodeUrl = $"https://api.weixin.qq.com/cgi-bin/message/subscribe/send?access_token={access_token}";

            //获取微信凭证
            var requestDto = new WxSubscribeResult
            {
                template_id = templateId,
                touser = openid,
                data = data
            };
            RestRequest request = new RestRequest(wxacodeUrl, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(requestDto);
            var result = await RestHelper.ExecuteAsync<WxacodeReturn>(request);

            return result;
        }

        /// <summary>
        /// 获取小程序二维码
        /// </summary>
        /// <param name="access_token"></param>
        /// <param name="scene"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public async Task<WxacodeReturn> GetWxORCode(string scene, string page)
        {
            string wxacodeUrl = "https://api.weixin.qq.com";
            string url = string.Format("/wxa/getwxacodeunlimit?access_token={0}", access_token);

            //获取微信凭证
            var inputstr = new WxacodeInfoInput
            {
                scene = scene,
                page = page,
                width = 240
            };
            var param = JsonConvert.SerializeObject(inputstr);

            WxacodeReturn result = new WxacodeReturn();
            try
            {
                string strURL = wxacodeUrl + url;
                System.Net.HttpWebRequest request;
                request = (System.Net.HttpWebRequest)WebRequest.Create(strURL);
                request.Method = "POST";
                request.ContentType = "application/json";
                string paraUrlCoded = param;
                byte[] payload;
                payload = System.Text.Encoding.UTF8.GetBytes(paraUrlCoded);
                request.ContentLength = payload.Length;
                Stream writer = request.GetRequestStream();
                writer.Write(payload, 0, payload.Length);
                writer.Close();
                System.Net.HttpWebResponse response;
                response =  (System.Net.HttpWebResponse) await request.GetResponseAsync();
                System.IO.Stream s;
                s = response.GetResponseStream();//返回图片数据流
                byte[] tt = StreamToBytes(s);//将数据流转为byte[]
                if (response.ContentType == "image/jpeg")
                {
                    result.ImgBase64 = Convert.ToBase64String(tt);
                }
                else
                {
                    string str = System.Text.Encoding.Default.GetString(tt);
                    result = JsonConvert.DeserializeObject<WxacodeReturn>(str);
                    Console.WriteLine("[小程序生成二维码消息推送]errorcode:" + result.errcode + "errormsg:" + result.errmsg);
                }
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[小程序生成二维码消息推送]error:" + ex.Message);
                result.errcode = "-1";
                result.errmsg = ex.Message;
                return result;
            }
        }


        /// <summary>
        /// 获取用户访问小程序数据日趋势（需登录/仅支持单天）
        /// </summary>
        /// <returns></returns>
        public async Task<WxDailyVisitTrendResult> GetDailyVisitTrend(DateTime begin_date, DateTime end_date)
        {
            string wxacodeUrl = $"https://api.weixin.qq.com/datacube/getweanalysisappiddailyvisittrend?access_token={access_token}";

            var requestDto = new
            {
                begin_date = begin_date.ToString("yyyyMMdd"),
                end_date = end_date.ToString("yyyyMMdd")
            };
            RestRequest request = new RestRequest(wxacodeUrl, Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(requestDto);
            var result = await RestHelper.ExecuteAsync<WxDailyVisitTrendResult>(request);
            return result;
        }

        public byte[] StreamToBytes(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            int temp = stream.ReadByte();
            while (temp != -1)
            {
                bytes.Add((byte)temp);
                temp = stream.ReadByte();
            }
            return bytes.ToArray();
        }

    }
}
