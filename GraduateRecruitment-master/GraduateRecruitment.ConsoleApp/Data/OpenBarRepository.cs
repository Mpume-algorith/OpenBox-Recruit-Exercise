using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using GraduateRecruitment.ConsoleApp.Data.Entities;
using GraduateRecruitment.ConsoleApp.ViewModels;
using GraduateRecruitment.ConsoleApp.Data.Models;
using LumenWorks.Framework.IO.Csv;

namespace GraduateRecruitment.ConsoleApp.Data
{
    internal class OpenBarRepository
    {
        private readonly IList<OpenBarRecordDto> _openBarRecordsDto;
        private readonly IList<FridgeStockTakeDto> _fridgeStockDto;
        private readonly IList<InventoryDto> _inventoryDto;

        public IList<OpenBarRecord> AllOpenBarRecords { get; private set; } = new List<OpenBarRecord>();
        public IList<FridgeStockTake> AllFridgeStocks { get; private set; } = new List<FridgeStockTake>();
        public IList<Inventory> AllInventory { get; private set; } = new List<Inventory>();

        #region Solutions 
        public List<QuantityByInventory> QuantByInventoryWed()
        {
            List<QuantityByInventory> quantByInv = new List<QuantityByInventory>();

            var StockTakeJoinOpenBar = (from item1 in _fridgeStockDto
                                        join item2 in _openBarRecordsDto on
                                        item1.OpenBarRecordId equals item2.Id
                                        select new
                                        {
                                            Id = item1.InventoryId,
                                            Quantity = item1.Quantity.Taken,
                                            Day = item2.Date.DayOfWeek
                                        });
            var StockByWednesday = (from item in StockTakeJoinOpenBar
                                    where item.Day.Equals(DayOfWeek.Wednesday)
                                    select item);
            var StockByWedJoinInventory = (from item1 in _inventoryDto
                                           join item2 in StockByWednesday
                                           on item1.Id equals item2.Id
                                           group item2 by item1.Name into g
                                           orderby g.Sum(x => x.Quantity) descending
                                           select new QuantityByInventory
                                           {
                                               InventoryName = g.Key,
                                               QuantityTaken = g.Sum(x => x.Quantity)
                                           }).Take(1).ToList();

            foreach (var item in StockByWedJoinInventory)
            {
                QuantityByInventory obj = new QuantityByInventory();
                obj.InventoryName = item.InventoryName;
                obj.QuantityTaken = item.QuantityTaken;
                quantByInv.Add(obj);
            }

            return quantByInv;
        }

        public List<QuantityByInventory> QuantityByInventoryByDay()
        {
            List<QuantityByInventory> inventoryByDay = new List<QuantityByInventory>();
            var StockTakeJoinOpenBar = (from item1 in _fridgeStockDto
                                        join item2 in _openBarRecordsDto on
                                        item1.OpenBarRecordId equals item2.Id
                                        select new
                                        {
                                            Id = item1.InventoryId,
                                            Quantity = item1.Quantity.Taken,
                                            Day = item2.Date.DayOfWeek
                                        });
            var StockNameByDay = (from item1 in _inventoryDto
                                  join item2 in StockTakeJoinOpenBar
                                  on item1.Id equals item2.Id
                                  group item2 by new { item2.Day, item1.Name } into g
                                  //orderby g.Sum(x => x.Quantity) descending
                                  select new QuantityByInventory
                                  {
                                      InventoryName = g.Key.Name,
                                      DayOfWeek = g.Key.Day,
                                      QuantityTaken = g.Sum(x => x.Quantity)

                                  }).ToList();
            var StockMaxByDay = (from item in StockNameByDay
                                 group item by item.DayOfWeek into g
                                 let maxSum = g.Max(x => x.QuantityTaken)
                                 select new QuantityByInventory
                                 {
                                     InventoryName = g.First(y => y.QuantityTaken == maxSum).InventoryName,
                                     DayOfWeek = g.Key,
                                     QuantityTaken = maxSum
                                 }).ToList();
            foreach (var item in StockMaxByDay)
            {
                QuantityByInventory obj = new QuantityByInventory();
                obj.InventoryName = item.InventoryName;
                obj.DayOfWeek = item.DayOfWeek;
                obj.QuantityTaken = item.QuantityTaken;
                inventoryByDay.Add(obj);
            }
            return inventoryByDay;
        }

