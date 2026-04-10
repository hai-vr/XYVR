Changelog for the current unreleased iteration
=====

The changelog for the current iteration is now part of the repository.

In the `hai-vr/XYVR` repository, Haï~ will be in change of writing this changelog.

## 0.0.1-alpha.18

### Privacy and data changes

### Application changes

Features:

Fixes:
- Thumbnails that exist in the file cache will no longer be redownloaded, even if the cached world data was flagged to be refreshed.
  - We're now assuming that the thumbnail URL will change whenever the thumbnail itself changes.
- If XYVR is launched twice, the second instance will no longer try to open the database before getting a hold on the application lock.

Changes:
- The display options for search are now checkboxes instead of icons.
- Add checkbox to show bios.
- The text "Type `bio:` to show bios" that shows up when doing a search has been removed.
  - The `bio:` keyword continues to work and is still mentioned in the `:help`.

Internal changes:
- Obsolete world cache is now cleared when the application closes, rather than when the application starts.
  - That way, if the application starts after it has not executed for a while, it has a chance to keep using existing entries.
- Cached world names are now removed when the world has not been seen for more than 15 days, rather than for more than 45 days.
  - That way, thumbnails for those worlds are removed sooner, freeing up disk space.
- When a lot of live sessions are queued, submit batches of updates every 50 live sessions if the queue still contains at least 50 more items.
