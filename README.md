# Soulseek.NET

[![build status](https://jpdillingham.visualstudio.com/Soulseek.NET/_apis/build/status/Soulseek.NET-CI)](https://jpdillingham.visualstudio.com/Soulseek.NET/_build/latest?definitionId=2)
[![codecov](https://codecov.io/gh/jpdillingham/Soulseek.NET/branch/master/graph/badge.svg)](https://codecov.io/gh/jpdillingham/Soulseek.NET)
[![quality](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=alert_status)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)
[![lines of code](https://sonarcloud.io/api/project_badges/measure?project=jpdillingham_Soulseek.NET&metric=ncloc)](https://sonarcloud.io/dashboard?id=jpdillingham_Soulseek.NET)
[![license](https://img.shields.io/github/license/jpdillingham/Soulseek.NET.svg)](https://github.com/jpdillingham/Soulseek.NET/blob/master/LICENSE)

A .NET Standard client library for the Soulseek network.

This library aims to provide more control over searches and downloads
from the Soulseek network, namely to support download automation and
quality control.

This library does NOT aim to provide the full functionality required to create 
a replacement for the desktop client.

The Soulseek network relies on sharing to operate.  If you're using this library to
download files, you should also run a copy of the desktop client to share a number of 
files proportional to your download activity.  Taking without giving goes against the
spirit of the network.

## Supported and Planned Functionality

- [ ] Private messaging
- [x] Searching the network 
- [x] Browsing individual user shares
- [x] Downloading of files

## Unsupported Functionality

- Sharing of files:
  - Providing the server with a list of shared files
  - Accepting or responding to distributed search requests
  - Uploading files
- Downloads from users behind a firewall
- Chat rooms
