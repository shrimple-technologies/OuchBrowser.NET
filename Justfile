set quiet

PREFIX := "/usr"
ID := "site.srht.shrimple.OuchBrowser"
BLUEPRINT_FILES := "OuchBrowser/UI/Window.blp OuchBrowser/UI/Preferences.blp"

run: build-blueprint compile-resources
	dotnet run --project OuchBrowser
	
build: build-blueprint compile-resources
	dotnet build OuchBrowser
	
publish: build-blueprint compile-resources
	dotnet publish OuchBrowser -c Release

fmt:
	blueprint-compiler format -f -t -s 4 {{ BLUEPRINT_FILES }}
	dotnet format
	
[group("build")]
build-blueprint:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}

[group("build")]
compile-resources:
	glib-compile-resources \
		--sourcedir OuchBrowser \
		--target=OuchBrowser/OuchBrowser.app.gresource \
		OuchBrowser/OuchBrowser.gresource.xml

[group("build")]
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

[group("build")]
build-schemas:
	@sudo cp OuchBrowser/OuchBrowser.gschema.xml {{ PREFIX }}/share/glib-2.0/schemas
	@sudo glib-compile-schemas \
		{{ PREFIX }}/share/glib-2.0/schemas \
		>/dev/null 2>/dev/null
		
[group("build")]
build-translations:
	@mkdir -p {{ PREFIX }}/share/locale/pt_BR/LC_MESSAGES
	@msgfmt -o {{ PREFIX }}/share/locale/pt_BR/LC_MESSAGES/OuchBrowser.mo OuchBrowser/Gettext/pt_BR.po
