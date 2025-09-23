# Security

To facilitate audits of this project, I have documented areas of concern.

## Basics

This application uses Microsoft WebView2 on Windows, and Photino on Linux.

It consists of a .NET backend and a React frontend, both of which are hosted locally.
- The backend establishes connections to the various APIs of the different social VR apps using a combination of HTTP requests, Websockets, and SignalR.
- The frontend communicates with the backend using .NET integration facilities through Microsoft WebView2 or Photino, depending on the variant of the application.
- The frontend does not communicate with the social VR apps, except for fetching thumbnail images.
- There is no XYVR server on the Internet; all operations are done by the users' local machine, and the data is stored on the users' local machine.

## URLs in use

All URLs that are used to perform requests to external services are gathered into one single file: [AuditUrls.cs](audit-urls/AuditUrls.cs)

The APIs in use by this application are directly referencing the constants contained in this file.

There are other URLs inside the code of the application. However, there are either:
- links to documentation, or
- used to build URLs for use as links to the web profiles of the social VR accounts, or
- used in string comparisons to check past requests.

The virtual host `appassets.example` using the [.example TLD](https://en.wikipedia.org/wiki/.example) is used for serving static files
in the Microsoft.Web.WebView2 variant of the application, as seen in [MainWindow.xaml.cs](ui-webview-windows/MainWindow.xaml.cs) near the invocation of
`WebView.CoreWebView2.SetVirtualHostNameToFolderMapping`.

## Possible areas of code injection

### Starting a process to open URLs in the user's default browser

We need to be able to open links in the user's default browser.

In [Scaffolding.cs](development-scaffold/Scaffolding.cs) the function `Scaffolding.DANGER_OpenUrl(string url)` does one of the following:
- On Windows: Executes `Process.Start(...)` with the URL being passed as the process to start.
- On Linux: Executes `xdg-open` with the URL being passed as the argument.

To prevent remote code execution, this function only accepts URLs that start with `http://` or `https://`, otherwise it will throw an exception.

### Submitting events from .NET to the WebView2

To submit backend-initiated events from .NET to WebView, [scripts are being injected into the WebView2](https://learn.microsoft.com/en-us/microsoft-edge/webview2/how-to/javascript).

In [MainWindow.xaml.cs](ui-webview-windows/MainWindow.xaml.cs) the function `SendScriptToReact(...)` does the following:
- It executes `window.dispatchEvent(new CustomEvent('{0}', {{ detail: {1} }}));` as a JavaScript script in the WebView, where
  - `{0}` is the name of the event, and
  - `{1}` is the JSON-encoded payload of the event.

To prevent code injection, the function only accepts event names in {0} that do not contain the character `'` (single quote).

In addition, the JSON payload in {1} is always the result of invoking `JsonConvert.SerializeObject(...)`, so it is unlikely to bleed unescaped single quotes.

### Serving static files for the React app in Photino

When running the `ui-photino-linux` variant of the project, the program will expose a service on a port, which will serve static files used
by the Photino WebView, and this service may be accessible in the local browser through localhost.

This can be seen in [Program.cs](ui-photino-linux/Program.cs), look for `PhotinoServer` and `CreateStaticFileServer`.

There is an inherent risk that if this service is misconfigured or if there is a vulnerability in the service,
then this application could be vulnerable to forms of path traversal, so the [photino.NET.Server](https://github.com/tryphotino/photino.NET.Server)
should be audited as well.

### Serving static files for the React app in Microsoft.Web.WebView2

Similarly, in `ui-webview-windows`, it uses some kind of virtual host to serve static files.

This may also deserve a look in [MainWindow.xaml.cs](ui-webview-windows/MainWindow.xaml.cs) near the invocation of
`WebView.CoreWebView2.SetVirtualHostNameToFolderMapping`.

## Frontend security

The frontend is written in React, which is displayed in either Microsoft.Web.WebView2, or Photino, depending on the variant of the application.

### Cross-site scripting

The frontend relies heavily on React's inherent protection against cross-site scripting, and does not engage in DOM manipulation
nor the use of `dangerouslySetInnerHTML`.

An area of risk is that external links are transmitted to the backend so that those links can be opened in the user's default browser.

A safeguard is put in place in the backend to prevent certain types of links from being opened, see *[Starting a process](#starting-a-process-to-open-urls-in-the-users-default-browser)* section above.

### Session tokens and cookies

The frontend does not use cookies nor session tokens nor local storage. All cookies and session tokens to communicate with external services are stored
and used by the backend, and they are not transmitted to the frontend in any way.

When the user logs in to a service in the Connections tab using their login and password (or access key in the case of ChilloutVR),
they transmit sensitive information to the backend. The backend takes care of actually making the API calls to the external services; not the frontend.

The session tokens and cookies obtained as a result of logging in are not transmitted back to the frontend upon successful login.

If there is code injection in the frontend, the frontend should not be able to extract session cookies and tokens as there is no endpoint in the backend
that allows this.

## Frontend communication with external services

Although it is not the responsibility of the frontend to communicate with external services, the frontend will use whatever URL is provided by the backend
as thumbnails for user profile picture and session thumbnails. This will establish a connection to an external service or CDN to retrieve the image.

Fetching the image and displaying it is the responsibility of the frontend. The backend will not retrieve that image nor perform any image processing to it.

The backend will apply the following restrictions to the URLs received from the various APIs, which are then passed to the frontend:
- For Resonite session thumbnails, the backend will only provide the URL to the frontend if the thumbnail host is equal to `resonite.com` or is a subdomain of it.

## Data storage

All data is stored in `%APPDATA%/XYVR` on Windows, and in `~/.config/XYVR` on Linux.

In addition, on Windows, a registry key is created to store an encryption key which is used to encrypt and decrypt the session tokens and cookies.
This is done so that if the file containing the session tokens and cookies is accidentally shared with other users, they will not be able to read the tokens
without also having access to the registry.

The Linux version of the app does not benefit from the same protection, as the encryption key is stored at pretty much the same location as the
file that contains the session tokens and cookies. If the directory is compromised, then the session data is compromised as well.

Emails and passwords are not stored on disk; if the user has typed them, they are forgotten upon successful login or when the application closes.

All the other data are not encrypted, so the address book is readable plain text.

## Development workflows

### npm

The [package.json](ui-frontend/src/package.json) and [package-lock.json](ui-frontend/src/package-lock.json) of the React app
use pinned version numbers for its dependencies; so it *should* not update itself through minor versions in case there is a supply chain attack
caused by a compromised account of a dependency.

### GitHub Actions

The following actions are in use in the GitHub Actions workflow responsible for building the app in [dotnet.yml](.github/workflows/dotnet.yml):

- `actions/checkout@v4`
- `actions/download-artifact@v4`
- `actions/setup-dotnet@v4`
- `actions/setup-node@v4`
- `actions/upload-artifact@v4`
- `notiz-dev/github-action-json-property@7c8cf5cc36eb85d8d287a8086a39dac59628eb31`
- `softprops/action-gh-release@1e07f4398721186383de40550babbdf2b84acfc5`
- `thedoctor0/zip-release@09336613be18a8208dfa66bd57efafd9e2685657`
