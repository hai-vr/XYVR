#!/bin/sh

set -e

SCRIPT_DIR=$( cd -- "$( dirname -- "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )

ReactAppPath=$SCRIPT_DIR/ui-frontend/src
echo Building react app from $ReactAppPath

# TODO: maybe add nicer error messages, currently it just says "npm: command not found"
cd $ReactAppPath
npm install
npm run build-and-copy

CsProjPath=$SCRIPT_DIR/ui-photino-linux/ui-photino-linux.csproj
OutputDir=$SCRIPT_DIR/build
mkdir -p $OutputDir
echo Building csharp app from $CsProjPath

dotnet build $CsProjPath -c Release -o $OutputDir
