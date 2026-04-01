using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using EulerSolver.Controls;
using EulerSolver.Models;

namespace EulerSolver.Views
{
    public partial class GraphWindow : Window
    {
        public GraphWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Показать график одного решения
        /// </summary>
        public void ShowSolution(SolverResult result)
        {
            titleText.Text = "График: " + result.EquationDescription;
            subtitleText.Text = "Отрезок [" + result.X0.ToString("F2") + "; " +
                                result.Xn.ToString("F2") + "], шаг h = " +
                                result.StepSize.ToString("G4");

            var lines = new List<PlotLineData>
            {
                new PlotLineData
                {
                    Points = result.Points,
                    Color = Colors.DodgerBlue,
                    Title = "Модифицированный Эйлер"
                }
            };

            graphControl.PlotMultiple(lines);
        }

        /// <summary>
        /// Показать несколько линий (для сравнения методов)
        /// </summary>
        public void ShowComparison(List<PlotLineData> lines, string title, string subtitle)
        {
            titleText.Text = title;
            subtitleText.Text = subtitle;
            graphControl.PlotMultiple(lines);
        }
    }
}