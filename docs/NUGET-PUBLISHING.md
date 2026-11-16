# Publishing CodeLines to NuGet.org

The workflow is `.github/workflows/publish-nuget.yml` (file name `publish-nuget.yml`, workflow name `Publish NuGet`). Only a `v*.*.*` tag push starts it. It accepts strict SemVer core and optional prerelease identifiers; build metadata (`+...`) is intentionally rejected because NuGet normalizes it away. The tag without `v` must exactly equal `<Version>` in `CodeLines.Cli.csproj`. The tagged commit must be an ancestor of the current remote `master` when verification runs. The full branch history is fetched by checkout on the runner, including for a detached tag checkout.

## One-time owner setup

1. Sign in to nuget.org as `algmironov`, the package owner and policy creator. Under account **Trusted Publishing**, create a GitHub policy with repository owner `algmironov`, repository `CodeLines`, workflow file `publish-nuget.yml` (file name only), and environment `nuget-release`. Confirm that the account is authorized to publish `CodeLines`. Follow [NuGet's Trusted Publishing instructions](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).
2. In GitHub repository settings create environment `nuget-release`; configure permitted release tags and, if desired, required reviewers. An environment approval is an optional additional protection; the workflow is started by a tag push, not a manual dispatch. Confirm the configured tag deployment rules permit the intended release tag.
3. Protect `master` with required CI and protect release tags `v*` using repository rulesets, limiting tag creation and preventing tag updates/deletion to the extent supported by the repository plan. Review who may change workflows, rulesets, environments and the NuGet policy. Branch ancestry alone does not prove that CI passed or that the tag creator is trusted.

## Release procedure

1. On `master`, review the release notes, package version and [release checklist](RELEASE-CHECKLIST.md); verify the GitHub CI for the exact commit succeeded. The release workflow also builds and runs tests independently.
2. Confirm that the intended version is not already published. The workflow checks NuGet's package version index before building; registry errors fail the check. Unlisted/deleted or racing releases may still result in a rejected push.
3. The owner creates and pushes the version tag for the reviewed commit manually, for example `git tag v1.1.0 <commit>` followed by `git push origin v1.1.0`. Do not move an existing release tag. No tag is created by the workflow.
4. Watch **Publish NuGet** in GitHub Actions and approve the environment only if reviewers are configured. The verify job validates the tag, builds, tests, inspects the package and transfers it by artifact. The publish job checks its digest, obtains an OIDC-derived temporary credential and pushes once.
5. Verify the package version and metadata on nuget.org and install it from a clean local source/cache. Registry indexing can take time.

If verification, OIDC exchange, or publication fails, inspect the failing job and NuGet policy/environment permissions. Before retrying a failed publish, check nuget.org: the push may have succeeded even if the workflow failed afterwards. An already published NuGet version cannot be replaced by a different package with the same number; prepare a new version and a new tag if correction is needed. If the version is absent, retry the failed workflow run after correcting the cause. The pipeline does not use `--skip-duplicate` and does not retain a permanent NuGet API key.

The workflow is tag-triggered; consequently the workflow definition itself comes from the tagged commit. Protect release tags and review workflow changes on `master`; a tag on an older ancestor uses that older workflow. GitHub concurrency serializes active runs but may replace a pending run when multiple tags arrive; verify each release run individually.
