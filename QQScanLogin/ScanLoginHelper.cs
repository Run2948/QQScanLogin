using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using QQScanLogin.Models;

namespace QQScanLogin
{
    public class ScanLoginHelper
    {
        private const string QrCodeUrl = "https://ssl.ptlogin2.qq.com/ptqrshow?appid=716027609&e=2&l=M&s=3&d=72&v=4&t=0.28351309688526216&daid=383&pt_3rd_aid=0";

        private const string ScanResultUrl = "https://ssl.ptlogin2.qq.com/ptqrlogin?u1=https%3A%2F%2Fgraph.qq.com%2Foauth2.0%2Flogin_jump&ptqrtoken={0}&ptredirect=0&h=1&t=1&g=1&from_ui=1&ptlang=2052&action=0-0-{1}&js_ver=21050810&js_type=1&login_sig=&pt_uistyle=40&aid=716027609&daid=383&pt_3rd_aid=100273020&has_onekey=1&";

        private static int ParsePtqrToken(string qrsig)
        {
            // jgCTgNuHlzBG-eszRnRVYHtW84zUvYCOAQPFOQcvk7S0QxK61XsauT3aTTgB*5MU  --> 46943141
            var e = qrsig.Aggregate(0, (current, c) => current + ((current << 5) + Convert.ToInt32(c)));
            return e & 0x7fffffff;
        }

        private static List<string> ParsePtuiCbResult(string ptuiCb)
        {
            // ptuiCB('66','0','','0','二维码未失效。(3640661724)', '')
            // ptuiCB('67','0','','0','二维码认证中。(3180360363)', '')
            // ptuiCB('0','0','https://ssl.ptlogin2.graph.qq.com/check_sig?pttype=1&uin=3160410046&service=ptqrlogin&nodirect=0&ptsigx=a2a410d8e6165a167055947a6898ef8a812913897ebef40b696507b2f68929ba52c47b117e6372687ad231364d37e378f9dd2963bc9fecdc2065b7c920005f26&s_url=https%3A%2F%2Fgraph.qq.com%2Foauth2.0%2Flogin_jump&f_url=&ptlang=2052&ptredirect=100&aid=716027609&daid=383&j_later=0&low_login_hour=0&regmaster=0&pt_login_type=3&pt_aid=0&pt_aaid=16&pt_light=0&pt_3rd_aid=100273020','0','登录成功！', '默者非靡')"
            var ptuiParams = Regex.Replace(ptuiCb, @"^ptuiCB\((.*?)\)$", "$1");
            var cbResult = ptuiParams.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Replace('\'', ' ').Trim()).ToList();
            return cbResult;
        }

        private static string ParseQqNumber(string url)
        {
            // https://ssl.ptlogin2.graph.qq.com/check_sig?pttype=1&uin=3160410046&service=ptqrlogin&nodirect=0&ptsigx=a2a410d8e6165a167055947a6898ef8a812913897ebef40b696507b2f68929ba52c47b117e6372687ad231364d37e378f9dd2963bc9fecdc2065b7c920005f26&s_url=https%3A%2F%2Fgraph.qq.com%2Foauth2.0%2Flogin_jump&f_url=&ptlang=2052&ptredirect=100&aid=716027609&daid=383&j_later=0&low_login_hour=0&regmaster=0&pt_login_type=3&pt_aid=0&pt_aaid=16&pt_light=0&pt_3rd_aid=100273020
            var kv = HttpUtility.ParseQueryString(url, Encoding.Default);
            return kv["uin"];
        }

        private static string GetTimeStamp()
        {
            var ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }


        public static (Stream, string) GetLoginQrCode()
        {
            var uri = new Uri(QrCodeUrl);
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AllowAutoRedirect = true,
                UseCookies = true
            };
            using var client = new HttpClient(handler);
            var response = client.Send(request);
            var stream = response.Content.ReadAsStreamAsync().Result;
            var cookies = cookieContainer.GetCookies(uri).ToList();
            var qrsig = cookies.FirstOrDefault(x => x.Name == "qrsig")?.Value;
            return (stream, qrsig);
        }

        public static (bool, string, ScanResult) GetQqScanResult(string qrsig)
        {
            var timeStamp = GetTimeStamp();
            var ptqrToken = ParsePtqrToken(qrsig);
            var uri = new Uri(string.Format(ScanResultUrl, ptqrToken, timeStamp));
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Cookie("qrsig", qrsig) { Domain = uri.Host });
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                AllowAutoRedirect = true,
                UseCookies = true
            };
            using var client = new HttpClient(handler);
            var response = client.Send(request);
            var content = response.Content.ReadAsStringAsync().Result;
            var result = ParsePtuiCbResult(content);
            return result[0] == "0" ? (true, result[4], new ScanResult(ParseQqNumber(result[2]), result[5])) : (false, result[4], null);
        }
    }
}
