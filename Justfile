set quiet

DOTNET_TARGET_FRAMEWORK := if `dotnet --version` =~ "^8.0" {
	"net8.0"
} else if `dotnet --version` =~ "^9.0" {
	"net9.0"
} else {
	"net8.0"
}
BLUEPRINT_FILES := "OuchBrowser/UI/Window.blp"

run:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	dotnet run --framework {{ DOTNET_TARGET_FRAMEWORK }} --project OuchBrowser

build:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	dotnet build OuchBrowser --framework {{ DOTNET_TARGET_FRAMEWORK }}

publish:
	blueprint-compiler batch-compile \
		OuchBrowser/UI \
		OuchBrowser/UI \
		{{ BLUEPRINT_FILES }}
	dotnet publish OuchBrowser --framework {{ DOTNET_TARGET_FRAMEWORK }}

fmt:
	blueprint-compiler format -f -t -s 4 {{ BLUEPRINT_FILES }}
	dotnet format
		
