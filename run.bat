@echo off
echo Building and running RestaurantManager.Wpf...
dotnet run --project RestaurantManager.Wpf
if %ERRORLEVEL% NEQ 0 (
    echo.
    echo The application exited with an error (Exit Code: %ERRORLEVEL%).
    pause
)
