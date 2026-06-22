param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

dotnet publish `
    .\src\GiglingBroadcastDeck.App\GiglingBroadcastDeck.App.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishProfile=win-x64-self-contained
