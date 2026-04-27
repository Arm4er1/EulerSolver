using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using EulerSolver.Core.Models;
using EulerSolver.Core.Services;


namespace EulerSolver.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        // Солверы и парсер
        private readonly EulerSolver.Core.Services.ModifiedEulerSolver _solver =
            new EulerSolver.Core.Services.ModifiedEulerSolver();
        private readonly EulerSolver.Core.Services.EulerSolver _eulerSolver =
            new EulerSolver.Core.Services.EulerSolver();
        private readonly EulerSolver.Core.Services.ExpressionParser _parser =
            new EulerSolver.Core.Services.ExpressionParser();

        // Флаг: не сбрасывать SelectedExample когда мы сами меняем поля
        private bool _isSettingFromExample;

        // Словарь известных точных решений
        // Ключ — нормализованная строка f(x,y), значение — формула y(x) и y0
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

        /// <summary>Правая часть ОДУ: y' = f(x, y)</summary>
        public string EquationText
        {
            get { return _equationText; }
            set
            {
                if (SetProperty(ref _equationText, value))
                {
                    ValidateEquation();

                    // Автоподстановка точного решения только при ручном вводе
                    if (!_isSettingFromExample)
                    {
                        SelectedExample = null;
                        TryAutoFillExactSolution();
                    }
                }
            }
        }

        private string _exactSolutionText = "";

        /// <summary>Аналитическое решение y(x) — необязательное поле</summary>
        public string ExactSolutionText
        {
            get { return _exactSolutionText; }
            set
            {
                if (SetProperty(ref _exactSolutionText, value))
                    ValidateExactSolution();
            }
        }

        /// <summary>Сообщение об ошибке синтаксиса уравнения</summary>
        private string _equationError = "";
        public string EquationError
        {
            get { return _equationError; }
            set { SetProperty(ref _equationError, value); }
        }

        /// <summary>Сообщение об ошибке синтаксиса точного решения</summary>
        private string _exactSolutionError = "";
        public string ExactSolutionError
        {
            get { return _exactSolutionError; }
            set { SetProperty(ref _exactSolutionError, value); }
        }

        /// <summary>True — уравнение прошло валидацию парсером</summary>
        private bool _isEquationValid = true;
        public bool IsEquationValid
        {
            get { return _isEquationValid; }
            set { SetProperty(ref _isEquationValid, value); }
        }

        /// <summary>Подсказка откуда взято точное решение (авто / из примера)</summary>
        private string _exactSolutionHint = "";
        public string ExactSolutionHint
        {
            get { return _exactSolutionHint; }
            set { SetProperty(ref _exactSolutionHint, value); }
        }

        #endregion

        #region Примеры

        /// <summary>Список готовых примеров для ComboBox</summary>
        public List<EquationExample> Examples { get; } = new List<EquationExample>
        {
            new EquationExample("y' = y",          "y",           "exp(x)",                        0, 1,   2, 0.1),
            new EquationExample("y' = -y",         "-y",          "exp(-x)",                       0, 1,   3, 0.1),
            new EquationExample("y' = x + y",      "x + y",       "2*exp(x) - x - 1",             0, 1,   2, 0.1),
            new EquationExample("y' = y - x² + 1", "y - x^2 + 1", "(x+1)^2 - 0.5*exp(x)",        0, 0.5, 2, 0.1),
            new EquationExample("y' = x·y",        "x*y",         "exp(x^2/2)",                   0, 1,   2, 0.1),
            new EquationExample("y' = sin(x) + y", "sin(x) + y",  "(exp(x) - sin(x) - cos(x))/2",0, 0,   3, 0.1),
            new EquationExample("y' = 2x",         "2*x",         "x^2",                          0, 0,   5, 0.5),
            new EquationExample("y' = x² - y",     "x^2 - y",     "",                             0, 1,   3, 0.1),
        };

        private EquationExample _selectedExample;

        /// <summary>
        /// Выбранный пример из ComboBox.
        /// При выборе автоматически заполняет все поля формы.
        /// </summary>
        public EquationExample SelectedExample
        {
            get { return _selectedExample; }
            set
            {
                if (SetProperty(ref _selectedExample, value) && value != null)
                {
                    // Блокируем автоподстановку пока сами меняем поля
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

        #region Параметры интегрирования

        /// <summary>Левая граница отрезка интегрирования</summary>
        private double _x0 = 0;
        public double X0
        {
            get { return _x0; }
            set { SetProperty(ref _x0, value); }
        }

        /// <summary>Начальное условие y(x0) = y0</summary>
        private double _y0 = 1;
        public double Y0
        {
            get { return _y0; }
            set { SetProperty(ref _y0, value); }
        }

        /// <summary>Правая граница отрезка интегрирования</summary>
        private double _xn = 2;
        public double Xn
        {
            get { return _xn; }
            set { SetProperty(ref _xn, value); }
        }

        /// <summary>Шаг интегрирования h</summary>
        private double _stepH = 0.1;
        public double StepH
        {
            get { return _stepH; }
            set { SetProperty(ref _stepH, value); }
        }

        /// <summary>Включить автоматический контроль точности по правилу Рунге</summary>
        private bool _useRungeControl;
        public bool UseRungeControl
        {
            get { return _useRungeControl; }
            set { SetProperty(ref _useRungeControl, value); }
        }

        /// <summary>Желаемая точность ε для правила Рунге</summary>
        private double _epsilon = 1e-6;
        public double Epsilon
        {
            get { return _epsilon; }
            set { SetProperty(ref _epsilon, value); }
        }

        #endregion

        #region Результаты

        /// <summary>Точки решения для отображения в таблице</summary>
        private ObservableCollection<SolutionPoint> _resultPoints =
            new ObservableCollection<SolutionPoint>();
        public ObservableCollection<SolutionPoint> ResultPoints
        {
            get { return _resultPoints; }
            set { SetProperty(ref _resultPoints, value); }
        }

        /// <summary>Текст в статусной строке внизу окна</summary>
        private string _statusText = "Готов к вычислению";
        public string StatusText
        {
            get { return _statusText; }
            set { SetProperty(ref _statusText, value); }
        }

        /// <summary>Сводка по результату (метод, шаг, кол-во точек и т.д.)</summary>
        private string _resultSummary = "";
        public string ResultSummary
        {
            get { return _resultSummary; }
            set { SetProperty(ref _resultSummary, value); }
        }

        /// <summary>True — есть вычисленное решение, кнопки графика/сравнения активны</summary>
        private bool _hasResults;
        public bool HasResults
        {
            get { return _hasResults; }
            set { SetProperty(ref _hasResults, value); }
        }

        /// <summary>Последний результат решения — передаётся в дочерние окна</summary>
        private SolverResult _lastResult;
        public SolverResult LastResult
        {
            get { return _lastResult; }
            set { SetProperty(ref _lastResult, value); }
        }

        #endregion

        #region Команды

        public ICommand SolveCommand { get; }   // Решить ОДУ нашим методом
        public ICommand ClearCommand { get; }   // Очистить результаты
        public ICommand ExportCommand { get; }   // Сохранить CSV
        public ICommand ShowGraphCommand { get; }  // Открыть окно графика
        public ICommand CompareCommand { get; }   // Сравнить с обычным Эйлером
        public ICommand MatlabCommand { get; }   // Решить в MATLAB и сравнить

        #endregion

        public MainViewModel()
        {
            SolveCommand = new RelayCommand(ExecuteSolve, CanSolve);
            ClearCommand = new RelayCommand(ExecuteClear);
            ExportCommand = new RelayCommand(ExecuteExport, () => HasResults);
            ShowGraphCommand = new RelayCommand(ExecuteShowGraph, () => HasResults);
            CompareCommand = new RelayCommand(ExecuteCompare, () => HasResults);
            MatlabCommand = new RelayCommand(ExecuteMatlab, () => HasResults);

            // Первичная валидация значений по умолчанию
            ValidateEquation();
            TryAutoFillExactSolution();
        }

        #region Автоподстановка точного решения

        /// <summary>
        /// Приводим строку к единому виду для сравнения со словарём:
        /// убираем пробелы, заменяем запятые на точки, приводим к нижнему регистру.
        /// </summary>
        private string Normalize(string s)
        {
            return s.Replace(" ", "").Replace(",", ".").ToLower();
        }

        /// <summary>
        /// Ищем введённое уравнение в словаре известных решений.
        /// Если нашли — подставляем формулу и показываем подсказку.
        /// </summary>
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

        /// <summary>
        /// Проверяем синтаксис f(x,y) через парсер.
        /// Результат пишем в EquationError и IsEquationValid.
        /// </summary>
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

        /// <summary>
        /// Проверяем синтаксис точного решения y(x).
        /// Поле необязательное — пустая строка не является ошибкой.
        /// </summary>
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

        #region Вспомогательные методы построения уравнения

        /// <summary>
        /// Создаём объект DifferentialEquation из текущего ввода пользователя.
        /// </summary>
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

        /// <summary>
        /// Парсим строку точного решения в функцию x → y(x).
        /// Возвращает null если поле пустое или содержит ошибку.
        /// </summary>
        private Func<double, double> ParseExactSolution()
        {
            if (string.IsNullOrWhiteSpace(ExactSolutionText))
                return null;

            try
            {
                // Парсер возвращает f(x, y) — для точного решения y не нужен
                var exactFunc = _parser.Parse(ExactSolutionText);
                return x => exactFunc(x, 0);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        /// <summary>
        /// Условие активности кнопки «Решить»:
        /// уравнение валидно, шаг положителен, правая граница > левой.
        /// </summary>
        private bool CanSolve()
        {
            return IsEquationValid && StepH > 0 && Xn > X0;
        }

        #region Выполнение команд

        /// <summary>
        /// Запускает решение ОДУ модифицированным методом Эйлера.
        /// Заполняет таблицу и сводку результатов.
        /// </summary>
        private void ExecuteSolve()
        {
            try
            {
                var exactSolution = ParseExactSolution();
                var equation = BuildEquation(exactSolution);

                SolverResult result;

                if (UseRungeControl)
                    // Адаптивный шаг с контролем по правилу Рунге
                    result = _solver.SolveWithRungeControl(
                        equation, X0, Y0, Xn, StepH, Epsilon);
                else
                    // Фиксированный шаг
                    result = _solver.Solve(equation, X0, Y0, Xn, StepH);

                ResultPoints = new ObservableCollection<SolutionPoint>(result.Points);
                LastResult = result;
                HasResults = true;

                // Формируем текстовую сводку
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

        /// <summary>Открывает окно с 3D-графиком последнего решения.</summary>
        private void ExecuteShowGraph()
        {
            if (LastResult == null) return;

            var window = new Views.GraphWindow();
            window.Owner = GetMainWindow();
            window.ShowSolution(LastResult);
            window.Show();
        }

        /// <summary>
        /// Запускает MATLAB (ode45), ждёт результат через временный CSV-файл,
        /// затем открывает окно сравнения нашего метода с MATLAB.
        /// </summary>
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

        /// <summary>
        /// Решает ОДУ обоими методами Эйлера, строит таблицу сравнения
        /// и открывает окно ComparisonWindow.
        /// Если точное решение задано — показывает погрешности,
        /// иначе — только разницу между методами.
        /// </summary>
        private void ExecuteCompare()
        {
            if (LastResult == null) return;

            try
            {
                var exactSolution = ParseExactSolution();
                var equation = BuildEquation(exactSolution);

                // Решаем обоими методами с одинаковыми параметрами
                var eulerResult = _eulerSolver.Solve(equation, X0, Y0, Xn, StepH);
                var modifiedResult = _solver.Solve(equation, X0, Y0, Xn, StepH);

                // Формируем набор точек точного решения (если оно задано)
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

                // Собираем таблицу сравнения поточечно
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
                        // null если точного решения нет — колонки скроются в окне
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

        /// <summary>Очищает таблицу и сбрасывает состояние результатов.</summary>
        private void ExecuteClear()
        {
            ResultPoints.Clear();
            HasResults = false;
            LastResult = null;
            ResultSummary = "";
            StatusText = "Результаты очищены";
        }

        /// <summary>
        /// Экспортирует таблицу результатов в CSV-файл.
        /// Разделитель — точка с запятой (совместимо с Excel в русской локали).
        /// </summary>
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
                    // Заголовок
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

        /// <summary>
        /// Ищет видимое окно MainWindow среди всех открытых окон приложения.
        /// Нужно потому что после SplashScreen главное окно создаётся заранее,
        /// и Application.Current.MainWindow может указывать не на то окно.
        /// </summary>
        private Window GetMainWindow()
        {
            foreach (Window w in Application.Current.Windows)
            {
                if (w is Views.MainWindow && w.IsVisible)
                    return w;
            }
            return Application.Current.MainWindow;
        }

        #endregion
    }

    // Вспомогательные классы

    /// <summary>
    /// Хранит формулу точного решения и начальное значение y0
    /// для одного известного ОДУ из словаря.
    /// </summary>
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

    /// <summary>
    /// Один элемент списка готовых примеров в ComboBox.
    /// Хранит все параметры задачи: уравнение, точное решение, границы, шаг.
    /// </summary>
    public class EquationExample
    {
        public string DisplayName { get; private set; }  // Отображается в ComboBox
        public string FunctionText { get; private set; }  // f(x,y)
        public string ExactText { get; private set; }  // y(x), может быть пустым
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

        // Используется ComboBox для отображения элемента
        public override string ToString() => DisplayName;
    }
}