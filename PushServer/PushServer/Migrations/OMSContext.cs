
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace OMS.Models
{
    public class OMSContext: DbContext
    {
        public OMSContext():base("name=papa")
        {
             Database.SetInitializer<OMSContext>(null);
            //  Database.SetInitializer<OMSContext>(new CreateDatabaseIfNotExists<OMSContext>());
           //   Database.SetInitializer<OMSContext>(new DropCreateDatabaseIfModelChanges<OMSContext>());
           // Database.SetInitializer(new MigrateDatabaseToLatestVersion<OMSContext, PushServer.Migrations.Configuration>());
        }
        public IDbSet<AddressEntity> AddressSet { get; set; }
        public IDbSet<ProductEntity> ProductsSet { get; set; }
        public IDbSet<CustomerEntity> CustomersSet { get; set; }
        public IDbSet<OrderEntity> OrderSet { get; set; }
        public IDbSet<OrderRepurchase> OrderRepurchases { get; set; }
        public IDbSet<OrderDateInfo> OrderDateInfos { get; set; }

        public IDbSet<Statistic> StatisticSet { get; set; }
        public IDbSet<OrderProductInfo> OrderProductSet { get; set; }
        public IDbSet<OrderExtendInfo> OrderExtendInfoSet { get; set; }
        public IDbSet<ProductDictionary> ProductDictionarySet { get; set; }
        public IDbSet<WeightCode> WeightCodeSet { get; set; }
        public IDbSet<LogisticsInfo> logisticsInfoSet { get; set; }
        public IDbSet<OrderLogisticsDetail> OrderLogisticsDetailSet { get; set; }
        public IDbSet<OrderPandianWithMonth> OrderPandianWithMonthsSet { get; set; }
        public IDbSet<OrderPandianProductInfo> OrderPandianProductInfoSet { get; set; }
        public IDbSet<StatisticProduct> StatisticProductSet { get; set; }
        public IDbSet<StatisticDistrictItem> StatisticDistrictItems { get; set; }
        public IDbSet<StatisticDistrict> StatisticDistricts { get; set; }
        public IDbSet<ExceptionOrder> ExceptionOrders { get; set; }
        public IDbSet<ChinaAreaData> ChinaAreaDatas  { get; set; }
        public IDbSet<CustomStrategy> CustomStrategies { get; set; }
        public IDbSet<LogisticsProductInfo> LogisticsProductInfos { get; set; }
        public IDbSet<OrderOptionRecord> OrderOptionRecords { get; set; }
        public IDbSet<BusinessBuyer> BusinessBuyers { get; set; }
        public IDbSet<BusinessSupplier> BusinessSuppliers { get; set; }

    }
}