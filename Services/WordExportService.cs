using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using EulerSolver.Models;
using Word = Microsoft.Office.Interop.Word;

namespace EulerSolver.Services
{
    public class WordExportService
    {
        public void Export(SolverResult result)
        {
            Word.Application word = null;
            Word.Document doc = null;

            try
            {
                word = new Word.Application();
                word.Visible = false;

                doc = word.Documents.Add();

                // Настройки страницы
                doc.PageSetup.TopMargin = word.CentimetersToPoints(2);
                doc.PageSetup.BottomMargin = word.CentimetersToPoints(2);
                doc.PageSetup.LeftMargin = word.CentimetersToPoints(2.5f);
                doc.PageSetup.RightMargin = word.CentimetersToPoints(1.5f);

                Word.Paragraphs paragraphs = doc.Paragraphs;

                // ====== Заголовок документа ======
                AddTitle(doc, "Отчёт о решении ОДУ");

                // ====== Информация о задаче ======
                AddHeading(doc, "1. Постановка задачи");
                AddParagraph(doc,
                    "Решается задача Коши для обыкновенного дифференциального " +
                    "уравнения первого порядка:");
                AddFormula(doc, result.EquationDescription);
                AddParagraph(doc,
                    "Начальное условие: y(x₀) = " + result.Y0.ToString("G6") +
                    " при x₀ = " + result.X0.ToString("G6") + ".");
                AddParagraph(doc,
                    "Область интегрирования: [" +
                    result.X0.ToString("G6") + "; " +
                    result.Xn.ToString("G6") + "].");

                // ====== Описание метода ======
                AddHeading(doc, "2. Метод решения");
                AddParagraph(doc,
                    "Для решения задачи применяется модифицированный метод Эйлера " +
                    "(метод Эйлера-Коши). Метод имеет второй порядок точности O(h²).");
                AddParagraph(doc, "Формулы метода:");
                AddFormula(doc, "Предиктор:  ŷₙ₊₁ = yₙ + h · f(xₙ, yₙ)");
                AddFormula(doc,
                    "Корректор:  yₙ₊₁ = yₙ + (h/2) · [f(xₙ, yₙ) + f(xₙ₊₁, ŷₙ₊₁)]");

                // ====== Параметры вычисления ======
                AddHeading(doc, "3. Параметры вычисления");
                AddParamsTable(doc, result);

                // ====== Результаты ======
                AddHeading(doc, "4. Результаты вычислений");
                AddParagraph(doc,
                    "Таблица содержит " + result.Points.Count +
                    " точек решения с шагом h = " +
                    result.StepSize.ToString("G6") + ".");
                AddResultsTable(doc, result);

                // ====== Погрешность ======
                if (result.MaxAbsoluteError.HasValue)
                {
                    AddHeading(doc, "5. Анализ погрешности");
                    AddParagraph(doc,
                        "Максимальная абсолютная погрешность составила: " +
                        result.MaxAbsoluteError.Value.ToString("E4") + ".");
                    AddParagraph(doc,
                        "Погрешность соответствует теоретической оценке O(h²) " +
                        "для модифицированного метода Эйлера.");
                }

                // ====== Вывод ======
                AddHeading(doc,
                    result.MaxAbsoluteError.HasValue ? "6. Вывод" : "5. Вывод");
                AddParagraph(doc,
                    "Задача Коши успешно решена модифицированным методом Эйлера. " +
                    "Вычисления выполнены за " +
                    result.ElapsedMilliseconds.ToString("F3") +
                    " мс. Получено " + result.Points.Count + " точек решения.");

                // Показываем Word
                word.Visible = true;
                word.WindowState = Word.WdWindowState.wdWindowStateMaximize;
            }
            catch (Exception ex)
            {
                if (doc != null)
                {
                    doc.Close(false);
                    Marshal.ReleaseComObject(doc);
                }
                if (word != null)
                {
                    word.Quit();
                    Marshal.ReleaseComObject(word);
                }

                throw new Exception("Ошибка при экспорте в Word:\n" + ex.Message);
            }
        }

        #region Вспомогательные методы

        private void AddTitle(Word.Document doc, string text)
        {
            Word.Paragraph para = doc.Paragraphs.Add();
            para.Range.Text = text;
            para.Range.Font.Size = 18;
            para.Range.Font.Bold = 1;
            para.Range.Font.Color =
                (Word.WdColor)RgbToWordColor(0x1976D2);
            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            para.SpaceAfter = 12;
            para.Range.InsertParagraphAfter();
        }

