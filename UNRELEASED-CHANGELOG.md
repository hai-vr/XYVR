Changelog for the current unreleased iteration
=====

The changelog for the current iteration is now part of the repository.

In the `hai-vr/XYVR` repository, Haï~ will be in change of writing this changelog.

## 0.0.1-alpha.18

### Privacy and data changes

### Application changes

Features:

Fixes:

Changes:
- The display options for search are now checkboxes instead of icons.
- Obsolete world cache is now cleared when the application closes, rather than when the application starts.
  - That way, if the application starts after it has not executed for a while, it has a chance to keep using existing entries.
- Cached world names are now removed when the world has not been seen for more than 15 days, rather than for more than 45 days.
  - That way, thumbnails for those worlds are removed sooner, freeing up disk space.

Internal changes:
