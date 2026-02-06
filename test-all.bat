@echo off
echo ========================================
echo   MistralSDK - Tous les Tests
echo ========================================
echo.

REM Charger la cle API depuis le fichier api-key.txt si disponible
if exist "api-key.txt" (
    set /p MISTRAL_API_KEY=<api-key.txt
    set MISTRAL_ENABLE_INTEGRATION_TESTS=true
    echo Cle API chargee - Tests d'integration actives
) else (
    echo Fichier api-key.txt non trouve - Tests d'integration desactives
    set MISTRAL_ENABLE_INTEGRATION_TESTS=false
)

echo.
echo Execution de tous les tests...
dotnet test --verbosity normal
if %errorlevel% neq 0 (
    echo.
    echo ATTENTION: Certains tests ont echoue.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================
echo   Tous les tests ont reussi!
echo ========================================
echo.
pause
