# Soulseek.NET

[![Azure DevOps builds](https://img.shields.io/vso/build/jpdillingham/3416406e-e4a8-4bf2-8a6e-75beec2d24e3/2.svg)](https://jpdillingham.visualstudio.com/Soulseek.NET/_build?definitionId=2)
[![license](https://img.shields.io/github/license/jpdillingham/Soulseek.NET.svg)](https://github.com/jpdillingham/Soulseek.NET/blob/master/LICENSE)

A .NET Standard client library for the Soulseek network.

This library aims to provide more control over searches and downloads
from the Soulseek network, namely to support download automation and
quality control.

This library does NOT aim to provide the full functionality required to create 
a replacement for the desktop client.

The Soulseek network relys on sharing to operate.  If you're using this library to
download files, you should also run a copy of the desktop client to share a number of 
files proportional to your download activity.  Taking without giving goes against the
spirit of the network.

## Supported and Planned Functionality

- [ ] Private messaging
- [x] Searching the network 
- [x] Browsing individual user shares
- [ ] Downloading of files

## Unsupported Functionality

- Sharing of files:
  - Providing the server with a list of shared files
  - Accepting or responding to distributed search requests
  - Uploading files
- Downloads from users behind a firewall
- Chat rooms