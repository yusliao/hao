using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public class HttpHelper
    {
        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="strFileName">保存文件的全路径</param>
        /// <param name="md5">md5</param>
        /// <param name="http">网络地址</param>
        /// <param name="callback">回调</param>
        /// <returns></returns>
        public static bool DownloadFile(string strFileName, string md5, string http, Action<int> callback,Action completed=null)
        {
            var flag = false;
            FileStream FStream = null;
            try
            {
                long SPosition = 0;
                if (File.Exists(strFileName))
                {
                    if (md5 == GetFileMd5(strFileName))//如果文件相同
                    {
                        return true;
                    }
                    FStream = File.OpenWrite(strFileName);
                    SPosition = FStream.Length;//断点续传
                    FStream.Seek(SPosition, SeekOrigin.Current);
                }
                else
                {
                    FStream = new FileStream(strFileName, FileMode.Create);
                    SPosition = 0;
                }

                var myRequest = (HttpWebRequest)WebRequest.Create(http);
                if (SPosition > 0)
                    myRequest.AddRange((int)SPosition);
                var myStream = myRequest.GetResponse().GetResponseStream();
                var btContent = new byte[512];
                if (myStream != null)
                {
                    var intSize = myStream.Read(btContent, 0, 512);
                    while (intSize > 0)
                    {
                        callback?.Invoke(intSize);
                        FStream.Write(btContent, 0, intSize);
                        intSize = myStream.Read(btContent, 0, 512);
                    }
                    FStream.Close();
                    myStream.Close();
                }
                flag = true;
            }
            catch (Exception ex)
            {
                FStream?.Close();
                flag = false;
                File.Delete(strFileName);
            }
            if(flag) completed?.Invoke();
            return flag;
        }


        #region 辅助

        #region MD5加密

        /// <summary>
        /// MD5加密
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static string GetFileMd5(string src)
        {
            byte[] result = File.ReadAllBytes(src); //tbPass为输入密码的文本框  
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            return BitConverter.ToString(output).Replace("-", "").ToLower(); //tbMd5pass为输出加密文本的文本框 .Replace("-", "")
        }

        #endregion

        #region AES加密

        /// <summary>
        /// AES加密
        /// </summary>
        /// <param name="toEncrypt">需要加密的字符串</param>
        /// <param name="key">加密密钥</param>
        /// <returns></returns>
        public static string AESEncrypt(string toEncrypt, string key)
        {
            if (toEncrypt == null || toEncrypt.Equals("") || key == null || key.Equals(""))
            {
                return "";
            }

            try
            {
                byte[] keyArray = Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

                //用密码对令牌源串分段AES128位加密 最后一段如果不足16字节，用空格补齐。
                int nCount = toEncryptArray.Length % 16;
                if (nCount > 0)
                    nCount = 16 - nCount;
                while (nCount-- > 0)
                    toEncrypt += " ";
                toEncryptArray = Encoding.UTF8.GetBytes(toEncrypt);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.None;

                ICryptoTransform cTransform = rDel.CreateEncryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                string cryStr = Convert.ToBase64String(resultArray, 0, resultArray.Length);
                //为了方便用于URL传达室输，对特殊字符进行替换处理
                return cryStr.Replace('/', '.').Replace('+', '*');
            }
            catch
            {
                return "";
            }
        }

        #endregion

        #region AES解密

        /// <summary>
        /// AES解密
        /// </summary>
        /// <param name="toDecrypt">需要解密的字符串</param>
        /// <param name="key">解密密钥</param>
        /// <returns></returns>
        public static string AESDecrypt(string toDecrypt, string key)
        {
            if (toDecrypt == null || toDecrypt.Equals("") || key == null || key.Equals(""))
            {
                return "";
            }

            try
            {
                //为了方便用于URL传达室输，对特殊字符进行还原处理
                toDecrypt = toDecrypt.Replace('.', '/').Replace('*', '+');
                byte[] keyArray = Encoding.UTF8.GetBytes(key);
                byte[] toEncryptArray = Convert.FromBase64String(toDecrypt);

                RijndaelManaged rDel = new RijndaelManaged();
                rDel.Key = keyArray;
                rDel.Mode = CipherMode.ECB;
                rDel.Padding = PaddingMode.None;

                ICryptoTransform cTransform = rDel.CreateDecryptor();
                byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

                return Encoding.UTF8.GetString(resultArray).Trim();
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        #endregion

        #endregion

        #region 异步返回文件流(提高应用程序的吞吐能力)
        //public async Task<ActionResult> AwaitFile()
        //{
        //    var responseHtml = await GetResponseStream("http://static.xuexiba.com/uploadfile/UserInfo/Avatar/201311/1302844759237319155175.jpg");
        //    return File(responseHtml, "image/jpeg");
        //}

        public static async Task<Stream> GetResponseStream(string url)
        {
            return await GetResponseContentAsync(url);
        }

        private static async Task<Stream> GetResponseContentAsync(string url)
        {
            //var myRequest = (HttpWebRequest)WebRequest.Create(url);
            //return  myRequest.GetResponse().GetResponseStream();
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return await response.Content.ReadAsStreamAsync();
            }
            else
            {
                return null;//error
            }
        }

        #endregion
    }
}
