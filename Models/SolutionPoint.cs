namespace EulerSolver.Models
{
    public class SolutionPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double? ExactY { get; set; }

        public double? AbsoluteError => ExactY.HasValue ? Math.Abs(Y - ExactY.Value) : null;

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

        public string XFormatted => X.ToString("F6");
        public string YFormatted => Y.ToString("F6");
        public string ExactYFormatted => ExactY?.ToString("F6") ?? "—";
        public string AbsoluteErrorFormatted => AbsoluteError?.ToString("E4") ?? "—";
        public string RelativeErrorFormatted => RelativeError?.ToString("F4") ?? "—";
    }
}