        public List<InventoryRunningOut> SavannaRunOutDate()
        {
            List<InventoryRunningOut> date = new List<InventoryRunningOut>();
            var savanna = (from item in _fridgeStockDto
                           where item.InventoryId == 1
                           select new InventoryRunningOut
                           {
                               OpenBarID = item.OpenBarRecordId,
                               Id = item.InventoryId,
                               QuantityAdded = item.Quantity.Added,
                               QuantityTaken = item.Quantity.Taken,

                           });
            //here we select the OpenBarId where the sum taken is equal to sum added 
            int sumTaken = 0;
            int sumAdded = 0;

            IList<InventoryRunningOut> OpenBarIdList = new List<InventoryRunningOut>();
            foreach (var item in savanna)

            {
                sumTaken = sumTaken + item.QuantityTaken;
                sumAdded = sumAdded + item.QuantityAdded;

                if (sumTaken == sumAdded)
                {
                    InventoryRunningOut obj = new InventoryRunningOut();
                    obj.OpenBarID = item.OpenBarID;
                    OpenBarIdList.Add(obj);
                }

            }

            var OpenBarDate = (from item1 in _openBarRecordsDto
                               join item2 in OpenBarIdList
                               on item1.Id equals item2.OpenBarID
                               let lastMonth = DateTime.Parse("2022/03/31")
                               where item1.Date > lastMonth
                               select new InventoryRunningOut
                               {
                                   OpenBarID = item1.Id,
                                   DateString = item1.Date.ToString("yyyy/MM/dd")
                               }).ToList();


            foreach (var item in OpenBarDate)
            {
                InventoryRunningOut obj = new InventoryRunningOut();
                obj.OpenBarID = item.OpenBarID;
                obj.DateString = item.DateString;

                date.Add(obj);
            }

            return date;
        }

        public List<InventoryRunningOut> FantaOrangeOrder()
        {
            List<InventoryRunningOut> fantaList = new List<InventoryRunningOut>();

            var fantaFilter = (from item1 in _fridgeStockDto
                               join item2 in _openBarRecordsDto
                               on item1.OpenBarRecordId equals item2.Id
                               where item1.InventoryId == 7 && item2.Date > DateTime.Parse("2021/12/31")

                               select new InventoryRunningOut
                               {
                                   DateString = item2.Date.ToString("yyyy/MMMM/dd"),
                                   Id = item1.InventoryId,
                                   QuantityTaken = item1.Quantity.Taken,
                                   QuantityAdded = item1.Quantity.Added,
                                   Day = item2.Date.DayOfWeek

                               }).ToList();
            foreach (var item in fantaFilter)
            {
                InventoryRunningOut obj = new InventoryRunningOut();
                obj.DateString = item.DateString;
                //obj.Id = item.Id;
                obj.QuantityTaken = item.QuantityTaken;
                obj.QuantityAdded = item.QuantityAdded;
                obj.Day = item.Day;
                fantaList.Add(obj);
            }

            return fantaList;
        }
        public double InventoryUageRate()
        {
            /* with this method I wnt to determine the inventory usage, 
             * devide according to the time frame and use it to 
             * predict the order schedule for any timeframe
             */

            var fantaFilter = (from item1 in _fridgeStockDto
                               join item2 in _openBarRecordsDto
                               on item1.OpenBarRecordId equals item2.Id
                               where item1.InventoryId == 7

                               select new InventoryRunningOut
                               {
                                   Date = item2.Date,
                                   Id = item1.InventoryId,
                                   QuantityTaken = item1.Quantity.Taken,
                                   QuantityAdded = item1.Quantity.Added,


                               }).ToList();


            //int sumAdded = 0;
            int sumTaken = 0;
            //int openingInventory = 0;
            foreach (var item in fantaFilter)
            {
                //sumAdded = sumAdded + item.QuantityAdded;
                sumTaken = sumTaken + item.QuantityTaken;
            }
            //openingInventory = sumAdded - sumTaken;
            //int receivedInventory = fantaFilter.Select(x => x.QuantityAdded).Sum();
            int usedInventory = fantaFilter.Select(x => x.QuantityTaken).Sum();
            //usage rate for Fanta Orange per month in 2022
            int usageRate = usedInventory / 62;





            return usageRate;

        }

