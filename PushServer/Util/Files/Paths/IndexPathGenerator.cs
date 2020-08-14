using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util.Randoms;

namespace Util.Files.Paths
{
    public class IndexPathGenerator : PathGeneratorBase
    {
        /// <summary>
        /// path
        /// </summary>
        private readonly string _basePath;

        /// <summary>
        /// 初始化路径生成器
        /// </summary>
        /// <param name="basePath">基础路径</param>
        /// <param name="randomGenerator">随机数生成器</param>
        public IndexPathGenerator(string basePath, IRandomGenerator randomGenerator = null) : base(randomGenerator)
        {
            _basePath = basePath;
        }
        protected override string GeneratePath(string fileName)
        {
            return GetNewFileName(fileName,1);
        }
        /// <summary>
        /// 获取文件名
        /// </summary>
        /// <param name="combinePath">原始文件名</param>
        /// <param name="index">新文件序列号默认从1开始</param>
        /// <returns></returns>
        public  string GetNewFileName(string combinePath, int index)
        {
            //新文件名称
            var filename = $"{Path.GetFileNameWithoutExtension(combinePath)}({index}){Path.GetExtension(combinePath)}";

            if (index == 1)//首次
            {
                if (File.Exists(combinePath))//原始文件存在
                {
                    var temp = $"{Path.Combine(Path.GetDirectoryName(combinePath), filename)}";
                    if (File.Exists(temp))//新文件已经存在
                    {
                        index += 1;
                        return GetNewFileName(combinePath, index);//递增序号
                    }
                    else
                    {

                        return temp;
                    }
                }
                else//原始文件不存在
                {
                    return combinePath;
                }
            }
            else
            {
                var temp = $"{Path.Combine(Path.GetDirectoryName(combinePath), filename)}";
                if (File.Exists(temp))//新文件已经存在
                {
                    index += 1;
                    return GetNewFileName(combinePath, index);//递增序号
                }
                else
                {

                    return temp;
                }

            }

        }
    }
}
