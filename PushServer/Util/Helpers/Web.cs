using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Text;
using System.Web;


namespace Util.Helpers
{
    /// <summary>
    /// Web操作
    /// </summary>
    public static class Web
    {

        #region 静态构造方法

       

        #endregion

        #region 属性




        #endregion

        #region User(当前用户安全主体)

        

        #endregion

        #region Identity(当前用户身份)

       

        #endregion

        #region Body(请求正文)

      

        #endregion

        #region Client( Web客户端 )

        /// <summary>
        /// Web客户端，用于发送Http请求
        /// </summary>
        public static Util.Webs.Clients.WebClient Client()
        {
            return new Util.Webs.Clients.WebClient();
        }

        /// <summary>
        /// Web客户端，用于发送Http请求
        /// </summary>
        /// <typeparam name="TResult">返回结果类型</typeparam>
        public static Util.Webs.Clients.WebClient<TResult> Client<TResult>() where TResult : class
        {
            return new Util.Webs.Clients.WebClient<TResult>();
        }

        #endregion

        #region Url(请求地址)

       

        #endregion

        #region Ip(客户端Ip地址)

        /// <summary>
        /// Ip地址
        /// </summary>
        private static string _ip;

        /// <summary>
        /// 设置Ip地址
        /// </summary>
        /// <param name="ip">Ip地址</param>
        public static void SetIp(string ip)
        {
            _ip = ip;
        }

        /// <summary>
        /// 重置Ip地址
        /// </summary>
        public static void ResetIp()
        {
            _ip = null;
        }

        /// <summary>
        /// 客户端Ip地址
        /// </summary>
        public static string Ip
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_ip) == false)
                    return _ip;
                
                    
                return GetLanIp();
            }
        }

        /// <summary>
        /// 获取局域网IP
        /// </summary>
        private static string GetLanIp()
        {
            foreach (var hostAddress in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (hostAddress.AddressFamily == AddressFamily.InterNetwork)
                    return hostAddress.ToString();
            }
            return string.Empty;
        }

        #endregion

        #region Host(主机)

        /// <summary>
        /// 主机
        /// </summary>
        public static string Host => Dns.GetHostName();

       

        

        #endregion
       
    }
}