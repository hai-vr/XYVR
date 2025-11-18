XYVR
====

*XYVR* is an address book application that lets you search through your VRChat, Resonite, and ChilloutVR contacts, including live sessions.

Once data is retrieved, it is stored offline, no Internet connection needed.
The address book remains fully functional even if access to the original account is lost for any reason.

If you or your contacts have multiple accounts on different social VR apps and platforms (or even on the same platform), you can
view them as one single address book.

XYVR is designed to be [least intrusive as possible](https://github.com/hai-vr/XYVR/blob/main/CONTRIBUTING.md#prohibited-features):
- We do not expose an event log to track the addition or removal from contact lists.
- This application does not modify any data on your VRChat, Resonite, or ChilloutVR accounts through the API, and it does not read
nor modify any VRChat, Resonite, or ChilloutVR files, log files, nor screenshots on your computer.
- There is no telemetry, no calls to third-party services, and we do not have servers. Read our [Privacy and Data considerations](#privacy-and-data-considerations) below.

<img width="416" height="499" alt="XYVR_164tRkiXkB" src="https://github.com/user-attachments/assets/76dcca6a-d428-4739-ace8-bb10add4b2e4" />

<img width="416" height="564" alt="gFXOM4aH1Z" src="https://github.com/user-attachments/assets/55f337a2-87d8-467b-bfdc-a0e6dc848087" /><img width="376" height="564" alt="ui-webview_YEm46Ch1Qv" src="https://github.com/user-attachments/assets/916fe6b4-6373-44fd-a6be-be17ead6b8fa" />

# User documentation

If you are a user looking to use this software: Sorry, but this application is currently under development. This section will become available once this
application goes out of Alpha. 

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
  - for ChilloutVR accounts: `https://api.chilloutvr.net/1` (live updates: `wss://api.chilloutvr.net/1/users/ws`)
  - for Cluster accounts: `https://api.cluster.mu/v1`
  - Information from one account is never transmitted to the service of another account (e.g., Resonite account data is never transmitted to VRChat servers, etc.)
- We fetch thumbnails:
  - for VRChat: from any thumbnail URL provided by the VRChat API.
  - for Resonite: only from URLs which hostname is `resonite.com`, or any subdomain of `resonite.com`.
  - for ChilloutVR: from any thumbnail URL provided by the ChilloutVR API.
  - for Cluster: thumbnails are not yet fetched.
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
- Typing on the search bar or opening a users' profile in the UI does not perform any API request to the VRChat, Resonite, or ChilloutVR servers.

The following are exceptions to this rule:
  - At your request, the application may submit an invitation to yourself to join an instance (VRChat only).

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

The following sources were also used:
- [`parseLocation` function of VRCX](https://github.com/vrcx-team/VRCX/blob/master/src/shared/utils/location.js#L35C1-L145C2)
  ([MIT License](https://github.com/vrcx-team/VRCX/blob/dda3d2dda9c8f4c840f230072f2ebefb72d58623/LICENSE)) maintained by the [VRCX Team](https://github.com/vrcx-team).