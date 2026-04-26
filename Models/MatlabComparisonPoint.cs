using System;

namespace EulerSolver.Models
{
    public class MatlabComparisonPoint
    {
        public double X { get; set; }
        public double YEuler { get; set; }
        public double YMatlab { get; set; }

        public double Difference => Math.Abs(YEuler - YMatlab);

        public string XFormatted => X.ToString("F6");
        public string YEulerFormatted => YEuler.ToString("F8");
        public string YMatlabFormatted => YMatlab.ToString("F8");
        public string DifferenceFormatted => Difference.ToString("E4");
    }
}