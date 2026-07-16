using System;
using System.Collections.Generic;
using System.Linq;

namespace ExcelMerge
{
    public class ExcelColumn : IEquatable<ExcelColumn>
    {
        private int? cachedHashCode;

        public List<ExcelCell> Cells { get; private set; }
        public int HeaderIndex { get; set; }

        public ExcelColumn()
        {
            Cells = new List<ExcelCell>();
        }

        public ExcelColumn(IEnumerable<ExcelCell> cells)
        {
            Cells = cells.ToList();
        }

        public override bool Equals(object obj)
        {
            var other = obj as ExcelColumn;

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

        public bool Equals(ExcelColumn other)
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
    }

    internal class HeaderComparer : IEqualityComparer<ExcelColumn>
    {
        public bool Equals(ExcelColumn x, ExcelColumn y)
        {
            if (x == null || y == null)
                return false;

            var valueX = x.Cells.ElementAtOrDefault(x.HeaderIndex)?.Value ?? string.Empty;
            var valueY = y.Cells.ElementAtOrDefault(y.HeaderIndex)?.Value ?? string.Empty;

            return string.Equals(valueX, valueY, StringComparison.Ordinal);
        }

        public int GetHashCode(ExcelColumn obj)
        {
            var value = obj.Cells.ElementAtOrDefault(obj.HeaderIndex)?.Value ?? string.Empty;
            return value.GetHashCode();
        }
    }
}
