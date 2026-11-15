# CL-001 audit

- Results previously depended on `--verbose` for cached per-file statistics; paths were relative to the working directory rather than the project root.
- `.cline/history.json` could be scanned as project input; result filenames collided within a second.
- File and history errors were swallowed, and a failed read could be reported as a successful scan.
- Previous comment counting missed code after block comments and confused markers in ordinary strings.
- Ignore matching did not implement documented recursive globs.
- The old JSON serializer used reflection, unsuitable for Native AOT.

Older plain JSON and gzip result files and history are read. Cache version 2 forces recomputation of older file statistics because counting semantics have changed. MD5 is retained for compatibility and is only a change detector, not a security primitive.
