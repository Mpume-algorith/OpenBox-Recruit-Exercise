using System;
using System.Collections.Generic;
using System.Text;

namespace GraduateRecruitment.ConsoleApp.ViewModels
{
    internal class InventoryRunningOut
    {
        public int OpenBarID { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int QuantityAdded { get; set; }
        public int QuantityTaken { get; set; }
        public DateTime Date { get; set; }
        public DayOfWeek Day { get; set; }
        public Decimal Amount { get; set; }
        public string DateString { get; set; }
        public string DateStringMonth { get; set; }
    }
}
