using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OMS.Models.DTO
{
    public class OrderImportSummary
    {
        public int IncomingOrders { get; set; }
        public int IncomingOrderItems { get; set; }
        public int IncomingCustomers { get; set; }
        public int IncomingProducts { get; set; }
        public int IncomingRecipients { get; set; }

        public int ExistingOrders { get; set; }
        public int LoadedExsistingOrders { get; set; }
        public int UpdatedExistingOrders { get; set; }

        public int NewOrders { get; set; }
        public int NewOrderItems { get; set; }
        public int NewCustomers { get; set; }
        public int NewProducts { get; set; }
        public int NewRecipients { get; set; }
        public int NewDeliveries { get; set; }

        //TODO:Time Analyzing

    }
}
