using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using EulerSolver.Controls;
using EulerSolver.Models;
using EulerSolver.Services;

namespace EulerSolver.Views
{
    public partial class MatlabComparisonWindow : Window
    {
        public MatlabComparisonWindow()
        {
            InitializeComponent();
        }

        public void ShowComparison(
            SolverResult eulerResult,
            MatlabResult matlabResult)
        {
            // Заголовок
            titleText.Text = "Сравнение с MATLAB: " +
                             eulerResult.EquationDescription;
            subtitleText.Text =
                "Отрезок [" + eulerResult.X0.ToString("F2") + "; " +
                eulerResult.Xn.ToString("F2") + "], шаг h = " +
                eulerResult.StepSize.ToString("G4");

            // Блоки с инфо
            eulerInfoText.Text =
                "Точек: " + eulerResult.Points.Count + "\n" +
                "Время: " + eulerResult.ElapsedMilliseconds.ToString("F3") + " мс";

            matlabInfoText.Text =
                "Точек: " + matlabResult.Points.Count + "\n" +
                "Решатель: ode45";

            // Строим таблицу сравнения
            var compPoints = BuildComparisonPoints(
                eulerResult.Points, matlabResult.Points);

            comparisonGrid.ItemsSource = compPoints;

            // Вывод
            if (compPoints.Count > 0)
            {
                double maxDiff = 0;
                foreach (var p in compPoints)
                    if (p.Difference > maxDiff)
                        maxDiff = p.Difference;

                conclusionText.Text =
                    "Макс. разница:\n" + maxDiff.ToString("E4");
            }

            // График
            var lines = new List<PlotLineData>
            {
                new PlotLineData
                {
                    Points = eulerResult.Points,
                    Color = Color.FromRgb(30, 136, 229),
                    Title = "Модифицированный Эйлер"
                },
                new PlotLineData
                {
                    Points = matlabResult.Points,
                    Color = Color.FromRgb(255, 112, 67),
                    Title = "MATLAB (ode45)"
                }
            };

            comparisonGraph.PlotMultiple(lines);
        }

        /// <summary>
        /// Сопоставляем точки двух методов по X
        /// </summary>
        private List<MatlabComparisonPoint> BuildComparisonPoints(
            List<SolutionPoint> eulerPoints,
            List<SolutionPoint> matlabPoints)
        {
            var result = new List<MatlabComparisonPoint>();

            foreach (var ep in eulerPoints)
            {
                // Ищем ближайшую точку MATLAB по X
                SolutionPoint closest = null;
                double minDist = double.MaxValue;

                foreach (var mp in matlabPoints)
                {
                    double dist = Math.Abs(mp.X - ep.X);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = mp;
                    }
                }

                if (closest != null && minDist < 1e-6)
                {
                    result.Add(new MatlabComparisonPoint
                    {
                        X = ep.X,
                        YEuler = ep.Y,
                        YMatlab = closest.Y
                    });
                }
            }

            return result;
        }
    }
}