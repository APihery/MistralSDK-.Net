@echo off
echo ========================================
echo   MistralSDK - Tests avec Couverture
echo ========================================
echo.

echo Execution des tests avec collecte de couverture...
dotnet test --filter "TestCategory=Unit" --collect:"XPlat Code Coverage" --results-directory:"./TestResults"

if %errorlevel% neq 0 (
    echo.
    echo ATTENTION: Certains tests ont echoue.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================
echo   Tests termines!
echo ========================================
echo.
echo Les rapports de couverture sont dans:
echo   TestResults\
echo.
echo Pour generer un rapport HTML, installez ReportGenerator:
echo   dotnet tool install -g dotnet-reportgenerator-globaltool
echo   reportgenerator -reports:"TestResults\**\coverage.cobertura.xml" -targetdir:"TestResults\CoverageReport" -reporttypes:Html
echo.
pause
