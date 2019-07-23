
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using FusionStone.WeiXin;


namespace M2.OrderManagement.Sync
{
    public class WxPushNews
    {
        public static string CreateWxNewsOAuthUrl(string redirectUri)
        {
            return string.Format(@"https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_base&state=LJNY&connect_redirect=1#wechat_redirect", WxConfiguration.CorpId, redirectUri);
        }
        public static void OrderStatistic(List<WxArticle> wxArticles)
        {
            var wxTargets = System.Configuration.ConfigurationManager.AppSettings["WxNewsTargets"].Split(new char[] { ','}).ToList();

            var wxMassApiList = new List<WxMassApiWrapper>();
            foreach (var targetName in wxTargets)
            {
                wxMassApiList.Add(new WxMassApiWrapper(targetName));
            }
            

            foreach (var massApi in wxMassApiList)
            {
                WxApiRetryBlock.Run(() => massApi.SendNews(wxArticles));
              
            }
        }
    }
}
