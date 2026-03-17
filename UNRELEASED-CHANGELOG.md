Changelog for the current unreleased iteration
=====

The changelog for the current iteration is now part of the repository.

In the `hai-vr/XYVR` repository, Haï~ will be in change of writing this changelog.

## 0.0.1-alpha.17

Features:
- Current progress is now displayed on the data collection button, contributed by @art0007i.
- Session names (typically, world names) that match certain strings can be pushed to the bottom of the live sessions list, from the settings page (Deprioritized Virtual Spaces).

Fixes:
- Fix Booth.pm wishlist URLs are no longer displayed as being an account name.
- Fix some profile links to Patreon service starting containing patreon.com/c/ were incorrectly displayed.
- Fix log file was previously truncated or had missing lines, if it was opened by an external program while XYVR is running.

Internal changes:
- Log when worlds are removed from the cache.
- If an API repeatedly returns a server error, we may wait for even longer periods before retrying.
  - Previously, the maximum delay was 80 seconds; it could now last 5 minutes.
- Public Resonite sessions are no longer printed into the logs.
