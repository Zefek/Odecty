using Microsoft.Office.Interop.Excel;
using OdectyStat.Entities;
using OdectyStat1.Contracts;
using System.Data;
using System.Data.OleDb;
using System.Runtime.InteropServices;

namespace OdectyStat1.DataLayer
{
    public class ExcelProvider : IExcelProvider
    {
        public void UpdateExcel()
        {
            var excelApp = new Microsoft.Office.Interop.Excel.Application();
           var workBook = excelApp.Workbooks.Open("");
            workBook.RefreshAll();
            foreach (Worksheet pivotSheet in workBook.Worksheets)
            {
                int c = 0;
                do
                {
                    try
                    {
                        var pivotTables = (PivotTables)pivotSheet.PivotTables();
                        for (int i = 1; i <= pivotTables.Count; i++)
                        {
                            pivotTables.Item(i).RefreshTable();
                        }
                        break;
                    }
                    catch (COMException ex)
                    {
                        var error = ex.ErrorCode;
                        Thread.Sleep(5000);
                        c++;
                    }
                }
                while (c <= 5);
            }
            workBook.Save();
            workBook.Close();
            excelApp.Quit();
        }
    }
}
