using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OMS.Models;
using PushServer.Configuration;
namespace PushServer.Commands
{
    /// <summary>
    /// 订单操作
    /// </summary>
    interface IOrderOption
    {
        IClientConfig clientConfig { get; }
        /// <summary>
        /// 导入OMS,生成ERP导入单
        /// </summary>
        /// <returns></returns>
        bool ExcelToOMS();
        /// <summary>
        /// EXCEL文件，导出银行回传单
        /// </summary>
        /// <returns></returns>
        DataTable ExportExcel(List<OrderEntity> orders);
        void Dispose();
       
       
        
    }
}
