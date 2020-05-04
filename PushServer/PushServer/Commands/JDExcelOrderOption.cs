using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using DistrictService;
using OMS.Models;
using OMS.Models.DTO;
using PushServer.Configuration;
using PushServer.ModelServer;
using Util;
using Util.Files;

namespace PushServer.Commands
{
    [Export(typeof(IOrderOption))]
    public class JDExcelOrderOption : OrderOptionBase
    {
        public override string Name => OMS.Models.OrderSource.JINGDONG;
        private string NameDesc = Util.Helpers.Reflection.GetDescription<OrderSource>(OrderSource.JINGDONG);


        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];

       

        protected  List<FileInfo> GetExcelFiles()
        {
            FileScanner.ScanAllFiles(new DirectoryInfo(clientConfig.ExcelOrderFolder), "*.csv");
            if (FileScanner.ScannedFiles.Any())
            {
                return FileScanner.ScannedFiles;
            }
            else
                return new List<FileInfo>();
        }
       
      

        protected override List<OrderEntity> FetchOrders()
        {

            var ordersList = new List<OrderEntity>();

            foreach (var file in this.GetExcelFiles())
            {
                OnUIMessageEventHandle($"正在解析ERP导出单文件：{file.FullName}");

                using (var csv = new CsvReader(new StreamReader(file.FullName, Encoding.Default)))
                {
                    ResolveOrders(csv, file.FullName, ref ordersList);
                }
            }
            return ordersList;
        }


