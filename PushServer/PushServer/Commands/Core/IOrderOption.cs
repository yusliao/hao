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
    interface IOrderOption
    {
        IClientConfig clientConfig { get; }
        /// <summary>
        /// 导入OMS,生成ERP导入单
        /// </summary>
        /// <returns></returns>
        bool ImportToOMS();
        /// <summary>
        /// EXCEL文件，导出银行回传单
        /// </summary>
        /// <returns></returns>
        DataTable ExportExcel(List<OrderEntity> orders);
        void Dispose();
       
       
        
    }
}