        public IList<InventoryRunningOut> SumByDrinkNamePerMonth(int x)
        {
            IList<InventoryRunningOut> list = new List<InventoryRunningOut>();
            var query = (from item1 in _fridgeStockDto
                         join item2 in _openBarRecordsDto
                         on item1.OpenBarRecordId equals item2.Id
                         where item1.InventoryId == x
                         select new InventoryRunningOut
                         {
                             Id = item1.InventoryId,
                             Date = item2.Date,
                             QuantityTaken = item1.Quantity.Taken
                         });
            var drinkSumByMonth = (from item1 in query
                                   join item2 in _inventoryDto
                                   on item1.Id equals item2.Id
                                   let year = item1.Date.ToString("yyyy")
                                   let month = item1.Date.ToString("MMMM")
                                   group item1 by new { year, month } into g
                                   select new InventoryRunningOut
                                   {
                                       DateString = g.Key.year,
                                       DateStringMonth = g.Key.month,
                                       QuantityTaken = g.Sum(x => x.QuantityTaken)
                                   }).ToList();
            foreach (var item in drinkSumByMonth)
            {
                InventoryRunningOut obj = new InventoryRunningOut();
                obj.DateString = item.DateString;
                obj.DateStringMonth = item.DateStringMonth;
                obj.QuantityTaken = item.QuantityTaken;
                list.Add(obj);
            }
            return list;

        }
        public decimal CeresOrangeBudget()
        {
            double consumptionAverage = SumByDrinkNamePerMonth(9).Average(x => x.QuantityTaken);
            decimal priceOfJuice = GetInventory(9).Price;
            decimal monthlyBudget = Convert.ToDecimal(consumptionAverage) * priceOfJuice;

            return monthlyBudget;
        }
        public decimal MonthlyRestockBudget()
        {
            decimal consumptionAverage1 = Convert.ToDecimal(SumByDrinkNamePerMonth(1).Average(x => x.QuantityTaken));

            decimal consumptionAverage2 = Convert.ToDecimal(SumByDrinkNamePerMonth(2).Average(x => x.QuantityTaken));
            decimal consumptionAverage3 = Convert.ToDecimal(SumByDrinkNamePerMonth(3).Average(x => x.QuantityTaken));
            decimal consumptionAverage4 = Convert.ToDecimal(SumByDrinkNamePerMonth(4).Average(x => x.QuantityTaken));
            decimal consumptionAverage5 = Convert.ToDecimal(SumByDrinkNamePerMonth(5).Average(x => x.QuantityTaken));
            decimal consumptionAverage6 = Convert.ToDecimal(SumByDrinkNamePerMonth(6).Average(x => x.QuantityTaken));
            decimal consumptionAverage7 = Convert.ToDecimal(SumByDrinkNamePerMonth(7).Average(x => x.QuantityTaken));
            decimal consumptionAverage8 = Convert.ToDecimal(SumByDrinkNamePerMonth(8).Average(x => x.QuantityTaken));
            decimal consumptionAverage9 = Convert.ToDecimal(SumByDrinkNamePerMonth(9).Average(x => x.QuantityTaken));
            decimal consumptionAverage10 = Convert.ToDecimal(SumByDrinkNamePerMonth(10).Average(x => x.QuantityTaken));
            decimal price1 = GetInventory(1).Price;
            decimal price2 = GetInventory(2).Price;
            decimal price3 = GetInventory(3).Price;
            decimal price4 = GetInventory(4).Price;
            decimal price5 = GetInventory(5).Price;
            decimal price6 = GetInventory(6).Price;
            decimal price7 = GetInventory(7).Price;
            decimal price8 = GetInventory(8).Price;
            decimal price9 = GetInventory(9).Price;
            decimal price10 = GetInventory(10).Price;

            decimal averageMonthlyBudget = (consumptionAverage1 * price1) + (consumptionAverage2 * price2) + (consumptionAverage3 * price3) + (consumptionAverage4 * price4) + (consumptionAverage5 * price5) + (consumptionAverage6 * price6) + (consumptionAverage7 * price7) + (consumptionAverage8 * price8) + (consumptionAverage9 * price9) + (consumptionAverage10 * price10);


            return averageMonthlyBudget;
        }

        #endregion
        public OpenBarRepository()
        {
            _openBarRecordsDto = GetOpenBarRecordData();
            _fridgeStockDto = GetFridgeStockData();
            _inventoryDto = GetInventoryData();
            AllOpenBarRecords = GetOpenBarRecords();
            AllFridgeStocks = GetFridgeStockTakes();
            AllInventory = GetInventoryList();
        }

        private IList<OpenBarRecord> GetOpenBarRecords()
        {
            var openBarRecords = new List<OpenBarRecord>();
            foreach (var openBarRecord in _openBarRecordsDto)
            {
                openBarRecords.Add(new OpenBarRecord
                {
                    Id = openBarRecord.Id,
                    Date = openBarRecord.Date,
                    DayOfWeek = openBarRecord.Date.DayOfWeek,
                    NumberOfPeopleInBar = openBarRecord.NumberOfPeopleInBar,
                    FridgeStockTakeList = GetFridgeStockTakes(openBarRecord.Id)
                });
            }

            return openBarRecords;
        }

