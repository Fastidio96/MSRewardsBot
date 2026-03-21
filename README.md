# Microsoft Rewards Bot

![Server](https://img.shields.io/badge/Server-Windows%20%7C%20Linux%20%7C%20Docker-blue)
[![Client](https://img.shields.io/badge/Client-WPF%20(Windows%20only)-informational)](https://github.com/fastidio96/MSRewardsBot/releases/latest)
[![Docker Image](https://img.shields.io/docker/v/fastidio96/msrb?label=Docker%20Image)](https://hub.docker.com/r/fastidio96/msrb)
[![Docker Pulls](https://img.shields.io/docker/pulls/fastidio96/msrb?label=Docker%20Pulls)](https://hub.docker.com/r/fastidio96/msrb)
![Server](https://img.shields.io/badge/Server-.NET%209-success)
![License](https://img.shields.io/badge/License-MIT-green)

> ⚠️ **Disclaimer**  
> This project **violates Microsoft Rewards Terms of Service**.  
> It was created **for educational and entertainment purposes only**.  
> I take **no responsibility** for any misuse, bans, penalties, or damages resulting from the use of this software.  
> **Use it at your own risk.**

---

## 📑 Table of Contents

- [Overview](#-overview)
- [Features](#-features)
- [Automation & Stealth Features](#%EF%B8%8F-automation--stealth-features)
  - [Human-like Automation](#-human-like-automation)
  - [Anti-Bot Browser Patching)](#%EF%B8%8F-anti-bot-browser-patching)
- [Authentication & Multi-User Support](#-authentication--multi-user-support)
- [Project Structure](#-project-structure)
  - [Client (WPF – Windows only)](#%EF%B8%8F-client-wpf--windows-only)
  - [Server (Cross-platform)](#-server-cross-platform)
- [Server Configuration](#%EF%B8%8F-server-configuration)
- [Supported Platforms](#-supported-platforms)
- [Technologies Used](#-technologies-used)
- [Legal Notice](#%EF%B8%8F-legal-notice)
- [Final Notes](#-final-notes)

---

## 📌 Overview

This project is a **Microsoft Rewards automation bot** composed of a **Windows client** and a **cross-platform server**.

The goal is to automate searches and periodically update Microsoft Rewards statistics using a **scheduled and configurable system**.

---

## ✨ Features

| Feature | Description |
|-------|-------------|
| 🎯 Dashboard Tasks | Collect additional Microsoft Rewards points available in the dashboard |
| 🖥️ PC Searches | Perform automated searches from desktop |
| 📱 Mobile Searches | Perform automated searches emulating a mobile device |

---

## ⚙️ Automation & Stealth Features

### 🧠 Human-like Automation

| Feature | Description |
|--------|--------|
| 🤖 Human-like Behavior | Write searches as a real user |
| 🤖 Human-like Behavior | Scroll pages naturally |
| 🤖 Human-like Behavior | Randomized waiting times between actions |
| 🔍 Search Logic | Searches based on real keywords that change over time |
| 🌍 Localization | Automatically imports the current locale |
| 🧭 Browser Identity | Realistic user agent |
| 🧭 Browser Identity | Realistic viewport size |

### 🛡️ Anti-Bot Browser Patching

The browser environment is patched using **JavaScript injection** to reduce automation fingerprints.

- Remove `navigator.webdriver` and related automation flags  
- Provide believable **plugins** and **languages**  
- Patch **WebGL `getParameter`** to return realistic vendor/renderer  
- Ensure `window.chrome` exists and looks native  
- Fake `deviceMemory` and `hardwareConcurrency` with realistic values  
- Remove Playwright/CDP visible globals  
- Provide `chrome.loadTimes` and `chrome.csi` shims used by some detectors  
- Worker patch: ensure workers see the same navigator-like values and no webdriver  
- Invisible iframe getter patch: implement a native-like `contentWindow` getter  
- Patch stack trace strings used by some CDP detection techniques  
- Remove suspicious iframe overrides done by naive automation patches  
- Small timing/randomization helpers to make execution less deterministic

---

## 🔐 Authentication & Multi-User Support

The application is designed with a **multi-user client–server architecture**.

### 👤 User Accounts
- Users can register and log in with a **username and password**
- Credentials are **stored on the server**
- Passwords are hashed using a **salted hashing algorithm**

### 🔑 Microsoft Account Data
- **Microsoft account credentials are never stored**
- Authentication is handled via the browser
- Only **session cookies** are stored in the **server database**
- Cookies are used **exclusively by the server** for automation
- Once sent, cookies are **never transmitted back to the client**

### 🔌 Client–Server Connection
- The Windows client can connect to a **custom server endpoint**
- From the **Settings window**, users can:
  - Configure a new server connection
  - Replace the current connection with another server

This design enables safe **multi-user support** while keeping sensitive data protected.

---

## 🧩 Project Structure

### 🖥️ Client (WPF – Windows only)

The client is a **WPF application** running **exclusively on Windows**.

**Features:**
- Add new Microsoft accounts
- Register and login
- View Microsoft Rewards account statistics
- Communicate with the server for updates

> ❗ The client **cannot run on Linux or macOS**.

---

### 🖧 Server (Cross-platform)

The server handles **all core logic** and automation.

It is composed of **four main components**:

#### 🌐 Browser Manager
- Executes the jobs from the task scheduler
- Manages browser automation using **Playwright**
- Supports:
  - **Firefox** (less detectable, less stable)
  - **Chromium** (more detectable, more stable)

#### ⏱️ Task Scheduler
- Manages scheduled jobs:
  - Searches (pc & mobile)
  - Additional points (extra points from the dashboard)
  - Dashboard updates
  - Keyword refresh

#### 🧠 Core Server
- Handles client connections
- Manages Microsoft accounts
- Decides **when and which job** to run for each account

#### 🔑 Keyword Provider
- Fetches valid keywords for searches
- Supplies keywords to the Core Server

---

## ⚙️ Server Configuration

The server can be fully configured using the `appsettings.json` file.

### 🔌 Network Configuration
```json
"IsHttpsEnabled": false,
"ServerHost": "0.0.0.0",
"ServerPort": "10500"
```

### 🌍 Browser Settings
```json
"UseFirefox": true
```
> - `true` → Firefox (less detectable, less stable)  
> - `false` → Chromium (more detectable, more stable)  

### ⏳ Search Timing (Randomized)
```json
"MinSecsWaitBetweenSearches": 180,
"MaxSecsWaitBetweenSearches": 600
```
> The values are expressed in seconds

### 📊 Scheduled Checks
```json
"DashboardCheck": "12:00:00",
"SearchesCheck": "06:00:00",
"KeywordsListRefresh": "03:00:00"
```
> The format is hh:mm:ss

### 🔑 Keywords Configuration
```json
"KeywordsListCountries": [ "IT", "US", "GB", "DE", "FR", "ES" ]
```

> **Behavior:**
> - 20 keywords are downloaded per country
> - Each keyword is consumed after use
> - When all keywords are used, the list restarts
> - Intended behavior: **keywords should never be reused once consumed**

### 📋 Logging
```json
"WriteLogsOnFile": true,
"LogsGroupedCategories": true,
"MinimumLogLevel": "Debug"
```

---

## 🐧 Supported Platforms

### 🖥️ Client
- ✅ Windows only (available on [GitHub Releases](https://github.com/fastidio96/MSRewardsBot/releases/latest))

### 🖧 Server
- ✅ Linux
- ✅ Windows (Console / Windows Service)
- ✅ Docker (Dockerfile / Docker Compose)
- 🐳 Docker Image available on [Docker Hub](https://hub.docker.com/r/fastidio96/msrb)

---

## 🚀 Technologies Used

- C#
- WPF
- ASP.NET / .NET 9
- SignalR
- Playwright
- Docker

---

## ⚠️ Legal Notice

Automating Microsoft Rewards actions **violates Microsoft’s Terms of Service**.

This project is independent and is **not affiliated, endorsed, sponsored, or approved by Microsoft™**.  
Use this software at your own risk. The author is **not responsible** for any issues arising from its use.

This repository exists **only for learning, experimentation, and fun**.

---

## ⭐ Final Notes

If you find this project interesting, feel free to explore the code and learn from it.  
Please **do not use it on real accounts**.
