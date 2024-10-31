using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory2.Models
{
    internal class ImportSettings
    {
        public int Id { get; set; }
        public int ProductID { get; set; }
        public int EAN { get; set; }
        public int SKU { get; set; }
        public int Description { get; set; }
        public int Warehouse { get; set; }
        public int Address { get; set; }
    }
}
