using ExcelDataReader;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace MUFGCircleService
{
    public static class ExcelToDataSet
    {
        public static DataSet CreateDataTable(string FilePath)
        {
            DataSet dataSet = new DataSet();
            var extension = Path.GetExtension(FilePath).ToLower(); 
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IExcelDataReader reader = null;
                if (extension == ".xls")
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (extension == ".xlsx")
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else if (extension == ".csv")
                {
                    reader = ExcelReaderFactory.CreateCsvReader(stream);
                }

                if (reader == null)
                    return null;

                using (reader)
                {
                    dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = false,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });
                }

                var tablenames = GetTablenames(dataSet.Tables);
                return dataSet;
            }
        }

        private static IList<string> GetTablenames(DataTableCollection tables)
        {
            var tableList = new List<string>();
            foreach (var table in tables)
            {
                tableList.Add(table.ToString());
            }

            return tableList;
        }
    }
}
