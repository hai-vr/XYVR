XYVR
====

*XYVR* is an address book application that lets you search through your VRChat and Resonite contacts.

Once data is retrieved, it is stored offline, no Internet connection needed.
The address book remains fully functional even if access to the original account is lost for any reason.

If you have multiple accounts on different social VR platforms (or even on the same platform), you can
view your contacts as one single address book.

<img width="416" height="564" alt="gFXOM4aH1Z" src="https://github.com/user-attachments/assets/55f337a2-87d8-467b-bfdc-a0e6dc848087" /><img width="376" height="564" alt="ui-webview_YEm46Ch1Qv" src="https://github.com/user-attachments/assets/916fe6b4-6373-44fd-a6be-be17ead6b8fa" />

# User documentation

If you are a user looking to use this software: Sorry, but this application is currently under development.

Please check back at another time.

- ~~**[📘 Open documentation](https://docs.hai-vr.dev/docs/products/xyvr)**~~

If you are a developer, feel free to read below.

&nbsp;

&nbsp;

# Privacy and Data considerations

- **This application does not store your email and password.**
- We store a cookie file and session tokens upon login, if requested.
- We store your address book in the `%APPDATA%/XYVR/` folder.
- We only perform requests to the following URLs:
  - for VRChat accounts: `https://api.vrchat.cloud/api/1` (live updates: `wss://pipeline.vrchat.cloud/`)
  - for Resonite accounts: `https://api.resonite.com/` (live updates: `https://api.resonite.com/hub`)
- There are no requests to any other server.
- There is no telemetry, no analytics, and no cloud storage.

For more details regarding privacy and data considerations, please refer to the following page:

**[📘 Privacy and data considerations](https://docs.hai-vr.dev/docs/products/xyvr/privacy)**


&nbsp;

&nbsp;

&nbsp;

&nbsp;

&nbsp;

-----

# Developer documentation

If you are a developer looking to modify or audit this software, the developer documentation can be found
in the [README-DEVELOPER.md](README-DEVELOPER.md) file.
