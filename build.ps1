$CurrentDir = Get-Location

try {
    $ReactAppPath = Join-Path $PSScriptRoot -ChildPath "ui-frontend" | Join-Path -ChildPath "src"
    Write-Host Building react app from $ReactAppPath
    cd $ReactAppPath
    
    npm install
    if (!$?) {
        throw "Failed to run 'npm install', make sure you have npm installed and read the error above."
    }
    npm run build-and-copy
    if (!$?) {
        throw "Failed to run 'npm install', please read the error above."
    }

    $CsProjPath = Join-Path $PSScriptRoot  -ChildPath "ui-webview-windows" | Join-Path  -ChildPath "ui-webview-windows.csproj"
    $OutputDir = Join-Path $PSScriptRoot "build"
    Write-Host Building csharp app from $CsProjPath
    dotnet build $CsProjPath -c Release -o $OutputDir
    if (!$?) {
        throw "Failed to run 'dotnet build', make sure you have dotnet installed and read the error above."
    }
    
    Write-Host "Successfully built XYVR in $OutputDir"
}
catch {
    Write-Error "Build has failed, reason: $_"
}
finally {
    Set-Location -Path $CurrentDir
}