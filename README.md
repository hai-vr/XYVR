XYVR
====

*XYVR* is a personal address book that merges Resonite and VRChat friend lists into a single view.

Once data is retrieved, it is stored offline, no Internet connection needed.
The address book remains fully functional even if access to the original account is lost for any reason.

# User documentation

If you are a user looking to use this software: Sorry, but this application is not currently readily usable,
as it involves typing commands.

Please check back another time.

If you are a developer, feel free to read below.

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
- It is not a goal to track friendship status changes: The address book will only show users in the UI who are friends or who have
  a note attached to them. This application purposefully will not keep track of status changes.

## Current status of this app

At first, this app is under development as a personal application tailored for my own personal needs, which are described above.

It is not readily usable by non-developer users without significant additional work.

The following work has been done:
- Fetch the contact and friend user data directly from Resonite and VRChat into a custom account data structure.
- Expose code APIs to fusion accounts belonging to the same individual.
- Expose backend APIs to a React application frontend.
- Create an address book frontend UI.

The following work remains to be done:
- Improve the data structure used to store the original API responses from the various social VR applications.
- Provide a graphical UI to login into the various social VR applications and update the data.
- Provide a graphical UI to import an existing set of pre-made requests so that it does not require a direct connection.
- Make the connection code easily auditable.
- Provide a graphical UI to group accounts belonging to a single individual.
- Write the necessary API connectors to fetch live session data and display the current social VR application that a user is playing.
- Set up workflow automation to build the React app and the .NET app.

## Technology in use

The app is written in .NET 9 and uses a WebView pointing to a React app set up with Vite. Offline data is stored in plain JSON.
- As I am originally a backend developer, the WebView and React frontend are built using heavy assistance from Claude 4 integrated
  with Jetbrains Rider.
- The backend is designed without much assistance.

## Executing the app as a developer

*The instructions to execute this application have not been written yet.*
