XYVR
====

*XYVR* is a personal address book that merges Resonite and VRChat friend lists into a single view.

Once data is retrieved, it is stored offline, no Internet connection needed.
The address book remains fully functional even if access to the original account is lost for any reason.

<img width="484" height="617" alt="ui-webview_W5YVhJwwnL" src="https://github.com/user-attachments/assets/d64cfad9-bdc5-4319-b05f-88204e36c5eb" />

# User documentation

If you are a user looking to use this software: Sorry, but this application is not currently readily usable, it currently has an issue with 2FA,
and it is not properly documented, and some features deemed essential for are not available yet.

Please check back at another time.

- ~~**[📘 Open documentation](https://docs.hai-vr.dev/docs/products/xyvr)**~~

If you are a developer, feel free to read below.

&nbsp;

&nbsp;

# Privacy and Data considerations

In a nutshell:
- **We do not store your email and password.**
- If you check the "Stay logged in" checkbox, we store your session cookies and tokens in an encrypted file.
- We store your user ID, real username, and address book in `%APPDATA%/XYVR/connectors.json`
- We store non-login API responses in `%APPDATA%/XYVR/response-collection.jsonl`

HTTP requests:
- Requests to the VRChat API are done using the url `https://api.vrchat.cloud/api/1` at the rate of one request per second on average.
- Requests to the Resonite API are done using the url `https://api.resonite.com/` (**requests are currently unmetered, this needs to be fixed**).
- **There are no requests to any other external services.**
    - There is no telemetry, analytics, or tracking of any kind.

For more details regarding privacy and data considerations, please refer to the following page:

**[📘 Privacy and data considerations](https://docs.hai-vr.dev/docs/products/xyvr/privacy)**


&nbsp;

&nbsp;

&nbsp;

&nbsp;

&nbsp;

-----

# Developer documentation

## Goals of this address book application

*For the purposes of this README, the words "friend" and "contact" refer to the same social VR concept.*

This app has the following goals:
- Create an address book that can handle friends who are playing across **several different social VR platforms**.
- Create an **offline** address book that remains functional even when access to the original account is lost for any reason.
- Remain least intrusive as possible.

The following are **not** goals:
- It is not a goal to add, edit, or remove friends: All write operations must be done through the original social VR apps or websites.
- It is not a goal to track removals from friend lists: The address book will only show users in the UI who are friends or who have
  a note attached to them. This application purposefully will not keep track of status changes.

## Current status of this app

At first, this app is under development as a personal application tailored for my own personal needs, which are described above.

It is not readily usable by non-developer users without significant additional work.

The following work has been done:
- Fetch the contact and friend user data directly from Resonite and VRChat into a custom account data structure.
- Expose code APIs to fusion accounts belonging to the same individual.
- Expose backend APIs to a React application frontend.
- Create an address book frontend UI.
- Improve the data structure used to store the original API responses from the various social VR applications.
- Improve data handling when the owner of the address book has multiple accounts on the same social VR platform with different friend lists.
- Provide a graphical UI to login into the various social VR applications.
- Provide a graphical UI to update the data.
- Make the connection code easily auditable.
- Set up workflow automation to build the React app and the .NET app.

The following work remains to be done:
- Provide a graphical UI to group accounts belonging to a single individual.
- Write the necessary API connectors to fetch live session data and display the current social VR application that a user is playing.
- Provide a graphical UI to import an existing set of pre-made requests so that it does not require a direct connection.

## Technology in use

The app is written in .NET 9 and uses a WebView pointing to a React app set up with Vite. Offline data is stored in plain JSON.
- As I am originally a backend developer, the WebView and React frontend are built using heavy assistance from Claude 4 integrated
  with Jetbrains Rider.
- The backend is designed without much assistance.

## Executing the app as a developer

*The instructions to execute this application have not been written yet.*

#### Build the React app

To run the desktop app, we need to generate the `ui-webview/src/dist/` folder containing the React webapp
that will be then copied to the desktop application during the .NET build process.

- Requires Node or something.
- In `ui-webview/src/`, run `npm install` and then `npm run build`

#### Build the desktop app that shows the React app

Build the `ui-webview/` project.

#### Importing the data

*The instructions to import the data have not been written yet.*

- Login to the VRChat and Resonite accounts using the `ui-webview` project by launching the WebView application.
  - This app does not currently support 2FA using authenticator, please check back another time.
- In the Connections tab of the desktop app, click the "Start data collection" button, then go back to the Contacts page and wait.
  - There is currently no indication of when the data collection is finished.

## Project structure

Main application execution projects:

- **program**: This is a developer program to import the data. It is not user-friendly whatsoever.
- **ui-webview**: This is a WebView program that shows a React app that has been built.
- **ui-webview/src/**: This is the unbuilt React app. It is meant to be built using `npm run build` so that the .NET program will copy its contents to the desktop app.

Core projects:

- **core** contains data structures shared by many projects in this solution and the repository of Individuals that handles the business logic of ingesting incoming data.
- **development-scaffold** loads and saves the data into a JSON file.
- **data-collection** orchestrates calls to the external systems and retrieves data from them.

External system projects:

- **account-resonite** communicates with **api-resonite** and returns core objects.
- **account-vrchat** communicates with **api-vrchat** and returns core objects.
- **api-resonite** makes HTTP requests (*and in the future, will receive SignalR*) with the [Resonite API](https://wiki.resonite.com/API).
- **api-vrchat** makes HTTP requests to the [VRChat API](https://vrchat.community).
