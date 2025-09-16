# Linux

The **ui-photino-linux** is the .NET project you need to build, along with the **ui-frontend** React project.

To run on Linux, you need to:
- Install [.NET 9 SDK](https://learn.microsoft.com/en-us/dotnet/core/install/linux?WT.mc_id=dotnet-35129-website).
- Install [`npm` and `node`](https://nodejs.org/en/download)
- Install `libwebkit2gtk-4.0` see [below](#install-libwebkit2gtk-40).

Then:
- In **ui-frontend/**, run `npm install`
- Run the **ui-photino-linux/** project.
  - `cd ui-photino-linux; dotnet run`
  - Building this project *should* automatically build the frontend project, as it internally calls `npm run build-and-copy`.

### Install libwebkit2gtk-4.0

If you're on Ubuntu 24, you need to install `libwebkit2gtk-4.0`, which is not available on the official Ubuntu 24 repository.

A workaround is described here in the Tauri issue tracker: *[libwebkit2gtk-4.0 not available in Ubuntu 24 & Debian 13 repositories](https://github.com/tauri-apps/tauri/issues/9662#:~:text=I%20worked%20around%20this%20by%20adding%20the%20following%20line)*