        private Inventory GetInventory(int id)
        {
            var inventoryDto = _inventoryDto.FirstOrDefault(i => i.Id == id);
            return new Inventory
            {
                Id = inventoryDto.Id,
                Name = inventoryDto.Name,
                Price = inventoryDto.Price,
            };
        }

        private IList<FridgeStockTake> GetFridgeStockTakes(int openBarRecordId)
        {
            var fridgeStockTakes = new List<FridgeStockTake>();
            foreach (var frideStockTake in _fridgeStockDto.Where(f => f.OpenBarRecordId == openBarRecordId))
            {
                fridgeStockTakes.Add(new FridgeStockTake
                {
                    Id = frideStockTake.Id,
                    Quantity = frideStockTake.Quantity,
                    Inventory = GetInventory(frideStockTake.InventoryId),
                });
            }

            return fridgeStockTakes;
        }

        private IList<FridgeStockTake> GetFridgeStockTakes()
        {
            var fridgeStockTakes = new List<FridgeStockTake>();
            foreach (var frideStockTake in _fridgeStockDto)
            {
                fridgeStockTakes.Add(new FridgeStockTake
                {
                    Id = frideStockTake.Id,
                    Quantity = frideStockTake.Quantity,
                    Inventory = GetInventory(frideStockTake.InventoryId),
                });
            }

            return fridgeStockTakes;
        }

        private IList<Inventory> GetInventoryList()
        {
            var inventoryList = new List<Inventory>();
            foreach (var inventoryDto in _inventoryDto)
            {
                inventoryList.Add(new Inventory
                {
                    Id = inventoryDto.Id,
                    Name = inventoryDto.Name,
                    Price = inventoryDto.Price,
                });
            }

            return inventoryList;
        }

        private IList<OpenBarRecordDto> GetOpenBarRecordData()
        {
            var csvTable = new DataTable();
            using (var csvReader = new CsvReader(new StreamReader(File.OpenRead(@"./Data/Files/OpenBarRecord.csv")), true))
            {
                csvTable.Load(csvReader);
            }

            var openBarRecords = new List<OpenBarRecordDto>();

            for (int i = 0; i < csvTable.Rows.Count; i++)
            {
                var date = DateTime.Parse(csvTable.Rows[i][1].ToString());
                openBarRecords.Add(new OpenBarRecordDto
                {
                    Id = int.Parse(csvTable.Rows[i][0].ToString()),
                    Date = date,
                    NumberOfPeopleInBar = int.Parse(csvTable.Rows[i][2].ToString()),
                });
            }

            return openBarRecords;
        }

        private IList<FridgeStockTakeDto> GetFridgeStockData()
        {
            var csvTable = new DataTable();
            using (var csvReader = new CsvReader(new StreamReader(File.OpenRead(@"./Data/Files/FridgeStockTake.csv")), true))
            {
                csvTable.Load(csvReader);
            }

            var fridgeStockList = new List<FridgeStockTakeDto>();

            for (int i = 0; i < csvTable.Rows.Count; i++)
            {
                fridgeStockList.Add(new FridgeStockTakeDto
                {
                    Id = int.Parse(csvTable.Rows[i][0].ToString()),
                    OpenBarRecordId = int.Parse(csvTable.Rows[i][1].ToString()),
                    InventoryId = int.Parse(csvTable.Rows[i][2].ToString()),
                    Quantity = new Quantity
                    {
                        Taken = int.Parse(csvTable.Rows[i][3].ToString()),
                        Added = int.Parse(csvTable.Rows[i][4].ToString()),
                    }
                });
            }

            return fridgeStockList;
        }

        private IList<InventoryDto> GetInventoryData()
        {
            var csvTable = new DataTable();
            using (var csvReader = new CsvReader(new StreamReader(File.OpenRead(@"./Data/Files/Inventory.csv")), true))
            {
                csvTable.Load(csvReader);
            }

            var inventoryList = new List<InventoryDto>();

            for (int i = 0; i < csvTable.Rows.Count; i++)
            {
                var style = NumberStyles.Number | NumberStyles.AllowCurrencySymbol;
                var provider = new CultureInfo("en-GB");

                inventoryList.Add(new InventoryDto
                {
                    Id = int.Parse(csvTable.Rows[i][0].ToString()),
                    Name = csvTable.Rows[i][1].ToString(),
                    Price = decimal.Parse(csvTable.Rows[i][2].ToString(), style, provider),
                });
            }

            return inventoryList;
        }
    }
}