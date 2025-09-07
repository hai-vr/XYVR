# Caution: This React project was created using heavy use of machine assistance

This is Haï~ writing all of this README by hand:

The React portion of this project (all JSX and CSS) was created using heavy use of machine assistance.

## Context

Before being a Unity developer, I was originally a Java backend developer, and part of this project was a way for me
to discover how to use machine assistance on web libraries that have been heavily trained upon.

As a Unity developer, I have learned how to build Unity projects without the use of machine assistance and I had mostly
ignored the use of machine assistance in Unity projects: Every time I tried to evaluate the performance of machine assistance
over the years while working on Unity projects, I found out that the results were massively hallucinated and generally poor
and unreliable, so I kept ignoring the use of machine assistance outside single-line completion. It was just not worth the effort
to ask for machine assistance only to redo the work by hand afterward.

I did find out that it was much easier to ask for machine assistance to formulate maths problems (most of my Unity problems are
essentially Vector, Quaternion, and Matrix related); then I would reinterpret the math problems as code myself.

It turns out that when I started working on hobby projects that involved JavaScript, especially React, my experience with machine assistance
was completely different to the miserable experience I had with Unity projects. That prompted me to reevaluate my use
of machine assistance in general.

I have no real experience with React outside using it sparringly in Docusaurus out of necessity.

## Acknowledgement of flaw

Most of this React project was created through the use of prompts with Claude 4 integrated with Jetbrains Rider; in some areas
it was spliced together by hand, but the majority is created using machine assistance.

As a developer, I do recognize that a significant part of the React project is a total mess: The location of state across the components
doesn't make a lot of sense, the CSS is disorganized, especially the color variants between light and dark mode, and many of the
side effects were left unauthored after the machine assistance created them.

## Backend

The backend portion of this project was largely created by hand. Some areas in the .NET code were modified using machine assistance
in some very limited ways, such as converting `Task<List<Account>>` from paginated requests to `IAsyncEnumerable<Account>`, or
initializing a WebView2 with a virtual host pointing to this React project, or creating classes that match undocumented JSON responses
to deserialize into; but otherwise the .NET project was made by hand.

---

Original README:

# React + Vite

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## Expanding the ESLint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and [`typescript-eslint`](https://typescript-eslint.io) in your project.
