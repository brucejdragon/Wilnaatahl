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

### ✅ 2. Install Project Dependencies

Navigate to the root of the project directory and run:

   ```powershell
   npm install
   ```

This installs all dependencies listed in `package.json`, including React, Three.js, and Vite.

### ✅ 3. (Optional) Install `serve` to Preview Production Builds

If you want to serve the `dist` folder (after building) with a static server:

   ```powershell
   npm install -g serve
   ```

This will start a local static server, typically accessible at: http://localhost:3000

## Commands for Dev Inner Loop

The following terminal commands are your dev inner loop:
- To build and run in the dev server for iterative development: `npm run dev`
- To build for deployment: `npm run build`
- To host the deployment-ready build locally for testing: `serve dist`
