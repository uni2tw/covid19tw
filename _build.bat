rem dotnet publish -r win-x64 -c Release /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true /p:SelfContained=true
dotnet publish -r win-x64 -c Release
xcopy assets C:\Git2019\Utilities\covid19tw\bin\Release\net6.0\win-x64\publish\assets\ /Y