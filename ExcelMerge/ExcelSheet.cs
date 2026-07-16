using System;
using System.Collections.Generic;
using System.Linq;
using NPOI.SS.UserModel;
using NetDiff;
using SKCore.Collection;

namespace ExcelMerge
{
    public class ExcelSheet
    {
        public SortedDictionary<int, ExcelRow> Rows { get; private set; }

        public ExcelSheet()
        {
            Rows = new SortedDictionary<int, ExcelRow>();
        }

        public static ExcelSheet Create(ISheet srcSheet, ExcelSheetReadConfig config)
        {
            var rows = ExcelReader.Read(srcSheet);

            return CreateSheet(rows, config);
        }

        public static ExcelSheet CreateFromCsv(string path, ExcelSheetReadConfig config)
        {
            var rows = CsvReader.Read(path);

            return CreateSheet(rows, config);
        }

        public static ExcelSheet CreateFromTsv(string path, ExcelSheetReadConfig config)
        {
            var rows = TsvReader.Read(path);

            return CreateSheet(rows, config);
        }

        private static ExcelSheet CreateSheet(IEnumerable<ExcelRow> rows, ExcelSheetReadConfig config)
        {
            var sheet = CreateSheet(rows);

            if (config.TrimFirstBlankRows)
                sheet.TrimFirstBlankRows();

            if (config.TrimFirstBlankColumns)
                sheet.TrimFirstBlankColumns();

            if (config.TrimLastBlankRows)
                sheet.TrimLastBlankRows();

            if (config.TrimLastBlankColumns)
                sheet.TrimLastBlankColumns();

            return sheet;
        }

        public void TrimFirstBlankRows()
        {
            var rows = new SortedDictionary<int, ExcelRow>();
            var index = 0;
            foreach (var row in Rows.SkipWhile(r => r.Value.IsBlank()))
            {
                rows.Add(index, new ExcelRow(index, row.Value.Cells));
                index++;
            }

            Rows = rows;
        }

        public void TrimFirstBlankColumns()
        {
            var columns = CreateColumns();
            var indices = columns.Select((v, i) => new { v, i }).TakeWhile(c => c.v.IsBlank()).Select(c => c.i).ToList();

            if (indices.Count > 0)
                RemoveColumns(indices);
        }

        public void TrimLastBlankRows()
        {
            var rows = new SortedDictionary<int, ExcelRow>();
            var index = 0;
            foreach (var row in Rows.Reverse().SkipWhile(r => r.Value.IsBlank()).Reverse())
            {
                rows.Add(index, new ExcelRow(index, row.Value.Cells));
                index++;
            }

            Rows = rows;
        }

        public void TrimLastBlankColumns()
        {
            var columns = CreateColumns();
            var indices = columns.Select((v, i) => new { v, i }).Reverse().TakeWhile(c => c.v.IsBlank()).Select(c => c.i).ToList();

            if (indices.Count > 0)
                RemoveColumns(indices);
        }

        public void RemoveColumn(int column)
        {
            foreach (var row in Rows)
            {
                if (row.Value.Cells.Count > column)
                    row.Value.Cells.RemoveAt(column);
            }
        }

        public void RemoveColumns(IEnumerable<int> columns)
        {
            var sortedColumns = new SortedSet<int>(columns);

            foreach (var row in Rows)
            {
                var cells = row.Value.Cells;
                if (cells.Count == 0)
                    continue;

                // Remove from the end to avoid index shifting issues
                foreach (var col in sortedColumns.Reverse())
                {
                    if (col < cells.Count)
                        cells.RemoveAt(col);
                }
            }
        }

        private static ExcelSheet CreateSheet(IEnumerable<ExcelRow> rows)
        {
            var sheet = new ExcelSheet();
            foreach (var row in rows)
            {
                sheet.Rows.Add(row.Index, row);
            }

            return sheet;
        }

