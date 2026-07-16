using System.Collections.Generic;
using System.Text;
using System.IO;

namespace ExcelMerge
{
    public class TsvReader
    {
        internal static IEnumerable<ExcelRow> Read(string path)
        {
            using (var sr = new StreamReader(path, Encoding.UTF8))
            {
                var rowIndex = 0;
                while (!sr.EndOfStream)
                {
                    var cells = new List<ExcelCell>();
                    var values = sr.ReadLine().Split('\t');
                    for (int columnIndex = 0; columnIndex < values.Length; columnIndex++)
                        cells.Add(new ExcelCell(values[columnIndex], columnIndex, rowIndex));

                    yield return new ExcelRow(rowIndex++, cells);
                }
            }
        }
    }
}
