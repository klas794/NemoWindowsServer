using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NemoWindowsServerXaml2
{
    class Order
    {
        public Order()
        {
            _orderTime = DateTime.Now;
        }

        public int OrderNumber { get; set; }
        public string DishName { get; set; }
        public bool ReadyStatus { get; set; }
        private DateTime _orderTime;
        public string OrderTime {
            get {
                return _orderTime.ToShortTimeString();
            }
        }
    }
}
