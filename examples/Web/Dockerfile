FROM microsoft/dotnet/core/runtime:2.2.5
COPY api/bin/Release/netcoreapp2.2/publish app/
ENTRYPOINT ["dotnet", "app/WebAPI.dll"]