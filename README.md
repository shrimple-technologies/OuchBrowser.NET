> [!CAUTION]
> 
> Ouch Browser is currently in alpha stage and is changing rapidly. While it is
> in a usable state, it does not have a large number of common web browser
> functions, and can have many bugs, which [can be reported here](https://codeberg.org/shrimple/OuchBrowser.NET/issues).

---

<img src="https://codeberg.org/shrimple/OuchBrowser.NET/raw/branch/main/OuchBrowser/Data/Icons/Hicolor/Scalable/Apps/site.srht.shrimple.OuchBrowser.svg" width="112" height="112" align="left" style="vertical-align: middle;" />

# Ouch Browser

Focus on your browsing

## Features

- **!bangs** — Easily search other websites
- **Command Palette** — A multi-purpose search bar that allows you to search,
  find !bangs, and more
- **Vertical Tabs** — Helps browser power users manage their tabs well

## Screenshots

![Ouch Browser Screenshot](https://codeberg.org/shrimple/OuchBrowser.NET/raw/branch/main/OuchBrowser/Data/Screenshots/Screenshot%20with%20Sidebar%20Open.png)

## Packages

These are official packages built and maintained by the Shrimple Technologies
team. Any packages not listed here are unofficial, and you must proceed with
caution with those packages.

### Fedora COPR

> [!NOTE]
> 
> The COPR package updates every commit pushed to the repository. You may need
> to update regularly to recieve new features.

```sh
sudo dnf copr enable shrimple/OuchBrowser
sudo dnf install OuchBrowser
```

### Flatpak

> [!NOTE]
> 
> For now, you are required to manually build the Flatpak, due to the
> prematurity of Ouch Browser.

```sh
just build-flatpak
```

## Running

> [!NOTE]
> 
> You need the dotnet 10.0 SDK, GTK 4, libadwaita, WebKitGTK, and Just to run
> Ouch Browser.

```sh
git clone --recurse-submodules https://codeberg.org/shrimple/OuchBrowser.NET
just run
```
