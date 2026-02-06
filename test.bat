@echo off
echo ========================================
echo   MistralSDK - Tests Unitaires
echo ========================================
echo.

echo Execution des tests unitaires...
dotnet test --filter "TestCategory=Unit" --verbosity normal
if %errorlevel% neq 0 (
    echo.
    echo ATTENTION: Certains tests ont echoue.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================
echo   Tous les tests unitaires ont reussi!
echo ========================================
echo.
pause
