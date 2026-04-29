using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EulerSolver.Core.Models;
using Word = Microsoft.Office.Interop.Word;


namespace EulerSolver.Services
{
    public class WordExportService
    {
        private Word.Application _word;
        private Word.Document _doc;

        public void Export(SolverResult result)
        {
            try
            {
                _word = new Word.Application();
                _word.Visible = false;
                _word.DisplayAlerts = Word.WdAlertLevel.wdAlertsNone;

                _doc = _word.Documents.Add();

                _doc.PageSetup.PaperSize = Word.WdPaperSize.wdPaperA4;
                _doc.PageSetup.TopMargin = _word.CentimetersToPoints(2);
                _doc.PageSetup.BottomMargin = _word.CentimetersToPoints(2);
                _doc.PageSetup.LeftMargin = _word.CentimetersToPoints(3);
                _doc.PageSetup.RightMargin = _word.CentimetersToPoints(1.5f);

                _doc.Content.Text = "";

                BuildDocument(result);

                _word.Visible = true;
                _word.WindowState = Word.WdWindowState.wdWindowStateMaximize;
            }
            catch (Exception ex)
            {
                TryClose();
                throw new Exception("Ошибка при экспорте в Word:\n" + ex.Message);
            }
        }

        private void BuildDocument(SolverResult result)
        {
            bool hasExact = result.Points.Count > 0 &&
                            result.Points[0].ExactY.HasValue;

            // Титульный заголовок
            WriteTitle("ОТЧЁТ О РЕШЕНИИ ОДУ");
            WriteTitle("Модифицированный метод Эйлера (Эйлер-Коши)");
            WriteEmptyLine();

            // 1. Постановка задачи
            WriteHeading("1. Постановка задачи");
            WriteNormal(
                "Решается задача Коши для обыкновенного дифференциального " +
                "уравнения первого порядка вида y' = f(x, y).");
            WriteNormal("Уравнение: " + result.EquationDescription);
            WriteNormal(
                "Начальное условие: y(" + result.X0.ToString("G6") + ") = " +
                result.Y0.ToString("G6"));
            WriteNormal(
                "Область интегрирования: [" +
                result.X0.ToString("G6") + "; " +
                result.Xn.ToString("G6") + "]");
            WriteEmptyLine();

            // 2. Метод решения
            WriteHeading("2. Метод решения");
            WriteNormal(
                "Для решения применяется модифицированный метод Эйлера. " +
                "Метод имеет второй порядок точности O(h^2). " +
                "На каждом шаге выполняются два вычисления:");
            WriteMonospace("Шаг 1 (предиктор): y*[n+1] = y[n] + h * f(x[n], y[n])");
            WriteMonospace("Шаг 2 (корректор): y[n+1]  = y[n] + (h/2) * [f(x[n], y[n]) + f(x[n+1], y*[n+1])]");
            WriteEmptyLine();

            // 3. Параметры
            WriteHeading("3. Параметры вычисления");
            WriteEmptyLine();

            var paramRows = new List<(string, string)>
            {
                ("Уравнение",        result.EquationDescription),
                ("Метод",            "Модифицированный Эйлер (Эйлер-Коши)"),
                ("Порядок точности", "O(h^2)"),
                ("X0",               result.X0.ToString("G6")),
                ("Xn",               result.Xn.ToString("G6")),
                ("Шаг h",            result.StepSize.ToString("G6")),
                ("Кол-во шагов",     result.StepsCount.ToString()),
                ("Кол-во точек",     result.Points.Count.ToString()),
                ("Время вычисления", result.ElapsedMilliseconds.ToString("F3") + " мс"),
            };

            if (result.MaxAbsoluteError.HasValue)
                paramRows.Add(("Макс. погрешность",
                    result.MaxAbsoluteError.Value.ToString("E4")));

            WriteParamsTable(paramRows);
            WriteEmptyLine();

            // 4. Результаты
            WriteHeading("4. Результаты вычислений");

            int step = 1;
            if (result.Points.Count > 40)
                step = result.Points.Count / 40;

            var displayPoints = new List<SolutionPoint>();
            for (int i = 0; i < result.Points.Count; i += step)
                displayPoints.Add(result.Points[i]);

            var last = result.Points[result.Points.Count - 1];
            var prev = displayPoints[displayPoints.Count - 1];
            if (Math.Abs(prev.X - last.X) > 1e-10)
                displayPoints.Add(last);

            if (step > 1)
                WriteNormal(
                    "Показана каждая " + step + "-я точка из " +
                    result.Points.Count + " вычисленных.");

            WriteEmptyLine();
            WriteResultsTable(displayPoints, hasExact);
            WriteEmptyLine();

            // 5. График
            WriteHeading("5. График решения");
            WriteNormal(
                "На графике представлено численное решение ОДУ," +
                (hasExact ? " а также точное аналитическое решение." : "."));
            WriteEmptyLine();
            AddChart(result, hasExact);
            WriteEmptyLine();

            // 6. Погрешность или вывод
            if (hasExact && result.MaxAbsoluteError.HasValue)
            {
                WriteHeading("6. Анализ погрешности");
                WriteNormal(
                    "Максимальная абсолютная погрешность: " +
                    result.MaxAbsoluteError.Value.ToString("E4"));
                WriteNormal(
                    "Погрешность соответствует теоретической оценке O(h^2) " +
                    "для модифицированного метода Эйлера.");
                WriteEmptyLine();
                WriteHeading("7. Вывод");
            }
            else
            {
                WriteHeading("6. Вывод");
            }

            WriteNormal(
                "Задача Коши успешно решена модифицированным методом Эйлера. " +
                "Вычисления выполнены за " +
                result.ElapsedMilliseconds.ToString("F3") + " мс. " +
                "Получено " + result.Points.Count + " точек решения на " +
                "отрезке [" + result.X0.ToString("G4") + "; " +
                result.Xn.ToString("G4") + "] " +
                "с шагом h = " + result.StepSize.ToString("G4") + ".");
        }

