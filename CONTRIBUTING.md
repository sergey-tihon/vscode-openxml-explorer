# How to contribute

Please take a moment to review this document in order to make the contribution process easy and effective for everyone involved!

## How to build and test a local version

### Requirements

- VSCode
- Node.js 24 (or 22.18+)
- Yarn 1.x
- .NET 10.0 SDK (10.0.401 or newer)

### How to build

1. Run `yarn install` to restore `npm` dependencies
1. Build project `./build.sh -p Build`
1. Press `F5` for single build, or run `Watch` task and `Launch Only` debug configuration for watch mode compilation.

## Updating dependencies

- NuGet packages are managed by Paket: `dotnet paket update`
- npm packages: `yarn upgrade --latest`
- Dependabot (`.github/dependabot.yml`) opens weekly PRs for GitHub Actions, npm packages, dotnet tools (`.config/dotnet-tools.json`) and the .NET SDK (`global.json`, 10.x only). It does not support Paket, so `paket.dependencies` / `paket.lock` must be updated manually.

