using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using EulerSolver.Core.Models;

namespace EulerSolver.Controls
{
    public partial class Graph3DControl : UserControl
    {
        private bool _isDragging;
        private Point _lastMousePos;
        private double _cameraAngleH = 5;
        private double _cameraAngleV = 15;
        private double _cameraDistance = 8;
        private Point3D _cameraTarget = new Point3D(0, 0, 0);

        private double _xMin, _xMax, _yMin, _yMax;
        private double _xRange, _yRange;
        private const double SceneSize = 3.0;

        // Храним точки для tooltip
        private List<SphereHitInfo> _sphereInfos = new List<SphereHitInfo>();
        private List<PlotLineData> _currentLines;

        public Graph3DControl()
        {
            InitializeComponent();
            UpdateCamera();
        }

        public void PlotSolution(List<SolutionPoint> points, string title)
        {
            var lines = new List<PlotLineData>();
            lines.Add(new PlotLineData
            {
                Points = points,
                Color = Colors.DodgerBlue,
                Title = title
            });
            PlotMultiple(lines);
        }

        public void PlotMultiple(List<PlotLineData> lines)
        {
            if (lines == null || lines.Count == 0) return;

            _currentLines = lines;
            _sphereInfos.Clear();
            overlayCanvas.Children.Clear();

            // Границы данных
            _xMin = double.MaxValue;
            _xMax = double.MinValue;
            _yMin = double.MaxValue;
            _yMax = double.MinValue;

            foreach (var line in lines)
            {
                foreach (var p in line.Points)
                {
                    if (p.X < _xMin) _xMin = p.X;
                    if (p.X > _xMax) _xMax = p.X;
                    if (p.Y < _yMin) _yMin = p.Y;
                    if (p.Y > _yMax) _yMax = p.Y;
                }
            }

            double xPadding = (_xMax - _xMin) * 0.08 + 0.01;
            double yPadding = (_yMax - _yMin) * 0.08 + 0.01;
            _xMin -= xPadding;
            _xMax += xPadding;
            _yMin -= yPadding;
            _yMax += yPadding;
            _xRange = _xMax - _xMin;
            _yRange = _yMax - _yMin;

            _cameraTarget = new Point3D(SceneSize / 2, SceneSize / 2, 0);

            var group = new Model3DGroup();

            AddGrid(group);
            AddAxes(group);
            AddZeroLines(group);

            foreach (var line in lines)
            {
                AddLine(group, line.Points, line.Color);
                AddPointSpheres(group, line.Points, line.Color, line.Title);
            }

            graphContent.Content = group;
            UpdateCamera();
            UpdateTickLabels();
        }

        public void Clear()
        {
            graphContent.Content = null;
            overlayCanvas.Children.Clear();
            _sphereInfos.Clear();
            tooltipBorder.Visibility = Visibility.Collapsed;
        }

        #region Нормализация координат

        private double NormX(double x)
        {
            return (x - _xMin) / _xRange * SceneSize;
        }

        private double NormY(double y)
        {
            return (y - _yMin) / _yRange * SceneSize;
        }

        // Обратное преобразование
        private double RealX(double normX)
        {
            return normX / SceneSize * _xRange + _xMin;
        }

        private double RealY(double normY)
        {
            return normY / SceneSize * _yRange + _yMin;
        }

        #endregion

        #region Построение 3D

        private void AddAxes(Model3DGroup group)
        {
            double t = 0.02;

            // Определяем где находится реальный ноль в нормализованных координатах
            double xZero = NormX(0);
            double yZero = NormY(0);

            // Ограничиваем — ось не должна выходить за пределы сцены
            bool xZeroVisible = (0 >= _xMin && 0 <= _xMax);
            bool yZeroVisible = (0 >= _yMin && 0 <= _yMax);

            // Если ноль не попадает в диапазон — рисуем ось на краю
            if (!xZeroVisible) xZero = 0;
            if (!yZeroVisible) yZero = 0;

            // Ось X (горизонтальная) — на уровне Y=0
            AddBox(group, new Point3D(SceneSize / 2, yZero, 0.005),
                   SceneSize + 0.3, t, t, Color.FromRgb(180, 50, 50));

            // Ось Y (вертикальная) — на уровне X=0
            AddBox(group, new Point3D(xZero, SceneSize / 2, 0.005),
                   t, SceneSize + 0.3, t, Color.FromRgb(50, 150, 50));

            // Стрелка X — на конце оси X, на уровне Y=0
            AddCone(group, new Point3D(SceneSize + 0.15, yZero, 0), 0.05, 0.12,
                    Color.FromRgb(180, 50, 50), 0);

            // Стрелка Y — на конце оси Y, на уровне X=0
            AddCone(group, new Point3D(xZero, SceneSize + 0.15, 0), 0.05, 0.12,
                    Color.FromRgb(50, 150, 50), 90);
        }