        #region График

        private void AddChart(SolverResult result, bool hasExact)
        {
            dynamic excelApp = null;
            dynamic workbook = null;
            string tempImg = null;

            try
            {
                // Проверяем, установлен ли Excel
                Type excelType = Type.GetTypeFromProgID("Excel.Application");
                if (excelType == null)
                {
                    WriteNormal("(График недоступен: Microsoft Excel не установлен)");
                    return;
                }

                // Создаем Excel с дополнительными настройками
                excelApp = Activator.CreateInstance(excelType);

                // Настройки для предотвращения ошибок
                excelApp.Visible = false;
                excelApp.DisplayAlerts = false;
                excelApp.ScreenUpdating = false;
                excelApp.AskToUpdateLinks = false;
                excelApp.AlertBeforeOverwriting = false;

                // Добавляем книгу
                workbook = excelApp.Workbooks.Add(Type.Missing);
                dynamic worksheet = workbook.Worksheets[1];

                // Заполняем данные
                int count = result.Points.Count;

                // Заголовки
                worksheet.Cells[1, 1] = "X";
                worksheet.Cells[1, 2] = "Y числ.";
                if (hasExact)
                    worksheet.Cells[1, 3] = "Y точн.";

                // Данные
                for (int i = 0; i < count; i++)
                {
                    int row = i + 2;
                    worksheet.Cells[row, 1] = result.Points[i].X;
                    worksheet.Cells[row, 2] = result.Points[i].Y;
                    if (hasExact && result.Points[i].ExactY.HasValue)
                        worksheet.Cells[row, 3] = result.Points[i].ExactY.Value;
                }

                int lastRow = count + 1;

                // Создаём диапазон данных
                dynamic xRange = worksheet.Range[$"A2:A{lastRow}"];
                dynamic yRange = worksheet.Range[$"B2:B{lastRow}"];

                // Создаём диаграмму с помощью ChartObjects
                dynamic chartObjects = worksheet.ChartObjects(Type.Missing);
                dynamic chartObject = chartObjects.Add(50, 50, 600, 400);
                dynamic chart = chartObject.Chart;

                // Устанавливаем тип диаграммы (xlXYScatterLines = 74)
                chart.ChartType = 74;

                // Удаляем стандартные серии
                while (chart.SeriesCollection().Count > 0)
                {
                    chart.SeriesCollection(1).Delete();
                }

                // Добавляем численное решение
                dynamic series1 = chart.SeriesCollection().NewSeries();
                series1.Name = "Мод. Эйлер";
                series1.XValues = xRange;
                series1.Values = yRange;
                series1.Border.Color = 0x0000FF; // RGB: синий
                series1.Border.Weight = 2;
                series1.MarkerStyle = -4142; // xlMarkerStyleNone
                series1.MarkerSize = 2;

                // Добавляем точное решение, если есть
                if (hasExact)
                {
                    dynamic exactRange = worksheet.Range[$"C2:C{lastRow}"];
                    dynamic series2 = chart.SeriesCollection().NewSeries();
                    series2.Name = "Точное решение";
                    series2.XValues = xRange;
                    series2.Values = exactRange;
                    series2.Border.Color = 0x00CC00; // RGB: зелёный
                    series2.Border.Weight = 2;
                    series2.MarkerStyle = -4142;
                    series2.MarkerSize = 2;
                }

                // Настройка заголовков
                chart.HasTitle = true;
                chart.ChartTitle.Text = "График решения ОДУ: " + result.EquationDescription;
                chart.ChartTitle.Font.Size = 12;
                chart.ChartTitle.Font.Bold = true;

                chart.HasLegend = true;
                chart.Legend.Position = -4107; // xlLegendPositionBottom

                // Настройка осей
                try
                {
                    dynamic axisX = chart.Axes(1, 1);
                    axisX.HasTitle = true;
                    axisX.AxisTitle.Text = "X (аргумент)";
                    axisX.AxisTitle.Font.Size = 10;

                    dynamic axisY = chart.Axes(2, 2);
                    axisY.HasTitle = true;
                    axisY.AxisTitle.Text = "Y (значение функции)";
                    axisY.AxisTitle.Font.Size = 10;
                }
                catch (Exception ex)
                {
                    // Игнорируем ошибки осей
                    System.Diagnostics.Debug.WriteLine("Axis error: " + ex.Message);
                }

                // Экспорт во временный файл
                tempImg = System.IO.Path.GetTempFileName();
                System.IO.File.Delete(tempImg);
                tempImg = System.IO.Path.ChangeExtension(tempImg, ".png");

                // Экспортируем с проверкой
                bool exportSuccess = false;
                try
                {
                    chart.Export(tempImg, "PNG", false);
                    exportSuccess = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Export error: " + ex.Message);
                }

                // Обязательно закрываем и освобождаем
                if (chartObject != null)
                    Marshal.ReleaseComObject(chartObject);
                if (chartObjects != null)
                    Marshal.ReleaseComObject(chartObjects);
                if (xRange != null)
                    Marshal.ReleaseComObject(xRange);
                if (yRange != null)
                    Marshal.ReleaseComObject(yRange);

                // Закрываем Excel
                workbook.Close(false);
                Marshal.ReleaseComObject(worksheet);
                Marshal.ReleaseComObject(workbook);

                excelApp.Quit();
                Marshal.ReleaseComObject(excelApp);

                excelApp = null;

                // Даем время на завершение
                System.Threading.Thread.Sleep(200);

                // Вставляем изображение в Word
                if (exportSuccess && System.IO.File.Exists(tempImg))
                {
                    Word.Range imgRange = _doc.Paragraphs.Last.Range;
                    imgRange.Collapse(Word.WdCollapseDirection.wdCollapseEnd);

                    Word.InlineShape pic = _doc.InlineShapes.AddPicture(
                        tempImg,
                        LinkToFile: false,
                        SaveWithDocument: true,
                        Range: imgRange);

                    // Масштабируем
                    float pageWidth = (float)(_doc.PageSetup.PageWidth
                        - _doc.PageSetup.LeftMargin
                        - _doc.PageSetup.RightMargin);

                    if (pic.Width > pageWidth)
                    {
                        float originalWidth = pic.Width;
                        if (originalWidth > pageWidth)
                        {
                            float ratio = pageWidth / originalWidth;
                            pic.Width = pageWidth;
                            pic.Height = pic.Height * ratio;
                        }
                    }

                    // Добавляем подпись
                    imgRange = pic.Range;
                    imgRange.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
                    imgRange.Text = "\nРисунок 1. Сравнение численного и точного решений";
                    imgRange.Font.Size = 10;
                    imgRange.Font.Italic = 1;
                    imgRange.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                }
                else
                {
                    WriteNormal("(График не может быть отображен)");
                }

                // Чистим временные файлы
                try
                {
                    if (tempImg != null && System.IO.File.Exists(tempImg))
                        System.IO.File.Delete(tempImg);
                }
                catch { }
            }
            catch (Exception ex)
            {
                WriteNormal($"(Ошибка создания графика: {ex.Message})");
                System.Diagnostics.Debug.WriteLine("Chart error: " + ex.ToString());
            }
            finally
            {
                // Принудительная сборка мусора
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        /// <summary>
        /// Конвертирует RGB цвет в OLE формат для COM объектов Office
        /// </summary>
        private int RgbToOleColor(int rgb)
        {
            int r = (rgb >> 16) & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int b = rgb & 0xFF;
            return r | (g << 8) | (b << 16);
        }

        #endregion

        #region Методы записи текста

        private void WriteTitle(string text)
        {
            Word.Paragraph para = _doc.Paragraphs.Add();
            Word.Range range = para.Range;

            range.Text = text;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 16;
            range.Font.Bold = 1;
            range.Font.Color = (Word.WdColor)RgbToWdColor(0x1565C0);

            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            para.SpaceBefore = 0;
            para.SpaceAfter = 6;
            para.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;

            range.InsertParagraphAfter();
            range.Collapse(Word.WdCollapseDirection.wdCollapseEnd);
        }

        private void WriteHeading(string text)
        {
            Word.Paragraph para = _doc.Paragraphs.Add();
            Word.Range range = para.Range;

            range.Text = text;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 13;
            range.Font.Bold = 1;
            range.Font.Color = (Word.WdColor)RgbToWdColor(0x1976D2);

            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
            para.SpaceBefore = 12;
            para.SpaceAfter = 6;
            para.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
            para.FirstLineIndent = 0;

            range.InsertParagraphAfter();
        }

        private void WriteNormal(string text)
        {
            Word.Paragraph para = _doc.Paragraphs.Add();
            Word.Range range = para.Range;

            range.Text = text;
            range.Font.Name = "Times New Roman";
            range.Font.Size = 12;
            range.Font.Bold = 0;
            range.Font.Color = Word.WdColor.wdColorAutomatic;

            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphJustify;
            para.SpaceBefore = 0;
            para.SpaceAfter = 4;
            para.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
            para.FirstLineIndent = _word.CentimetersToPoints(1.25f);

            range.InsertParagraphAfter();
        }

        private void WriteMonospace(string text)
        {
            Word.Paragraph para = _doc.Paragraphs.Add();
            Word.Range range = para.Range;

            range.Text = text;
            range.Font.Name = "Courier New";
            range.Font.Size = 11;
            range.Font.Bold = 0;
            range.Font.Color = (Word.WdColor)RgbToWdColor(0x1A237E);

            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            para.SpaceBefore = 4;
            para.SpaceAfter = 4;
            para.LineSpacingRule = Word.WdLineSpacing.wdLineSpaceSingle;
            para.FirstLineIndent = 0;

            range.InsertParagraphAfter();
        }

        private void WriteEmptyLine()
        {
            Word.Paragraph para = _doc.Paragraphs.Add();
            para.Range.Text = "";
            para.SpaceBefore = 0;
            para.SpaceAfter = 0;
            para.Range.Font.Size = 6;
            para.Range.InsertParagraphAfter();
        }

        #endregion

        #region Таблицы

        private void WriteParamsTable(List<(string, string)> rows)
        {
            Word.Range tableRange = _doc.Paragraphs.Last.Range;

            Word.Table table = _doc.Tables.Add(
                tableRange,
                rows.Count, 2,
                Word.WdDefaultTableBehavior.wdWord9TableBehavior,
                Word.WdAutoFitBehavior.wdAutoFitFixed);

            float pageWidth = _doc.PageSetup.PageWidth
                - _doc.PageSetup.LeftMargin
                - _doc.PageSetup.RightMargin;

            table.PreferredWidthType = Word.WdPreferredWidthType.wdPreferredWidthPoints;
            table.PreferredWidth = pageWidth * 0.8f;
            table.Columns[1].Width = pageWidth * 0.8f * 0.38f;
            table.Columns[2].Width = pageWidth * 0.8f * 0.62f;

            StyleTableBorders(table, 0x1565C0);
            table.Rows.Alignment = Word.WdRowAlignment.wdAlignRowCenter;

            for (int i = 0; i < rows.Count; i++)
            {
                int wordColor = i % 2 == 0
                    ? RgbToWdColor(0xE3F2FD)
                    : RgbToWdColor(0xFFFFFF);

                Word.Cell c1 = table.Cell(i + 1, 1);
                c1.Range.Text = rows[i].Item1;
                c1.Range.Font.Name = "Times New Roman";
                c1.Range.Font.Size = 11;
                c1.Range.Font.Bold = 1;
                c1.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                c1.Range.ParagraphFormat.SpaceBefore = 2;
                c1.Range.ParagraphFormat.SpaceAfter = 2;
                c1.Shading.BackgroundPatternColor = (Word.WdColor)wordColor;
                c1.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                Word.Cell c2 = table.Cell(i + 1, 2);
                c2.Range.Text = rows[i].Item2;
                c2.Range.Font.Name = "Courier New";
                c2.Range.Font.Size = 11;
                c2.Range.Font.Bold = 0;
                c2.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphLeft;
                c2.Range.ParagraphFormat.SpaceBefore = 2;
                c2.Range.ParagraphFormat.SpaceAfter = 2;
                c2.Shading.BackgroundPatternColor = (Word.WdColor)wordColor;
                c2.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            }
        }

        private void WriteResultsTable(List<SolutionPoint> points, bool hasExact)
        {
            int colCount = hasExact ? 5 : 2;

            string[] headers = hasExact
                ? new[] { "X", "Y (числ.)", "Y (точное)", "Абс. погр.", "Отн. погр. (%)" }
                : new[] { "X", "Y (числ.)" };

            int[] hColors = hasExact
                ? new[] { 0x1565C0, 0x1565C0, 0x2E7D32, 0xC62828, 0xC62828 }
                : new[] { 0x1565C0, 0x1565C0 };

            Word.Range tableRange = _doc.Paragraphs.Last.Range;

            Word.Table table = _doc.Tables.Add(
                tableRange,
                points.Count + 1,
                colCount,
                Word.WdDefaultTableBehavior.wdWord9TableBehavior,
                Word.WdAutoFitBehavior.wdAutoFitFixed);

            float pageWidth = _doc.PageSetup.PageWidth
                - _doc.PageSetup.LeftMargin
                - _doc.PageSetup.RightMargin;

            table.PreferredWidthType = Word.WdPreferredWidthType.wdPreferredWidthPoints;
            table.PreferredWidth = pageWidth;

            float colWidth = pageWidth / colCount;
            for (int i = 1; i <= colCount; i++)
                table.Columns[i].Width = colWidth;

            StyleTableBorders(table, 0x90A4AE);

            // Заголовки
            for (int i = 0; i < colCount; i++)
            {
                Word.Cell cell = table.Cell(1, i + 1);
                cell.Range.Text = headers[i];
                cell.Range.Font.Name = "Times New Roman";
                cell.Range.Font.Size = 10;
                cell.Range.Font.Bold = 1;
                cell.Range.Font.Color = Word.WdColor.wdColorWhite;
                cell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
                cell.Range.ParagraphFormat.SpaceBefore = 2;
                cell.Range.ParagraphFormat.SpaceAfter = 2;
                cell.Shading.BackgroundPatternColor = (Word.WdColor)RgbToWdColor(hColors[i]);
                cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            }

            // Данные
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                int row = i + 2;
                int bgColor = i % 2 == 0
                    ? RgbToWdColor(0xF5F5F5)
                    : RgbToWdColor(0xFFFFFF);

                SetResultCell(table, row, 1, p.X.ToString("F6"), bgColor);
                SetResultCell(table, row, 2, p.Y.ToString("F6"), bgColor);

                if (hasExact)
                {
                    SetResultCell(table, row, 3,
                        p.ExactY.HasValue ? p.ExactY.Value.ToString("F6") : "—",
                        bgColor);
                    SetResultCell(table, row, 4,
                        p.AbsoluteError.HasValue ? p.AbsoluteError.Value.ToString("E4") : "—",
                        bgColor);
                    SetResultCell(table, row, 5,
                        p.RelativeError.HasValue ? p.RelativeError.Value.ToString("F4") : "—",
                        bgColor);
                }
            }
        }

        private void SetResultCell(Word.Table table, int row, int col,
                                   string text, int bgColor)
        {
            Word.Cell cell = table.Cell(row, col);
            cell.Range.Text = text;
            cell.Range.Font.Name = "Courier New";
            cell.Range.Font.Size = 9;
            cell.Range.Font.Bold = 0;
            cell.Range.ParagraphFormat.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            cell.Range.ParagraphFormat.SpaceBefore = 1;
            cell.Range.ParagraphFormat.SpaceAfter = 1;
            cell.Shading.BackgroundPatternColor = (Word.WdColor)bgColor;
            cell.VerticalAlignment = Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
        }

        private void StyleTableBorders(Word.Table table, int color)
        {
            table.Borders[Word.WdBorderType.wdBorderTop].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderBottom].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderLeft].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderRight].LineStyle = Word.WdLineStyle.wdLineStyleSingle;

