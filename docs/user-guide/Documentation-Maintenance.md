# Documentation Maintenance

This source-controlled documentation is the task-oriented end-user guide. The repository README remains the build/development and complete CLI reference, while deep solver methodology and benchmark reports stay under [`docs/`](https://github.com/laqieer/FEMapCreator/tree/main/docs).

## Update checklist

For every user-facing release:

1. Verify download/platform instructions against the release workflow.
2. Update GUI controls and solver settings if labels/defaults changed.
3. Update CLI commands, JSON schemas, examples, and exit behavior.
4. Update map-format support and known limitations.
5. Add new troubleshooting entries for resolved recurring issues.
6. Verify Issues, Discussions, User guide, Web app, and latest-release links.
7. Check the sidebar links and page titles.

## Sources of truth

- GUI behavior: `FE_Map_Creator.Gui`
- CLI commands/help: `FE_Map_Creator.Cli`
- Legacy Windows editor: `FE_Map_Creator`
- Shared codecs/generation: `FE_Map_Creator.Core`
- Release packaging: `.github/workflows/release.yml`
- CI behavior: `.github/workflows/ci.yml`
- Release notes: [GitHub Releases](https://github.com/laqieer/FEMapCreator/releases)

Documentation changes should cite or link these sources instead of copying implementation details that are likely to drift.

The `Publish Wiki` GitHub Actions workflow mirrors this directory to the repository's GitHub Wiki. GitHub requires the Wiki's first page to be created once in the web UI before its backing git repository exists; after initialization, normal guide changes on `main` sync automatically and the workflow can also be dispatched manually.