        /// <summary>
        /// 解析订单
        /// </summary>
        /// <param name="csv">待解析目标对象</param>
        /// <param name="file">待解析目标文件名</param>
        /// <param name="items">已解析订单集合</param>
        protected void ResolveOrders(CsvReader csv, string file, ref List<OrderEntity> items)
        {
            System.Collections.Concurrent.ConcurrentDictionary<long, int> orderProductCountDic = new System.Collections.Concurrent.ConcurrentDictionary<long, int>();
            csv.Read();
            csv.ReadHeader();
         
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.fileName = file;
            List<string> badRecord = new List<string>();
            csv.Configuration.BadDataFound = context => badRecord.Add(context.RawRecord);
            while (csv.Read())
            {
                /*处理逻辑：
                 * 平台单号是否为空
                 * 销售订单 平台单号是否存在数据库中
                 * 
                 */

                using (var db = new OMSContext())
                {
                    var desc = csv.GetField<string>("店铺名称").Trim();
                    
                    var config = AppServer.Instance.ConfigDictionary.Values.FirstOrDefault(c => c.Tag.Contains(desc));
                    if (config == null)
                    {
                        OnUIMessageEventHandle($"ERP导出单：{file}。未识别的订单渠道：{desc}");

                        continue;
                    }
                    else if (config.Name != OrderSource.JINGDONG && config.Name != OrderSource.TIANMAO)
                        continue;

                    orderDTO.sourceSN = csv.GetField<string>("平台单号").Trim();


                    if (string.IsNullOrEmpty(orderDTO.sourceSN))
                    {
                        //TODO:
                        InputExceptionOrder(orderDTO, ExceptionType.SourceSnIsNull);
                        continue;
                    }
                    string ordertype = csv.GetField<string>("订单类型").Trim();


                    switch (ordertype)
                    {
                        case "销售订单":
                            orderDTO.orderType = 0;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                        case "换货订单":
                        case "补发货订单":
                            orderDTO.orderType = 1;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                        case "退货退钱订单":
                            orderDTO.orderType = 2;
                            orderDTO.orderStatus = OrderStatus.Cancelled;
                            break;
                        default:
                            orderDTO.orderType = 0;
                            orderDTO.orderStatus = OrderStatus.Delivered;
                            break;
                    }

                    var order = db.OrderSet.Include(o => o.OrderLogistics.Select(l => l.LogisticsProducts)).Include(o => o.Products).FirstOrDefault(o => o.SourceSn == orderDTO.sourceSN);
                    
                    if (order==null)//新增订单信息
                    {
                        // Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");

                        ResolveOrdersFromERPExcel(csv, file, items);

                    }
                    OnUIMessageEventHandle($"ERP导出单：{file}。该文件中订单编号：{orderDTO.sourceSN}解析完毕");

                }
            }
          

            if (badRecord.Count > 0)
            {
                foreach (var item in badRecord)
                {
                    Util.Logs.Log.GetLog(nameof(ERPExcelOrderOption)).Debug(item);
                }

            }


        }
        /// <summary>
        /// 从ERP导出单解析订单对象
        /// </summary>
        /// <param name="csv"></param>
        /// <param name="file"></param>
        /// <param name="items">已解析订单集合</param>
        /// <returns></returns>
        private OrderEntity ResolveOrdersFromERPExcel(CsvReader csv, string file, List<OrderEntity> items)
        {
            var desc = csv.GetField<string>("店铺名称").Trim();
            // var opt = this.OrderOptSet.FirstOrDefault(o => Util.Helpers.Reflection.GetDescription<OrderSource>(o.clientConfig.Name.ToUpper()) == desc);
            var config = AppServer.Instance.ConfigDictionary.Values.FirstOrDefault(c => c.Tag.Contains(desc));
            if (config == null)
            {
                OnUIMessageEventHandle($"ERP导出单：{file}。未识别的订单渠道：{desc}");

                return null;
            }
            
            OrderDTO orderDTO = new OrderDTO();
            orderDTO.fileName = file;
            orderDTO.source = config.Name;
            orderDTO.sourceDesc = desc;
            orderDTO.sourceSN = csv.GetField<string>("平台单号").Trim();
            if (string.IsNullOrEmpty(orderDTO.sourceSN))
                return null;

            orderDTO.orderSN = string.Format("{0}-{1}_{2}", orderDTO.source, orderDTO.sourceSN, DateTime.Now.ToString("yyyyMMdd"));
            var orderDate = csv.GetField<string>("付款时间");
            if (string.IsNullOrEmpty(orderDate))
                orderDate = csv.GetField<string>("配货时间");

            orderDTO.createdDate = DateTime.Parse(orderDate);


            orderDTO.productName = csv.GetField<string>("平台商品名称").Trim();
            orderDTO.productsku = csv.GetField<string>("商品代码").Trim();
            var quantity = orderDTO.count = csv.GetField<string>("订购数").ToInt();
            decimal weight = csv.GetField<string>("总重量").ToInt();
            orderDTO.consigneeName = csv.GetField<string>("收货人").Trim();
            orderDTO.consigneePhone = csv.GetField<string>("收货人手机").Trim();
           
           
            orderDTO.consigneeAddress = csv.GetField<string>("收货地址").Trim();
           

            var addrInfo = DistrictService.DistrictService.ResolveAddress(orderDTO.consigneeAddress);
            orderDTO.consigneeProvince = addrInfo.Province;
            orderDTO.consigneeCity = addrInfo.City;
            orderDTO.consigneeCounty = addrInfo.County;
                //   consigneeAddress = addrInfo.Address;
            
            orderDTO.consigneeZipCode = string.Empty;
            int weightcode = 0;
            csv.TryGetField<int>("规格代码", out weightcode);
            orderDTO.weightCode = weightcode;
            orderDTO.weightCodeDesc = csv.GetField<string>("规格名称");


            string ordertype = csv.GetField<string>("订单类型").Trim();
            orderDTO.OrderComeFrom = 2;
            switch (ordertype)
            {
                case "销售订单":
                    orderDTO.orderType = 0;
                    orderDTO.orderStatus = OrderStatus.Delivered;
                    break;
                case "换货订单":
                case "补发货订单":
                    orderDTO.orderType = 1;
                    orderDTO.orderStatus = OrderStatus.Delivered;
                    break;
                case "退货退钱订单":
                    orderDTO.orderType = 2;
                    orderDTO.orderStatus = OrderStatus.Cancelled;
                    break;
                default:
                    orderDTO.orderType = 0;
                    break;
            }

            //这里的订单都是数据库中没有的订单
            //已解析集合中查找，没找到就新增对象，找到就关联新的商品
            var item = items.Find(o => o.OrderSn == orderDTO.orderSN);
            if (item == null)//集合中不存在该订单对象
            {
                var orderItem = OrderEntityService.CreateOrderEntity(orderDTO);


                if (orderItem.OrderType == 0)
                {
                    using (var db = new OMSContext())
                    {


                        //查找联系人
                        if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                        {
                            OrderEntityService.InputConsigneeInfo(orderItem, db);

                        }
                        else //异常订单
                        {
                            InputExceptionOrder(orderDTO, ExceptionType.PhoneNumOrPersonNameIsNull);
                            return null;
                        }



                        if (!InputProductInfoWithoutSaveChange(db, orderDTO, orderItem))
                        {
                            return null;
                        }

                        OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                        orderLogisticsDetail.OrderSn = orderItem.OrderSn;





                        db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                        db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                        //  db.OrderProductSet.Add(orderProductInfo);
                        db.SaveChanges();
                        items.Add(orderItem);
                        return orderItem;
                    }
                }
                else
                    return null;

            }
            else
            {
                orderDTO.consigneeName = csv.GetField<string>("收货人").Trim();
                orderDTO.consigneePhone = csv.GetField<string>("收货人手机").Trim();

                orderDTO.count = csv.GetField<string>("订购数").ToInt();

                using (var db = new OMSContext())
                {
                    InputProductInfoWithoutSaveChange(db, orderDTO, item);
                }
                return null;
            }



        }
        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("订单号");
            dt.Columns.Add("货单号");
            dt.Columns.Add("发货日期");
            dt.Columns.Add("发货时间");
            dt.Columns.Add("物流公司编码");
            dt.Columns.Add("物流公司");
            dt.Columns.Add("客服电话");
            dt.Columns.Add("客服网址");
            dt.Columns.Add("物流公司投递员");
            dt.Columns.Add("投递员手机号码");
            dt.Columns.Add("处理备注");
            dt.Columns.Add("物流平台");
            if(orders!=null&&orders.Any())
            {
                using (var db = new OMSContext())
                {
                    foreach (var item in orders)
                    {
                        if (item.OrderLogistics != null && item.OrderLogistics.Any())
                        {
                            foreach (var logisticsDetail in item.OrderLogistics)
                            {
                                var dr = dt.NewRow();
                                dr["订单号"] = item.SourceSn;
                                dr["货单号"] = logisticsDetail.LogisticsNo;
                                dr["发货日期"] = logisticsDetail.SendingTime.HasValue ? logisticsDetail.SendingTime.Value.ToShortDateString() : logisticsDetail.PickingTime.Value.ToShortDateString();
                                dr["发货时间"] = logisticsDetail.SendingTime.HasValue ? logisticsDetail.SendingTime.Value.ToShortTimeString() : logisticsDetail.PickingTime.Value.ToShortTimeString();
                                dr["物流公司编码"] = db.logisticsInfoSet.FirstOrDefault(l => l.FullName == logisticsDetail.Logistics)?.BankLogisticsCode;
                                dr["物流公司"] = logisticsDetail.Logistics;
                                dt.Rows.Add(dr);
                            }
                        }

                       
                    }
                }
            }
            return dt;
            
        }
    }
}
