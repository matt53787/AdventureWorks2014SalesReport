using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AdventureWorks2014SalesReport.Controllers
{
    /*
 * Copyright (c) 2014, Apex Remington, Inc.
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the <organization> nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

    public static class SpreadsheetBuilder
    {
        public const string ACCOUNTING_FORMAT = @"_($* #,##0.00_);_($* (#,##0.00);_($* "" - ""??_);_(@_)";
        public const string PERCENT_FORMAT = @"0.00%";
        public const string DECIMAL_FORMAT = @"_(* #,##0.00_);_(* (#,##0.00);_(* "" - ""??_);_(@_)";
        public const string INTEGER_FORMAT = @"_(* #,##0_);_(* (#,##0);_(* "" - ""??_);_(@_)";
        public const string DATE_FORMAT = @"mm-dd-yy";

        public static string CreateAccountingFormat(this ExcelWorkbook workbook)
        {
            var name = "Accounting";
            if (!workbook.Styles.NamedStyles.Any(x => x.Name == name))
            {
                var style = workbook.Styles.CreateNamedStyle(name).Style;
                style.Numberformat.Format = SpreadsheetBuilder.ACCOUNTING_FORMAT;
            }

            return name;
        }

        public static string CreateDecimalFormat(this ExcelWorkbook workbook)
        {
            var name = "Decimal";
            if (!workbook.Styles.NamedStyles.Any(x => x.Name == name))
            {
                var style = workbook.Styles.CreateNamedStyle(name).Style;
                style.Numberformat.Format = SpreadsheetBuilder.DECIMAL_FORMAT;
            }

            return name;
        }

        public static string CreateIntegerFormat(this ExcelWorkbook workbook)
        {
            var name = "Integer";
            if (!workbook.Styles.NamedStyles.Any(x => x.Name == name))
            {
                var style = workbook.Styles.CreateNamedStyle(name).Style;
                style.Numberformat.Format = SpreadsheetBuilder.INTEGER_FORMAT;
            }

            return name;
        }

        public static string CreatePercentFormat(this ExcelWorkbook workbook)
        {
            var name = "Percent";
            if (!workbook.Styles.NamedStyles.Any(x => x.Name == name))
            {
                var style = workbook.Styles.CreateNamedStyle(name).Style;
                style.Numberformat.Format = SpreadsheetBuilder.PERCENT_FORMAT;
            }

            return name;
        }

        public static string CreateDateFormat(this ExcelWorkbook workbook)
        {
            var name = "Date";
            if (!workbook.Styles.NamedStyles.Any(x => x.Name == name))
            {
                var style = workbook.Styles.CreateNamedStyle(name).Style;
                style.Numberformat.Format = SpreadsheetBuilder.DATE_FORMAT;
            }

            return name;
        }

        public static void SaveData(this ExcelWorksheet sheet, IEnumerable<string> title, IEnumerable<IEnumerable<object>> data)
        {
            sheet.SaveData(title, null, data, null);
        }

        public static void SaveData(this ExcelWorksheet sheet, IEnumerable<string> title, IEnumerable<string> columnStyles, IEnumerable<IEnumerable<object>> data)
        {
            sheet.SaveData(title, columnStyles, data, null);
        }

        public static void SaveData(this ExcelWorksheet sheet, IEnumerable<string> title, IEnumerable<string> columnStyles, IEnumerable<IEnumerable<object>> data, IEnumerable<object> totals)
        {
            var x = title.ToList();
            sheet.SaveTitle(x);

            int lastRow = 2;
            var styles = columnStyles != null ? columnStyles.ToList() : null;
            foreach (var row in data.Select((r, i) => new { r, i }))
            {
                foreach (var col in row.r.Select((c, i) => new { c, i }))
                {
                    // row index + 2 => 0-based index -> 1-based & plus header row
                    // col index + 1 => 0-based index -> 1-based
                    var cell = sheet.Cells[row.i + 2, col.i + 1];
                    cell.Value = col.c;

                    if (styles != null)
                        cell.StyleName = styles[col.i];
                }

                lastRow = row.i + 2;
            }

            if (totals != null)
                sheet.SaveTotals(totals, styles, lastRow + 1);

            foreach (var _ in x.Select((s, i) => new { s, i }))
            {
                sheet.Column(_.i + 1).AutoFit();
            }
        }

        public class ColumnTemplate<T>
        {
            public string Title;
            public string Style;
            public Func<T, object> Action;
            public Func<object> TotalAction;
        }

        public static void SaveData<T>(this ExcelWorksheet sheet, IEnumerable<ColumnTemplate<T>> columns, IEnumerable<T> data)
        {
            sheet.SaveData(
                title: columns.Select(x => x.Title),
                columnStyles: columns.Select(x => x.Style),
                data: data.Select(i => columns.Select(x => x.Action(i))),
                totals: columns.Select(x => x.TotalAction != null ? x.TotalAction() : null));
        }

        public static void SaveTitle(this ExcelWorksheet sheet, IEnumerable<string> title)
        {
            var row = sheet.Row(1);
            row.Style.Font.Bold = true;

            foreach (var _ in title.Select((s, i) => new { s, i }))
                sheet.Cells[1, _.i + 1].Value = _.s;

            sheet.View.FreezePanes(2, 1);
        }

        public static void SaveTotals(this ExcelWorksheet sheet, IEnumerable<object> totals, IList<string> styles, int row)
        {
            foreach (var _ in totals.Select((v, i) => new { v, i }))
                if (_.v != null)
                {
                    var cell = sheet.Cells[row, _.i + 1];
                    cell.Value = _.v;

                    if (styles != null)
                        cell.StyleName = styles[_.i];

                    cell.ApplyTotalFormat();
                }
        }

        public static void ApplyTotalFormat(this ExcelRange cell)
        {
            cell.Style.Font.Bold = true;
            cell.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            cell.Style.Border.Bottom.Style = ExcelBorderStyle.Double;
        }
    }
}