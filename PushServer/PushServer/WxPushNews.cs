
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using FusionStone.WeiXin;
using OMS.Models;

namespace PushServer
{
    public class WxPushNews
    {
        public static string CreateWxNewsOAuthUrl(string redirectUri)
        {
            return string.Format(@"https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_base&state=LJNY&connect_redirect=1#wechat_redirect", WxConfiguration.CorpId, redirectUri);
        }
        public static void OrderStatistic(List<WxArticle> wxArticles,string serverName)
        {
            var wxTargets = System.Configuration.ConfigurationManager.AppSettings["WxNewsTargets"].Split(new char[] { ','}).ToList();

            var wxMassApiList = new List<WxMassApiWrapper>();
            foreach (var targetName in wxTargets)
            {
                var item = new WxMassApiWrapper(targetName);
                switch (serverName)
                {
                    case OrderSource.CIB:
                    case OrderSource.CIBAPP:
                    case OrderSource.CIBVIP:
                        if(targetName=="LJNY")
                            WxApiRetryBlock.Run(() => item.SendNews(wxArticles));
                        break;
                    default:
                        if(targetName=="ALL")
                            WxApiRetryBlock.Run(() => item.SendNews(wxArticles));
                        break;
                }
                
            }
            
        }
        public static void SendErrorText(string msg)
        {
            var massApi = new WxMassApiWrapper("ERROR");

            WxApiRetryBlock.Run(() => massApi.SendText($"您有新的错误订单需要处理!]\r\n{msg}"));
        }
    }
}
