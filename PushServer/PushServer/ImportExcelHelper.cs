using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using PushServer.Commands;
using PushServer.Configuration;

namespace PushServer
{
    /// <summary>
    /// EXCEL 导入帮助类
    /// </summary>
    public class ExcelHelper
    {
        /// <summary>
        /// EXCEL订单处理
        /// </summary>
        /// <param name="isInput">是否导入订单</param>
        /// <returns></returns>
        public static bool Dowork(bool isInput=true)
        {
            if (isInput)
                return AppServer.ImportToOMS();
            else
                return AppServer.ExportExcel();
        }
        /// <summary>
        /// 导出银行回传单
        /// </summary>
        /// <param name="isInput"></param>
        /// <returns></returns>
        public static bool ExportExcel(bool isInput = true)
        {
             return AppServer.ImportErpToOMS();
        }
       

    }
}
