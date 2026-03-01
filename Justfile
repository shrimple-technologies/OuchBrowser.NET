set quiet

ID := "site.srht.shrimple.OuchBrowser"
BLUEPRINT_FILES := "OuchBrowser/UI/Window.blp OuchBrowser/UI/Preferences.blp"

run:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	glib-compile-resources \
		--sourcedir OuchBrowser \
		--target=OuchBrowser/OuchBrowser.app.gresource \
		OuchBrowser/OuchBrowser.gresource.xml
	dotnet run --project OuchBrowser

build:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	glib-compile-resources \
		--sourcedir OuchBrowser \
		--target=OuchBrowser/OuchBrowser.app.gresource \
		OuchBrowser/OuchBrowser.gresource.xml
	dotnet build OuchBrowser

build-flatpak:
	@flatpak-builder \
		--force-clean \
		--user \
		--repo=.build/repo \
		.build \
		{{ ID }}.json
	@flatpak build-bundle \
		.build/repo \
		{{ ID }}.flatpak \
		{{ ID }} \
		--runtime-repo=https://flathub.org/repo/flathub.flatpakrepo
	
publish:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	glib-compile-resources \
		--sourcedir OuchBrowser \
		--target=OuchBrowser/OuchBrowser.app.gresource \
		OuchBrowser/OuchBrowser.gresource.xml
	dotnet publish OuchBrowser

fmt:
	blueprint-compiler format -f -t -s 4 {{ BLUEPRINT_FILES }}
	dotnet format
