using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using EulerSolver.Core.Models;

namespace EulerSolver.Services
{
    public class MatlabService
    {
        private static readonly string[] _defaultPaths = new[]
        {
            @"D:\Dowloads\MATLAB\bin\matlab.exe",
            @"C:\Program Files\MATLAB\R2024b\bin\matlab.exe",
            @"C:\Program Files\MATLAB\R2024a\bin\matlab.exe",
            @"C:\Program Files\MATLAB\R2023b\bin\matlab.exe",
            @"C:\Program Files\MATLAB\R2023a\bin\matlab.exe",
            @"C:\Program Files\MATLAB\R2022b\bin\matlab.exe",
            @"C:\Program Files\MATLAB\R2022a\bin\matlab.exe",
        };

        private string FindMatlabPath()
        {
            foreach (var path in _defaultPaths)
            {
                if (File.Exists(path))
                    return path;
            }
            throw new FileNotFoundException(
                "MATLAB не найден. Проверьте путь установки.\n" +
                "Искал в:\n" + string.Join("\n", _defaultPaths));
        }

        public async Task<MatlabResult> SolveAsync(
            string equation, double x0, double y0,
            double xn, double h)
        {
            string matlabExe = FindMatlabPath();

            // Папка для обмена файлами
            string tempDir = Path.Combine(Path.GetTempPath(), "EulerSolver");
            Directory.CreateDirectory(tempDir);

            string scriptPath = Path.Combine(tempDir, "solve_ode.m");
            string resultPath = Path.Combine(tempDir, "result.csv");
            string donePath = Path.Combine(tempDir, "done.txt");

            // Удаляем старые файлы
            if (File.Exists(resultPath)) File.Delete(resultPath);
            if (File.Exists(donePath)) File.Delete(donePath);

            // Конвертируем уравнение в MATLAB синтаксис
            string matlabEq = ConvertToMatlabSyntax(equation);

            // Генерируем .m скрипт
            string script = GenerateScript(
                matlabEq, x0, y0, xn, h,
                resultPath, donePath);

            File.WriteAllText(scriptPath, script);

            // ====================================================
            // Запускаем MATLAB с GUI — открывается редактор
            // со скриптом, скрипт запускается автоматически
            // ====================================================
            string scriptPathM = scriptPath.Replace("\\", "/");

            var psi = new ProcessStartInfo
            {
                FileName = matlabExe,

                // -r "команда"  — выполняет команду ПОСЛЕ загрузки GUI
                // edit + run    — открывает файл в редакторе и запускает
                Arguments = $"-r \"edit('{scriptPathM}'); run('{scriptPathM}');\"",

                UseShellExecute = true,   // ← TRUE чтобы открылось окно MATLAB
                CreateNoWindow = false,  // ← FALSE чтобы окно было видно
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(psi);

            // Ждём появления файла done.txt (макс. 120 секунд)
            // MATLAB с GUI грузится дольше чем -batch
            int waited = 0;
            int maxWait = 120_000; // 2 минуты

            while (!File.Exists(donePath) && waited < maxWait)
            {
                await Task.Delay(500);
                waited += 500;
            }

            if (!File.Exists(donePath))
                throw new Exception(
                    "MATLAB не ответил за 2 минуты.\n" +
                    "Возможно MATLAB ещё загружается — подождите и попробуйте снова.");

            // Проверяем содержимое done.txt (OK или ERROR)
            string doneContent = File.ReadAllText(donePath).Trim();
            if (doneContent.StartsWith("ERROR"))
                throw new Exception("MATLAB сообщил об ошибке:\n" + doneContent);

            if (!File.Exists(resultPath))
                throw new Exception(
                    "MATLAB не сохранил результат.\n" +
                    "Проверьте корректность уравнения.");

            // Читаем результат
            var points = ReadCsv(resultPath);

            // Чистим временные файлы
            try
            {
                File.Delete(scriptPath);
                File.Delete(resultPath);
                File.Delete(donePath);
            }
            catch { /* не критично */ }

            return new MatlabResult
            {
                Points = points,
                Equation = equation
            };
        }

        private string GenerateScript(
            string matlabEq, double x0, double y0,
            double xn, double h,
            string resultPath, string donePath)
        {
            string resultPathM = resultPath.Replace("\\", "/");
            string donePathM = donePath.Replace("\\", "/");

            string x0s = x0.ToString("G10", CultureInfo.InvariantCulture);
            string y0s = y0.ToString("G10", CultureInfo.InvariantCulture);
            string xns = xn.ToString("G10", CultureInfo.InvariantCulture);
            string hs = h.ToString("G10", CultureInfo.InvariantCulture);

            return
$@"%% ============================================================
%% Скрипт сгенерирован программой EulerSolver
%% Решение ОДУ методом ode45
%% ============================================================

try
    %% Правая часть уравнения: y' = f(x, y)
    f = @(x, y) {matlabEq};

    %% Параметры задачи
    x0 = {x0s};
    y0 = {y0s};
    xn = {xns};
    h  = {hs};

    %% Точки вывода (совпадают с нашим методом)
    xspan = (x0 : h : xn);
    if xspan(end) ~= xn
        xspan = [xspan, xn];
    end

    %% Решение через ode45 (Рунге-Кутта 4-5 порядка)
    opts = odeset('RelTol', 1e-8, 'AbsTol', 1e-10);
    [xSol, ySol] = ode45(f, xspan, y0, opts);

    %% Сохраняем результат в CSV
    fid = fopen('{resultPathM}', 'w');
    fprintf(fid, 'X,Y\n');
    for i = 1 : length(xSol)
        fprintf(fid, '%.15f,%.15f\n', xSol(i), ySol(i));
    end
    fclose(fid);

    %% Выводим график прямо в MATLAB
    figure('Name', 'Решение ОДУ — ode45', 'NumberTitle', 'off');
    plot(xSol, ySol, 'b-o', 'LineWidth', 2, 'MarkerSize', 4);
    grid on;
    xlabel('x');
    ylabel('y');
    title(['Решение: y'' = {matlabEq}']);
    legend('ode45');

    %% Сигнал завершения
    fid = fopen('{donePathM}', 'w');
    fprintf(fid, 'OK');
    fclose(fid);

    disp('=== EulerSolver: вычисление завершено успешно ===');

catch err
    %% Сигнал об ошибке
    fid = fopen('{donePathM}', 'w');
    fprintf(fid, 'ERROR: %s', err.message);
    fclose(fid);

    disp(['=== EulerSolver: ОШИБКА: ', err.message, ' ===']);
end
";
        }

        private string ConvertToMatlabSyntax(string eq)
        {
            // Для скалярного ОДУ (ode45 работает со скалярами)
            // поэлементные операторы .* ./ .^ не нужны
            // Только заменяем ln → log (в MATLAB натуральный логарифм это log)
            return eq
                .Replace("ln(", "log(")
                .Replace("LN(", "log(");
        }

        private List<SolutionPoint> ReadCsv(string path)
        {
            var points = new List<SolutionPoint>();
            var lines = File.ReadAllLines(path);

            // Пропускаем заголовок
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line)) continue;

                string[] parts = line.Split(',');
                if (parts.Length < 2) continue;

                if (double.TryParse(parts[0], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(parts[1], NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double y))
                {
                    points.Add(new SolutionPoint(x, y));
                }
            }

            return points;
        }
    }

    public class MatlabResult
    {
        public List<SolutionPoint> Points { get; set; }
        public string Equation { get; set; }
    }
}