        /// <summary>
        /// Линия Y=0 (если ноль входит в диапазон)
        /// </summary>
        private void AddZeroLines(Model3DGroup group)
        {
            // Горизонтальная линия Y=0
            if (_yMin < 0 && _yMax > 0)
            {
                double yZero = NormY(0);
                AddBox(group, new Point3D(SceneSize / 2, yZero, 0.001),
                       SceneSize, 0.015, 0.015, Color.FromRgb(100, 100, 100));
            }

            // Вертикальная линия X=0
            if (_xMin < 0 && _xMax > 0)
            {
                double xZero = NormX(0);
                AddBox(group, new Point3D(xZero, SceneSize / 2, 0.001),
                       0.015, SceneSize, 0.015, Color.FromRgb(100, 100, 100));
            }
        }

        private void AddGrid(Model3DGroup group)
        {
            double t = 0.004;
            Color gridColor = Color.FromRgb(210, 210, 210);
            int gridLines = 10;

            for (int i = 0; i <= gridLines; i++)
            {
                double pos = (double)i / gridLines * SceneSize;

                AddBox(group, new Point3D(SceneSize / 2, pos, -0.01),
                       SceneSize, t, t, gridColor);
                AddBox(group, new Point3D(pos, SceneSize / 2, -0.01),
                       t, SceneSize, t, gridColor);
            }
        }

        private void AddLine(Model3DGroup group, List<SolutionPoint> points, Color color)
        {
            double thickness = 0.022;

            for (int i = 0; i < points.Count - 1; i++)
            {
                Point3D p1 = new Point3D(NormX(points[i].X), NormY(points[i].Y), 0);
                Point3D p2 = new Point3D(NormX(points[i + 1].X), NormY(points[i + 1].Y), 0);
                AddTube(group, p1, p2, thickness, color);
            }
        }

        private void AddPointSpheres(Model3DGroup group, List<SolutionPoint> points,
                                     Color color, string lineTitle)
        {
            double radius = 0.045;

            int step = 1;
            if (points.Count > 50) step = points.Count / 30;

            for (int i = 0; i < points.Count; i += step)
            {
                Point3D center = new Point3D(NormX(points[i].X), NormY(points[i].Y), 0);
                AddSphere(group, center, radius, color);

                _sphereInfos.Add(new SphereHitInfo
                {
                    Center3D = center,
                    RealX = points[i].X,
                    RealY = points[i].Y,
                    Radius = radius,
                    LineTitle = lineTitle
                });
            }

            // Последняя точка
            if (points.Count > 0)
            {
                var last = points[points.Count - 1];
                Point3D lastCenter = new Point3D(NormX(last.X), NormY(last.Y), 0);

                bool alreadyAdded = false;
                foreach (var info in _sphereInfos)
                {
                    if (Math.Abs(info.RealX - last.X) < 1e-10 &&
                        Math.Abs(info.RealY - last.Y) < 1e-10)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }

                if (!alreadyAdded)
                {
                    AddSphere(group, lastCenter, radius, color);
                    _sphereInfos.Add(new SphereHitInfo
                    {
                        Center3D = lastCenter,
                        RealX = last.X,
                        RealY = last.Y,
                        Radius = radius,
                        LineTitle = lineTitle
                    });
                }
            }
        }

        #endregion

        #region Подписи осей (2D overlay)