        private void AddHeading(Word.Document doc, string text)
        {
            Word.Paragraph para = doc.Paragraphs.Add();
            para.Range.Text = text;
            para.Range.Font.Size = 14;
            para.Range.Font.Bold = 1;
            para.Range.Font.Color =
                (Word.WdColor)RgbToWordColor(0x1565C0);
            para.SpaceBefore = 12;
            para.SpaceAfter = 6;
            para.Range.InsertParagraphAfter();
        }

        private void AddParagraph(Word.Document doc, string text)
        {
            Word.Paragraph para = doc.Paragraphs.Add();
            para.Range.Text = text;
            para.Range.Font.Size = 12;
            para.Range.Font.Bold = 0;
            para.Range.Font.Color = Word.WdColor.wdColorAutomatic;
            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphJustify;
            para.FirstLineIndent = doc.Application.CentimetersToPoints(1.25f);
            para.SpaceAfter = 6;
            para.Range.InsertParagraphAfter();
        }

        private void AddFormula(Word.Document doc, string text)
        {
            Word.Paragraph para = doc.Paragraphs.Add();
            para.Range.Text = text;
            para.Range.Font.Size = 12;
            para.Range.Font.Name = "Courier New";
            para.Range.Font.Bold = 0;
            para.Range.Font.Color =
                (Word.WdColor)RgbToWordColor(0x1A237E);
            para.Alignment = Word.WdParagraphAlignment.wdAlignParagraphCenter;
            para.SpaceBefore = 4;
            para.SpaceAfter = 4;
            para.Range.InsertParagraphAfter();
        }

        private void AddParamsTable(Word.Document doc, SolverResult result)
        {
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
                ("Время",            result.ElapsedMilliseconds.ToString("F3") + " мс"),
            };

            if (result.MaxAbsoluteError.HasValue)
                rows.Add(("Макс. погрешность",
                          result.MaxAbsoluteError.Value.ToString("E4")));

            // Добавляем пустой параграф перед таблицей
            Word.Paragraph spacePara = doc.Paragraphs.Add();
            spacePara.Range.Text = "";
            spacePara.Range.InsertParagraphAfter();

            // Создаём таблицу
            Word.Range tableRange = doc.Paragraphs.Last.Range;
            Word.Table table = doc.Tables.Add(
                tableRange, rows.Count, 2,
                Word.WdDefaultTableBehavior.wdWord9TableBehavior,
                Word.WdAutoFitBehavior.wdAutoFitFixed);

            table.PreferredWidthType =
                Word.WdPreferredWidthType.wdPreferredWidthPercent;
            table.PreferredWidth = 80;

            // Стиль таблицы
            table.Borders.InsideLineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.OutsideLineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.InsideColor =
                (Word.WdColor)RgbToWordColor(0xDDDDDD);
            table.Borders.OutsideColor =
                (Word.WdColor)RgbToWordColor(0x1565C0);

            // Заполняем
            for (int i = 0; i < rows.Count; i++)
            {
                Word.Cell labelCell = table.Cell(i + 1, 1);
                labelCell.Range.Text = rows[i].Item1;
                labelCell.Range.Font.Bold = 1;
                labelCell.Range.Font.Size = 11;
                labelCell.Shading.BackgroundPatternColor =
                    i % 2 == 0
                        ? (Word.WdColor)RgbToWordColor(0xE3F2FD)
                        : (Word.WdColor)RgbToWordColor(0xFFFFFF);

                Word.Cell valueCell = table.Cell(i + 1, 2);
                valueCell.Range.Text = rows[i].Item2;
                valueCell.Range.Font.Bold = 0;
                valueCell.Range.Font.Size = 11;
                valueCell.Range.Font.Name = "Courier New";
                valueCell.Shading.BackgroundPatternColor =
                    labelCell.Shading.BackgroundPatternColor;
            }

            // Ширина столбцов
            table.Columns[1].Width = doc.Application.CentimetersToPoints(6);
            table.Columns[2].Width = doc.Application.CentimetersToPoints(10);

            // Параграф после таблицы
            Word.Paragraph afterPara = doc.Paragraphs.Add();
            afterPara.Range.Text = "";
            afterPara.Range.InsertParagraphAfter();
        }

