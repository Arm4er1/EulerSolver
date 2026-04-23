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

        public void ShowComparison(ComparisonResult result,
            SolverResult eulerResult,
            SolverResult modifiedResult,
            SolverResult exactResult)
        {
            // Заголовок
            titleText.Text = "Сравнение методов: " + result.EquationDescription;
            subtitleText.Text = "Отрезок [" + result.X0.ToString("F2") + "; " +
                                result.Xn.ToString("F2") + "], шаг h = " +
                                result.StepSize.ToString("G4");

            // Блоки с погрешностями
            if (result.MaxErrorEuler.HasValue)
                eulerErrorText.Text = "Макс. погрешность: " +
                                      result.MaxErrorEuler.Value.ToString("E4");
            else
                eulerErrorText.Text = "Макс. погрешность: —";

            if (result.MaxErrorModified.HasValue)
                modifiedErrorText.Text = "Макс. погрешность: " +
                                         result.MaxErrorModified.Value.ToString("E4");
            else
                modifiedErrorText.Text = "Макс. погрешность: —";

            conclusionText.Text = result.AccuracyComparison;

            // Таблица
            comparisonGrid.ItemsSource = result.Points;

            // Легенда точного решения
            if (exactResult != null)
                exactLegendText.Visibility = Visibility.Visible;
            else
            {
                exactLegendText.Visibility = Visibility.Collapsed;
                // Убираем кружок точного решения
            }

            // График
            var lines = new List<PlotLineData>();

            // Обычный Эйлер — красный
            lines.Add(new PlotLineData
            {
                Points = eulerResult.Points,
                Color = Color.FromRgb(229, 57, 53),
                Title = "Обычный Эйлер"
            });

            // Модифицированный — синий
            lines.Add(new PlotLineData
            {
                Points = modifiedResult.Points,
                Color = Color.FromRgb(30, 136, 229),
                Title = "Модифицированный Эйлер"
            });

            // Точное решение — зелёное (если есть)
            if (exactResult != null)
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