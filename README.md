﻿XYVR
====

*XYVR* is an address book application that lets you search through your VRChat, Resonite, and ChilloutVR contacts.

Once data is retrieved, it is stored offline, no Internet connection needed.
The address book remains fully functional even if access to the original account is lost for any reason.

If you have multiple accounts on different social VR platforms (or even on the same platform), you can
view your contacts as one single address book.

<img width="416" height="564" alt="gFXOM4aH1Z" src="https://github.com/user-attachments/assets/55f337a2-87d8-467b-bfdc-a0e6dc848087" /><img width="376" height="564" alt="ui-webview_YEm46Ch1Qv" src="https://github.com/user-attachments/assets/916fe6b4-6373-44fd-a6be-be17ead6b8fa" />

# User documentation

If you are a user looking to use this software: Sorry, but this application is currently under development.

Please check back at another time.

- ~~**[📘 Open documentation](https://docs.hai-vr.dev/docs/xyvr)**~~

If you are a developer, feel free to read below.

&nbsp;

&nbsp;

# Privacy and Data considerations

#### Where is my data stored?

XYVR stores all data on your computer.

- **This application does not store your email and password.**
- We store a cookie file and session tokens upon login, if requested.
- We store your address book in the `%APPDATA%/XYVR/` folder.

#### What servers are called?

XYVR does not operate any server, and it does not have analytics. The only services are those of the social VR apps.

- We only perform requests to the following URLs:
  - for VRChat accounts: `https://api.vrchat.cloud/api/1` (live updates: `wss://pipeline.vrchat.cloud/`)
  - for Resonite accounts: `https://api.resonite.com/` (live updates: `https://api.resonite.com/hub`)
  - for ChilloutVR accounts: `https://api.chilloutvr.net/1`
  - Information from one account is never transmitted to the service of another account (e.g., Resonite account data is never transmitted to VRChat servers, etc.)
- We fetch thumbnails:
  - for VRChat: from any thumbnail URL provided by the VRChat API.
  - for Resonite: only from URLs which hostname is `resonite.com`, or any subdomain of `resonite.com`.
- There are no requests to any other server.
- There is no telemetry, no analytics, and no cloud storage.

#### What is the nature of the requests to those servers?

XYVR is only interested in building an address book and monitoring live sessions.

- This application does not modify, add, or remove any data from your VRChat, Resonite, or ChilloutVR accounts.
- The following requests are for building an address book:
  - Get all of your friends' usernames, bio, links, notes.
  - Get all notes. If someone who has a note is not a friend, then get their username, bio, links, notes.
- The following requests are for live monitoring:
  - Receive live events regarding user status changes and user sessions.
  - Get the currently active sessions. 
  - Get the session of a contact.
  - Get the world of a session and its thumbnail.

#### More info

For more details regarding privacy and data considerations, please refer to the following page:

**[📘 Privacy and data considerations](https://docs.hai-vr.dev/docs/xyvr/privacy)**


&nbsp;

&nbsp;

&nbsp;

&nbsp;

&nbsp;

-----

# Developer documentation

If you are a developer looking to modify or audit this software, the developer documentation can be found
in the [README-DEVELOPER.md](README-DEVELOPER.md) file, and [README-SECURITY.md](README-SECURITY.md)
can help you conduct an audit.

# Third-party acknowledgements

The communication with the APIs is a custom implementation, with the requests based on the following reference documents:
- The API page on the official [Resonite wiki](https://wiki.resonite.com/API).
- The community-maintained VRChat API documentation at [vrchat.community](https://vrchat.community/) maintained by [github.com/vrchatapi](https://github.com/vrchatapi).
- Due to the absence of documentation, the [CVRX](https://github.com/AstroDogeDX/CVRX/blob/472cceec651abbeff9c76ae8412522d27015bfd9/server/api_cvr_http.js) app ([MIT License](https://github.com/AstroDogeDX/CVRX/blob/472cceec651abbeff9c76ae8412522d27015bfd9/LICENSE))
  maintained by [github.com/AstroDogeDX](https://github.com/AstroDogeDX/CVRX) was used as a reference for the API endpoints.
