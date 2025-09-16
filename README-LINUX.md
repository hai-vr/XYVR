# Linux

**This app is not currently working on Linux**, there's still some issues to figure out.

To run on Linux, you need to build the **ui-photino-linux** project instead of ~~ui-webview-windows~~.

### Install libwebkit2gtk-4.0

If you're on Ubuntu 24, you need to install `libwebkit2gtk-4.0`, which is not available on the official Ubuntu 24 repository.

A workaround is described here in the Tauri issue tracker: [libwebkit2gtk-4.0 not available in Ubuntu 24 & Debian 13 repositories](- https://github.com/tauri-apps/tauri/issues/9662#:~:text=I%20worked%20around%20this%20by%20adding%20the%20following%20line)
