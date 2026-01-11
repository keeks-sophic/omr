param(
    [string]$Configuration = "Debug"
)

dotnet build -c $Configuration
dotnet run