        private void UpdateTickLabels()
        {
            overlayCanvas.Children.Clear();

            if (_xRange <= 0 || _yRange <= 0) return;

            // Где реальный ноль
            double xZeroNorm = NormX(0);
            double yZeroNorm = NormY(0);
            bool xZeroVisible = (0 >= _xMin && 0 <= _xMax);
            bool yZeroVisible = (0 >= _yMin && 0 <= _yMax);
            if (!xZeroVisible) xZeroNorm = 0;
            if (!yZeroVisible) yZeroNorm = 0;

            int tickCount = 6;

            // Подписи по оси X (под осью X, на уровне Y=0)
            for (int i = 0; i <= tickCount; i++)
            {
                double normX = (double)i / tickCount * SceneSize;
                double realX = RealX(normX);

                // Засечка на оси
                Point3D tickWorld = new Point3D(normX, yZeroNorm - 0.15, 0);
                Point screenPos = Project3DTo2D(tickWorld);

                if (double.IsNaN(screenPos.X) || double.IsInfinity(screenPos.X)) continue;

                var label = new TextBlock
                {
                    Text = FormatTickValue(realX),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    FontFamily = new FontFamily("Consolas")
                };

                Canvas.SetLeft(label, screenPos.X - 15);
                Canvas.SetTop(label, screenPos.Y);
                overlayCanvas.Children.Add(label);
            }

            // Подписи по оси Y (слева от оси Y, на уровне X=0)
            for (int i = 0; i <= tickCount; i++)
            {
                double normY = (double)i / tickCount * SceneSize;
                double realY = RealY(normY);

                Point3D tickWorld = new Point3D(xZeroNorm - 0.25, normY, 0);
                Point screenPos = Project3DTo2D(tickWorld);

                if (double.IsNaN(screenPos.X) || double.IsInfinity(screenPos.X)) continue;

                var label = new TextBlock
                {
                    Text = FormatTickValue(realY),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                    FontFamily = new FontFamily("Consolas")
                };

                Canvas.SetLeft(label, screenPos.X - 40);
                Canvas.SetTop(label, screenPos.Y - 7);
                overlayCanvas.Children.Add(label);
            }

            // Подпись "X"
            Point3D xLabelPos = new Point3D(SceneSize + 0.3, yZeroNorm - 0.15, 0);
            Point xScreen = Project3DTo2D(xLabelPos);
            if (!double.IsNaN(xScreen.X))
            {
                var xLabel = new TextBlock
                {
                    Text = "X",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(180, 50, 50))
                };
                Canvas.SetLeft(xLabel, xScreen.X);
                Canvas.SetTop(xLabel, xScreen.Y);
                overlayCanvas.Children.Add(xLabel);
            }

