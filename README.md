# Wilnaatahl

A tool for visualizing the genealogical relationships of Gitxsan huwilp members.

# Development Instructions

These instructions assume Windows 11 and PowerShell, but should be easily adaptable to other environments.

## 🛠️ Dev Environment Setup (Windows 11, PowerShell)

These instructions assume **Git** and **Visual Studio Code** are already installed.

---

### ✅ 1. Install Node Version Manager for Windows (nvm-windows)

> Allows you to install and switch between Node.js versions easily on Windows.

1. Download and install the latest `nvm-setup.exe` from:  
   👉 https://github.com/coreybutler/nvm-windows/releases

2. Open a new PowerShell window, then install and use a stable Node.js version:

```powershell
nvm install 23.11.0
nvm use 23.11.0
```

3. Verify the installation:

```powershell
node -v    # Should return v23.11.0
npm -v     # Should return a recent npm version (e.g. 10.x)
```

### ✅ 2. Install the .NET SDK

This project targets .NET 9. To make setup easier we pin the SDK via `global.json` so the correct SDK is used automatically by the `dotnet` CLI.

If you don't have the pinned SDK installed, install any 9.0.x SDK from:

👉 https://dotnet.microsoft.com/download

Verify installation:

```powershell
dotnet --version   # Should return 9.0.x
```

### ✅ 3. Restore Local Tools and Dependencies

From the project root, run:

```powershell
dotnet tool restore
dotnet restore
npm install
```

## Commands for Dev Inner Loop

The following terminal commands are your dev inner loop:

- To build and run in the dev server for iterative development: `npm run dev`
- To build for deployment: `npm run build`
- To host the deployment-ready build locally for testing: `npx serve dist`
- To run unit tests: `npm test`
- To format the TypeScript code with Prettier: `npm run format`
  - Prettier is configured via the `.prettierrc` file in the project root. Files and folders to ignore are listed in `.prettierignore`.
- To collect Code Coverage data and generate a Code Coverage report:

```powershell
npm run coverage                                               # Will output link to coverage .xml file
npm run report --coveragefile=<path-to-coverage-xml-file>  # Pass link to coverage .xml file
```
