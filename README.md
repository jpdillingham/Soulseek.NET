# Soulseek.NET
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

- [x] Searching the network 
- [x] Browsing individual user shares
- [ ] Downloading of files
- [ ] Private messaging

## Unsupported Functionality

- Downloads from users behind a firewall
- Sharing of files:
  - Accepting or responding to distributed search requests
  - Providing the server with a list of shared files
  - Uploading files
- Chat rooms