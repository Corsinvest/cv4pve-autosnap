@echo off
REM SPDX-License-Identifier: GPL-3.0-only
REM SPDX-FileCopyrightText: Copyright Corsinvest Srl
REM
REM PowerShell wrapper for cv4pve-autosnap hook scripts
REM This wrapper allows executing PowerShell scripts as hooks on Windows systems
REM Usage: --script=hooks/template-ps.bat

powershell.exe -ExecutionPolicy Bypass -File "%~dp0template.ps1"
exit /b %ERRORLEVEL%
