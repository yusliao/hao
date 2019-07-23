using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Util.Webs.Clients
{
    /// <summary>
    /// 表示文件内容
    /// </summary>
    class MulitpartFileContent : ByteArrayContent
    {
        /// <summary>
        /// 文件内容
        /// </summary>
        /// <param name="stream">文件流</param>
        /// <param name="name">名称</param>
        /// <param name="fileName">文件名</param>
        /// <param name="contentType">文件Mime</param>
        public MulitpartFileContent(byte[] data, string name,int blocknum,long blocksize, string fileName, string contentType)
            : base(data)
        {
            if (this.Headers.ContentDisposition == null)
            {
                var disposition = new ContentDispositionHeaderValue("form-data");
                //{
                //    Name = name,
                    
                //    FileName = fileName
                //};
                disposition.Parameters.Add(new NameValueHeaderValue("fileName", fileName));
                disposition.Parameters.Add(new NameValueHeaderValue("name", name));
                disposition.Parameters.Add(new NameValueHeaderValue("blocknum", blocknum.ToString()));
                disposition.Parameters.Add(new NameValueHeaderValue("blocksize", blocksize.ToString()));
                this.Headers.ContentDisposition = disposition;
            }

            if (string.IsNullOrEmpty(contentType) == true)
            {
                contentType = "application/octet-stream";
            }
            this.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
    }
}