            // Подпись "Y"
            Point3D yLabelPos = new Point3D(xZeroNorm - 0.15, SceneSize + 0.2, 0);
            Point yScreen = Project3DTo2D(yLabelPos);
            if (!double.IsNaN(yScreen.X))
            {
                var yLabel = new TextBlock
                {
                    Text = "Y",
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(50, 150, 50))
                };
                Canvas.SetLeft(yLabel, yScreen.X);
                Canvas.SetTop(yLabel, yScreen.Y);
                overlayCanvas.Children.Add(yLabel);
            }
        }

        private string FormatTickValue(double value)
        {
            if (Math.Abs(value) < 1e-10) return "0";

            string result;
            if (Math.Abs(value) >= 100)
                result = value.ToString("F0");
            else if (Math.Abs(value) >= 1)
                result = value.ToString("F2");
            else
                result = value.ToString("F3");

            // Заменяем запятую на точку (для единообразия)
            return result.Replace(',', '.');
        }

        /// <summary>
        /// Проецирует 3D точку в 2D координаты экрана
        /// </summary>
        private Point Project3DTo2D(Point3D point3D)
        {
            bool success = false;
            GeneralTransform3DTo2D transform = null;

            try
            {
                // Создаём временный объект для проекции
                var testModel = new ModelVisual3D();
                testModel.Content = new GeometryModel3D
                {
                    Geometry = new MeshGeometry3D
                    {
                        Positions = new Point3DCollection { point3D }
                    }
                };

                viewport.Children.Add(testModel);
                transform = testModel.TransformToAncestor(viewport);
                viewport.Children.Remove(testModel);

                if (transform != null)
                {
                    Point result = transform.Transform(point3D);
                    return result;
                }
            }
            catch
            {
                // Игнорируем ошибки проекции
            }

            return new Point(double.NaN, double.NaN);
        }

        #endregion

        #region Tooltip при наведении

        private void CheckTooltip(Point mousePos)
        {
            if (_sphereInfos.Count == 0)
            {
                tooltipBorder.Visibility = Visibility.Collapsed;
                return;
            }

            // Находим ближайшую сферу к позиции мыши
            double bestDist = double.MaxValue;
            SphereHitInfo bestHit = null;

            foreach (var info in _sphereInfos)
            {
                Point screenPos = Project3DTo2D(info.Center3D);
                if (double.IsNaN(screenPos.X)) continue;

                double dist = Math.Sqrt(
                    (mousePos.X - screenPos.X) * (mousePos.X - screenPos.X) +
                    (mousePos.Y - screenPos.Y) * (mousePos.Y - screenPos.Y));

                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestHit = info;
                }
            }

            // Показываем tooltip если мышь близко к точке (< 30 пикселей)
            if (bestHit != null && bestDist < 30)
            {
                tooltipText.Text = bestHit.LineTitle + "\n" +
                                   "X = " + bestHit.RealX.ToString("F6") + "\n" +
                                   "Y = " + bestHit.RealY.ToString("F6");

                // Позиционируем tooltip рядом с курсором
                tooltipBorder.Margin = new Thickness(mousePos.X + 15, mousePos.Y - 10, 0, 0);
                tooltipBorder.Visibility = Visibility.Visible;
            }
            else
            {
                tooltipBorder.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region 3D примитивы

        private void AddSphere(Model3DGroup group, Point3D center, double radius, Color color)
        {
            int segments = 12;
            int rings = 8;
            var mesh = new MeshGeometry3D();

            for (int ring = 0; ring <= rings; ring++)
            {
                double phi = Math.PI * ring / rings;
                double y = Math.Cos(phi) * radius;
                double ringRadius = Math.Sin(phi) * radius;

                for (int seg = 0; seg <= segments; seg++)
                {
                    double theta = 2 * Math.PI * seg / segments;
                    double x = Math.Cos(theta) * ringRadius;
                    double z = Math.Sin(theta) * ringRadius;

                    mesh.Positions.Add(new Point3D(center.X + x, center.Y + y, center.Z + z));
                    mesh.Normals.Add(new Vector3D(x, y, z));
                }
            }

            for (int ring = 0; ring < rings; ring++)
            {
                for (int seg = 0; seg < segments; seg++)
                {
                    int current = ring * (segments + 1) + seg;
                    int next = current + segments + 1;

                    mesh.TriangleIndices.Add(current);
                    mesh.TriangleIndices.Add(next);
                    mesh.TriangleIndices.Add(current + 1);

                    mesh.TriangleIndices.Add(current + 1);
                    mesh.TriangleIndices.Add(next);
                    mesh.TriangleIndices.Add(next + 1);
                }
            }

            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            material.Children.Add(new SpecularMaterial(Brushes.White, 60));

            group.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            });
        }

        private void AddBox(Model3DGroup group, Point3D center,
                           double sizeX, double sizeY, double sizeZ, Color color)
        {
            double hx = sizeX / 2, hy = sizeY / 2, hz = sizeZ / 2;
            double cx = center.X, cy = center.Y, cz = center.Z;

            var mesh = new MeshGeometry3D();

            mesh.Positions.Add(new Point3D(cx - hx, cy - hy, cz - hz));
            mesh.Positions.Add(new Point3D(cx + hx, cy - hy, cz - hz));
            mesh.Positions.Add(new Point3D(cx + hx, cy + hy, cz - hz));
            mesh.Positions.Add(new Point3D(cx - hx, cy + hy, cz - hz));
            mesh.Positions.Add(new Point3D(cx - hx, cy - hy, cz + hz));
            mesh.Positions.Add(new Point3D(cx + hx, cy - hy, cz + hz));
            mesh.Positions.Add(new Point3D(cx + hx, cy + hy, cz + hz));
            mesh.Positions.Add(new Point3D(cx - hx, cy + hy, cz + hz));

            int[] idx = {
                0,2,1, 0,3,2,
                4,5,6, 4,6,7,
                0,1,5, 0,5,4,
                2,3,7, 2,7,6,
                0,4,7, 0,7,3,
                1,2,6, 1,6,5
            };

            foreach (int i in idx) mesh.TriangleIndices.Add(i);

            var material = new DiffuseMaterial(new SolidColorBrush(color));
            group.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            });
        }

        private void AddTube(Model3DGroup group, Point3D p1, Point3D p2,
                            double radius, Color color)
        {
            Vector3D direction = p2 - p1;
            double length = direction.Length;
            if (length < 1e-10) return;

            direction.Normalize();

            Vector3D up = Math.Abs(direction.Y) < 0.99
                ? new Vector3D(0, 1, 0)
                : new Vector3D(1, 0, 0);

            Vector3D side = Vector3D.CrossProduct(direction, up);
            side.Normalize();
            up = Vector3D.CrossProduct(side, direction);
            up.Normalize();

            int segments = 6;
            var mesh = new MeshGeometry3D();

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                Vector3D offset = side * Math.Cos(angle) * radius
                                + up * Math.Sin(angle) * radius;

                mesh.Positions.Add(p1 + offset);
                mesh.Positions.Add(p2 + offset);

                Vector3D normal = offset;
                normal.Normalize();
                mesh.Normals.Add(normal);
                mesh.Normals.Add(normal);
            }

            for (int i = 0; i < segments; i++)
            {
                int a = i * 2;
                int b = i * 2 + 1;
                int c = i * 2 + 2;
                int d = i * 2 + 3;

                mesh.TriangleIndices.Add(a);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(c);

                mesh.TriangleIndices.Add(c);
                mesh.TriangleIndices.Add(b);
                mesh.TriangleIndices.Add(d);
            }

            var material = new MaterialGroup();
            material.Children.Add(new DiffuseMaterial(new SolidColorBrush(color)));
            material.Children.Add(new SpecularMaterial(Brushes.White, 40));

            group.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            });
        }

        private void AddCone(Model3DGroup group, Point3D tip, double radius,
                            double height, Color color, double rotationDeg)
        {
            int segments = 12;
            var mesh = new MeshGeometry3D();

            Point3D baseCenter;
            if (rotationDeg == 0)
                baseCenter = new Point3D(tip.X - height, tip.Y, tip.Z);
            else
                baseCenter = new Point3D(tip.X, tip.Y - height, tip.Z);

            mesh.Positions.Add(tip);

            for (int i = 0; i <= segments; i++)
            {
                double angle = 2 * Math.PI * i / segments;
                double dx, dy, dz;

                if (rotationDeg == 0)
                {
                    dx = 0;
                    dy = Math.Cos(angle) * radius;
                    dz = Math.Sin(angle) * radius;
                }
                else
                {
                    dx = Math.Cos(angle) * radius;
                    dy = 0;
                    dz = Math.Sin(angle) * radius;
                }

                mesh.Positions.Add(new Point3D(
                    baseCenter.X + dx, baseCenter.Y + dy, baseCenter.Z + dz));
            }

            for (int i = 1; i <= segments; i++)
            {
                mesh.TriangleIndices.Add(0);
                mesh.TriangleIndices.Add(i);
                mesh.TriangleIndices.Add(i + 1);
            }

            var material = new DiffuseMaterial(new SolidColorBrush(color));
            group.Children.Add(new GeometryModel3D
            {
                Geometry = mesh,
                Material = material,
                BackMaterial = material
            });
        }

        #endregion

        #region Камера

        private void UpdateCamera()
        {
            double angleH = _cameraAngleH * Math.PI / 180;
            double angleV = _cameraAngleV * Math.PI / 180;

            if (_cameraAngleV > 89) _cameraAngleV = 89;
            if (_cameraAngleV < -89) _cameraAngleV = -89;

            double x = _cameraDistance * Math.Cos(angleV) * Math.Cos(angleH);
            double y = _cameraDistance * Math.Sin(angleV);
            double z = _cameraDistance * Math.Cos(angleV) * Math.Sin(angleH);

            camera.Position = new Point3D(
                _cameraTarget.X + x,
                _cameraTarget.Y + y,
                _cameraTarget.Z + z);

            camera.LookDirection = new Vector3D(
                _cameraTarget.X - camera.Position.X,
                _cameraTarget.Y - camera.Position.Y,
                _cameraTarget.Z - camera.Position.Z);

            camera.UpDirection = new Vector3D(0, 1, 0);

            // Обновляем подписи при каждом движении камеры
            if (_currentLines != null)
                UpdateTickLabels();
        }

        private void OnMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePos = e.GetPosition(rootGrid);
            ((UIElement)sender).CaptureMouse();
        }

        private void OnMouseLeftUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point currentPos = e.GetPosition(rootGrid);

            if (_isDragging)
            {
                double dx = currentPos.X - _lastMousePos.X;
                double dy = currentPos.Y - _lastMousePos.Y;

                _cameraAngleH += dx * 0.5;
                _cameraAngleV += dy * 0.3;

                _lastMousePos = currentPos;
                UpdateCamera();
            }

            // Tooltip — проверяем всегда при движении мыши
            Point viewportPos = e.GetPosition(viewport);
            CheckTooltip(viewportPos);
        }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _cameraDistance -= e.Delta * 0.005;
            if (_cameraDistance < 2) _cameraDistance = 2;
            if (_cameraDistance > 25) _cameraDistance = 25;
            UpdateCamera();
        }

        private void OnResetView(object sender, MouseButtonEventArgs e)
        {
            _cameraAngleH = 5;
            _cameraAngleV = 15;
            _cameraDistance = 8;
            UpdateCamera();
        }

        #endregion
    }

    /// <summary>
    /// Информация о сфере для tooltip
    /// </summary>
    public class SphereHitInfo
    {
        public Point3D Center3D { get; set; }
        public double RealX { get; set; }
        public double RealY { get; set; }
        public double Radius { get; set; }
        public string LineTitle { get; set; }
    }

    /// <summary>
    /// Данные линии графика
    /// </summary>
    public class PlotLineData
    {
        public List<SolutionPoint> Points { get; set; }
        public Color Color { get; set; }
        public string Title { get; set; }
    }
}