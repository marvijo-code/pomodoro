# Start the backend server
Start-Process powershell -ArgumentList "npm run server"

# Start the frontend
Start-Process powershell -ArgumentList "npm run dev"

# Keep the script running
Write-Host "Servers are running. Press Ctrl+C to stop all servers."
while ($true) { Start-Sleep -Seconds 1 } 