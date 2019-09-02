
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using FusionStone.WeiXin;
using OMS.Models;

namespace M2.OrderManagement.Sync
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
                if (targetName == "ALL")
                    WxApiRetryBlock.Run(() => item.SendNews(wxArticles));
                else if (serverName == OrderSource.CIB || serverName == OrderSource.CIBAPP || serverName == OrderSource.CIBVIP)
                {
                    WxApiRetryBlock.Run(() => item.SendNews(wxArticles));
                }
                else
                    continue;
                
            }
            

            //foreach (var massApi in wxMassApiList)
            //{
            //    if(massApi.)
            //    if(serverName== OrderSource.CIB||serverName==OrderSource.CIBAPP||serverName==OrderSource.CIBVIP)
            //    { }
            //    WxApiRetryBlock.Run(() => massApi.SendNews(wxArticles));
              
            //}
        }
    }
}
