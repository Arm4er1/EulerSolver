using System;

namespace EulerSolver.Core.Models
{
    /// <summary>
    /// Описание задачи Коши: y' = F(x,y), y(x0) = y0.
    /// Опционально содержит аналитическое решение для проверки точности.
    /// </summary>
    public class DifferentialEquation
    {
        /// <summary>Краткое название уравнения</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Строка вида "y' = f(x,y)" для отображения</summary>
        public string Formula { get; set; } = string.Empty;

        /// <summary>Строка с формулой точного решения (для отображения)</summary>
        public string ExactFormula { get; set; } = string.Empty;

        /// <summary>Правая часть ОДУ: f(x, y)</summary>
        public Func<double, double, double> F { get; set; } = (x, y) => 0;

        /// <summary>
        /// Аналитическое решение y(x).
        /// Null если точное решение неизвестно.
        /// </summary>
        public Func<double, double>? ExactSolution { get; set; }

        // Параметры по умолчанию для данного уравнения
        public double DefaultX0 { get; set; } = 0;
        public double DefaultY0 { get; set; } = 1;
        public double DefaultXn { get; set; } = 2;
        public double DefaultStep { get; set; } = 0.1;

        public override string ToString() => Name;
    }
}