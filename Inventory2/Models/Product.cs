using System;
using System.Collections.Generic;
using System.Text;

namespace Inventory2.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string ProductId { get; set; }
        public string EAN { get; set; }
        public string SKU { get; set; }
        public string Description { get; set; }
        public string Warehouse { get; set; }
        public string Address { get; set; }
    }
}
