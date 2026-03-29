using System;

namespace EulerSolver.Models
{
    public class DifferentialEquation
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string ExactFormula { get; set; } = string.Empty;
        public Func<double, double, double> F { get; set; } = (x, y) => 0;
        public Func<double, double>? ExactSolution { get; set; }
        public double DefaultX0 { get; set; } = 0;
        public double DefaultY0 { get; set; } = 1;
        public double DefaultXn { get; set; } = 2;
        public double DefaultStep { get; set; } = 0.1;

        public override string ToString() => Name;
    }
}