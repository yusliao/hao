namespace PushServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class price : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderExtendInfoes", "TotalCostPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.OrderProductInfoes", "TotalCostPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.ProductDictionaries", "Source", c => c.String());
            AddColumn("dbo.ProductDictionaries", "PayPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.ProductDictionaries", "State", c => c.Int(nullable: false));
            AddColumn("dbo.ProductDictionaries", "CreateTime", c => c.DateTime(nullable: false));
            AddColumn("dbo.ProductInfos", "CostPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.ProductInfos", "FlatPrice", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.StatisticProducts", "ProductTotalCostAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.StatisticProducts", "ProductTotalFlatAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StatisticProducts", "ProductTotalFlatAmount");
            DropColumn("dbo.StatisticProducts", "ProductTotalCostAmount");
            DropColumn("dbo.ProductInfos", "FlatPrice");
            DropColumn("dbo.ProductInfos", "CostPrice");
            DropColumn("dbo.ProductDictionaries", "CreateTime");
            DropColumn("dbo.ProductDictionaries", "State");
            DropColumn("dbo.ProductDictionaries", "PayPrice");
            DropColumn("dbo.ProductDictionaries", "Source");
            DropColumn("dbo.OrderProductInfoes", "TotalCostPrice");
            DropColumn("dbo.OrderExtendInfoes", "TotalCostPrice");
        }
    }
}
