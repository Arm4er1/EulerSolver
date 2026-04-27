using System;
using System.Collections.Generic;

namespace EulerSolver.Core.Models
{
    /// <summary>
    /// Результат сравнения двух методов в одной точке x.
    /// </summary>
    public class ComparisonPoint
    {
        public double X { get; set; }
        public double YEuler { get; set; }
        public double YModified { get; set; }

        /// <summary>Точное значение — null если решение не задано</summary>
        public double? YExact { get; set; }

        /// <summary>Абсолютная погрешность обычного Эйлера</summary>
        public double? ErrorEuler =>
            YExact.HasValue ? Math.Abs(YEuler - YExact.Value) : null;

        /// <summary>Абсолютная погрешность модифицированного Эйлера</summary>
        public double? ErrorModified =>
            YExact.HasValue ? Math.Abs(YModified - YExact.Value) : null;

        /// <summary>
        /// Разница между двумя численными методами.
        /// Используется как оценка погрешности по Рунге
        /// когда точное решение неизвестно.
        /// </summary>
        public double Difference => Math.Abs(YEuler - YModified);

        // Форматированные строки для таблицы
        public string XFormatted => X.ToString("F4");
        public string YEulerFormatted => YEuler.ToString("F6");
        public string YModifiedFormatted => YModified.ToString("F6");
        public string YExactFormatted => YExact?.ToString("F6") ?? "—";
        public string ErrorEulerFormatted => ErrorEuler?.ToString("E4") ?? "—";
        public string ErrorModifiedFormatted => ErrorModified?.ToString("E4") ?? "—";
        public string DifferenceFormatted => Difference.ToString("E4");
    }

    /// <summary>
    /// Полный результат сравнения двух методов на всём отрезке.
    /// </summary>
    public class ComparisonResult
    {
        public List<ComparisonPoint> Points { get; set; } = new List<ComparisonPoint>();
        public string EquationDescription { get; set; } = string.Empty;
        public double StepSize { get; set; }
        public double X0 { get; set; }
        public double Xn { get; set; }

        /// <summary>Максимальная погрешность обычного Эйлера по всем точкам</summary>
        public double? MaxErrorEuler
        {
            get
            {
                double? max = null;
                foreach (var p in Points)
                    if (p.ErrorEuler.HasValue)
                        if (!max.HasValue || p.ErrorEuler.Value > max.Value)
                            max = p.ErrorEuler.Value;
                return max;
            }
        }

        /// <summary>Максимальная погрешность модифицированного Эйлера по всем точкам</summary>
        public double? MaxErrorModified
        {
            get
            {
                double? max = null;
                foreach (var p in Points)
                    if (p.ErrorModified.HasValue)
                        if (!max.HasValue || p.ErrorModified.Value > max.Value)
                            max = p.ErrorModified.Value;
                return max;
            }
        }

        /// <summary>
        /// Текстовый вывод о соотношении точности методов.
        /// Если точного решения нет — сообщает об этом.
        /// </summary>
        public string AccuracyComparison
        {
            get
            {
                if (!MaxErrorEuler.HasValue || !MaxErrorModified.HasValue)
                    return "Точное решение не задано";

                if (MaxErrorModified.Value < 1e-15)
                    return "Модифицированный метод значительно точнее";

                double ratio = MaxErrorEuler.Value / MaxErrorModified.Value;
                return "Модифицированный метод точнее в " +
                       ratio.ToString("F1") + " раз";
            }
        }
    }
}