# Troubleshooting and Support

## The Web app cannot open or save a file

- Use the in-app **Open** or **Save As** action; browsers block silent filesystem access.
- Allow downloads/file access for `laqieer.github.io`.
- Retry with a current browser.
- For MAR, set the correct dimensions and tileset before opening.
- Keep the page open until the browser confirms the save/download.

## No bundled tileset matches

- Confirm the file's tileset identifier or TMX image source.
- Select the matching bundled tileset manually for MAR.
- In the CLI, run `tilesets list` and use an unambiguous selector.
- If using external assets, verify PNG and generation-data basenames match.

## Generation leaves unresolved cells

- Record the reported seed.
- Increase the search node limit or restart count.
- Compare constraint, hybrid, and legacy algorithms.
- Remove contradictory locks or terrain constraints.
- Check that the selected tileset has matching generation data.

## macOS blocks the app

Control-click the app, choose **Open**, and confirm. Download only from the project's GitHub Releases page.

## Report a reproducible bug

Open a [GitHub Issue](https://github.com/laqieer/FEMapCreator/issues) and include:

- release version or commit;
- frontend, OS, and browser;
- map format, dimensions, tileset, and operation;
- minimal reproduction steps;
- expected and actual behavior;
- exact error text and safe screenshots.

Do not upload ROMs, executables, secrets, or copyrighted/sensitive archives.

## Questions, ideas, and outcomes

- [Q&A](https://github.com/laqieer/FEMapCreator/discussions/categories/q-a) for help.
- [Ideas](https://github.com/laqieer/FEMapCreator/discussions/categories/ideas) for feature proposals.
- [Show and tell](https://github.com/laqieer/FEMapCreator/discussions/categories/show-and-tell) for workflows, generated maps, feedback, and outcomes.
