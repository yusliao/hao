using PushServer.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace OMS.Models
{
    public class DistrictServiceContext : DbContext
    {
        public DistrictServiceContext():base("name=papa")
        {
             Database.SetInitializer<DistrictServiceContext>(null);
            //  Database.SetInitializer<OMSContext>(new CreateDatabaseIfNotExists<OMSContext>());
           //   Database.SetInitializer<OMSContext>(new DropCreateDatabaseIfModelChanges<OMSContext>());
           // Database.SetInitializer(new MigrateDatabaseToLatestVersion<DistrictServiceContext, PushServer.Migrations.Configuration>());
        }
        public IDbSet<ChinaAreaData>  ChinaAreaDatas { get; set; }
    }
}