# Test script for BaseProcessor.Application startup
# This script tests the processor application startup without requiring full infrastructure

Write-Host "Testing BaseProcessor.Application Startup" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Green
Write-Host ""

# Set environment for development
$env:DOTNET_ENVIRONMENT = "Development"

Write-Host "Environment: $env:DOTNET_ENVIRONMENT" -ForegroundColor Yellow
Write-Host "Testing processor startup (will timeout after 10 seconds)..." -ForegroundColor Yellow
Write-Host ""

# Build the project first
Write-Host "Building the project..." -ForegroundColor Cyan
dotnet build FlowOrchestrator.BaseProcessor.Application.csproj
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Test startup with timeout
Write-Host "Starting processor application (will stop after 10 seconds)..." -ForegroundColor Cyan

# Start the process in background
$process = Start-Process -FilePath "dotnet" -ArgumentList "run --project FlowOrchestrator.BaseProcessor.Application.csproj" -PassThru -NoNewWindow

# Wait for 10 seconds
Start-Sleep -Seconds 10

# Check if process is still running
if (!$process.HasExited) {
    Write-Host "✓ Processor application started successfully and is running!" -ForegroundColor Green
    Write-Host "✓ Application did not crash during startup" -ForegroundColor Green
    
    # Stop the process
    Write-Host "Stopping the processor application..." -ForegroundColor Yellow
    $process.Kill()
    $process.WaitForExit()
    Write-Host "✓ Application stopped successfully" -ForegroundColor Green
} else {
    Write-Host "✗ Processor application exited unexpectedly" -ForegroundColor Red
    Write-Host "Exit code: $($process.ExitCode)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Test completed successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps for full testing:" -ForegroundColor Yellow
Write-Host "1. Start RabbitMQ server" -ForegroundColor White
Write-Host "2. Start Hazelcast server" -ForegroundColor White
Write-Host "3. Start EntitiesManager API" -ForegroundColor White
Write-Host "4. Create a test protocol entity" -ForegroundColor White
Write-Host "5. Send test activity messages" -ForegroundColor White
