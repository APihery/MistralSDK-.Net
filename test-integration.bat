@echo off
echo ========================================
echo   MistralSDK - Tests d'Integration
echo ========================================
echo.

REM Charger la cle API depuis le fichier api-key.txt
if exist "api-key.txt" (
    set /p MISTRAL_API_KEY=<api-key.txt
    echo Cle API chargee depuis api-key.txt
) else (
    echo ERREUR: Le fichier api-key.txt n'existe pas.
    echo.
    echo Creez un fichier api-key.txt contenant votre cle API Mistral.
    echo Vous pouvez obtenir une cle sur: https://console.mistral.ai/
    echo.
    pause
    exit /b 1
)

REM Activer les tests d'integration
set MISTRAL_ENABLE_INTEGRATION_TESTS=true

echo.
echo Execution des tests d'integration...
echo (Ces tests font de vrais appels a l'API Mistral)
echo.

dotnet test --filter "TestCategory=Integration" --verbosity normal
if %errorlevel% neq 0 (
    echo.
    echo ATTENTION: Certains tests d'integration ont echoue.
    pause
    exit /b %errorlevel%
)

echo.
echo ========================================
echo   Tous les tests d'integration ont reussi!
echo ========================================
echo.
pause
