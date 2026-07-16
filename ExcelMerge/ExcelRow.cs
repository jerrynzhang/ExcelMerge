using System;
using System.Collections.Generic;
using System.Linq;

namespace ExcelMerge
{
    public class ExcelRow : IEquatable<ExcelRow>
    {
        private int? cachedHashCode;

        public int Index { get; private set; }
        public List<ExcelCell> Cells { get; private set; }

        public ExcelRow(int index, IEnumerable<ExcelCell> cells)
        {
            Index = index;
            Cells = cells.ToList();
        }

        public override bool Equals(object obj)
        {
            var other = obj as ExcelRow;

            return Equals(other);
        }

        public override int GetHashCode()
        {
            if (cachedHashCode.HasValue)
                return cachedHashCode.Value;

            var hash = 7;
            foreach (var cell in Cells)
            {
                hash = hash * 13 + (cell.Value?.GetHashCode() ?? 0);
            }

            cachedHashCode = hash;
            return hash;
        }

        public bool Equals(ExcelRow other)
        {
            if (other == null)
                return false;

            if (Cells.Count != other.Cells.Count)
                return false;

            for (int i = 0; i < Cells.Count; i++)
            {
                if (!string.Equals(Cells[i].Value, other.Cells[i].Value, StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        public bool IsBlank()
        {
            return Cells.All(c => string.IsNullOrEmpty(c.Value));
        }

        public void UpdateCells(IEnumerable<ExcelCell> cells)
        {
            Cells = cells.ToList();
            cachedHashCode = null;
        }
    }

    internal class RowComparer : IEqualityComparer<ExcelRow>
    {
        public HashSet<int> IgnoreColumns { get; private set; }

        public RowComparer(HashSet<int> ignoreColumns)
        {
            IgnoreColumns = ignoreColumns;
        }

        public bool Equals(ExcelRow x, ExcelRow y)
        {
            if (x == null || y == null)
                return false;

            if (ReferenceEquals(x, y))
                return true;

            var xCells = x.Cells;
            var yCells = y.Cells;

            int xIdx = 0, yIdx = 0;
            while (xIdx < xCells.Count || yIdx < yCells.Count)
            {
                while (xIdx < xCells.Count && IgnoreColumns.Contains(xIdx))
                    xIdx++;
                while (yIdx < yCells.Count && IgnoreColumns.Contains(yIdx))
                    yIdx++;

                if (xIdx >= xCells.Count && yIdx >= yCells.Count)
                    return true;
                if (xIdx >= xCells.Count || yIdx >= yCells.Count)
                    return false;

                if (!string.Equals(xCells[xIdx].Value, yCells[yIdx].Value, StringComparison.Ordinal))
                    return false;

                xIdx++;
                yIdx++;
            }

            return true;
        }

        public int GetHashCode(ExcelRow obj)
        {
            var hash = 7;
            var index = 0;
            foreach (var cell in obj.Cells)
            {
                if (IgnoreColumns.Contains(index))
                    continue;

                hash = hash * 13 + (cell.Value?.GetHashCode() ?? 0);

                index++;
            }

            return hash;
        }
    }
}
