namespace PushServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class price2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Statistics", "TotalCostAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.Statistics", "TotalFlatAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Statistics", "TotalFlatAmount");
            DropColumn("dbo.Statistics", "TotalCostAmount");
        }
    }
}
