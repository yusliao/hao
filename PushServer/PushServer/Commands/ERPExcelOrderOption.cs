using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using OMS.Models;
using PushServer.Configuration;
using Util;

namespace PushServer.Commands
{
    public class ERPExcelOrderOption : OrderOptionBase
    {
        public override string Name => OrderSource.ERP;
        [ImportMany(typeof(IOrderOption))]
        private IEnumerable<IOrderOption> OrderOptSet { get; set; }

        public override IClientConfig clientConfig => AppServer.Instance.ConfigDictionary[Name];
        public ERPExcelOrderOption()
        {
            #region MEF配置
            MyComposePart();
            #endregion
        }
        void MyComposePart()
        {
            var catalog = new AssemblyCatalog(Assembly.GetExecutingAssembly());
            var container = new CompositionContainer(catalog);
            //将部件（part）和宿主程序添加到组合容器
            container.ComposeParts(this);
        }

        public override DataTable ExportExcel(List<OrderEntity> orders)
        {
            return null;
        }
        public  bool ImportErpToOMS()
        {
            var lst = FetchOrders();
            if (lst != null && lst.Any())
            {
                OnUIMessageEventHandle($"正在生成ERP回传物流订单,订单数量：{lst.Count}");
               
                OnPostCompleted(true, lst,OptionType.LogisticsExcel);
                return true;
            }
            else
                return false;
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
        protected List<FileInfo> GetExcelFiles()
        {


            FileScanner.ScanAllFiles(new DirectoryInfo(Path.Combine(clientConfig.ExcelOrderFolder, "export")), "*.csv");
            if (FileScanner.ScannedFiles.Any())
            {
                return FileScanner.ScannedFiles;
            }
            else
                return new List<FileInfo>();
        }
        protected void ResolveOrders(CsvReader csv, string file, ref List<OrderEntity> items)
        {
            csv.Read();
            csv.ReadHeader();
            List<OrderEntity> newOrderlst = new List<OrderEntity>();
            while (csv.Read())
            {
                using (var db = new OMSContext())
                {

                    string sn = csv.GetField<string>("平台单号").Trim();
                    var order = db.OrderSet.Include(o => o.OrderLogistics).Include(o => o.Products).FirstOrDefault(o => o.SourceSn == sn);
                    if (order != null)//
                    {
                        string warehouse = csv.GetField<string>("仓库名称");
                        string pcode = csv.GetField<string>("商品代码").Trim();//ERP标识的商品编码
                        decimal weight = csv.GetField<string>("总重量").ToInt();
                        int count = csv.GetField<string>("数量").ToInt();

                        var p1 = order.Products.FirstOrDefault(p => p.sku == pcode);
                        if (p1 != null)//修改已有商品汇总信息
                        {
                            p1.Warehouse = warehouse;
                            p1.ProductWeight = weight;
                            p1.ProductCount = count;

                        }
                        else//新增订单商品
                        {
                            OrderProductInfo orderProductInfo = new OrderProductInfo()
                            {
                                ProductPlatName = csv.GetField<string>("商品名称").Trim(),
                                ProductPlatId = pcode,
                                MonthNum = order.CreatedDate.Month,
                                Warehouse = warehouse,

                                weightCode = csv.GetField<string>("规格代码").ToInt(),
                                weightCodeDesc = csv.GetField<string>("规格名称").Trim(),
                                OrderSn = order.OrderSn,
                                //  TotalAmount = csv.GetField<string>("规格名称"),
                                ProductCount = count,
                                ProductWeight = weight,
                                Source = order.Source,
                                sku = pcode

                            };
                            db.OrderProductSet.Add(orderProductInfo);
                            order.Products.Add(orderProductInfo);

                        }

                        OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                        orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                        orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                        orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                        orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                        //发货时间为空时，从配货时间中取日期作为发货时间
                        orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                        var templ = order.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo);//是否已经存在该物流信息，防止重复添加
                        if (templ == null)
                        {
                            if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                            {
                                db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                                order.OrderLogistics.Add(orderLogisticsDetail);
                            }

                        }
                        db.SaveChanges();
                        items.Add(order);
                    }
                    else//新增订单信息
                    {
                        // Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file}。该文件中订单编号：{sn}在OMS系统中不存在");
                     
                        var item = ResolveOrdersFromERPExcel(csv, file, items);
                        if (item != null)
                            newOrderlst.Add(item);
                        
                       

                    }
                    OnUIMessageEventHandle($"ERP导出单：{file}。该文件中订单编号：{sn}解析完毕");
                   
                }
            }
            if(newOrderlst.Any())
                InsertDB(newOrderlst);


        }
        private OrderEntity ResolveOrdersFromERPExcel(CsvReader csv, string file, List<OrderEntity> items)
        {
            var desc = csv.GetField<string>("店铺名称").Trim();
            var opt = this.OrderOptSet.FirstOrDefault(o => Util.Helpers.Reflection.GetDescription<OrderSource>(o.clientConfig.Name.ToUpper()) == desc);
            if (opt == null)
            {

                Util.Logs.Log.GetLog(nameof(AppServer)).Error($"ERP导出单：{file},未识别的订单渠道：{desc}");
                OnUIMessageEventHandle($"ERP导出单：{file}。未识别的订单渠道：{desc}");
                
                return null;
            }
#if CESHI
             if (opt.clientConfig.Name.ToUpper() == OrderSource.TIANMAO)
            {
                Console.WriteLine("TM");
            }
#endif

            var source = opt.clientConfig.Name;
            var sourcedesc = desc;
            var sourceSN = csv.GetField<string>("平台单号").Trim();
            if (string.IsNullOrEmpty(sourceSN))
                return null;
            var orderSN = string.Format("{0}-{1}", source, sourceSN); //订单SN=来源+原来的SN

            var orderDate = csv.GetField<string>("付款时间");
            if (string.IsNullOrEmpty(orderDate))
                orderDate = csv.GetField<string>("配货时间");

            var createdDate = DateTime.Parse(orderDate);


            var orderStatus = OrderStatus.Delivered;


            var productName = csv.GetField<string>("平台商品名称").Trim();
            var quantity = csv.GetField<string>("数量").ToInt();

            var consigneeName = csv.GetField<string>("收货人").Trim();
            var consigneePhone = csv.GetField<string>("收货人手机").Trim();
            if (string.IsNullOrEmpty(consigneePhone))
                consigneePhone = csv.GetField<string>("收货人电话").Trim();

            var consigneePhone2 = csv.GetField<string>("收货人电话").Trim();

            var consigneeProvince = csv.GetField<string>("省").Trim();
            var consigneeCity = csv.GetField<string>("市").Trim();
            var consigneeCounty = csv.GetField<string>("区/县").Trim();
            var consigneeAddress = csv.GetField<string>("收货地址").Trim();
            var consigneeZipCode = string.Empty;


            //内存中查找，没找到就新增对象，找到就关联新的商品
            var item = items.Find(o => o.OrderSn == orderSN);
            if (item == null)
            {
                var orderItem = new OrderEntity()
                {
                    SourceSn = sourceSN,
                    OrderSn = orderSN,
                    Source = source,
                    SourceDesc = sourcedesc,
                    CreatedDate = createdDate,
                    Consignee = new CustomerEntity()
                    {
                        Name = consigneeName,
                        Phone = consigneePhone,
                        Phone2 = consigneePhone2,
                        CreateDate = createdDate
                    },
                    ConsigneeAddress = new AddressEntity()
                    {
                        Address = consigneeAddress,
                        City = consigneeCity,
                        Province = consigneeProvince,
                        County = consigneeCounty,
                        ZipCode = consigneeZipCode

                    },
                    OrderDateInfo = new OrderDateInfo()
                    {
                        CreateTime = createdDate,
                        DayNum = createdDate.DayOfYear,
                        MonthNum = createdDate.Month,
                        WeekNum = Util.Helpers.Time.GetWeekNum(createdDate),
                        SeasonNum = Util.Helpers.Time.GetSeasonNum(createdDate),
                        Year = createdDate.Year,
                        TimeStamp = Util.Helpers.Time.GetUnixTimestamp(createdDate)
                    },


                    OrderStatus = (int)orderStatus,
                    OrderStatusDesc = "已发货",
                    OrderComeFrom = 2,

                    Remarks = string.Empty
                };
                if (orderItem.Products == null)
                    orderItem.Products = new List<OrderProductInfo>();
                //处理订单与地址、收货人、商品的关联关系。消除重复项
                using (var db = new OMSContext())
                {
                    //查找联系人
                    if (!string.IsNullOrEmpty(orderItem.Consignee.Phone))
                    {
                        string md5 = Util.Helpers.Encrypt.Md5By32(orderItem.ConsigneeAddress.Address.Trim().Replace(" ", ""));
                        var s = db.CustomersSet.Include<CustomerEntity, ICollection<AddressEntity>>(c => c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
                        if (s != null)
                        {

                            orderItem.Consignee = s;
                            orderItem.OrderExtendInfo = new OrderExtendInfo() { IsReturningCustomer = true };
                            DateTime startSeasonTime, endSeasonTime, startYearTime, endYearTime, startWeekTime, endWeekTime;
                            Util.Helpers.Time.GetTimeBySeason(orderItem.CreatedDate.Year, Util.Helpers.Time.GetSeasonNum(orderItem.CreatedDate), out startSeasonTime, out endSeasonTime);
                            Util.Helpers.Time.GetTimeByYear(orderItem.CreatedDate.Year, out startYearTime, out endYearTime);
                            Util.Helpers.Time.GetTimeByWeek(orderItem.CreatedDate.Year, Util.Helpers.Time.GetWeekNum(orderItem.CreatedDate), out startWeekTime, out endWeekTime);

                            orderItem.OrderRepurchase = new OrderRepurchase()
                            {
                                DailyRepurchase = true,
                                MonthRepurchase = s.CreateDate.Value.Date < new DateTime(orderItem.CreatedDate.Year, orderItem.CreatedDate.Month, 1).Date ? true : false,
                                SeasonRepurchase = s.CreateDate.Value.Date < startSeasonTime.Date ? true : false,
                                WeekRepurchase = s.CreateDate.Value.Date < startWeekTime.Date ? true : false,
                                YearRepurchase = s.CreateDate.Value.Date < startYearTime.Date ? true : false,

                            };
                            //更新收件人与地址的关系

                            if (s.Addresslist.Any(a => a.MD5 == md5))
                            {
                                var addr = s.Addresslist.First(a => a.MD5 == md5);
                                orderItem.ConsigneeAddress = addr;//替换地址对象
                            }
                            else
                            {
                                orderItem.ConsigneeAddress.MD5 = md5;
                                s.Addresslist.Add(orderItem.ConsigneeAddress);
                            }
                        }
                        else//没找到备案的收货人
                        {
                            orderItem.OrderExtendInfo = new OrderExtendInfo() { IsReturningCustomer = false };
                            orderItem.OrderRepurchase = new OrderRepurchase();

                            orderItem.ConsigneeAddress.MD5 = md5;
                            if (orderItem.Consignee.Addresslist == null)
                                orderItem.Consignee.Addresslist = new List<AddressEntity>();
                            orderItem.Consignee.Addresslist.Add(orderItem.ConsigneeAddress);

                            db.AddressSet.Add(orderItem.ConsigneeAddress);
                            db.CustomersSet.Add(orderItem.Consignee);
                        }

                    }
                    else //异常订单
                    {
                        ExceptionOrder exceptionOrder = new ExceptionOrder()
                        {
                            OrderFileName = file,
                            OrderInfo = Util.Helpers.Json.ToJson(orderItem),
                            Source = this.Name
                        };
                        db.ExceptionOrders.Add(exceptionOrder);
                        db.SaveChanges();
                        return null;
                    }

                    decimal weight = csv.GetField<string>("总重量").Trim().ToDecimal();
                    OrderProductInfo orderProductInfo = new OrderProductInfo()
                    {

                        ProductPlatName = productName,
                        //  Warehouse = orderItem.OrderLogistics.Logistics,
                        ProductPlatId = csv.GetField<string>("商品代码").Trim(),
                        MonthNum = createdDate.Month,
                        weightCode = csv.GetField<int>("规格代码"),
                        weightCodeDesc = csv.GetField<string>("规格名称"),
                        OrderSn = orderItem.OrderSn,
                        TotalAmount = 0,
                        ProductCount = quantity,
                        ProductWeight = weight,
                        Source = source,
                        Warehouse = csv.GetField<string>("仓库名称").Trim(),

                        sku = csv.GetField<string>("商品代码").Trim()
                    };
                    orderItem.Products.Add(orderProductInfo);


                    OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                    orderLogisticsDetail.OrderSn = orderItem.OrderSn;
                    orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                    orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                    orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                    orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                    //发货时间为空时，从配货时间中取日期作为发货时间
                    orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                    if (orderItem.OrderLogistics == null)
                        orderItem.OrderLogistics = new List<OrderLogisticsDetail>();
                    if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                    {
                   //     db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                       
                        orderItem.OrderLogistics.Add(orderLogisticsDetail);
                    }



                    db.OrderRepurchases.Add(orderItem.OrderRepurchase);
                    db.OrderDateInfos.Add(orderItem.OrderDateInfo);

                    //  db.OrderProductSet.Add(orderProductInfo);
                    db.SaveChanges();
                    items.Add(orderItem);
                    return orderItem;
                }

            }
            else
            {
                using (var db = new OMSContext())
                {
                    OrderLogisticsDetail orderLogisticsDetail = new OrderLogisticsDetail();
                    orderLogisticsDetail.OrderSn = item.OrderSn;
                    orderLogisticsDetail.Logistics = csv.GetField<string>("物流公司");
                    orderLogisticsDetail.LogisticsNo = csv.GetField<string>("物流单号").Trim();
                    orderLogisticsDetail.LogisticsPrice = csv.GetField<string>("物流费用").ToDecimalOrNull();
                    orderLogisticsDetail.PickingTime = DateTime.Parse(csv.GetField<string>("配货时间"));
                    //发货时间为空时，从配货时间中取日期作为发货时间
                    orderLogisticsDetail.SendingTime = csv.GetField<string>("发货时间") == "" ? orderLogisticsDetail.PickingTime.Value.Date : DateTime.Parse(csv.GetField<string>("发货时间"));
                    var templ = item.OrderLogistics.FirstOrDefault(o => o.LogisticsNo == orderLogisticsDetail.LogisticsNo);//是否已经存在该物流信息，防止重复添加
                    if (templ == null)
                    {
                        if (!string.IsNullOrEmpty(orderLogisticsDetail.LogisticsNo))//物流单号为空，不保存此信息
                        {
                            //db.OrderLogisticsDetailSet.Add(orderLogisticsDetail);
                            item.OrderLogistics.Add(orderLogisticsDetail);
                        }

                    }

                    decimal weight = csv.GetField<string>("总重量").Trim().ToDecimal();
                    OrderProductInfo orderProductInfo = new OrderProductInfo()
                    {

                        ProductPlatName = productName,
                        //  Warehouse = orderItem.OrderLogistics.Logistics,
                        ProductPlatId = csv.GetField<string>("商品代码").Trim(),
                        MonthNum = createdDate.Month,
                        weightCode = csv.GetField<int>("规格代码"),
                        weightCodeDesc = csv.GetField<string>("规格名称"),
                        OrderSn = item.OrderSn,
                        TotalAmount = 0,
                        ProductCount = quantity,
                        ProductWeight = weight,
                        Source = source,
                        Warehouse = csv.GetField<string>("仓库名称").Trim(),

                        sku = csv.GetField<string>("商品代码").Trim()
                    };

                    if (item.Products.FirstOrDefault(p => p.sku == orderProductInfo.sku) == null)
                    {
                        item.Products.Add(orderProductInfo);

                    }

                }
                return null;
            }





        }
        public bool ExportExcel(OptionType optionType, List<OMS.Models.OrderEntity> obj)
        {
            if(optionType== OptionType.ErpExcel)
            {
                CreateErpExcel(obj);
            }
            else
            {
                CreateLogisticsExcel(obj);
            }
            return true;
        }
        protected void CreateErpExcel(List<OMS.Models.OrderEntity> obj)
        {
            DataTable dt = new DataTable();
            #region create columns


            dt.Columns.Add("店铺");
            dt.Columns.Add("订单编号");
            dt.Columns.Add("买家会员");
            dt.Columns.Add("支付金额");
            dt.Columns.Add("商品名称");
            dt.Columns.Add("商品代码");
            dt.Columns.Add("规格代码");
            dt.Columns.Add("规格名称");
            dt.Columns.Add("是否赠品");
            dt.Columns.Add("数量");
            dt.Columns.Add("价格");
            dt.Columns.Add("商品备注");
            dt.Columns.Add("运费");
            dt.Columns.Add("买家留言");
            dt.Columns.Add("收货人");
            dt.Columns.Add("联系电话");
            dt.Columns.Add("联系手机");
            dt.Columns.Add("收货地址");
            dt.Columns.Add("省");
            dt.Columns.Add("市");
            dt.Columns.Add("区");
            dt.Columns.Add("邮编");
            dt.Columns.Add("订单创建时间");
            dt.Columns.Add("订单付款时间");
            dt.Columns.Add("发货时间");
            dt.Columns.Add("物流单号");
            dt.Columns.Add("物流公司");
            dt.Columns.Add("卖家备注");
            dt.Columns.Add("发票种类");
            dt.Columns.Add("发票类型");
            dt.Columns.Add("发票抬头");
            dt.Columns.Add("纳税人识别号");
            dt.Columns.Add("开户行");
            dt.Columns.Add("账号");

            dt.Columns.Add("地址");
            dt.Columns.Add("电话");
            dt.Columns.Add("是否手机订单");
            dt.Columns.Add("是否货到付款");
            dt.Columns.Add("支付方式");
            dt.Columns.Add("支付交易号");
            dt.Columns.Add("真实姓名");
            dt.Columns.Add("身份证号");
            dt.Columns.Add("仓库名称");
            dt.Columns.Add("预计发货时间");
            dt.Columns.Add("预计送达时间");
            dt.Columns.Add("订单类型");
            dt.Columns.Add("是否分销商订单");
            #endregion

            using (var db = new OMSContext())
            {
                foreach (var item in obj)
                {
                    foreach (var productInfo in item.Products)
                    {
                        var dr = dt.NewRow();
                        dr["店铺"] = item.SourceDesc;
                        dr["订单编号"] = item.SourceSn;
                        dr["买家会员"] = item.Customer == null ? item.Consignee.Name : item.Customer.Name;
                        dr["商品名称"] = productInfo.ProductPlatName;
                        dr["商品代码"] = productInfo.sku;
                        dr["规格代码"] = productInfo.weightCode == 0 ? string.Empty : productInfo.weightCode.ToString();
                        dr["数量"] = productInfo.ProductCount.ToString();
                        dr["价格"] = productInfo.TotalAmount.ToString();
                        dr["商品备注"] = item.Remarks;
                        dr["收货人"] = item.Consignee.Name;
                        dr["联系手机"] = item.Consignee.Phone ?? item.Consignee.Phone2;
                        dr["收货地址"] = item.ConsigneeAddress.Address;
                        dr["省"] = item.ConsigneeAddress.Province;
                        dr["市"] = item.ConsigneeAddress.City;
                        dr["区"] = item.ConsigneeAddress.County;
                        dr["邮编"] = item.ConsigneeAddress.ZipCode;
                        var foo = db.CustomStrategies.Include(c => c.Customer).FirstOrDefault(c => c.Customer.CustomerId == item.Consignee.CustomerId);
                        if (foo != null && Util.Helpers.Enum.Parse<CustomStrategyEnum>(foo.StrategyValue).HasFlag(CustomStrategyEnum.EmptyPackage))
                            dr["卖家备注"] = Util.Helpers.Enum.GetDescription<CustomStrategyEnum>(CustomStrategyEnum.EmptyPackage);
                        //dr["仓库名称"] = productInfo.Warehouse;
                        dt.Rows.Add(dr);

                    }

                }

            }
            var filename = System.IO.Path.Combine(clientConfig.ExcelOrderFolder, "import", $"ERP-{obj[0].Source}导入订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
            NPOIExcel.Export(dt, filename);
            OnUIMessageEventHandle($"ERP-{obj[0].Source}导入订单生成成功。文件名:{filename}");
           
            
        }
        protected void CreateLogisticsExcel(List<OMS.Models.OrderEntity> lst)
        {
            var glst = lst.GroupBy(o => o.Source).ToList();
            DataTable cib_dt = new DataTable();
            cib_dt.Columns.Add("订单号");
            cib_dt.Columns.Add("物流编号");
            cib_dt.Columns.Add("物流单号");
            int taskcount = glst.Count;
            foreach (var item in glst)
            {
                var option = OrderOptSet.FirstOrDefault(i => i.clientConfig.Name == item.Key);
                if (option != null)
                {
                    System.Threading.ThreadPool.QueueUserWorkItem(o =>
                    {
                        var dt = option.ExportExcel(item.ToList());
                        if (option.clientConfig.Name == OrderSource.CIB || option.clientConfig.Name == OrderSource.CIBAPP)
                        {
                            if (dt != null)
                            {
                                cib_dt.Merge(dt);
                            }
                        }
                        else
                        {
                            if (dt != null&&dt.Rows.Count>0)
                            {
                                string filename = Path.Combine(clientConfig.ExcelOrderFolder, "logistics", $"ERP-{item.Key}回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                                NPOIExcel.Export(dt, filename);
                                OnUIMessageEventHandle($"ERP-{item.Key}回传订单生成成功。文件名:{filename}");
                               
                            }
                        }
                        taskcount--;
                    });
                }
            }
            while (taskcount > 0)
            {
                System.Threading.Thread.Sleep(1000);
            }
            if (cib_dt.Rows.Count > 0)
            {
                string filename = Path.Combine(clientConfig.ExcelOrderFolder, "logistics", $"ERP-兴业银行积分回传订单{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx");
                NPOIExcel.Export(cib_dt, filename);
                OnUIMessageEventHandle($"ERP-兴业银行积分回传订单生成成功。文件名:{filename}");
               
            }
        }
    }
}
