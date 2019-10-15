namespace PushServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class price1 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.OrderExtendInfoes", "TotalFlatAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
            AddColumn("dbo.OrderProductInfoes", "TotalFlatAmount", c => c.Decimal(nullable: false, precision: 18, scale: 2));
        }
        
        public override void Down()
        {
            DropColumn("dbo.OrderProductInfoes", "TotalFlatAmount");
            DropColumn("dbo.OrderExtendInfoes", "TotalFlatAmount");
        }
    }
}
