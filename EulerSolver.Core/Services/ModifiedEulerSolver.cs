using System;
using System.Collections.Generic;
using System.Diagnostics;
using EulerSolver.Core.Models;

namespace EulerSolver.Core.Services
{
    /// <summary>
    /// Модифицированный метод Эйлера (метод Эйлера-Коши).
    /// Порядок точности O(h²).
    ///
    /// Предиктор:  ŷₙ₊₁ = yₙ + h·f(xₙ, yₙ)
    /// Корректор:  yₙ₊₁ = yₙ + (h/2)·[f(xₙ,yₙ) + f(xₙ₊₁,ŷₙ₊₁)]
    /// </summary>
    public class ModifiedEulerSolver
    {
        /// <summary>
        /// Решает задачу Коши с фиксированным шагом h.
        /// </summary>
        public SolverResult Solve(
            DifferentialEquation equation,
            double x0, double y0, double xn, double h)
        {
            if (h <= 0)
                throw new ArgumentException("Шаг должен быть положительным", nameof(h));
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

                // Предиктор — обычный шаг Эйлера
                double fCurrent = f(x, y);
                double yPredictor = y + currentH * fCurrent;

                // Корректор — уточняем через среднее наклонов
                double fPredictor = f(xNext, yPredictor);
                double yNext = y + (currentH / 2.0) * (fCurrent + fPredictor);

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

        /// <summary>
        /// Решает с автоматическим контролем точности по правилу Рунге.
        /// Если погрешность превышает epsilon — уменьшает шаг вдвое.
        /// </summary>
        public SolverResult SolveWithRungeControl(
            DifferentialEquation equation,
            double x0, double y0, double xn,
            double h, double epsilon)
        {
            var result1 = Solve(equation, x0, y0, xn, h);
            var result2 = Solve(equation, x0, y0, xn, h / 2.0);

            if (result1.Points.Count > 0 && result2.Points.Count > 0)
            {
                double yH = result1.Points[^1].Y;
                double yH2 = result2.Points[^1].Y;

                // Оценка погрешности по правилу Рунге для метода 2-го порядка
                double rungeError = Math.Abs(yH - yH2) / 3.0;

                if (rungeError > epsilon)
                    return SolveWithRungeControl(
                        equation, x0, y0, xn, h / 2.0, epsilon);
            }

            return result2;
        }
    }
}