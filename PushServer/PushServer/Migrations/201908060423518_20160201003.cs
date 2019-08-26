namespace PushServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class _20160201003 : DbMigration
    {
        public override void Up()
        {
           
            
          
            
            CreateTable(
                "dbo.CustomStrategies",
                c => new
                    {
                        ID = c.Long(nullable: false, identity: true),
                        StrategyValue = c.Int(nullable: false),
                        Customer_CustomerId = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.CustomerInfo", t => t.Customer_CustomerId)
                .Index(t => t.Customer_CustomerId);
            
           
            
         
           
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.StatisticDistrictItems", "StatisticDistrict_StatisticDistrictID1", "dbo.StatisticDistricts");
            DropForeignKey("dbo.StatisticDistrictItems", "StatisticDistrict_StatisticDistrictID", "dbo.StatisticDistricts");
            DropForeignKey("dbo.StatisticDistrictItems", "AddressID_ID", "dbo.ChinaAreaDatas");
            DropForeignKey("dbo.ProductInfos", "weightModel_Code", "dbo.WeightCodes");
            DropForeignKey("dbo.ProductInfos", "Address_Id", "dbo.AddressInfo");
            DropForeignKey("dbo.OrderProductInfoes", "OrderSn", "dbo.OrderInfo");
            DropForeignKey("dbo.OrderInfo", "OrderRepurchase_ID", "dbo.OrderRepurchases");
            DropForeignKey("dbo.OrderLogisticsDetails", "OrderSn", "dbo.OrderInfo");
            DropForeignKey("dbo.OrderInfo", "OrderExtendInfo_Id", "dbo.OrderExtendInfoes");
            DropForeignKey("dbo.OrderInfo", "OrderDateInfo_ID", "dbo.OrderDateInfoes");
            DropForeignKey("dbo.OrderInfo", "Customer_CustomerId", "dbo.CustomerInfo");
            DropForeignKey("dbo.OrderInfo", "ConsigneeAddress_Id", "dbo.AddressInfo");
            DropForeignKey("dbo.OrderInfo", "Consignee_CustomerId", "dbo.CustomerInfo");
            DropForeignKey("dbo.OrderPandianProductInfoes", "OrderPandianWithMonth_SourceSn", "dbo.OrderPandianWithMonths");
            DropForeignKey("dbo.CustomStrategies", "Customer_CustomerId", "dbo.CustomerInfo");
            DropForeignKey("dbo.AddressInfo", "CustomerEntity_CustomerId", "dbo.CustomerInfo");
            DropIndex("dbo.StatisticDistrictItems", new[] { "StatisticDistrict_StatisticDistrictID1" });
            DropIndex("dbo.StatisticDistrictItems", new[] { "StatisticDistrict_StatisticDistrictID" });
            DropIndex("dbo.StatisticDistrictItems", new[] { "AddressID_ID" });
            DropIndex("dbo.ProductInfos", new[] { "weightModel_Code" });
            DropIndex("dbo.ProductInfos", new[] { "Address_Id" });
            DropIndex("dbo.OrderInfo", new[] { "OrderRepurchase_ID" });
            DropIndex("dbo.OrderInfo", new[] { "OrderExtendInfo_Id" });
            DropIndex("dbo.OrderInfo", new[] { "OrderDateInfo_ID" });
            DropIndex("dbo.OrderInfo", new[] { "Customer_CustomerId" });
            DropIndex("dbo.OrderInfo", new[] { "ConsigneeAddress_Id" });
            DropIndex("dbo.OrderInfo", new[] { "Consignee_CustomerId" });
            DropIndex("dbo.OrderProductInfoes", new[] { "OrderSn" });
            DropIndex("dbo.OrderPandianProductInfoes", new[] { "OrderPandianWithMonth_SourceSn" });
            DropIndex("dbo.OrderLogisticsDetails", new[] { "OrderSn" });
            DropIndex("dbo.CustomStrategies", new[] { "Customer_CustomerId" });
            DropIndex("dbo.AddressInfo", new[] { "CustomerEntity_CustomerId" });
            DropTable("dbo.Statistics");
            DropTable("dbo.StatisticProducts");
            DropTable("dbo.StatisticDistricts");
            DropTable("dbo.StatisticDistrictItems");
            DropTable("dbo.WeightCodes");
            DropTable("dbo.ProductInfos");
            DropTable("dbo.ProductDictionaries");
            DropTable("dbo.OrderInfo");
            DropTable("dbo.OrderRepurchases");
            DropTable("dbo.OrderProductInfoes");
            DropTable("dbo.OrderPandianWithMonths");
            DropTable("dbo.OrderPandianProductInfoes");
            DropTable("dbo.OrderLogisticsDetails");
            DropTable("dbo.OrderExtendInfoes");
            DropTable("dbo.OrderDateInfoes");
            DropTable("dbo.LogisticsInfoes");
            DropTable("dbo.ExceptionOrders");
            DropTable("dbo.CustomStrategies");
            DropTable("dbo.CustomerInfo");
            DropTable("dbo.ChinaAreaDatas");
            DropTable("dbo.AddressInfo");
        }
    }
}
