### 0.1.0 - 15.05.2020
* Migration to .NET 6
* Dependencies update

### 0.0.13 - 21.07.2021
* Dependencies update

### 0.0.12 - 19.04.2021
* Continuous deployment

### 0.0.11 - 18.04.2021
* Lazy server start (on first request instead of extension actication)

### 0.0.10 - 17.04.2021
* Fixed typo in Action name

### 0.0.9 - 17.04.2021
* Fixed process leak
* Server API process stopped on VSCode close (extension deactivation)

### 0.0.8 - 17.04.2021
* Added `OpenXml Package Explorer` output channel
* Improved application logging

### 0.0.7 - 13.04.2021
* Dynamic port number for API Server [#10](https://github.com/sergey-tihon/vscode-openxml-explorer/pull/10)
* Stop process on extension diactivation
* Added `Restart OpenXml Server` command

### 0.0.6 - 10.04.2021
* Fixed file open issue on Windows [#9](https://github.com/sergey-tihon/vscode-openxml-explorer/pull/9)

### 0.0.5 - 10.04.2021
* Added 'openxml' output channel
* Fixed Server app port number

### 0.0.4 - 05.04.2021
* Removed dependency on Saturn and Giraffe
* Add new logo

### 0.0.3 - 04.04.2021
* Updated dependencies
* Fixed [CVE-2021-27290](https://github.com/advisories/GHSA-vx3p-948g-6vhq)
* Fixed [CVE-2020-28168](https://github.com/advisories/GHSA-4w2v-q235-vp99)

### 0.0.2 - 04.04.2021
* Tree view for OpenXml package
* XML part content provider with formatting
* .NET backend on top of [System.IO.Packaging](https://www.nuget.org/packages/System.IO.Packaging/)
* Documentation and extension info

### 0.0.1 - 04.04.2021
* Initial release
