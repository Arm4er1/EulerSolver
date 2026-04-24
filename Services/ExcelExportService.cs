using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EulerSolver.Models;
using Excel = Microsoft.Office.Interop.Excel;

namespace EulerSolver.Services
{
    public class ExcelExportService
    {
        public void Export(SolverResult result)
        {
            Excel.Application excel = null;
            Excel.Workbook workbook = null;

            try
            {
                excel = new Excel.Application();
                excel.Visible = false;
                excel.DisplayAlerts = false;

                workbook = excel.Workbooks.Add();

                Excel.Worksheet wsResults = (Excel.Worksheet)workbook.Sheets[1];
                wsResults.Name = "Результаты";
                FillResultsSheet(wsResults, result);

                Excel.Worksheet wsParams = (Excel.Worksheet)workbook.Sheets.Add(
                    After: workbook.Sheets[workbook.Sheets.Count]);
                wsParams.Name = "Параметры";
                FillParamsSheet(wsParams, result);

                AddChart(wsResults, result);

                excel.Visible = true;
                excel.WindowState = Excel.XlWindowState.xlMaximized;
            }
            catch (Exception ex)
            {
                if (workbook != null)
                {
                    workbook.Close(false);
                    Marshal.ReleaseComObject(workbook);
                }
                if (excel != null)
                {
                    excel.Quit();
                    Marshal.ReleaseComObject(excel);
                }

                throw new Exception("Ошибка при экспорте в Excel:\n" + ex.Message);
            }
        }

        private void FillResultsSheet(Excel.Worksheet ws, SolverResult result)
        {
            bool hasExact = result.Points.Count > 0 &&
                            result.Points[0].ExactY.HasValue;

            // Заголовок
            Excel.Range titleCell = ws.Cells[1, 1];
            titleCell.Value = "Решение ОДУ: " + result.EquationDescription;
            titleCell.Font.Bold = true;
            titleCell.Font.Size = 14;
            titleCell.Font.Color = ColorToExcel(0x1976D2);

            int colCount = hasExact ? 5 : 2;
            ws.Range[ws.Cells[1, 1], ws.Cells[1, colCount]].Merge();

            // Шапка таблицы
            int headerRow = 3;
            string[] headers = hasExact
                ? new[] { "X", "Y (числ.)", "Y (точное)", "Абс. погр.", "Отн. погр. (%)" }
                : new[] { "X", "Y (числ.)" };

            int[] headerColors = hasExact
                ? new[] { 0x1565C0, 0x1565C0, 0x2E7D32, 0xC62828, 0xC62828 }
                : new[] { 0x1565C0, 0x1565C0 };

            for (int i = 0; i < headers.Length; i++)
            {
                Excel.Range cell = (Excel.Range)ws.Cells[headerRow, i + 1];
                cell.Value = headers[i];
                cell.Font.Bold = true;
                cell.Font.Color = ColorToExcel(0xFFFFFF);
                cell.Interior.Color = ColorToExcel(headerColors[i]);
                cell.HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                cell.RowHeight = 22;
            }

            // Данные
            for (int i = 0; i < result.Points.Count; i++)
            {
                var p = result.Points[i];
                int row = headerRow + 1 + i;

                Excel.Range rowRange = ws.Range[
                    (Excel.Range)ws.Cells[row, 1],
                    (Excel.Range)ws.Cells[row, colCount]];

                rowRange.Interior.Color = i % 2 == 0
                    ? ColorToExcel(0xF8F9FA)
                    : ColorToExcel(0xFFFFFF);

                ((Excel.Range)ws.Cells[row, 1]).Value = p.X;
                ((Excel.Range)ws.Cells[row, 2]).Value = p.Y;

                if (hasExact && p.ExactY.HasValue)
                {
                    ((Excel.Range)ws.Cells[row, 3]).Value = p.ExactY.Value;

                    if (p.AbsoluteError.HasValue)
                        ((Excel.Range)ws.Cells[row, 4]).Value = p.AbsoluteError.Value;

                    if (p.RelativeError.HasValue)
                        ((Excel.Range)ws.Cells[row, 5]).Value = p.RelativeError.Value;
                }
            }

            // Форматирование чисел
            int lastDataRow = headerRow + result.Points.Count;

            ws.Range[
                (Excel.Range)ws.Cells[headerRow + 1, 1],
                (Excel.Range)ws.Cells[lastDataRow, 2]
            ].NumberFormat = "0.000000";

            if (hasExact)
            {
                ws.Range[
                    (Excel.Range)ws.Cells[headerRow + 1, 3],
                    (Excel.Range)ws.Cells[lastDataRow, 3]
                ].NumberFormat = "0.000000";

                ws.Range[
                    (Excel.Range)ws.Cells[headerRow + 1, 4],
                    (Excel.Range)ws.Cells[lastDataRow, 5]
                ].NumberFormat = "0.0000E+00";
            }

            // Ширина столбцов
            for (int i = 1; i <= colCount; i++)
                ((Excel.Range)ws.Columns[i]).AutoFit();

            for (int i = 1; i <= colCount; i++)
            {
                Excel.Range col = (Excel.Range)ws.Columns[i];
                if ((double)col.ColumnWidth < 14)
                    col.ColumnWidth = 14;
            }

            // Границы
            Excel.Range tableRange = ws.Range[
                (Excel.Range)ws.Cells[headerRow, 1],
                (Excel.Range)ws.Cells[lastDataRow, colCount]];

            tableRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            tableRange.Borders.Weight = Excel.XlBorderWeight.xlThin;
            tableRange.Borders.Color = ColorToExcel(0xDDDDDD);
        }

