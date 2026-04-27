using System;

namespace EulerSolver.Core.Models
{
    /// <summary>
    /// Точка сравнения нашего метода с результатом MATLAB (ode45).
    /// </summary>
    public class MatlabComparisonPoint
    {
        public double X { get; set; }
        public double YEuler { get; set; }  // наш модифицированный Эйлер
        public double YMatlab { get; set; }  // MATLAB ode45

        /// <summary>Абсолютная разница между методами</summary>
        public double Difference => Math.Abs(YEuler - YMatlab);

        public string XFormatted => X.ToString("F6");
        public string YEulerFormatted => YEuler.ToString("F8");
        public string YMatlabFormatted => YMatlab.ToString("F8");
        public string DifferenceFormatted => Difference.ToString("E4");
    }
}