set quiet

alias fmt := format
alias pot := generate-pot

PREFIX := "/usr"
ID := "one.lunaris.OuchBrowser"

format:
	blueprint-compiler format -f -t -s 4 OuchBrowser/Data/UI/*.blp
	dotnet format

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
	cp OuchBrowser/Data/OuchBrowser.gschema.xml {{ PREFIX }}/share/glib-2.0/schemas
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
		--output OuchBrowser/Po/OuchBrowser.pot \
		--from-code UTF-8 \
		--add-comments=TRANSLATORS: \
		--keyword \
		--keyword=_ \
		--keyword=C_:1c,2 \
		--keyword=__ \
		--keyword=N__:1,2 \
		--msgid-bugs-address https://codeberg.org/shrimple/OuchBrowser.NET/issues
