Remove-Item -Path ./AvaChat.Server/Migrations -Recurse -Force
Remove-Item -Path ./AvaChat.Client/Migrations -Recurse -Force

dotnet ef migrations add Init --project ./AvaChat.Server/AvaChat.Server.csproj --context ServerDbContext
dotnet ef migrations add Init --project ./AvaChat.Client/AvaChat.Client.csproj --context ClientDbContext

dotnet ef database update --project ./AvaChat.Server/AvaChat.Server.csproj --context ServerDbContext
dotnet ef database update --project ./AvaChat.Client/AvaChat.Client.csproj --context ClientDbContext