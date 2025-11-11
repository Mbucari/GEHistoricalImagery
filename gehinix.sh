#!/bin/bash

dotnet_channel="10.0"
dotnet=~/.dotnet/dotnet

install_dotnet() {
    echo "Downloading and installing the .net $dotnet_channel SDK."
    wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
    chmod +x ./dotnet-install.sh
    ./dotnet-install.sh --channel $dotnet_channel
}

if [ ! -f $dotnet ]; then
    install_dotnet
fi

dotnet_versions=$($dotnet --list-sdks)
regex="10\.0\.[0-9]{3}"

if [[ ! $dotnet_versions =~ $regex ]]; then
    install_dotnet
fi

projectDir="./GEHistoricalImagery-master/src/GEHistoricalImagery"
csproj="$projectDir/GEHistoricalImagery.csproj"
buildDir="$projectDir/bin/Release"

if [ ! -f $csproj ]; then    
    echo "Cloning the GEHistoricalImagery master repo"
    wget https://github.com/Mbucari/GEHistoricalImagery/archive/master.tar.gz -O GEHistoricalImagery.tar.gz
    tar -xf GEHistoricalImagery.tar.gz -C ./
fi

if [ ! -f "$buildDir/GEHistoricalImagery.dll" ]; then
    echo "Building GEHistoricalImagery"
    $dotnet build $csproj -c Release /p:DefineConstants=LINUX
fi

cd $buildDir
$dotnet "GEHistoricalImagery.dll" "$@"
