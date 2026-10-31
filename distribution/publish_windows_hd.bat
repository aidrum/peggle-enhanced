cd /d "%~dp0.."
call distribution\msvc_env.bat || exit /b 1
dotnet publish Peggle.Desktop -c Release -r win-x64 -p:PublishAot=true -p:IlcUseEnvironmentalTools=true -o Peggle.Desktop\bin\Publish\PeggleHD