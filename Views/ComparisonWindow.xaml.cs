using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using EulerSolver.Controls;
using EulerSolver.Models;

namespace EulerSolver.Views
{
    public partial class ComparisonWindow : Window
    {
        public ComparisonWindow()
        {
            InitializeComponent();
        }

        public void ShowComparison(
            ComparisonResult result,
            SolverResult eulerResult,
            SolverResult modifiedResult,
            SolverResult exactResult)
        {
            // Заголовок
            titleText.Text = "Сравнение методов: " + result.EquationDescription;
            subtitleText.Text =
                "Отрезок [" + result.X0.ToString("F2") + "; " +
                result.Xn.ToString("F2") + "], шаг h = " +
                result.StepSize.ToString("G4");

            bool hasExact = exactResult != null;

            // ── Блоки погрешностей ──────────────────────────────────────
            if (result.MaxErrorEuler.HasValue)
                eulerErrorText.Text =
                    "Макс. погрешность: " +
                    result.MaxErrorEuler.Value.ToString("E4");
            else
                eulerErrorText.Text = "Макс. погрешность: —";

            if (result.MaxErrorModified.HasValue)
                modifiedErrorText.Text =
                    "Макс. погрешность: " +
                    result.MaxErrorModified.Value.ToString("E4");
            else
                modifiedErrorText.Text = "Макс. погрешность: —";

            // ── Блок "Вывод" ─────────────────────────────────────────────
            if (hasExact)
            {
                // Есть точное решение — показываем во сколько раз точнее
                conclusionText.Text = result.AccuracyComparison;
            }
            else
            {
                // Нет точного решения — показываем макс. разницу между методами
                double maxDiff = 0;
                foreach (var p in result.Points)
                    if (p.Difference > maxDiff)
                        maxDiff = p.Difference;

                conclusionText.Text =
                    "Макс. разница\nмежду методами:\n" +
                    maxDiff.ToString("E4") + "\n" +
                    "(оценка по Рунге)";
            }

            // ── Подсказка о режиме без точного решения ──────────────────
            noExactHintBorder.Visibility =
                hasExact ? Visibility.Collapsed : Visibility.Visible;

            // ── Колонки таблицы ──────────────────────────────────────────
            // Если точного решения нет — скрываем три колонки
            var hiddenCols = Visibility.Collapsed;
            var visibleCols = Visibility.Visible;

            colErrorEuler.Visibility = hasExact ? visibleCols : hiddenCols;
            colErrorModified.Visibility = hasExact ? visibleCols : hiddenCols;
            colExact.Visibility = hasExact ? visibleCols : hiddenCols;

            // Колонка разницы — всегда видна, но меняем заголовок
            colDifference.Header = hasExact
                ? "|Δ| между методами"
                : "|Δ| между методами  (≈ погрешность)";

            // ── Таблица ──────────────────────────────────────────────────
            comparisonGrid.ItemsSource = result.Points;

            // ── Легенда точного решения ───────────────────────────────────
            exactLegendPanel.Visibility =
                hasExact ? Visibility.Visible : Visibility.Collapsed;

            // ── График ───────────────────────────────────────────────────
            var lines = new List<PlotLineData>
            {
                new PlotLineData
                {
                    Points = eulerResult.Points,
                    Color  = Color.FromRgb(229, 57, 53),
                    Title  = "Обычный Эйлер"
                },
                new PlotLineData
                {
                    Points = modifiedResult.Points,
                    Color  = Color.FromRgb(30, 136, 229),
                    Title  = "Модифицированный Эйлер"
                }
            };

            if (hasExact)
            {
                lines.Add(new PlotLineData
                {
                    Points = exactResult.Points,
                    Color = Color.FromRgb(67, 160, 71),
                    Title = "Точное решение"
                });
            }

            comparisonGraph.PlotMultiple(lines);
        }
    }
}