            table.Borders[Word.WdBorderType.wdBorderHorizontal].LineStyle = Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderVertical].LineStyle = Word.WdLineStyle.wdLineStyleSingle;

            table.Borders[Word.WdBorderType.wdBorderTop].Color = (Word.WdColor)RgbToWdColor(color);
            table.Borders[Word.WdBorderType.wdBorderBottom].Color = (Word.WdColor)RgbToWdColor(color);
            table.Borders[Word.WdBorderType.wdBorderLeft].Color = (Word.WdColor)RgbToWdColor(color);
            table.Borders[Word.WdBorderType.wdBorderRight].Color = (Word.WdColor)RgbToWdColor(color);

            table.Borders[Word.WdBorderType.wdBorderHorizontal].Color = (Word.WdColor)RgbToWdColor(0xDDDDDD);
            table.Borders[Word.WdBorderType.wdBorderVertical].Color = (Word.WdColor)RgbToWdColor(0xDDDDDD);
        }

        #endregion

        private int RgbToWdColor(int rgb)
        {
            int r = (rgb >> 16) & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int b = rgb & 0xFF;
            return b << 16 | g << 8 | r;
        }

        private void TryClose()
        {
            try
            {
                if (_doc != null)
                {
                    _doc.Close(false);
                    Marshal.ReleaseComObject(_doc);
                }
                if (_word != null)
                {
                    _word.Quit();
                    Marshal.ReleaseComObject(_word);
                }
            }
            catch { }
        }
    }
}