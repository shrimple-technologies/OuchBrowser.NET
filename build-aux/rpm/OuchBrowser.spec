%global debug_package %{nil}
%global __os_install_post %{nil}
%global __strip /bin/true
%global __brp_strip /bin/true
%global __brp_strip_lto /bin/true
%global __brp_mangle_shebangs /bin/true
%define build_timestamp %{lua: print(os.date("%Y%m%d"))}

Name: OuchBrowser
Version: 0.1.0
Release: 0.%{build_timestamp}%{?dist}
Summary: Focus on your browsing
License: GPL-3.0-or-later
URL: https://codeberg.org/shrimple/OuchBrowser.NET
Source0: %{URL}/archive/main.tar.gz

BuildRequires: just
BuildRequires: git
BuildRequires: dotnet-sdk-10.0
BuildRequires: blueprint-compiler
BuildRequires: gtk4-devel
BuildRequires: libadwaita-devel
Requires: libadwaita
Requires: gtk4
Requires: webkitgtk6.0
Requires: gsettings-desktop-schemas

%description
Ouch Browser is a browser that utilizes the vertical tabs layout, which allows
for better tab management for more heavy browser users. Made for focusing on
your browsing, Ouch Browser comes with !bangs support, a multi-purpose command
palette, and mobile support.

%prep
%autosetup -n ouchbrowser.net

%build
just build-blueprint
just compile-resources
git clone https://github.com/kagisearch/bangs OuchBrowser/Bangs --depth=1
%ifarch x86_64
dotnet publish OuchBrowser -c Release -r linux-x64
%elifarch aarch64
dotnet publish OuchBrowser -c Release -r linux-arm64
%endif

%install
just PREFIX=%{buildroot}%{_prefix} build-translations
just PREFIX=%{buildroot}%{_prefix} build-schemas

%ifarch x86_64
install -Dm755 OuchBrowser/bin/Release/net10.0/linux-x64/publish/OuchBrowser --target-directory %{buildroot}%{_bindir}
%elifarch aarch64
install -Dm755 OuchBrowser/bin/Release/net10.0/linux-arm64/publish/OuchBrowser --target-directory %{buildroot}%{_bindir}
%endif
install -Dm644 OuchBrowser/Data/OuchBrowser.gschema.xml --target-directory %{buildroot}%{_datadir}/glib-2.0/schemas
install -Dm644 OuchBrowser/Data/Icons/Hicolor/Symbolic/Apps/site.srht.shrimple.OuchBrowser-symbolic.svg --target-directory %{buildroot}%{_datadir}/icons/hicolor/symbolic/apps
install -Dm644 OuchBrowser/Data/Icons/Hicolor/Scalable/Apps/site.srht.shrimple.OuchBrowser.svg --target-directory %{buildroot}%{_datadir}/icons/hicolor/scalable/apps
install -Dm644 OuchBrowser/Data/site.srht.shrimple.OuchBrowser.desktop --target-directory %{buildroot}%{_datadir}/applications
install -Dm644 OuchBrowser/Data/site.srht.shrimple.OuchBrowser.metainfo.xml --target-directory %{buildroot}%{_datadir}/metainfo
rm %{buildroot}%{_datadir}/glib-2.0/schemas/gschemas.compiled

%files
%license licenses/GPL-3.0-or-later.txt
%{_bindir}/OuchBrowser
%{_datadir}/applications/site.srht.shrimple.OuchBrowser.desktop
%{_datadir}/icons/hicolor/scalable/apps/site.srht.shrimple.OuchBrowser.svg
%{_datadir}/icons/hicolor/symbolic/apps/site.srht.shrimple.OuchBrowser-symbolic.svg
%{_datadir}/glib-2.0/schemas/OuchBrowser.gschema.xml
%{_datadir}/metainfo/site.srht.shrimple.OuchBrowser.metainfo.xml
%{_datadir}/locale/et/LC_MESSAGES/OuchBrowser.mo
%{_datadir}/locale/nb_NO/LC_MESSAGES/OuchBrowser.mo
%{_datadir}/locale/pt/LC_MESSAGES/OuchBrowser.mo
%{_datadir}/locale/pt_BR/LC_MESSAGES/OuchBrowser.mo

%changelog
%autochangelog
