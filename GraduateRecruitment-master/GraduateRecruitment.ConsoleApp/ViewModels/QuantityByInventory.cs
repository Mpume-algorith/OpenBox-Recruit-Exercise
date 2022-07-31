using System;
using System.Collections.Generic;
using System.Text;

namespace GraduateRecruitment.ConsoleApp.ViewModels
{
    internal class QuantityByInventory
    {
        public int InventoryId { get; set; }
        public string InventoryName { get; set; }
        public int QuantityTaken { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
    }
}
