using System;
using System.Collections.Generic;
using System.Diagnostics;
using EulerSolver.Models;

namespace EulerSolver.Services
{
    /// <summary>
    /// Обычный метод Эйлера (первый порядок точности)
    /// Формула: y_{n+1} = y_n + h * f(x_n, y_n)
    /// Порядок точности: O(h)
    /// </summary>
    public class EulerSolver
    {
        public SolverResult Solve(DifferentialEquation equation,
            double x0, double y0, double xn, double h)
        {
            if (h <= 0)
                throw new ArgumentException("Шаг должен быть положительным");
            if (xn <= x0)
                throw new ArgumentException("Xn должен быть больше X0");

            var stopwatch = Stopwatch.StartNew();

            var result = new SolverResult
            {
                EquationDescription = equation.Formula,
                StepSize = h,
                X0 = x0,
                Y0 = y0,
                Xn = xn
            };

            var f = equation.F;
            var points = new List<SolutionPoint>();

            double x = x0;
            double y = y0;
            double? exactY = equation.ExactSolution?.Invoke(x);
            points.Add(new SolutionPoint(x, y, exactY));

            int steps = (int)Math.Ceiling((xn - x0) / h);
            result.StepsCount = steps;

            for (int i = 0; i < steps; i++)
            {
                double currentH = Math.Min(h, xn - x);
                if (currentH <= 0) break;

                double xNext = x + currentH;

                // Обычный метод Эйлера — одна формула
                double yNext = y + currentH * f(x, y);

                x = xNext;
                y = yNext;

                exactY = equation.ExactSolution?.Invoke(x);
                points.Add(new SolutionPoint(x, y, exactY));
            }

            stopwatch.Stop();
            result.Points = points;
            result.ElapsedMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            return result;
        }
    }
}