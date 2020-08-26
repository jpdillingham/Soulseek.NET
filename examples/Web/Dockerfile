FROM mcr.microsoft.com/dotnet/core/aspnet:3.1

COPY api/bin/Release/netcoreapp3.1/publish app/

RUN mkdir /var/slsk
RUN mkdir /var/slsk/shared
RUN mkdir /var/slsk/download

ENV SLSK_OUTPUT_DIR=/var/slsk/download
ENV SLSK_SHARED_DIR=/var/slsk/shared

ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "app/WebAPI.dll"]