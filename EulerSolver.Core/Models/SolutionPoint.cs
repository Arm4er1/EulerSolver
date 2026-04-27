using System;

namespace EulerSolver.Core.Models
{
    /// <summary>
    /// Одна точка численного решения ОДУ.
    /// Хранит x, численное y, точное y (если известно)
    /// и вычисляет погрешности.
    /// </summary>
    public class SolutionPoint
    {
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>Точное значение y(x) — null если решение не задано</summary>
        public double? ExactY { get; set; }

        /// <summary>Абсолютная погрешность |y_числ - y_точн|</summary>
        public double? AbsoluteError =>
            ExactY.HasValue ? Math.Abs(Y - ExactY.Value) : null;

        /// <summary>
        /// Относительная погрешность в процентах.
        /// Не вычисляется если точное значение близко к нулю.
        /// </summary>
        public double? RelativeError
        {
            get
            {
                if (!ExactY.HasValue || Math.Abs(ExactY.Value) < 1e-15)
                    return null;
                return Math.Abs((Y - ExactY.Value) / ExactY.Value) * 100.0;
            }
        }

        public SolutionPoint(double x, double y, double? exactY = null)
        {
            X = x;
            Y = y;
            ExactY = exactY;
        }

        // Форматированные строки для отображения в таблице
        public string XFormatted => X.ToString("F6");
        public string YFormatted => Y.ToString("F6");
        public string ExactYFormatted => ExactY?.ToString("F6") ?? "—";
        public string AbsoluteErrorFormatted => AbsoluteError?.ToString("E4") ?? "—";
        public string RelativeErrorFormatted => RelativeError?.ToString("F4") ?? "—";
    }
}