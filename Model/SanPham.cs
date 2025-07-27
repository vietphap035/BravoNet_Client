using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BravoNet_Client.Model
{
    public class SanPham
    {
        public int P_Id { get; set; }
        public string P_Name { get; set; }
        public decimal P_Price { get; set; }
        public int P_Quantity { get; set; }
    }
}
