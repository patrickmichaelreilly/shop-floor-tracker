@echo off
echo Building SDF Reader (.NET Framework 4.8)...

REM Restore NuGet packages
nuget restore packages.config -PackagesDirectory ..\packages

REM Build the project using MSBuild
msbuild SdfReader.csproj /p:Configuration=Debug /p:Platform=AnyCPU

if %errorlevel% equ 0 (
    echo Build successful!
    echo SdfReader.exe created in bin\Debug\
) else (
    echo Build failed!
)

pause