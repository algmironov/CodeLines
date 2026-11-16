# Release checklist

- [ ] Review compatibility: .NET 10 runtime is required for the framework-dependent tool; upgrades from .NET 9 require a runtime upgrade. Confirm that `1.1.0` is acceptable under the project's versioning policy.
- [ ] Restore, build Release and run `dotnet test CodeLines.sln -c Release` on Windows and Linux.
- [ ] Publish all three `win-x64` variants and run `benchmarks/smoke.ps1`.
- [ ] Review AOT and trimming warnings, and verify old plain JSON and gzip results.
- [ ] Pack `CodeLines.Cli/CodeLines.Cli.csproj`, inspect metadata and locally install from a local source; run `cline --help`.
- [ ] Review release notes, CI results, licenses, and the working tree.
- [ ] Perform any commit, tag, upload and release manually after review; these actions are not automated here.
