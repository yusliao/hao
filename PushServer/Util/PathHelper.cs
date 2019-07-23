using System;
using System.IO;

namespace Util
{
    /// <summary>
    ///  路径助手
    /// </summary>
    public static class PathHelper
    {
        private static string fileCaches = @"\manjinba\{0}";
        public static string GetFileName(this string path)
        {
            return Path.GetFileName(path);
        }
        /// <summary>
        /// 检查子级路径，不存在则创建
        /// </summary>
        /// <param name="combinePahName"></param>
        /// <returns></returns>
        /// 
        public static string CheckPath(this string combinePahName)
        {
            var path = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" + combinePahName;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        public static string CheckPath(this string combinePahName,string userid)
        {
            string pathUser = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            pathUser = pathUser + string.Format(fileCaches, userid);
            var path = pathUser + "\\" + combinePahName;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
        /// <summary>
        /// 群头像
        /// </summary>
        public static string GroupFacePath
        {
            get { return "groupface".CheckPath(); }
        }        
        /// <summary>
        /// 用户头像
        /// </summary>
        public static string UserFacePath
        {
            get { return "userFace".CheckPath(); }
        }
        /// <summary>
        /// 表情头像
        /// </summary>
        public static string EmotionPath
        {
            get { return "Emotion".CheckPath(); }
        }
    }
}
