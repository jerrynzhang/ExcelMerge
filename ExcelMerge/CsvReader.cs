using System.IO;
using System.Collections.Generic;
using System.Text;

namespace ExcelMerge
{
    internal class CsvReader
    {
        internal static IEnumerable<ExcelRow> Read(string path)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                var rowIndex = 0;
                while (!sr.EndOfStream)
                {
                    var cells = new List<ExcelCell>();
                    var values = sr.ReadLine().Split(',');
                    for (int columnIndex = 0; columnIndex < values.Length; columnIndex++)
                        cells.Add(new ExcelCell(values[columnIndex], columnIndex, rowIndex));

                    yield return new ExcelRow(rowIndex++, cells);
                }
            }
        }
    }
}
