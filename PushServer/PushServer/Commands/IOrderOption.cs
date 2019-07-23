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
        /// 导入OMS
        /// </summary>
        /// <returns></returns>
        bool ImportToOMS();
        /// <summary>
        /// 创建EXCEL文件
        /// </summary>
        /// <returns></returns>
        DataTable ExportExcel(List<OrderEntity> orders);
        bool PushReport();
        
    }
}
