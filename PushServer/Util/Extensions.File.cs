using System.IO;

namespace Util {
    /// <summary>
    /// 系统扩展 - 文件或流相关扩展
    /// </summary>
    public static partial class Extensions {
        /// <summary>
        /// 将字节流写入文件
        /// </summary>
        /// <param name="stream">字节流</param>
        /// <param name="filePath">文件绝对路径</param>
        public static void ToFile( this byte[] stream, string filePath ) {
            var directoryPath = Path.GetDirectoryName( filePath );
            if ( Directory.Exists( directoryPath ) == false )
                Directory.CreateDirectory( directoryPath );
            File.WriteAllBytes( filePath, stream );
        }
        public static string GetFileSizeString(this long value)
        {
            var v = (long)value;

            if (v < 1024)
            {
                return value + "B";
            }
            else if ((v / 1024) < 1024)
            {
                return string.Format("{0:0.0}KB", v / 1024d);
            }
            else
            {
                return string.Format("{0:0.00}MB", v / (1024 * 1024d));
            }
        }
    }
}
