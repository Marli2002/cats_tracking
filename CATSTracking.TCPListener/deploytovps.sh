#!/bin/bash

# 💻 Local Build
echo "🐾 Publishing project for Linux (self-contained)..."
dotnet publish -c Release -r linux-x64 --self-contained true -o ./out

# 🌐 Upload via SCP
echo "📤 Uploading to VPS..."
ssh root@168.119.165.47 "rm -rf /home/apps/catslistener/*"
scp -r ./out/* root@168.119.165.47:/home/apps/catslistener/

# 🛠️ Post-upload configuration and start via SSH
ssh root@168.119.165.47 << 'ENDSSH'
echo "📂 Moving files if needed..."
cd /home/apps/catslistener

# 🧼 Cleanup nested out dir just in case
if [ -d "./out" ]; then
    mv ./out/* ./
    rmdir ./out
fi

echo "🔓 Making binary executable..."
chmod +x /home/apps/catslistener/catslistener

echo "🚀 Starting listener with nohup..."
nohup /home/apps/catslistener/catslistener > /home/apps/output.log 2>&1 &
ENDSSH

echo "✅ catslistener deployed and running in background!"