# Agent Guidelines

## Build & Test Commands
- Build: `./build.sh` (Linux/macOS) or `./build.cmd` (Windows)
- Test all: `dotnet test`
- Single test: `dotnet test --filter "verifyPackageInfo(word.docx)"`
- Format check: `dotnet fantomas src/**/*.fs tests/**/*.fs --check`
- Format fix: `dotnet fantomas src/**/*.fs tests/**/*.fs`

## Code Style (F#)
- Indent: 4 spaces, max line length: 150 chars
- No space before lowercase function invocation: `getPackageInfo filePath`
- Stroustrup-style multiline brackets (closing bracket on same column)
- Bar before first discriminated union case: `type Msg = | Action1 | Action2`
- Use `async { }` blocks with `let!` and `do!` for async code
- String interpolation: `$"value: %d{x}"`
- Underscore placeholder for member access: `|> Seq.sortBy _.Name`

## Project Structure
- `src/extension/` - VS Code extension (F# compiled to JS via Fable)
- `src/Server/` - .NET backend server
- `src/Shared/` - Shared types between extension and server
- `tests/Server.Tests/` - NUnit tests with Verify snapshots (`.verified.txt`)