        private void FillParamsSheet(Excel.Worksheet ws, SolverResult result)
        {
            Excel.Range title = (Excel.Range)ws.Cells[1, 1];
            title.Value = "Параметры решения";
            title.Font.Bold = true;
            title.Font.Size = 14;
            title.Font.Color = ColorToExcel(0x1976D2);

            ws.Range[ws.Cells[1, 1], ws.Cells[1, 2]].Merge();

            var rows = new List<(string, string)>
            {
                ("Уравнение",        result.EquationDescription),
                ("Метод",            "Модифицированный Эйлер (Эйлер-Коши)"),
                ("Порядок точности", "O(h²)"),
                ("X₀",               result.X0.ToString("G6")),
                ("Xₙ",               result.Xn.ToString("G6")),
                ("Шаг h",            result.StepSize.ToString("G6")),
                ("Кол-во шагов",     result.StepsCount.ToString()),
                ("Кол-во точек",     result.Points.Count.ToString()),
                ("Время вычисления", result.ElapsedMilliseconds.ToString("F3") + " мс"),
            };

            if (result.MaxAbsoluteError.HasValue)
                rows.Add(("Макс. абс. погрешность",
                          result.MaxAbsoluteError.Value.ToString("E4")));

            for (int i = 0; i < rows.Count; i++)
            {
                int row = i + 3;

                Excel.Range labelCell = (Excel.Range)ws.Cells[row, 1];
                labelCell.Value = rows[i].Item1;
                labelCell.Font.Bold = true;
                labelCell.Interior.Color = i % 2 == 0
                    ? ColorToExcel(0xF0F4FF)
                    : ColorToExcel(0xFFFFFF);

                Excel.Range valueCell = (Excel.Range)ws.Cells[row, 2];
                valueCell.Value = rows[i].Item2;
                valueCell.Interior.Color = labelCell.Interior.Color;
            }

            ((Excel.Range)ws.Columns[1]).ColumnWidth = 25;
            ((Excel.Range)ws.Columns[2]).ColumnWidth = 35;

            Excel.Range tableRange = ws.Range[
                ws.Cells[3, 1], ws.Cells[rows.Count + 2, 2]];
            tableRange.Borders.LineStyle = Excel.XlLineStyle.xlContinuous;
            tableRange.Borders.Weight = Excel.XlBorderWeight.xlThin;
            tableRange.Borders.Color = ColorToExcel(0xDDDDDD);
        }

        private void AddChart(Excel.Worksheet ws, SolverResult result)
        {
            bool hasExact = result.Points.Count > 0 &&
                            result.Points[0].ExactY.HasValue;

            int lastRow = 3 + result.Points.Count;

            Excel.ChartObjects chartObjects = (Excel.ChartObjects)ws.ChartObjects();
            Excel.ChartObject chartObj = chartObjects.Add(
                Left: 10,
                Top: ((Excel.Range)ws.Cells[lastRow + 3, 1]).Top,
                Width: 600,
                Height: 350);

            Excel.Chart chart = chartObj.Chart;
            chart.ChartType = Excel.XlChartType.xlXYScatterLinesNoMarkers;

            Excel.SeriesCollection seriesCol =
                (Excel.SeriesCollection)chart.SeriesCollection();

            Excel.Series seriesNum = seriesCol.NewSeries();
            seriesNum.Name = "Модифицированный Эйлер";
            seriesNum.XValues = ws.Range[
                (Excel.Range)ws.Cells[4, 1],
                (Excel.Range)ws.Cells[lastRow, 1]];
            seriesNum.Values = ws.Range[
                (Excel.Range)ws.Cells[4, 2],
                (Excel.Range)ws.Cells[lastRow, 2]];
            seriesNum.Border.Color = ColorToExcel(0x1E88E5);
            seriesNum.Border.Weight = Excel.XlBorderWeight.xlMedium;

            if (hasExact)
            {
                Excel.Series seriesExact = seriesCol.NewSeries();
                seriesExact.Name = "Точное решение";
                seriesExact.XValues = ws.Range[
                    (Excel.Range)ws.Cells[4, 1],
                    (Excel.Range)ws.Cells[lastRow, 1]];
                seriesExact.Values = ws.Range[
                    (Excel.Range)ws.Cells[4, 3],
                    (Excel.Range)ws.Cells[lastRow, 3]];
                seriesExact.Border.Color = ColorToExcel(0x43A047);
                seriesExact.Border.Weight = Excel.XlBorderWeight.xlMedium;
            }

            chart.HasTitle = true;
            chart.ChartTitle.Text = "График: " + result.EquationDescription;
            chart.ChartTitle.Font.Size = 12;
            chart.ChartTitle.Font.Bold = true;

            Excel.Axis axisX =
                (Excel.Axis)chart.Axes(Excel.XlAxisType.xlCategory);
            axisX.HasTitle = true;
            axisX.AxisTitle.Text = "X";

            Excel.Axis axisY =
                (Excel.Axis)chart.Axes(Excel.XlAxisType.xlValue);
            axisY.HasTitle = true;
            axisY.AxisTitle.Text = "Y";

            chart.HasLegend = true;
            chart.Legend.Position =
                Excel.XlLegendPosition.xlLegendPositionBottom;

            chart.PlotArea.Interior.Color = ColorToExcel(0xFAFAFA);
            chart.ChartArea.Border.LineStyle = Excel.XlLineStyle.xlContinuous;
            chart.ChartArea.Border.Color = ColorToExcel(0xDDDDDD);
        }

        private int ColorToExcel(int rgb)
        {
            int r = (rgb >> 16) & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int b = rgb & 0xFF;
            return b << 16 | g << 8 | r;
        }
    }
}