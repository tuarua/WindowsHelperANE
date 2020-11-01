$currentDir = (Get-Item -Path ".\" -Verbose).FullName
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri https://github.com/tuarua/FreSharp/releases/download/2.5.0/FreSharp.ane?raw=true -OutFile "$currentDir\..\native_extension\ane\FreSharp.ane"
