using System.Collections.Generic;

namespace EulerSolver.Core.Models
{
    /// <summary>
    /// Полный результат работы численного метода:
    /// набор точек, параметры задачи, статистика.
    /// </summary>
    public class SolverResult
    {
        public List<SolutionPoint> Points { get; set; } = new List<SolutionPoint>();

        /// <summary>Текстовое описание уравнения (y' = ...)</summary>
        public string EquationDescription { get; set; } = string.Empty;

        public double StepSize { get; set; }
        public int StepsCount { get; set; }
        public double X0 { get; set; }
        public double Y0 { get; set; }
        public double Xn { get; set; }
        public double ElapsedMilliseconds { get; set; }

        /// <summary>
        /// Максимальная абсолютная погрешность по всем точкам.
        /// Null если точное решение не задано.
        /// </summary>
        public double? MaxAbsoluteError
        {
            get
            {
                double? max = null;
                foreach (var p in Points)
                {
                    if (p.AbsoluteError.HasValue)
                    {
                        if (!max.HasValue || p.AbsoluteError.Value > max.Value)
                            max = p.AbsoluteError.Value;
                    }
                }
                return max;
            }
        }
    }
}