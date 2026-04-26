using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EulerSolver.Models;
using EulerSolver.Services;

namespace EulerSolver.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly ModifiedEulerSolver _solver = new ModifiedEulerSolver();
        private readonly Services.EulerSolver _eulerSolver = new Services.EulerSolver();
        private readonly ExpressionParser _parser = new ExpressionParser();
        private bool _isSettingFromExample;

        private readonly Dictionary<string, KnownSolution> _knownSolutions =
            new Dictionary<string, KnownSolution>
            {
                ["y"] = new KnownSolution("exp(x)", 1),
                ["-y"] = new KnownSolution("exp(-x)", 1),
                ["x+y"] = new KnownSolution("2*exp(x)-x-1", 1),
                ["y-x^2+1"] = new KnownSolution("(x+1)^2-0.5*exp(x)", 0.5),
                ["x*y"] = new KnownSolution("exp(x^2/2)", 1),
                ["sin(x)+y"] = new KnownSolution("(exp(x)-sin(x)-cos(x))/2", 0),
                ["2*x"] = new KnownSolution("x^2", 0),
                ["x^2+y"] = new KnownSolution("3*exp(x)-x^2-2*x-2", 1),
                ["-2*x*y"] = new KnownSolution("exp(-x^2)", 1),
                ["y/x"] = new KnownSolution("x", 1),
                ["x-y"] = new KnownSolution("x-1+2*exp(-x)", 1),
                ["cos(x)"] = new KnownSolution("sin(x)", 0),
                ["-sin(x)"] = new KnownSolution("cos(x)", 1),
                ["x^2"] = new KnownSolution("x^3/3", 0),
                ["1+y^2"] = new KnownSolution("tan(x)", 0),
            };

        #region Ввод уравнения

        private string _equationText = "x + y";
        public string EquationText
        {
            get { return _equationText; }
            set
            {
                if (SetProperty(ref _equationText, value))
                {
                    ValidateEquation();
                    if (!_isSettingFromExample)
                    {
                        SelectedExample = null;
                        TryAutoFillExactSolution();
                    }
                }
            }
        }

        private string _exactSolutionText = "";
        public string ExactSolutionText
        {
            get { return _exactSolutionText; }
            set
            {
                if (SetProperty(ref _exactSolutionText, value))
                    ValidateExactSolution();
            }
        }

        private string _equationError = "";
        public string EquationError
        {
            get { return _equationError; }
            set { SetProperty(ref _equationError, value); }
        }

        private string _exactSolutionError = "";
        public string ExactSolutionError
        {
            get { return _exactSolutionError; }
            set { SetProperty(ref _exactSolutionError, value); }
        }

        private bool _isEquationValid = true;
        public bool IsEquationValid
        {
            get { return _isEquationValid; }
            set { SetProperty(ref _isEquationValid, value); }
        }

        private string _exactSolutionHint = "";
        public string ExactSolutionHint
        {
            get { return _exactSolutionHint; }
            set { SetProperty(ref _exactSolutionHint, value); }
        }

        #endregion

        #region Примеры

        public List<EquationExample> Examples { get; } = new List<EquationExample>
        {
            new EquationExample("y' = y",           "y",           "exp(x)",                        0, 1,   2, 0.1),
            new EquationExample("y' = -y",          "-y",          "exp(-x)",                       0, 1,   3, 0.1),
            new EquationExample("y' = x + y",       "x + y",       "2*exp(x) - x - 1",             0, 1,   2, 0.1),
            new EquationExample("y' = y - x² + 1",  "y - x^2 + 1","(x+1)^2 - 0.5*exp(x)",         0, 0.5, 2, 0.1),
            new EquationExample("y' = x·y",         "x*y",         "exp(x^2/2)",                   0, 1,   2, 0.1),
            new EquationExample("y' = sin(x) + y",  "sin(x) + y", "(exp(x) - sin(x) - cos(x))/2", 0, 0,   3, 0.1),
            new EquationExample("y' = 2x",          "2*x",         "x^2",                          0, 0,   5, 0.5),
            new EquationExample("y' = x² - y",      "x^2 - y",    "",                             0, 1,   3, 0.1),
        };

        private EquationExample _selectedExample;
        public EquationExample SelectedExample
        {
            get { return _selectedExample; }
            set
            {
                if (SetProperty(ref _selectedExample, value) && value != null)
                {
                    _isSettingFromExample = true;

                    EquationText = value.FunctionText;
                    ExactSolutionText = value.ExactText;
                    X0 = value.X0;
                    Y0 = value.Y0;
                    Xn = value.Xn;
                    StepH = value.Step;

                    ExactSolutionHint = !string.IsNullOrWhiteSpace(value.ExactText)
                        ? "из примера"
                        : "";

                    _isSettingFromExample = false;
                }
            }
        }

        #endregion

        #region Параметры

        private double _x0 = 0;
        public double X0
        {
            get { return _x0; }
            set { SetProperty(ref _x0, value); }
        }

        private double _y0 = 1;
        public double Y0
        {
            get { return _y0; }
            set { SetProperty(ref _y0, value); }
        }

        private double _xn = 2;
        public double Xn
        {
            get { return _xn; }
            set { SetProperty(ref _xn, value); }
        }

        private double _stepH = 0.1;
        public double StepH
        {
            get { return _stepH; }
            set { SetProperty(ref _stepH, value); }
        }

        private bool _useRungeControl;
        public bool UseRungeControl
        {
            get { return _useRungeControl; }
            set { SetProperty(ref _useRungeControl, value); }
        }

        private double _epsilon = 1e-6;
        public double Epsilon
        {
            get { return _epsilon; }
            set { SetProperty(ref _epsilon, value); }
        }

        #endregion

        #region Результаты

        private ObservableCollection<SolutionPoint> _resultPoints =
            new ObservableCollection<SolutionPoint>();
        public ObservableCollection<SolutionPoint> ResultPoints
        {
            get { return _resultPoints; }
            set { SetProperty(ref _resultPoints, value); }
        }

        private string _statusText = "Готов к вычислению";
        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        private string _resultSummary = "";
        public string ResultSummary
        {
            get { return _resultSummary; }
            set { SetProperty(ref _resultSummary, value); }
        }

        private bool _hasResults;
        public bool HasResults
        {
            get { return _hasResults; }
            set { SetProperty(ref _hasResults, value); }
        }

        private SolverResult _lastResult;
        public SolverResult LastResult
        {
            get { return _lastResult; }
            set { SetProperty(ref _lastResult, value); }
        }

        #endregion

        #region Команды

        public ICommand SolveCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand ShowGraphCommand { get; }
        public ICommand CompareCommand { get; }
        public ICommand MatlabCommand { get; }

        #endregion

        public MainViewModel()
        {
            SolveCommand = new RelayCommand(ExecuteSolve, CanSolve);
            ClearCommand = new RelayCommand(ExecuteClear);
            ExportCommand = new RelayCommand(ExecuteExport, () => HasResults);
            ShowGraphCommand = new RelayCommand(ExecuteShowGraph, () => HasResults);
            CompareCommand = new RelayCommand(ExecuteCompare, () => HasResults);
            MatlabCommand = new RelayCommand(ExecuteMatlab, () => HasResults);

            ValidateEquation();
            TryAutoFillExactSolution();
        }

        #region Автоподстановка точного решения

        private string Normalize(string s)
        {
            return s.Replace(" ", "").Replace(",", ".").ToLower();
        }

        private void TryAutoFillExactSolution()
        {
            if (string.IsNullOrWhiteSpace(EquationText))
            {
                ExactSolutionText = "";
                ExactSolutionHint = "";
                return;
            }

            string normalized = Normalize(EquationText);

            KnownSolution known;
            if (_knownSolutions.TryGetValue(normalized, out known))
            {
                ExactSolutionText = known.Formula;
                ExactSolutionHint = "найдено автоматически ✔";
            }
            else
            {
                ExactSolutionText = "";
                ExactSolutionHint = "";
            }
        }

        #endregion

        #region Валидация

        private void ValidateEquation()
        {
            if (string.IsNullOrWhiteSpace(EquationText))
            {
                EquationError = "Введите f(x, y)";
                IsEquationValid = false;
                return;
            }

            string error;
            if (_parser.TryParse(EquationText, out error))
            {
                EquationError = "";
                IsEquationValid = true;
            }
            else
            {
                EquationError = error;
                IsEquationValid = false;
            }
        }

        private void ValidateExactSolution()
        {
            if (string.IsNullOrWhiteSpace(ExactSolutionText))
            {
                ExactSolutionError = "";
                return;
            }

            string error;
            if (_parser.TryParse(ExactSolutionText, out error))
                ExactSolutionError = "";
            else
                ExactSolutionError = error;
        }

        #endregion

        #region Вспомогательный метод построения уравнения

        private DifferentialEquation BuildEquation(Func<double, double> exactSolution)
        {
            var f = _parser.Parse(EquationText);
            return new DifferentialEquation
            {
                Name = "y' = " + EquationText,
                Formula = "y' = " + EquationText,
                F = f,
                ExactSolution = exactSolution
            };
        }

        private Func<double, double> ParseExactSolution()
        {
            if (string.IsNullOrWhiteSpace(ExactSolutionText))
                return null;

            try
            {
                var exactFunc = _parser.Parse(ExactSolutionText);
                return x => exactFunc(x, 0);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        private bool CanSolve()
        {
            return IsEquationValid && StepH > 0 && Xn > X0;
        }

        #region Выполнение команд

        private void ExecuteSolve()
        {
            try
            {
                var exactSolution = ParseExactSolution();
                var equation = BuildEquation(exactSolution);

                SolverResult result;
                if (UseRungeControl)
                    result = _solver.SolveWithRungeControl(
                        equation, X0, Y0, Xn, StepH, Epsilon);
                else
                    result = _solver.Solve(equation, X0, Y0, Xn, StepH);

                ResultPoints = new ObservableCollection<SolutionPoint>(result.Points);
                LastResult = result;
                HasResults = true;

                string summary =
                    "Уравнение: " + result.EquationDescription + "\n" +
                    "Метод: Модифицированный Эйлер (Эйлер-Коши)\n" +
                    "Отрезок: [" + result.X0.ToString("F4") + "; " +
                                   result.Xn.ToString("F4") + "]\n" +
                    "Шаг: h = " + result.StepSize.ToString("G6") + "\n" +
                    "Кол-во шагов: " + result.StepsCount + "\n" +
                    "Кол-во точек: " + result.Points.Count + "\n" +
                    "Время: " + result.ElapsedMilliseconds.ToString("F3") + " мс";

                if (result.MaxAbsoluteError.HasValue)
                    summary += "\nМакс. абс. погрешность: " +
                               result.MaxAbsoluteError.Value.ToString("E4");

                ResultSummary = summary;
                StatusText = "✔ Решение получено за " +
                             result.ElapsedMilliseconds.ToString("F3") +
                             " мс (" + result.Points.Count + " точек)";
            }
            catch (Exception ex)
            {
                StatusText = "✘ Ошибка: " + ex.Message;
                MessageBox.Show("Ошибка при вычислении:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteShowGraph()
        {
            if (LastResult == null) return;

            var window = new Views.GraphWindow();
            window.Owner = GetMainWindow();
            window.ShowSolution(LastResult);
            window.Show();
        }

        private async void ExecuteMatlab()
        {
            if (LastResult == null) return;

            try
            {
                StatusText = "⏳ Запускаю MATLAB...";

                var service = new Services.MatlabService();
                var matlabResult = await service.SolveAsync(
                    EquationText, X0, Y0, Xn, StepH);

                StatusText = "✔ MATLAB завершил вычисления";

                var window = new Views.MatlabComparisonWindow();
                window.Owner = GetMainWindow();
                window.ShowComparison(LastResult, matlabResult);
                window.Show();
            }
            catch (Exception ex)
            {
                StatusText = "✘ Ошибка MATLAB: " + ex.Message;
                MessageBox.Show(
                    "Ошибка при работе с MATLAB:\n\n" + ex.Message +
                    "\n\nПроверьте:\n" +
                    "1. MATLAB установлен по пути D:\\Dowloads\\MATLAB\\bin\\matlab.exe\n" +
                    "2. Уравнение корректно\n" +
                    "3. Параметры задачи верны",
                    "Ошибка MATLAB",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ExecuteCompare()
        {
            if (LastResult == null) return;

            try
            {
                var exactSolution = ParseExactSolution();
                var equation = BuildEquation(exactSolution);

                // Решаем обоими методами
                var eulerResult = _eulerSolver.Solve(equation, X0, Y0, Xn, StepH);
                var modifiedResult = _solver.Solve(equation, X0, Y0, Xn, StepH);

                // Точное решение как отдельный набор точек
                SolverResult exactResult = null;
                if (exactSolution != null)
                {
                    var exactPoints = new List<SolutionPoint>();
                    foreach (var p in modifiedResult.Points)
                        exactPoints.Add(new SolutionPoint(p.X, exactSolution(p.X)));

                    exactResult = new SolverResult
                    {
                        Points = exactPoints,
                        EquationDescription = equation.Formula,
                        StepSize = StepH,
                        X0 = X0,
                        Xn = Xn
                    };
                }

                // Строим таблицу сравнения
                var compPoints = new List<ComparisonPoint>();
                int count = Math.Min(
                    eulerResult.Points.Count,
                    modifiedResult.Points.Count);

                for (int i = 0; i < count; i++)
                {
                    var ep = eulerResult.Points[i];
                    var mp = modifiedResult.Points[i];

                    compPoints.Add(new ComparisonPoint
                    {
                        X = ep.X,
                        YEuler = ep.Y,
                        YModified = mp.Y,
                        YExact = exactSolution?.Invoke(ep.X)
                    });
                }

                var compResult = new ComparisonResult
                {
                    Points = compPoints,
                    EquationDescription = equation.Formula,
                    StepSize = StepH,
                    X0 = X0,
                    Xn = Xn
                };

                var window = new Views.ComparisonWindow();
                window.Owner = GetMainWindow();
                window.ShowComparison(compResult, eulerResult, modifiedResult, exactResult);
                window.Show();

                StatusText = "✔ Сравнение выполнено";
            }
            catch (Exception ex)
            {
                StatusText = "✘ Ошибка: " + ex.Message;
                MessageBox.Show("Ошибка при сравнении:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExecuteClear()
        {
            ResultPoints.Clear();
            HasResults = false;
            LastResult = null;
            ResultSummary = "";
            StatusText = "Результаты очищены";
        }

        private void ExecuteExport()
        {
            try
            {
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV файлы (*.csv)|*.csv|Текстовые файлы (*.txt)|*.txt",
                    DefaultExt = ".csv",
                    FileName = "ode_solution"
                };

                if (dialog.ShowDialog() == true)
                {
                    var lines = new List<string>();
                    lines.Add("X;Y (числ.);Y (точн.);Абс. погр.;Отн. погр. (%)");

                    foreach (var p in ResultPoints)
                    {
                        lines.Add(
                            p.X.ToString("F8") + ";" +
                            p.Y.ToString("F8") + ";" +
                            p.ExactYFormatted + ";" +
                            p.AbsoluteErrorFormatted + ";" +
                            p.RelativeErrorFormatted);
                    }

                    System.IO.File.WriteAllLines(
                        dialog.FileName, lines, System.Text.Encoding.UTF8);

                    StatusText = "✔ Экспортировано в " + dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при экспорте:\n" + ex.Message,
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Window GetMainWindow()
        {
            // Ищем первое видимое окно типа MainWindow
            foreach (Window w in Application.Current.Windows)
            {
                if (w is Views.MainWindow && w.IsVisible)
                    return w;
            }
            // Запасной вариант
            return Application.Current.MainWindow;
        }

        #endregion
    }

    public class KnownSolution
    {
        public string Formula { get; private set; }
        public double DefaultY0 { get; private set; }

        public KnownSolution(string formula, double defaultY0)
        {
            Formula = formula;
            DefaultY0 = defaultY0;
        }
    }

    public class EquationExample
    {
        public string DisplayName { get; private set; }
        public string FunctionText { get; private set; }
        public string ExactText { get; private set; }
        public double X0 { get; private set; }
        public double Y0 { get; private set; }
        public double Xn { get; private set; }
        public double Step { get; private set; }

        public EquationExample(string displayName, string functionText, string exactText,
            double x0, double y0, double xn, double step)
        {
            DisplayName = displayName;
            FunctionText = functionText;
            ExactText = exactText;
            X0 = x0;
            Y0 = y0;
            Xn = xn;
            Step = step;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}