        public static ExcelSheetDiff Diff(ExcelSheet src, ExcelSheet dst, ExcelSheetDiffConfig config)
        {
            var srcColumns = src.CreateColumns();
            var dstColumns = dst.CreateColumns();
            var columnStatusMap = CreateColumnStatusMap(srcColumns, dstColumns, config);

            var option = new DiffOption<ExcelRow>();
            option.EqualityComparer =
                new RowComparer(new HashSet<int>(columnStatusMap.Where(i => i.Value != ExcelColumnStatus.None).Select(i => i.Key)));

            // Shift cells for column alignment without creating intermediate queues
            var maxColumnIndex = columnStatusMap.Any() ? columnStatusMap.Keys.Max() + 1 : 0;
            foreach (var row in src.Rows.Values)
            {
                var shifted = new List<ExcelCell>(maxColumnIndex);
                int cellIdx = 0;
                for (int i = 0; i < maxColumnIndex && cellIdx < row.Cells.Count; i++)
                {
                    if (columnStatusMap.TryGetValue(i, out var status) && status == ExcelColumnStatus.Inserted)
                        shifted.Add(new ExcelCell(string.Empty, 0, 0));
                    else
                        shifted.Add(row.Cells[cellIdx++]);
                }
                row.UpdateCells(shifted);
            }

            foreach (var row in dst.Rows.Values)
            {
                var shifted = new List<ExcelCell>(maxColumnIndex);
                int cellIdx = 0;
                for (int i = 0; i < maxColumnIndex && cellIdx < row.Cells.Count; i++)
                {
                    if (columnStatusMap.TryGetValue(i, out var status) && status == ExcelColumnStatus.Deleted)
                        shifted.Add(new ExcelCell(string.Empty, 0, 0));
                    else
                        shifted.Add(row.Cells[cellIdx++]);
                }
                row.UpdateCells(shifted);
            }

            var r = DiffUtil.Diff(src.Rows.Values, dst.Rows.Values, option);
            r = DiffUtil.Order(r, DiffOrderType.LazyDeleteFirst);
            var resultArray = DiffUtil.OptimizeCaseDeletedFirst(r).ToArray();
            if (resultArray.Length > 10000)
            {
                var indices = new HashSet<int>();
                // Always include first 100 rows
                for (int i = 0; i < 100 && i < resultArray.Length; i++)
                    indices.Add(i);

                for (int i = 0; i < resultArray.Length; i++)
                {
                    if (resultArray[i].Status != DiffStatus.Equal)
                    {
                        // Include 100 rows before and after each diff
                        int start = Math.Max(0, i - 100);
                        int end = Math.Min(resultArray.Length - 1, i + 100);
                        for (int j = start; j <= end; j++)
                            indices.Add(j);
                    }
                }

                resultArray = indices.OrderBy(i => i).Select(i => resultArray[i]).ToArray();
            }

            var sheetDiff = new ExcelSheetDiff();
            DiffCells(resultArray, sheetDiff, columnStatusMap);

            return sheetDiff;
        }

        private static Dictionary<int, ExcelColumnStatus> CreateColumnStatusMap(
            IEnumerable<ExcelColumn> srcColumns, IEnumerable<ExcelColumn> dstColumns, ExcelSheetDiffConfig config)
        {
            var option = new DiffOption<ExcelColumn>();

            if (config.SrcHeaderIndex >= 0)
            {
                option.EqualityComparer = new HeaderComparer();
                foreach (var sc in srcColumns)
                    sc.HeaderIndex = config.SrcHeaderIndex;
            }

            if (config.DstHeaderIndex >= 0)
            {
                foreach (var dc in dstColumns)
                    dc.HeaderIndex = config.DstHeaderIndex;
            }

            var results = DiffUtil.Diff(srcColumns, dstColumns, option);
            results = DiffUtil.Order(results, DiffOrderType.LazyDeleteFirst);
            results = DiffUtil.OptimizeCaseDeletedFirst(results);
            var ret = new Dictionary<int, ExcelColumnStatus>();
            var columnIndex = 0;
            foreach (var result in results)
            {
                var status = ExcelColumnStatus.None;
                if (result.Status == DiffStatus.Deleted)
                    status = ExcelColumnStatus.Deleted;
                else if (result.Status == DiffStatus.Inserted)
                    status = ExcelColumnStatus.Inserted;

                ret.Add(columnIndex, status);
                columnIndex++;
            }

            return ret;
        }

        private IEnumerable<ExcelColumn> CreateColumns()
        {
            if (!Rows.Any())
                return Enumerable.Empty<ExcelColumn>();

            var columnCount = Rows.Max(r => r.Value.Cells.Count);
            var columns = new ExcelColumn[columnCount];
            for (int i = 0; i < columnCount; i++)
                columns[i] = new ExcelColumn();

            foreach (var row in Rows)
            {
                var cells = row.Value.Cells;
                for (int columnIndex = 0; columnIndex < cells.Count; columnIndex++)
                {
                    columns[columnIndex].Cells.Add(cells[columnIndex]);
                }
            }

            return columns;
        }

