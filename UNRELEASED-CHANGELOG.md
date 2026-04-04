Changelog for the current unreleased iteration
=====

The changelog for the current iteration is now part of the repository.

In the `hai-vr/XYVR` repository, Haï~ will be in change of writing this changelog.

## 0.0.1-alpha.17

### Privacy and data changes

This update adds the group names to the live sessions.

The Privacy and data considerations are being updated to reflect this.

- In "*What is the nature of the requests to those servers?*", add to the "*The following requests are for live monitoring*" section:
  - Get the group name of a session.

### Application changes

Important:
- The application now requires .NET 10 (instead of .NET 9).

Features:
- Current progress is now displayed on the data collection button, contributed by @art0007i.
- Group names of VRChat live sessions are now shown under the user count.
- Session names (typically, world names) that match certain strings can be pushed to the bottom of the live sessions list, from the settings page (Deprioritized Virtual Spaces).
- Third-party acknowledgements are now visible from inside the built application, in the Settings page.

Fixes:
- Fix Booth.pm wishlist URLs are no longer displayed as being an account name.
- Fix some profile links to Patreon service starting containing patreon.com/c/ were incorrectly displayed.
- Fix log file was previously truncated or had missing lines, if it was opened by an external program while XYVR is running.
- Fix pressing the log out button on a Resonite connection with an expired token should no longer trigger an error.
- Fix sqlite initialization on Linux, contributed by @art0007i.
- Fix VRChat login should now function on Linux/Photino, contributed by @art0007i.
- Attempt to fix a multithreading issue related to live session monitoring, which may have led to failing to list all sessions.

Changes:
- The Show/Hide portraits" button is now next to the "Light/Dark mode" button.
- All other buttons are now only shown when a search is being made, as those buttons only have an effect during searches.
- The thumbnail cache is now cleared when the application starts, as the storage could grow out of control.

Internal changes:
- Log when worlds are removed from the cache.
- If an API repeatedly returns a server error, we may wait for even longer periods before retrying.
  - Previously, the maximum delay was 80 seconds; it could now last 5 minutes.
- Public Resonite sessions are no longer printed into the logs.
- Builds are now output by default to the build/ and build-photino/ folder, contributed by @art0007i.
