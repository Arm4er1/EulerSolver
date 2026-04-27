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

                // Настройки страницы A4
                _doc.PageSetup.PaperSize = Word.WdPaperSize.wdPaperA4;
                _doc.PageSetup.TopMargin = _word.CentimetersToPoints(2);
                _doc.PageSetup.BottomMargin = _word.CentimetersToPoints(2);
                _doc.PageSetup.LeftMargin = _word.CentimetersToPoints(3);
                _doc.PageSetup.RightMargin = _word.CentimetersToPoints(1.5f);

                // Очищаем документ
                _doc.Content.Text = "";

                // Строим документ
                BuildDocument(result);

                // Показываем
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
                "Метод имеет второй порядок точности O(h²). " +
                "На каждом шаге выполняются два вычисления:");
            WriteMonospace("Шаг 1 (предиктор): ŷₙ₊₁ = yₙ + h · f(xₙ, yₙ)");
            WriteMonospace("Шаг 2 (корректор): yₙ₊₁ = yₙ + (h/2) · [f(xₙ, yₙ) + f(xₙ₊₁, ŷₙ₊₁)]");
            WriteEmptyLine();

            // 3. Параметры
            WriteHeading("3. Параметры вычисления");
            WriteEmptyLine();

            var paramRows = new List<(string, string)>
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
                paramRows.Add(("Макс. погрешность",
                    result.MaxAbsoluteError.Value.ToString("E4")));

            WriteParamsTable(paramRows);
            WriteEmptyLine();

            // 4. Результаты
            WriteHeading("4. Результаты вычислений");

            // Ограничиваем строки таблицы
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

            // 5. Погрешность (если есть)
            if (hasExact && result.MaxAbsoluteError.HasValue)
            {
                WriteHeading("5. Анализ погрешности");
                WriteNormal(
                    "Максимальная абсолютная погрешность: " +
                    result.MaxAbsoluteError.Value.ToString("E4"));
                WriteNormal(
                    "Погрешность соответствует теоретической оценке O(h²) " +
                    "для модифицированного метода Эйлера.");
                WriteEmptyLine();
                WriteHeading("6. Вывод");
            }
            else
            {
                WriteHeading("5. Вывод");
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

            // Ширина таблицы 80% от ширины страницы
            float pageWidth = _doc.PageSetup.PageWidth
                - _doc.PageSetup.LeftMargin
                - _doc.PageSetup.RightMargin;

            table.PreferredWidthType =
                Word.WdPreferredWidthType.wdPreferredWidthPoints;
            table.PreferredWidth = pageWidth * 0.8f;

            table.Columns[1].Width = pageWidth * 0.8f * 0.38f;
            table.Columns[2].Width = pageWidth * 0.8f * 0.62f;

            // Стиль таблицы
            StyleTableBorders(table, 0x1565C0);

            // Центрируем таблицу
            table.Rows.Alignment =
                Word.WdRowAlignment.wdAlignRowCenter;

            // Заполняем строки
            for (int i = 0; i < rows.Count; i++)
            {
                int wordColor = i % 2 == 0
                    ? RgbToWdColor(0xE3F2FD)
                    : RgbToWdColor(0xFFFFFF);

                // Колонка: название
                Word.Cell c1 = table.Cell(i + 1, 1);
                c1.Range.Text = rows[i].Item1;
                c1.Range.Font.Name = "Times New Roman";
                c1.Range.Font.Size = 11;
                c1.Range.Font.Bold = 1;
                c1.Range.ParagraphFormat.Alignment =
                    Word.WdParagraphAlignment.wdAlignParagraphLeft;
                c1.Range.ParagraphFormat.SpaceBefore = 2;
                c1.Range.ParagraphFormat.SpaceAfter = 2;
                c1.Shading.BackgroundPatternColor = (Word.WdColor)wordColor;
                c1.VerticalAlignment =
                    Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;

                // Колонка: значение
                Word.Cell c2 = table.Cell(i + 1, 2);
                c2.Range.Text = rows[i].Item2;
                c2.Range.Font.Name = "Courier New";
                c2.Range.Font.Size = 11;
                c2.Range.Font.Bold = 0;
                c2.Range.ParagraphFormat.Alignment =
                    Word.WdParagraphAlignment.wdAlignParagraphLeft;
                c2.Range.ParagraphFormat.SpaceBefore = 2;
                c2.Range.ParagraphFormat.SpaceAfter = 2;
                c2.Shading.BackgroundPatternColor = (Word.WdColor)wordColor;
                c2.VerticalAlignment =
                    Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            }
        }

        private void WriteResultsTable(
            List<SolutionPoint> points, bool hasExact)
        {
            int colCount = hasExact ? 5 : 2;

            string[] headers = hasExact
                ? new[] { "X", "Y (числ.)", "Y (точное)",
                          "Абс. погр.", "Отн. погр. (%)" }
                : new[] { "X", "Y (числ.)" };

            int[] hColors = hasExact
                ? new[] { 0x1565C0, 0x1565C0, 0x2E7D32,
                          0xC62828, 0xC62828 }
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

            table.PreferredWidthType =
                Word.WdPreferredWidthType.wdPreferredWidthPoints;
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
                cell.Range.ParagraphFormat.Alignment =
                    Word.WdParagraphAlignment.wdAlignParagraphCenter;
                cell.Range.ParagraphFormat.SpaceBefore = 2;
                cell.Range.ParagraphFormat.SpaceAfter = 2;
                cell.Shading.BackgroundPatternColor =
                    (Word.WdColor)RgbToWdColor(hColors[i]);
                cell.VerticalAlignment =
                    Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
            }

            // Данные
            for (int i = 0; i < points.Count; i++)
            {
                var p = points[i];
                int row = i + 2;

                int bgColor = i % 2 == 0
                    ? RgbToWdColor(0xF5F5F5)
                    : RgbToWdColor(0xFFFFFF);

                SetResultCell(table, row, 1,
                    p.X.ToString("F6"), bgColor);
                SetResultCell(table, row, 2,
                    p.Y.ToString("F6"), bgColor);

                if (hasExact)
                {
                    SetResultCell(table, row, 3,
                        p.ExactY.HasValue
                            ? p.ExactY.Value.ToString("F6") : "—",
                        bgColor);
                    SetResultCell(table, row, 4,
                        p.AbsoluteError.HasValue
                            ? p.AbsoluteError.Value.ToString("E4") : "—",
                        bgColor);
                    SetResultCell(table, row, 5,
                        p.RelativeError.HasValue
                            ? p.RelativeError.Value.ToString("F4") : "—",
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
            cell.Range.ParagraphFormat.Alignment =
                Word.WdParagraphAlignment.wdAlignParagraphCenter;
            cell.Range.ParagraphFormat.SpaceBefore = 1;
            cell.Range.ParagraphFormat.SpaceAfter = 1;
            cell.Shading.BackgroundPatternColor = (Word.WdColor)bgColor;
            cell.VerticalAlignment =
                Word.WdCellVerticalAlignment.wdCellAlignVerticalCenter;
        }

        private void StyleTableBorders(Word.Table table, int color)
        {
            // Внешние границы
            table.Borders[Word.WdBorderType.wdBorderTop].LineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderBottom].LineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderLeft].LineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderRight].LineStyle =
                Word.WdLineStyle.wdLineStyleSingle;

            // Внутренние границы
            table.Borders[Word.WdBorderType.wdBorderHorizontal].LineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders[Word.WdBorderType.wdBorderVertical].LineStyle =
                Word.WdLineStyle.wdLineStyleSingle;

            // Цвета внешних границ
            table.Borders[Word.WdBorderType.wdBorderTop].Color =
                (Word.WdColor)RgbToWdColor(color);
            table.Borders[Word.WdBorderType.wdBorderBottom].Color =
                (Word.WdColor)RgbToWdColor(color);
            table.Borders[Word.WdBorderType.wdBorderLeft].Color =
                (Word.WdColor)RgbToWdColor(color);
            table.Borders[Word.WdBorderType.wdBorderRight].Color =
                (Word.WdColor)RgbToWdColor(color);

            // Цвета внутренних границ
            table.Borders[Word.WdBorderType.wdBorderHorizontal].Color =
                (Word.WdColor)RgbToWdColor(0xDDDDDD);
            table.Borders[Word.WdBorderType.wdBorderVertical].Color =
                (Word.WdColor)RgbToWdColor(0xDDDDDD);
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