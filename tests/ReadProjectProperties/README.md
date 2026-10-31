#

```
cd E:\eplan_files\EplanSctripts\tests\ReadProjectProperties

dotnet build -c Debug
```

```
Copy-Item ".\bin\Debug\net481\ReadProjectProperties.exe" `
"E:\Eplan\Platform\2026.0.3\Bin\" -Force

cd E:\Eplan\Platform\2026.0.3\Bin

.\ReadProjectProperties.exe
```