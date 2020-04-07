# Soulseek.NET

[![NuGet version](https://img.shields.io/nuget/v/Soulseek.svg)](https://www.nuget.org/packages/Soulseek/)
[![Semantic Versioning](https://img.shields.io/badge/semver-2.0.0-3D9FE0.svg)](https://semver.org/)
[![license](https://img.shields.io/github/license/jpdillingham/Soulseek.NET.svg)](https://github.com/jpdillingham/Soulseek.NET/blob/master/LICENSE)

[![CircleCI](https://circleci.com/gh/jpdillingham/Soulseek.NET/tree/master.svg?style=shield)](https://circleci.com/gh/jpdillingham/Soulseek.NET/tree/master)
[![codecov](https://codecov.io/gh/jpdillingham/Soulseek.NET/branch/master/graph/badge.svg)](https://codecov.io/gh/jpdillingham/Soulseek.NET)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=reliability_rating)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=security_rating)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)

A .NET Standard client library for the Soulseek network.

# Quick Start

Install from [NuGet](https://www.nuget.org/packages/Soulseek/).

## Connect to a Soulseek server

```c#
var client = new SoulseekClient();
await client.ConnectAsync("Username", "Password");
```

## Search for something

```c#
IEnumerable<SearchResponse> responses = await Client.SearchAsync(SearchQuery.FromText("some search"));
```

Note: `SearchAsync` accepts a `SearchQuery` with the constructor `SearchQuery(string query, IEnumerable<string> exclusions, int? minimumBitrate, int? minimumFileSize, int? minimumFilesInFolder, bool isVBR, bool isCBR)`, allowing all of the options provided by the official client.

`SearchResponse` has the following shape:

```c#
int FileCount
IReadOnlyCollection<File> Files
int FreeUploadSlots
long QueueLength
int Token
int UploadSpeed
string Username
```

`File` has a number of properties; the one you'll need for downloading is `Filename`.

## Download a file

```c#
byte[] file = await Client.DownloadAsync("some username", "some fully qualified filename");
```

OR (ideally)

```c#
var fs = new FileStream("c:\downloads\local filename", FileMode.Create);
await Client.DownloadAsync("some username", "some fully qualified filename", fs);
```

Note: Download to a stream where possible to reduce memory overhead.

# Documentation

Coming soon!

# Example Web Application

Included is a small web application with a limited feature set:

* File search
* User browsing
* File downloads
* File uploads

It's important to note that there are currently no controls over uploads; anything you share can be downloaded by any number of people at any given time.  With this in mind, consider sharing a small number of files (or none at all) from the example.

## Running with Docker

A Docker image containing the application can be pulled from [jpdillingham/slsk-web-example](https://hub.docker.com/repository/docker/jpdillingham/slsk-web-example).

A minimal `run` would look like:

```
docker run -i \
    -p 5000:5000 \
    -v <path/to/downloads>:/var/slsk/download \
    -v <path/to/shared>:/var/slsk/shared \
    -e "SLSK_USERNAME=<your username>" \
    -e "SLSK_PASSWORD=<your password>" \
    slsk-web-example:latest
```

The application will then be accessible on port 5000 (e.g. http://localhost:5000).  With this configuration the application won't be able to accept incoming connections and won't connect to the distributed network.  You may receive limited search results and users won't find your files via search.  Other users may have difficulty browsing your shares.

The full set of options is as follows:

```
docker run -i \
    -p 5000:5000 \
    -p 50000:50000 \
    -v <path/to/downloads>:/var/slsk/download \
    -v <path/to/shared>:/var/slsk/shared \
    -e "SLSK_USERNAME=<your username>" \
    -e "SLSK_PASSWORD=<your password>" \
    -e "SLSK_LISTEN_PORT=50000" \
    -e "SLSK_ENABLE_DNET=true" \
    -e "SLSK_DNET_CHILD_LIMIT=10" \
    -e "SLSK_DIAGNOSTIC=Info" \
    -e "SLSK_CONNECT_TIMEOUT=5000" \
    -e "SLSK_INACTIVITY_TIMEOUT=15000" \
    slsk-web-example:latest
```

With this configuration the application will listen on port 50000 and will connect to the distributed network, allowing up to 10 child connections.  The application shouldn't have any trouble connecting provided you've forwarded port 50000 properly, and will receive and respond to distributed search requests.

## Running without Docker

The example application is split into two projects; a .NET Core 2.2 WebAPI and a React application bootstrapped with create-react-app.  If you'd like to run these outside of Docker you'll need to start both applications; `dotnet run` for the API and `yarn|npm start` for the React application.  You can connect to http://localhost:3000, or the API serves Swagger UI at http://localhost:5000/swagger.

A build script included in the `bin` directory of the example which will build the React app, copy the static files to the wwwroot directory of the API, build the API, then attempt to build the Docker image.