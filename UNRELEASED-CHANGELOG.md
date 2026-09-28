Changelog for the current unreleased iteration
=====

The changelog for the current iteration is now part of the repository.

In the `hai-vr/XYVR` repository, Haï~ will be in change of writing this changelog.

## 0.0.1-alpha.18

### Privacy and data changes

### Application changes

Features:
- Add `name:` filter to search only in active display names.
- Third-party acknowledgements are now shown on their own page, accessible from the Settings page.
  - Add acknowledgements for React, React Router DOM, React i18next, React Hot Toast, Lucide React, Vite, Photino.NET, Magick.NET, ImageMagick, Newtonsoft.Json, SQLite.

Fixes:
- Thumbnails that exist in the file cache will no longer be redownloaded, even if the cached world data was flagged to be refreshed.
  - We're now assuming that the thumbnail URL will change whenever the thumbnail itself changes.
- If XYVR is launched twice, the second instance will no longer try to open the database before getting a hold on the application lock.
- Attempt to fix one Live Monitoring agent failing to start should not prevent other Live Monitoring agents from trying to start (#35).
- Fix the main page was slow to scroll on Linux.
  - Profile illustrations were requested for every portrait, even for individuals that don't have one, and these requests were repeated on every repaint.
- Fix the layout of the live sessions did not adapt to the window width on Linux.
- Fix the focus ring was not visible on checkboxes and login/password fields.
- Fix the loading spinner shown while loading more entries was not animated.

Changes:
- The display options for search are now checkboxes instead of icons.
- Add checkbox to show bios.
- The text "Type `bio:` to show bios" that shows up when doing a search has been removed.
  - The `bio:` keyword continues to work and is still mentioned in the `:help`.
- The "Show Only Contacts" option has been renamed to "Include non-Contacts with Notes".
  - This makes it explicit that this is not meant to show former Contacts.
- Profile illustrations now show up right away after being assigned.
- Live session cards now have fixed widths that fit a whole number of portraits, stepping through 3, 2, then 1 column depending on the window width.
- Add spacing between the elements inside live session cards.
- Small stylistic improvements: elements with large rounded corners now use the same small radius as the rest of the UI, and some focus styles, hover effects and transitions were added.

Internal changes:
- Obsolete world cache is now cleared when the application closes, rather than when the application starts.
  - That way, if the application starts after it has not executed for a while, it has a chance to keep using existing entries.
- Cached world names are now removed when the world has not been seen for more than 15 days, rather than for more than 45 days.
  - That way, thumbnails for those worlds are removed sooner, freeing up disk space.
- When a lot of live sessions are queued, submit batches of updates every 50 live sessions if the queue still contains at least 50 more items.
- Resized profile illustrations are now kept in memory, rather than being resized again on every request.
- Add more logging when the application closes.
  - We're trying to figure out why the XYVR application sometimes fails to start because a process still exists.
- Update Microsoft.Data.Sqlite to 10.0.12 to fix a SQLite vulnerability (GHSA-2m69-gcr7-jv3q).
- Update ImageMagick to 14.15.0.
- Update frontend dependencies following alerts by Dependabot.
- Clean up unused frontend CSS.
