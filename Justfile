set quiet

BLUEPRINT_FILES := "OuchBrowser/UI/Window.blp"

run:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	dotnet run --project OuchBrowser

build:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	dotnet build OuchBrowser

publish:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	dotnet publish OuchBrowser 

fmt:
	blueprint-compiler format -f -t -s 4 {{ BLUEPRINT_FILES }}
	dotnet format
		
