using System;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using NPOI.XSSF.UserModel;

namespace ExcelMerge
{
    public class ExcelUtility
    {
        public static object GetCellValue(ICell cell)
        {
            if (cell == null)
                return null;

            return GetCellValue(cell, cell.CellType);
        }

        private static object GetCellValue(ICell cell, CellType type)
        {
            if (cell != null)
            {
                switch (type)
                {
                    case CellType.Numeric:
                        if (DateUtil.IsCellDateFormatted(cell))
                        {
                            return cell.DateCellValue;
                        }
                        else
                        {
                            return cell.NumericCellValue;
                        }
                    case CellType.String:
                        return cell.StringCellValue;
                    case CellType.Boolean:
                        return cell.BooleanCellValue;
                    case CellType.Formula:
                        return GetCellValue(cell, cell.CachedFormulaResultType);
                }
            }

            return string.Empty;
        }

        public static string GetCellStringValue(ICell cell)
        {
            if (cell == null)
                return string.Empty;

            return GetCellValue(cell).ToString();
        }

        public static void CreateWorkbook(string path, ExcelWorkbookType workbookType)
        {
            if (!ValidateExtension(path, workbookType))
                throw new ArgumentException("The specified Excel type and path extension do not match.");

            var workbook = CreateWorkbook(workbookType);
            var sheet = workbook.CreateSheet();

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                workbook.Write(fileStream);
            }
        }
        private static IWorkbook CreateWorkbook(ExcelWorkbookType workbookType)
        {
            switch (workbookType)
            {
                case ExcelWorkbookType.XLS: return new HSSFWorkbook() as IWorkbook;
                case ExcelWorkbookType.XLSX: return new XSSFWorkbook() as IWorkbook;
                default: break;
            }

            throw new ArgumentException("The specified excel type is not supported instantiating.");
        }

        private static bool ValidateExtension(string path, ExcelWorkbookType workbookType)
        {
            switch (workbookType)
            {
                case ExcelWorkbookType.XLS: return Path.GetExtension(path) == ".xls";
                case ExcelWorkbookType.XLSX: return Path.GetExtension(path) == ".xlsx";
                default: break;
            }

            return false;
        }

        public static ExcelWorkbookType GetWorkbookType(string path)
        {
            var extension = Path.GetExtension(path);
            switch (extension)
            {
                case ".xls": return ExcelWorkbookType.XLS;
                case ".xlsx": return ExcelWorkbookType.XLSX;
                default: break;
            }

            return ExcelWorkbookType.None;
        }

        public static ExcelWorkbookType GetWorkboolTypeStrict(string path)
        {
            var type = GetWorkbookType(path);

            if (type == ExcelWorkbookType.None)
            {
                if (IsXLS(path))
                    type = ExcelWorkbookType.XLS;
                else if (IsXLSX(path))
                    type = ExcelWorkbookType.XLSX;
            }

            return type;
        }

        public static bool IsXLS(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var header = new byte[8];
                    if (fs.Read(header, 0, 8) < 8)
                        return false;

                    // OLE2 Compound Document magic bytes: D0 CF 11 E0 A1 B1 1A E1
                    return header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11
                        && header[3] == 0xE0 && header[4] == 0xA1 && header[5] == 0xB1
                        && header[6] == 0x1A && header[7] == 0xE1;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool IsXLSX(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var header = new byte[4];
                    if (fs.Read(header, 0, 4) < 4)
                        return false;

                    // ZIP/Office Open XML magic bytes: 50 4B 03 04
                    return header[0] == 0x50 && header[1] == 0x4B
                        && header[2] == 0x03 && header[3] == 0x04;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
