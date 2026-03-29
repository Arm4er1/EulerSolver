using System;
using System.Collections.Generic;

namespace EulerSolver.Services
{
    /// <summary>
    /// Парсер математических выражений с переменными x и y.
    /// Поддерживает: +, -, *, /, ^, скобки, унарный минус,
    /// функции: sin, cos, tan, exp, ln, log, sqrt, abs
    /// константы: pi, e
    /// </summary>
    public class ExpressionParser
    {
        private string _expression = "";
        private int _pos;

        /// <summary>
        /// Парсит строковое выражение и возвращает функцию f(x, y)
        /// </summary>
        public Func<double, double, double> Parse(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression))
                throw new ArgumentException("Выражение не может быть пустым");

            // Нормализация
            expression = expression
                .Replace(" ", "")
                .Replace(",", ".")
                .ToLower();

            // Проверка и замена неявного умножения: 2x -> 2*x, 3sin -> 3*sin, x(... -> x*(
            expression = InsertImplicitMultiplication(expression);

            _expression = expression;
            _pos = 0;

            var tree = ParseExpression();

            if (_pos < _expression.Length)
                throw new ArgumentException($"Неожиданный символ '{_expression[_pos]}' на позиции {_pos + 1}");

            return (x, y) => tree(x, y);
        }

        #region Неявное умножение

        private string InsertImplicitMultiplication(string expr)
        {
            var result = new System.Text.StringBuilder();

            for (int i = 0; i < expr.Length; i++)
            {
                result.Append(expr[i]);

                if (i + 1 < expr.Length)
                {
                    char current = expr[i];
                    char next = expr[i + 1];

                    bool needMultiply = false;

                    // число перед буквой/скобкой: 2x, 2(, 2sin
                    if ((char.IsDigit(current) || current == '.') &&
                        (char.IsLetter(next) || next == '('))
                        needMultiply = true;

                    // закрывающая скобка перед числом/буквой/открывающей скобкой
                    if (current == ')' &&
                        (char.IsDigit(next) || char.IsLetter(next) || next == '('))
                        needMultiply = true;

                    // переменная перед открывающей скобкой: x(
                    if ((current == 'x' || current == 'y') && next == '(' &&
                        (i == 0 || !char.IsLetter(expr[i - 1])))
                        needMultiply = true;

                    // переменная перед числом: x2 (маловероятно, но на всякий случай)
                    if ((current == 'x' || current == 'y') && char.IsDigit(next) &&
                        (i == 0 || !char.IsLetter(expr[i - 1])))
                        needMultiply = true;

                    if (needMultiply)
                    {
                        // Не вставляем * если это часть имени функции
                        if (char.IsLetter(next) && char.IsLetter(current))
                        {
                            // Проверяем, не является ли текущая позиция частью имени функции
                            // Пропускаем вставку
                        }
                        else
                        {
                            result.Append('*');
                        }
                    }
                }
            }

            return result.ToString();
        }

        #endregion

        #region Рекурсивный спуск

        // Грамматика:
        // Expression = Term (('+' | '-') Term)*
        // Term       = Power (('*' | '/') Power)*
        // Power      = Unary ('^' Unary)*
        // Unary      = ('-' | '+') Unary | Atom
        // Atom       = Number | Variable | Function '(' Expression ')' | '(' Expression ')'

        private Func<double, double, double> ParseExpression()
        {
            var left = ParseTerm();

            while (_pos < _expression.Length && (_expression[_pos] == '+' || _expression[_pos] == '-'))
            {
                char op = _expression[_pos++];
                var right = ParseTerm();
                var l = left;
                var r = right;

                if (op == '+')
                    left = (x, y) => l(x, y) + r(x, y);
                else
                    left = (x, y) => l(x, y) - r(x, y);
            }

            return left;
        }

        private Func<double, double, double> ParseTerm()
        {
            var left = ParsePower();

            while (_pos < _expression.Length && (_expression[_pos] == '*' || _expression[_pos] == '/'))
            {
                char op = _expression[_pos++];
                var right = ParsePower();
                var l = left;
                var r = right;

                if (op == '*')
                    left = (x, y) => l(x, y) * r(x, y);
                else
                    left = (x, y) => l(x, y) / r(x, y);
            }

            return left;
        }

        private Func<double, double, double> ParsePower()
        {
            var baseExpr = ParseUnary();

            if (_pos < _expression.Length && _expression[_pos] == '^')
            {
                _pos++;
                var exponent = ParseUnary(); // Правоассоциативный
                var b = baseExpr;
                var e = exponent;
                return (x, y) => Math.Pow(b(x, y), e(x, y));
            }

            return baseExpr;
        }

        private Func<double, double, double> ParseUnary()
        {
            if (_pos < _expression.Length && _expression[_pos] == '-')
            {
                _pos++;
                var operand = ParseUnary();
                return (x, y) => -operand(x, y);
            }

            if (_pos < _expression.Length && _expression[_pos] == '+')
            {
                _pos++;
                return ParseUnary();
            }

            return ParseAtom();
        }

        private Func<double, double, double> ParseAtom()
        {
            if (_pos >= _expression.Length)
                throw new ArgumentException("Неожиданный конец выражения");

            // Скобки
            if (_expression[_pos] == '(')
            {
                _pos++; // пропускаем '('
                var expr = ParseExpression();

                if (_pos >= _expression.Length || _expression[_pos] != ')')
                    throw new ArgumentException("Ожидалась закрывающая скобка ')'");

                _pos++; // пропускаем ')'
                return expr;
            }

            // Число
            if (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.')
            {
                return ParseNumber();
            }

            // Переменная или функция или константа
            if (char.IsLetter(_expression[_pos]))
            {
                return ParseIdentifier();
            }

            throw new ArgumentException($"Неожиданный символ '{_expression[_pos]}' на позиции {_pos + 1}");
        }

        private Func<double, double, double> ParseNumber()
        {
            int start = _pos;

            while (_pos < _expression.Length && (char.IsDigit(_expression[_pos]) || _expression[_pos] == '.'))
                _pos++;

            // Научная нотация: 1e-5, 2.5E3
            if (_pos < _expression.Length && (_expression[_pos] == 'e') &&
                (_pos + 1 < _expression.Length) &&
                (char.IsDigit(_expression[_pos + 1]) || _expression[_pos + 1] == '-' || _expression[_pos + 1] == '+'))
            {
                _pos++; // 'e'
                if (_expression[_pos] == '-' || _expression[_pos] == '+')
                    _pos++;
                while (_pos < _expression.Length && char.IsDigit(_expression[_pos]))
                    _pos++;
            }

            string numStr = _expression.Substring(start, _pos - start);
            if (!double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double value))
            {
                throw new ArgumentException($"Не удалось распознать число: '{numStr}'");
            }

            return (x, y) => value;
        }

        private Func<double, double, double> ParseIdentifier()
        {
            int start = _pos;
            while (_pos < _expression.Length && char.IsLetter(_expression[_pos]))
                _pos++;

            string name = _expression.Substring(start, _pos - start);

            // Переменные
            if (name == "x") return (x, y) => x;
            if (name == "y") return (x, y) => y;

            // Константы
            if (name == "pi") return (x, y) => Math.PI;
            if (name == "e" && (_pos >= _expression.Length || _expression[_pos] != '('))
                return (x, y) => Math.E;

            // Функции — требуют аргумент в скобках
            if (_pos < _expression.Length && _expression[_pos] == '(')
            {
                _pos++; // пропускаем '('
                var arg = ParseExpression();

                if (_pos >= _expression.Length || _expression[_pos] != ')')
                    throw new ArgumentException($"Ожидалась ')' после аргумента функции '{name}'");

                _pos++; // пропускаем ')'

                return name switch
                {
                    "sin" => (x, y) => Math.Sin(arg(x, y)),
                    "cos" => (x, y) => Math.Cos(arg(x, y)),
                    "tan" or "tg" => (x, y) => Math.Tan(arg(x, y)),
                    "cot" or "ctg" => (x, y) => 1.0 / Math.Tan(arg(x, y)),
                    "asin" or "arcsin" => (x, y) => Math.Asin(arg(x, y)),
                    "acos" or "arccos" => (x, y) => Math.Acos(arg(x, y)),
                    "atan" or "arctan" or "arctg" => (x, y) => Math.Atan(arg(x, y)),
                    "exp" => (x, y) => Math.Exp(arg(x, y)),
                    "ln" => (x, y) => Math.Log(arg(x, y)),
                    "log" or "lg" => (x, y) => Math.Log10(arg(x, y)),
                    "sqrt" => (x, y) => Math.Sqrt(arg(x, y)),
                    "abs" => (x, y) => Math.Abs(arg(x, y)),
                    "sign" => (x, y) => Math.Sign(arg(x, y)),
                    "sinh" or "sh" => (x, y) => Math.Sinh(arg(x, y)),
                    "cosh" or "ch" => (x, y) => Math.Cosh(arg(x, y)),
                    "tanh" or "th" => (x, y) => Math.Tanh(arg(x, y)),
                    _ => throw new ArgumentException($"Неизвестная функция: '{name}'")
                };
            }

            throw new ArgumentException($"Неизвестный идентификатор: '{name}'. " +
                $"Допустимые переменные: x, y. Функции должны иметь аргумент в скобках, например sin(x)");
        }

        #endregion

        /// <summary>
        /// Проверяет корректность выражения без выполнения
        /// </summary>
        public bool TryParse(string expression, out string error)
        {
            try
            {
                var func = Parse(expression);
                // Пробное вычисление
                func(1.0, 1.0);
                error = "";
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}