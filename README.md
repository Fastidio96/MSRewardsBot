# Microsoft Rewards Bot

![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20Docker-blue)
![Client](https://img.shields.io/badge/client-WPF%20(Windows%20only)-informational)
![Server](https://img.shields.io/badge/server-.NET-success)
![License](https://img.shields.io/badge/license-MIT-green)

> ⚠️ **Disclaimer**  
> This project **violates Microsoft Rewards Terms of Service**.  
> It was created **for educational and entertainment purposes only**.  
> I take **no responsibility** for any misuse, bans, penalties, or damages resulting from the use of this software.  
> **Use it at your own risk.**

---

## 📌 Overview

This project is a **Microsoft Rewards automation bot** composed of a **Windows client** and a **cross-platform server**.

The goal of the project is to automate searches and periodically update Microsoft Rewards statistics using a scheduled and configurable system.

---

## 🧩 Project Structure

### 🖥️ Client (WPF – Windows only)

The client is a **WPF application** that runs **exclusively on Windows**.

**Features:**
- Add new Microsoft accounts
- Register and login
- View Microsoft Rewards account statistics
- Communicate with the server for updates

> ❗ The client **cannot run on Linux or macOS**.

---

### 🖧 Server (Cross-platform)

The server handles **all core logic** and automation.

It is composed of **three main components**:

#### 🌐 Browser Manager
- Manages browser automation using **Playwright**
- Supports:
  - **Firefox** (less detectable, less stable)
  - **Chromium** (more stable)

#### ⏱️ Task Scheduler
- Manages scheduled jobs such as:
  - Searches
  - Dashboard updates
  - Keyword refresh

#### 🧠 Core Server
- Handles client connections
- Manages Microsoft accounts
- Decides **when and which job** should be executed for each account

---

## ⚙️ Server Configuration

The server can be fully configured using the `appsettings.json` file.

### 🔌 Network Configuration
```json
"IsHttpsEnabled": false,
"ServerHost": "0.0.0.0",
"ServerPort": "10500"
```

### 🔄 Client Updater
```json
"IsClientUpdaterEnabled": false
```
> Enabling the updater increases CPU usage.

### 🌍 Browser Settings
```json
"UseFirefox": true
```
- `true` → Firefox (less detectable, less stable)  
- `false` → Chromium (more detectable, more stable)  

### ⏳ Search Timing (Randomized)
```json
"MinSecsWaitBetweenSearches": 180,
"MaxSecsWaitBetweenSearches": 600
```

### 📊 Scheduled Checks
```json
"DashboardCheck": "12:00:00",
"SearchesCheck": "06:00:00",
"KeywordsListRefresh": "03:00:00"
```

### 🔑 Keywords Configuration
```json
"KeywordsListCountries": [ "IT", "US", "GB", "DE", "FR", "ES" ]
```

**Behavior:**
- 50 keywords are downloaded per country
- Each keyword is consumed after use
- When all keywords are used, the list restarts
- Intended behavior: **keywords should never be reused once consumed**

### 🪵 Logging
```json
"WriteLogsOnFile": true,
"LogsGroupedCategories": true,
"MinimumLogLevel": "Debug"
```

---

## 🐧 Supported Platforms

### Client
- ✅ Windows only

### Server
- ✅ Linux
- ✅ Windows
  - Console application
  - Windows Service
- ✅ Docker
  - Dockerfile
  - Docker Compose

---

## 🚀 Technologies Used

- C#
- WPF
- ASP.NET / .NET
- SignalR
- Playwright
- Docker

---

## ⚠️ Legal Notice

Automating Microsoft Rewards actions **violates Microsoft’s Terms of Service**.

This project is an independent work and is **not affiliated, endorsed, sponsored, or approved by Microsoft™** or any other company mentioned in this repository.  
Microsoft™ is a registered trademark of Microsoft Corporation. All references to Microsoft products, services, or trademarks are used for informational purposes only and do not imply any association with or endorsement by Microsoft.  
Use this software at your own risk. The author is not responsible for any issues arising from the use of this project.

This repository exists **only for learning, experimentation, and fun**.

---

## ⭐ Final Notes

If you find this project interesting, feel free to explore the code and learn from it.  
Please **do not use it on real accounts**.

Have fun and stay safe 🚀
