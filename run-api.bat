@echo off
echo =====================================
echo ErpCloud API - Quick Start
echo =====================================
echo.
cd src\Api
echo Starting API on http://localhost:5000
echo.
echo Endpoints:
echo   - Swagger UI: http://localhost:5000/swagger
echo   - Health Check: http://localhost:5000/health
echo   - Root: http://localhost:5000/
echo.
dotnet run --urls "http://localhost:5000"
