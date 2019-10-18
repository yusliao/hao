using OMS.Models;
using OMS.Models.DTO;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PushServer.ModelServer
{
    /// <summary>
    /// 提供模型业务处理服务
    /// </summary>
     public class OrderEntityService
    {
        public static void InputConsigneeInfo(OrderEntity orderItem, OMSContext db)
        {

            var s = db.CustomersSet.Include<CustomerEntity, ICollection<AddressEntity>>(c => c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
            if (s != null)//通过姓名和手机号匹配是否是老用户
            {

                orderItem.Consignee = s;
            
                DateTime startSeasonTime, endSeasonTime, startYearTime, endYearTime, startWeekTime, endWeekTime;
                Util.Helpers.Time.GetTimeBySeason(orderItem.CreatedDate.Year, Util.Helpers.Time.GetSeasonNum(orderItem.CreatedDate), out startSeasonTime, out endSeasonTime);
                Util.Helpers.Time.GetTimeByYear(orderItem.CreatedDate.Year, out startYearTime, out endYearTime);
                Util.Helpers.Time.GetTimeByWeek(orderItem.CreatedDate.Year, Util.Helpers.Time.GetWeekNum(orderItem.CreatedDate), out startWeekTime, out endWeekTime);
                //复购
                orderItem.OrderRepurchase = new OrderRepurchase()
                {
                    DailyRepurchase = s.CreateDate.Value.Date < orderItem.CreatedDate.Date ? true : false,
                    MonthRepurchase = s.CreateDate.Value.Date < new DateTime(orderItem.CreatedDate.Year, orderItem.CreatedDate.Month, 1).Date ? true : false,
                    SeasonRepurchase = s.CreateDate.Value.Date < startSeasonTime.Date ? true : false,
                    WeekRepurchase = s.CreateDate.Value.Date < startWeekTime.Date ? true : false,
                    YearRepurchase = s.CreateDate.Value.Date < startYearTime.Date ? true : false,

                };
                //收获地址取MD5值进行比对，不同则新增到收货人地址列表中
                string addressstr = Util.Helpers.Encrypt.AesDecrypt(orderItem.ConsigneeAddress.Address);
                string md5 = Util.Helpers.Encrypt.Md5By32(addressstr);
                if (s.Addresslist.Any(a => a.MD5 == md5))
                {
                    var addr = s.Addresslist.First(a => a.MD5 == md5);
                    
                    addr.City = orderItem.ConsigneeAddress.City;
                    addr.County = orderItem.ConsigneeAddress.County;
                    addr.Province = orderItem.ConsigneeAddress.Province;

                    orderItem.ConsigneeAddress = addr;//替换地址对象
                    db.Entry<AddressEntity>(addr).State = EntityState.Modified;

                }
                else
                {
                    orderItem.ConsigneeAddress.MD5 = md5;
                    s.Addresslist.Add(orderItem.ConsigneeAddress);
                }
            }
            else//新用户
            {
                orderItem.OrderRepurchase = new OrderRepurchase();
                string addressstr = Util.Helpers.Encrypt.AesDecrypt(orderItem.ConsigneeAddress.Address);

                string md5 = Util.Helpers.Encrypt.Md5By32(addressstr);
                orderItem.ConsigneeAddress.MD5 = md5;
                if (orderItem.Consignee.Addresslist == null)
                    orderItem.Consignee.Addresslist = new List<AddressEntity>();

                
              
                orderItem.Consignee.Addresslist.Add(orderItem.ConsigneeAddress);
                db.AddressSet.Add(orderItem.ConsigneeAddress);
                db.CustomersSet.Add(orderItem.Consignee);
            }
        }
        public static void InputBusinessConsigneeInfo(OrderEntity orderItem, OMSContext db)
        {
            var s = db.CustomersSet.Include<CustomerEntity, ICollection<AddressEntity>>(c => c.Addresslist).FirstOrDefault(c => c.Name == orderItem.Consignee.Name && c.Phone == orderItem.Consignee.Phone);
            var b = db.OrderExtendInfoSet.Include(o=>o.Buyer).Include(o=>o.Supplier).FirstOrDefault(c => c.Buyer!=null&&c.Buyer.Name == orderItem.OrderExtendInfo.Buyer.Name);
            if(s!=null)
            {
                orderItem.Consignee = s;
                string addressstr = Util.Helpers.Encrypt.AesDecrypt(orderItem.ConsigneeAddress.Address);
                string md5 = Util.Helpers.Encrypt.Md5By32(addressstr);
               
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
            else
            {
                string addressstr = Util.Helpers.Encrypt.AesDecrypt(orderItem.ConsigneeAddress.Address);
                string md5 = Util.Helpers.Encrypt.Md5By32(addressstr);
                orderItem.ConsigneeAddress.MD5 = md5;
                if (orderItem.Consignee.Addresslist == null)
                    orderItem.Consignee.Addresslist = new List<AddressEntity>();
                orderItem.Consignee.Addresslist.Add(orderItem.ConsigneeAddress);
                db.AddressSet.Add(orderItem.ConsigneeAddress);
                db.CustomersSet.Add(orderItem.Consignee);
            }
            if (b != null)//通过姓名和手机号匹配是否是老用户
            {

               
                DateTime startSeasonTime, endSeasonTime, startYearTime, endYearTime, startWeekTime, endWeekTime;
                Util.Helpers.Time.GetTimeBySeason(orderItem.CreatedDate.Year, Util.Helpers.Time.GetSeasonNum(orderItem.CreatedDate), out startSeasonTime, out endSeasonTime);
                Util.Helpers.Time.GetTimeByYear(orderItem.CreatedDate.Year, out startYearTime, out endYearTime);
                Util.Helpers.Time.GetTimeByWeek(orderItem.CreatedDate.Year, Util.Helpers.Time.GetWeekNum(orderItem.CreatedDate), out startWeekTime, out endWeekTime);
                //复购
                orderItem.OrderRepurchase = new OrderRepurchase()
                {
                    DailyRepurchase = true,
                    MonthRepurchase = s.CreateDate.Value.Date < new DateTime(orderItem.CreatedDate.Year, orderItem.CreatedDate.Month, 1).Date ? true : false,
                    SeasonRepurchase = s.CreateDate.Value.Date < startSeasonTime.Date ? true : false,
                    WeekRepurchase = s.CreateDate.Value.Date < startWeekTime.Date ? true : false,
                    YearRepurchase = s.CreateDate.Value.Date < startYearTime.Date ? true : false,

                };
                //收获地址取MD5值进行比对，不同则新增到收货人地址列表中
              
                string addressstr = Util.Helpers.Encrypt.AesDecrypt(orderItem.ConsigneeAddress.Address);
                string md5 = Util.Helpers.Encrypt.Md5By32(addressstr);
                if (b.Buyer.Addresslist.Any(a => a.MD5 == md5))
                {
                    var addr = b.Buyer.Addresslist.First(a => a.MD5 == md5);
                    orderItem.ConsigneeAddress = addr;//替换地址对象
                }
                else
                {
                    orderItem.ConsigneeAddress.MD5 = md5;
                    b.Buyer.Addresslist.Add(orderItem.ConsigneeAddress);
                }
            }
            else//新用户
            {
                orderItem.OrderRepurchase = new OrderRepurchase();
                string addressstr = Util.Helpers.Encrypt.AesDecrypt(orderItem.ConsigneeAddress.Address);

                string md5 = Util.Helpers.Encrypt.Md5By32(addressstr);
                orderItem.ConsigneeAddress.MD5 = md5;
             

                orderItem.OrderExtendInfo.Buyer.Addresslist.Add(orderItem.ConsigneeAddress);
                db.AddressSet.Add(orderItem.ConsigneeAddress);
                db.CustomersSet.Add(orderItem.Consignee);

                
               
            }
        }
        public static OrderEntity CreateOrderEntity(OrderDTO orderDTO)
        {
            if (string.IsNullOrEmpty(orderDTO.consigneeCounty))
                orderDTO.consigneeCounty = "其它区";
            var orderItem = new OrderEntity()
            {
                SourceSn = orderDTO.sourceSN,
                OrderSn = orderDTO.orderSN,
                Source = orderDTO.source,
                SourceDesc = orderDTO.sourceDesc,
                CreatedDate = orderDTO.createdDate,
                Consignee = new CustomerEntity()
                {
                    Name = Util.Helpers.Encrypt.AesEncrypt(orderDTO.consigneeName),
                    Phone = Util.Helpers.Encrypt.AesEncrypt(orderDTO.consigneePhone),
                    Phone2 = Util.Helpers.Encrypt.AesEncrypt(orderDTO.consigneePhone2),
                    CreateDate = orderDTO.createdDate
                },
                ConsigneeAddress = new AddressEntity()
                {
                    Address = Util.Helpers.Encrypt.AesEncrypt(orderDTO.consigneeAddress.Trim().Replace(" ", "")),//加密
                    City = orderDTO.consigneeCity,
                    Province = orderDTO.consigneeProvince,
                    County = orderDTO.consigneeCounty,
                    ZipCode = orderDTO.consigneeZipCode

                },
                OrderDateInfo = new OrderDateInfo()
                {
                    CreateTime = orderDTO.createdDate,
                    DayNum = orderDTO.createdDate.DayOfYear,
                    MonthNum = orderDTO.createdDate.Month,
                    WeekNum = Util.Helpers.Time.GetWeekNum(orderDTO.createdDate),
                    SeasonNum = Util.Helpers.Time.GetSeasonNum(orderDTO.createdDate),
                    Year = orderDTO.createdDate.Year,
                    TimeStamp = Util.Helpers.Time.GetUnixTimestamp(orderDTO.createdDate)
                },
               
                OrderType = orderDTO.orderType,
                OrderComeFrom = orderDTO.OrderComeFrom,
                OrderStatus = (int)orderDTO.orderStatus,
                OrderStatusDesc = Util.Helpers.Enum.GetDescription(typeof(OrderStatus), orderDTO.orderStatus),


                Remarks = string.Empty
            };
           
            if (orderItem.Products == null)
                orderItem.Products = new List<OrderProductInfo>();
            return orderItem;
        }
    }
}
