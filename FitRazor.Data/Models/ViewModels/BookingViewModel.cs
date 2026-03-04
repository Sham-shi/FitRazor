using System;
using System.Collections.Generic;
using System.Text;

namespace FitRazor.Data.Models.ViewModels
{
    public class BookingViewModel
    {
        public int BookingID { get; set; }
        public string ClientName { get; set; }
        public string TrainerName { get; set; }
        public string ServiceName { get; set; }
        public DateTime BookingDateTime { get; set; }
        public int SessionsCount { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
