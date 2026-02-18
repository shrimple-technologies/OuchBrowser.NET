set quiet

BLUEPRINT_FILES := "OuchBrowser/UI/Window.blp"

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
