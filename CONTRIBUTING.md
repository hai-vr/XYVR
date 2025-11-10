Contributing
===

*The following guidelines only apply to PRs opened at [hai-vr/XYVR](https://github.com/hai-vr/XYVR/), they do not apply to any other fork.*

Before you consider contributing to this repository (*hai-vr/XYVR*), please make sure you read the goals of this project
in the [README-DEVELOPER.md](README-DEVELOPER.md) file.

The featureset of XYVR is **intentionally limited** so that users can set their expectations of how the application differs from existing apps,
and how this application may behave and evolve over time.

In particular, *"Remain least intrusive as possible"* is an important goal of this project.

## Prohibited features

Per the goals of this project, the following contributions will result in immediate rejection if a PR is opened:

- **Any feature that presents changes in an event log will be rejected.**
  - Example: Tracking addition or removal from contacts will be rejected. 
- Any feature pertaining to a social VR app that makes calls to a third-party service will be rejected.
- Any feature that intentionally adds, modifies, or deletes user data on the social VR application through the API will be rejected.
- Any feature that reads data or writes data directly from or to the social VR application running on the computer will be rejected.
  - Example: Parsing log files or editing screenshot files will be rejected.

## Please don't submit off-topic features

The address book function is the primary function of this application.

I would really rather not this application start booming with non-address book features pertaining to the various social VR applications.

For example:
- Please do not contribute a Resonite inventory viewer.
- Please do not contribute a VRChat prints and emoji viewer.
- Please do not contribute a world search and archival function, even if the worlds are created by contacts of the address book.
- Please do not contribute an avatar viewer.

You're free to do whatever you want on the forks of this repository, these are just the things I would rather not deal with in *hai-vr/XYVR*.

## Offline first: Social VR apps can go out of business without affecting XYVR

Except for live monitoring, the address book and its list of contacts should remain functional:
- when the computer is not connected to the Internet,
- when the social VR apps are experiencing service degradation,
- when your account is deleted,
- and even when a social VR app has gone out of business.

The ability for users to use multiple accounts across social VR apps and migrate between them is a central theme of XYVR;
**it is essential that XYVR remains functional even when access to a social VR app is lost for any reason**.

All contributions to parts of the application not pertaining to live monitoring should take this into account.
