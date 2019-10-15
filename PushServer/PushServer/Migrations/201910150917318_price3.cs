namespace PushServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class price3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.StatisticDistrictItems", "TotalCostAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.StatisticDistrictItems", "TotalFlatAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.StatisticDistrictItems", "TotalFlatAmount");
            DropColumn("dbo.StatisticDistrictItems", "TotalCostAmount");
        }
    }
}