        private void AddResultsTable(Word.Document doc, SolverResult result)
        {
            bool hasExact = result.Points.Count > 0 &&
                            result.Points[0].ExactY.HasValue;

            int colCount = hasExact ? 5 : 2;

            // Ограничиваем количество строк в таблице
            // (слишком большие таблицы в Word неудобны)
            int maxRows = 50;
            int step = result.Points.Count <= maxRows
                ? 1
                : result.Points.Count / maxRows;

            var displayPoints = new List<SolutionPoint>();
            for (int i = 0; i < result.Points.Count; i += step)
                displayPoints.Add(result.Points[i]);

            // Последнюю точку всегда включаем
            if (displayPoints[displayPoints.Count - 1] !=
                result.Points[result.Points.Count - 1])
                displayPoints.Add(result.Points[result.Points.Count - 1]);

            if (step > 1)
                AddParagraph(doc,
                    "Примечание: показана каждая " + step +
                    "-я точка из " + result.Points.Count + " вычисленных.");

            // Пустой параграф перед таблицей
            Word.Paragraph spacePara = doc.Paragraphs.Add();
            spacePara.Range.Text = "";
            spacePara.Range.InsertParagraphAfter();

            Word.Range tableRange = doc.Paragraphs.Last.Range;
            Word.Table table = doc.Tables.Add(
                tableRange,
                displayPoints.Count + 1,  // +1 для заголовка
                colCount,
                Word.WdDefaultTableBehavior.wdWord9TableBehavior,
                Word.WdAutoFitBehavior.wdAutoFitFixed);

            table.PreferredWidthType =
                Word.WdPreferredWidthType.wdPreferredWidthPercent;
            table.PreferredWidth = 100;

            // Границы
            table.Borders.InsideLineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.OutsideLineStyle =
                Word.WdLineStyle.wdLineStyleSingle;
            table.Borders.InsideColor =
                (Word.WdColor)RgbToWordColor(0xDDDDDD);

            // Заголовки столбцов
            string[] headers = hasExact
                ? new[] { "X", "Y (числ.)", "Y (точное)", "Абс. погр.", "Отн. погр. (%)" }
                : new[] { "X", "Y (числ.)" };

            int[] headerColors = hasExact
                ? new[] { 0x1565C0, 0x1565C0, 0x2E7D32, 0xC62828, 0xC62828 }
                : new[] { 0x1565C0, 0x1565C0 };

            for (int i = 0; i < colCount; i++)
            {
                Word.Cell cell = table.Cell(1, i + 1);
                cell.Range.Text = headers[i];
                cell.Range.Font.Bold = 1;
                cell.Range.Font.Size = 10;
                cell.Range.Font.Color = Word.WdColor.wdColorWhite;
                cell.Range.ParagraphFormat.Alignment =
                    Word.WdParagraphAlignment.wdAlignParagraphCenter;
                cell.Shading.BackgroundPatternColor =
                    (Word.WdColor)RgbToWordColor(headerColors[i]);
            }

            // Данные
            for (int i = 0; i < displayPoints.Count; i++)
            {
                var p = displayPoints[i];
                int row = i + 2;

                Word.WdColor rowColor = i % 2 == 0
                    ? (Word.WdColor)RgbToWordColor(0xF8F9FA)
                    : Word.WdColor.wdColorWhite;

                SetCell(table, row, 1, p.X.ToString("F6"), rowColor);
                SetCell(table, row, 2, p.Y.ToString("F6"), rowColor);

                if (hasExact)
                {
                    SetCell(table, row, 3,
                        p.ExactY.HasValue
                            ? p.ExactY.Value.ToString("F6")
                            : "—",
                        rowColor);
                    SetCell(table, row, 4,
                        p.AbsoluteError.HasValue
                            ? p.AbsoluteError.Value.ToString("E4")
                            : "—",
                        rowColor);
                    SetCell(table, row, 5,
                        p.RelativeError.HasValue
                            ? p.RelativeError.Value.ToString("F4")
                            : "—",
                        rowColor);
                }
            }

            // Ширина столбцов
            double pageWidth = 16.0; // см (A4 - поля)
            double colWidth = pageWidth / colCount;
            for (int i = 1; i <= colCount; i++)
                table.Columns[i].Width =
                    doc.Application.CentimetersToPoints((float)colWidth);

            // Параграф после таблицы
            Word.Paragraph afterPara = doc.Paragraphs.Add();
            afterPara.Range.Text = "";
            afterPara.Range.InsertParagraphAfter();
        }

        private void SetCell(Word.Table table, int row, int col,
                            string text, Word.WdColor bgColor)
        {
            Word.Cell cell = table.Cell(row, col);
            cell.Range.Text = text;
            cell.Range.Font.Size = 10;
            cell.Range.Font.Bold = 0;
            cell.Range.Font.Name = "Courier New";
            cell.Range.ParagraphFormat.Alignment =
                Word.WdParagraphAlignment.wdAlignParagraphCenter;
            cell.Shading.BackgroundPatternColor = bgColor;
        }

        /// <summary>
        /// Конвертирует RGB в формат Word WdColor
        /// </summary>
        private int RgbToWordColor(int rgb)
        {
            int r = (rgb >> 16) & 0xFF;
            int g = (rgb >> 8) & 0xFF;
            int b = rgb & 0xFF;
            return b << 16 | g << 8 | r;
        }

        #endregion
    }
}