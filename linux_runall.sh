#!/bin/bash

dotnet run --project ./CATSTracking.API/CATSTracking.API.csproj &
PID1=$!

dotnet run --project ./CATSTracking.UI/CATSTracking.UI.csproj &
PID2=$!

echo "Starting projects..."
sleep 10
clear
echo "API: http://localhost:6000/swagger"
echo "UI: http://localhost:5000/swagger"

xdg-open "http://localhost:5000" &
xdg-open "http://localhost:6000/swagger" &

trap 'kill $PID1 $PID2; sleep 1;
       lsof -t -i :5000 | xargs -r kill -9;
       lsof -t -i :6000 | xargs -r kill -9;
       exit' SIGINT

# Wait for both processes
wait $PID1
wait $PID2
