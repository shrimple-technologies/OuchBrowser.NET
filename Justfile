set quiet

PREFIX := "/usr"
ID := "site.srht.shrimple.OuchBrowser"
BLUEPRINT_FILES := "OuchBrowser/UI/Builder/Window.blp OuchBrowser/UI/Builder/Preferences.blp OuchBrowser/UI/Builder/About.blp OuchBrowser/UI/Builder/Shortcuts.blp"

alias fmt := format
alias pot := generate-pot

run: build-blueprint compile-resources
	dotnet run --project OuchBrowser
	
build: build-blueprint compile-resources
	dotnet build OuchBrowser
	
publish: build-blueprint compile-resources
	dotnet publish OuchBrowser -c Release

format:
	blueprint-compiler format -f -t -s 4 {{ BLUEPRINT_FILES }}
	dotnet format

# compiles all blueprint files
[group("build")]
build-blueprint:
	blueprint-compiler batch-compile \
		OuchBrowser/UI/Builder \
		OuchBrowser/UI/Builder \
		{{ BLUEPRINT_FILES }}

# compiles gresources for icons and other miscellaneous assets
[group("build")]
compile-resources:
	glib-compile-resources \
		--sourcedir OuchBrowser \
		--target=OuchBrowser/OuchBrowser.app.gresource \
		OuchBrowser/OuchBrowser.gresource.xml

# builds an release flatpak file for distribution
[group("build")]
build-flatpak:
	flatpak-builder \
		--force-clean \
		--user \
		--repo=.build/repo \
		.build \
		build-aux/flatpak/{{ ID }}.json
	flatpak build-bundle \
		.build/repo \
		{{ ID }}.flatpak \
		{{ ID }} \
		--runtime-repo=https://flathub.org/repo/flathub.flatpakrepo
	rm -rf .flatpak-builder .repo .build

# compiles gsettings schemas
[group("build")]
build-schemas:
	mkdir -p {{ PREFIX }}/share/glib-2.0/schemas
	cp OuchBrowser/OuchBrowser.gschema.xml {{ PREFIX }}/share/glib-2.0/schemas
	glib-compile-schemas \
		{{ PREFIX }}/share/glib-2.0/schemas

# compiles translations and installs them
[group("build")]
build-translations:
	mkdir -p {{ PREFIX }}/share/locale/et/LC_MESSAGES
	mkdir -p {{ PREFIX }}/share/locale/nb_NO/LC_MESSAGES
	mkdir -p {{ PREFIX }}/share/locale/pt/LC_MESSAGES
	mkdir -p {{ PREFIX }}/share/locale/pt_BR/LC_MESSAGES
	msgfmt -o {{ PREFIX }}/share/locale/et/LC_MESSAGES/OuchBrowser.mo OuchBrowser/Po/et.po
	msgfmt -o {{ PREFIX }}/share/locale/nb_NO/LC_MESSAGES/OuchBrowser.mo OuchBrowser/Po/nb_NO.po
	msgfmt -o {{ PREFIX }}/share/locale/pt/LC_MESSAGES/OuchBrowser.mo OuchBrowser/Po/pt.po
	msgfmt -o {{ PREFIX }}/share/locale/pt_BR/LC_MESSAGES/OuchBrowser.mo OuchBrowser/Po/pt_BR.po

[group("utils")]
generate-pot:
	xgettext \
		-f OuchBrowser/Po/POTFILES \
		--output OuchBrowser/Po/TEMPLATE.pot \
		--from-code UTF-8 \
		--add-comments \
		--keyword=_ \
		--keyword=C_:1c,2 \
		--keyword=__ \
		--width 80 \
		--add-comments=TRANSLATORS: \
		--msgid-bugs-address https://codeberg.org/shrimple/OuchBrowser.NET/issues
