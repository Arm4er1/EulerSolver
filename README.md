# 📐 EulerSolver

> *Numerical ODE solver using Euler and Modified Euler methods — with visualization, export, and MATLAB comparison.*

---

## 📖 About

**EulerSolver** is a desktop application for solving **ordinary differential equations (ODEs)** numerically using the **Euler** and **Modified Euler (Heun's)** methods. Built with **C# / .NET + WPF**, the app allows users to input a differential equation, configure solver parameters, visualize the solution in 2D/3D, compare results with MATLAB output, and export data to Excel or Word.

Built as an educational/pet project to practice numerical methods, expression parsing, and WPF desktop development with MVVM architecture.

---

## 🏗️ Project Structure

```
EulerSolver/
├── EulerSolver.Core/               # Core logic library
│   ├── Models/
│   │   ├── DifferentialEquation.cs # ODE model
│   │   ├── SolutionPoint.cs        # Single step result
│   │   ├── SolverResult.cs         # Full solution result
│   │   ├── ComparisonPoint.cs      # Euler vs Modified comparison
│   │   └── MatlabComparisonPoint.cs
│   └── Services/
│       ├── EulerSolver.cs          # Classic Euler method
│       ├── ModifiedEulerSolver.cs  # Modified Euler (Heun's method)
│       └── ExpressionParser.cs     # Math expression parser
│
├── ViewModels/                     # MVVM layer
│   ├── MainViewModel.cs
│   ├── BaseViewModel.cs
│   └── RelayCommand.cs
│
├── Views/                          # WPF windows
│   ├── MainWindow.xaml             # Main solver UI
│   ├── GraphWindow.xaml            # 2D solution graph
│   ├── ComparisonWindow.xaml       # Euler vs Modified comparison
│   ├── MatlabComparisonWindow.xaml # vs MATLAB output
│   ├── AboutWindow.xaml
│   ├── AuthorWindow.xaml
│   └── SplashWindow.xaml
│
├── Controls/
│   └── Graph3DControl.xaml         # 3D graph control
│
├── Services/
│   ├── ExcelExportService.cs       # Export to .xlsx
│   ├── WordExportService.cs        # Export to .docx
│   └── MatlabService.cs            # MATLAB integration
│
└── Help/                           # Built-in CHM help
    ├── index.html
    ├── method.html                 # Method description
    ├── syntax.html                 # Expression syntax guide
    ├── examples.html
    ├── comparison.html
    └── matlab.html
```

---

## ⚙️ Tech Stack

![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D6?style=for-the-badge&logo=windows&logoColor=white)
![MVVM](https://img.shields.io/badge/MVVM-FF6F00?style=for-the-badge&logo=blueprint&logoColor=white)
![MATLAB](https://img.shields.io/badge/MATLAB-E16737?style=for-the-badge&logo=mathworks&logoColor=white)

---

## 🎮 Features

- ➕ Input any first-order ODE as a math expression (e.g. `x + y`, `sin(x) * y`)
- 🔢 **Classic Euler method** and **Modified Euler (Heun's) method** solvers
- 📊 **2D graph** of the numerical solution
- 🧊 **3D graph** visualization via custom `Graph3DControl`
- 🔍 **Side-by-side comparison** of Euler vs Modified Euler results
- 🧪 **MATLAB comparison** — import MATLAB output and compare accuracy
- 📤 **Export to Excel** (.xlsx) and **Word** (.docx)
- 📘 **Built-in CHM help** with method description, syntax guide and examples
- 🖥️ Splash screen + About/Author windows

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Windows OS (WPF is Windows-only)
- Visual Studio 2022+
- *(Optional)* MATLAB for comparison feature

### Run

1. **Clone the repository**
   ```bash
   git clone https://github.com/Arm4er1/EulerSolver.git
   cd EulerSolver
   ```

2. **Open in Visual Studio**
   ```
   Open EulerSolver.sln → Build → Run
   ```

---

## 🧮 Supported Methods

| Method | Description |
|--------|-------------|
| **Euler** | Classic first-order explicit method: `y₁ = y₀ + h·f(x₀, y₀)` |
| **Modified Euler (Heun's)** | Predictor-corrector scheme with improved accuracy |

---

## 📫 Contact

[![Telegram](https://img.shields.io/badge/Telegram-26A5E4?style=for-the-badge&logo=telegram&logoColor=white)](https://t.me/Arm4er1)
[![Gmail](https://img.shields.io/badge/Gmail-EA4335?style=for-the-badge&logo=gmail&logoColor=white)](mailto:arm4er@gmail.com)
