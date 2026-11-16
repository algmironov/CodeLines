# CodeLines 1.1.0 release notes (draft)

- Requires .NET 10 for the framework-dependent .NET tool; existing .NET 9-only installations must install the .NET 10 runtime before upgrading.
- Updates System.CommandLine and preserves the existing command names and aliases.
- Improves cache reuse, relative paths, comment heuristics, ignore patterns, error reporting, and result/history storage.
- Reads historical plain JSON and gzip `.cline` results. Earlier cached counts are recomputed with the newer counting rules. The `.clineIgnore` glob subset is not full `.gitignore` compatibility.
- Comment handling is heuristic, not a language parser; raw or interpolated strings, embedded languages and other complex syntax may produce inaccurate counts.
- Adds publication variants and benchmark tooling. Results from one machine are not universal performance claims.

Version 1.1.0 is a candidate, not a published release. Validate the runtime requirement against the project's compatibility policy before release.