        private static void DiffCells(
            IEnumerable<DiffResult<ExcelRow>> results, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            foreach (var result in results)
            {
                switch (result.Status)
                {
                    case DiffStatus.Equal:
                        DiffCellsCaseEqual(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Modified:
                        DiffCellsCaseEqual(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Deleted:
                        DiffCellsCaseDeleted(result, sheetDiff, columnStatusMap);
                        break;
                    case DiffStatus.Inserted:
                        DiffCellsCaseInserted(result, sheetDiff, columnStatusMap);
                        break;
                }
            }
        }

        private static IEnumerable<Tuple<ExcelCell, ExcelCell>> EqualizeColumnCount(
            IEnumerable<ExcelCell> srcCells, IEnumerable<ExcelCell> dstCells, Dictionary<int, ExcelColumnStatus> columnStausMap)
        {
            var srcQueue = new Queue<ExcelCell>(srcCells);
            var dstQueue = new Queue<ExcelCell>(dstCells);
            foreach (var status in columnStausMap)
            {
                ExcelCell src = null;
                ExcelCell dst = null;

                if (srcQueue.Any()) src = srcQueue.Dequeue();
                if (dstQueue.Any()) dst = dstQueue.Dequeue();

                yield return Tuple.Create(src, dst);
            }
        }

        private static void DiffCellsCaseEqual(
            DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var row = sheetDiff.CreateRow();

            var equalizedCells = EqualizeColumnCount(result.Obj1.Cells, result.Obj2.Cells, columnStatusMap);
            var columnIndex = 0;
            foreach (var pair in equalizedCells)
            {
                var srcCell = pair.Item1;
                var dstCell = pair.Item2;

                if (srcCell != null && dstCell != null)
                {
                    var status = srcCell.Value.Equals(dstCell.Value) ? ExcelCellStatus.None : ExcelCellStatus.Modified;
                    if (columnStatusMap[columnIndex] == ExcelColumnStatus.Deleted)
                        status = ExcelCellStatus.Removed;
                    else if (columnStatusMap[columnIndex] == ExcelColumnStatus.Inserted)
                        status = ExcelCellStatus.Added;

                    row.CreateCell(srcCell, dstCell, columnIndex, status);
                }
                else if (srcCell != null && dstCell == null)
                {
                    dstCell = new ExcelCell(string.Empty, srcCell.OriginalColumnIndex, srcCell.OriginalColumnIndex);
                    row.CreateCell(srcCell, dstCell, columnIndex, ExcelCellStatus.Removed);
                }
                else if (srcCell == null && dstCell != null)
                {
                    srcCell = new ExcelCell(string.Empty, dstCell.OriginalColumnIndex, dstCell.OriginalColumnIndex);
                    row.CreateCell(srcCell, dstCell, columnIndex, ExcelCellStatus.Added);
                }
                else
                {
                    srcCell = new ExcelCell(string.Empty, 0, 0);
                    dstCell = new ExcelCell(string.Empty, 0, 0);
                    row.CreateCell(srcCell, dstCell, columnIndex, ExcelCellStatus.None);
                }

                columnIndex++;
            }
        }

        private static void DiffCellsCaseDeleted(
            DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var row = sheetDiff.CreateRow();

            var columnIndex = 0;
            foreach (var cell1 in result.Obj1.Cells)
            {
                var cell2 = new ExcelCell(string.Empty, cell1.OriginalColumnIndex, cell1.OriginalRowIndex);
                row.CreateCell(cell1, cell2, columnIndex, ExcelCellStatus.Removed);

                columnIndex++;
            }
        }

        private static void DiffCellsCaseInserted(
            DiffResult<ExcelRow> result, ExcelSheetDiff sheetDiff, Dictionary<int, ExcelColumnStatus> columnStatusMap)
        {
            var row = sheetDiff.CreateRow();

            var columnIndex = 0;
            foreach (var cell2 in result.Obj2.Cells)
            {
                var cell1 = new ExcelCell(string.Empty, cell2.OriginalColumnIndex, cell2.OriginalRowIndex);
                row.CreateCell(cell1, cell2, columnIndex, ExcelCellStatus.Added);

                columnIndex++;
            }
        }
    }
}
