# CL-001 audit

- Results previously depended on `--verbose` for cached per-file statistics; paths were relative to the working directory rather than the project root.
- `.cline/history.json` could be scanned as project input; result filenames collided within a second.
- File and history errors were swallowed, and a failed read could be reported as a successful scan.
- Previous comment counting missed code after block comments and confused markers in ordinary strings.
- Ignore matching did not implement documented recursive globs.
- The old JSON serializer used reflection, unsuitable for Native AOT.

Older plain JSON and gzip result files and history are read. Cache version 2 forces recomputation of older file statistics because counting semantics have changed. MD5 is retained for compatibility and is only a change detector, not a security primitive.

## CL-001-FINAL baseline

The starting commit was `df5a9ae8c2f10455a4a86f57bc870a1b613fabe3` with SDK 10.0.404, System.CommandLine 3.0.0, xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, and xunit.runner.visualstudio 3.1.5. Release publication uses win-x64, ReadyToRun=false and PublishSingleFile=false, with framework-dependent JIT, self-contained JIT, and Native AOT. The user reported successful tests, publication, smoke checks and an experiment on the main computer at that point; those reported outcomes have not been independently re-run here.

CL-001-FINAL changes test, CI, package version, documentation and benchmark scripts. Fixture contents and measurement order change, so historical timings do not form a directly comparable baseline for the new method. The application scanning, hash, cache and serialization algorithms are unchanged in this phase.
