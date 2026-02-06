@echo off
echo ========================================
echo   MistralSDK - Build
echo ========================================
echo.

echo [1/2] Restauration des packages NuGet...
dotnet restore
if %errorlevel% neq 0 (
    echo.
    echo ERREUR: La restauration des packages a echoue.
    pause
    exit /b %errorlevel%
)

echo.
echo [2/2] Compilation de la solution...
dotnet build --configuration Release --no-restore
if %errorlevel% neq 0 (
    echo.
    echo ERREUR: La compilation a echoue.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================
echo   Build termine avec succes!
echo ========================================
echo.
echo Les fichiers compiles sont dans:
echo   MistralSDK\bin\Release\net8.0\
echo.
pause
