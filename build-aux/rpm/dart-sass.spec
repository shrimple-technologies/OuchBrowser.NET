# Originally from
# <https://github.com/Sinomor/astalRPM/blob/main/specs/dart-sass/dart-sass.spec>

%global debug_package %{nil}

Name:           dart-sass
Version:        1.104.0
Release:        1%{?dist}
Summary:        CSS preprocessor

License:        MIT
URL:            https://sass-lang.com
Source0:        https://github.com/sass/dart-sass/releases/download/%{version}/dart-sass-%{version}-linux-x64.tar.gz
Source1:        https://github.com/sass/dart-sass/releases/download/%{version}/dart-sass-%{version}-linux-arm64.tar.gz

ExclusiveArch:  x86_64 aarch64

%description
CSS preprocessor written in Dart.

%prep

%ifarch x86_64
%autosetup -n dart-sass -T -b 0
%endif
%ifarch aarch64
%autosetup -n dart-sass -T -b 1
%endif

cp src/LICENSE LICENSE

%build

%install
mkdir -p %{buildroot}%{_libdir}/dart-sass/src
mkdir -p %{buildroot}%{_bindir}

install -Dm755 sass %{buildroot}%{_libdir}/dart-sass/sass

cp -r src/ %{buildroot}%{_libdir}/dart-sass/
ln -sr %{_libdir}/dart-sass/sass %{buildroot}%{_bindir}/sass

%files
%license LICENSE
%{_bindir}/sass
%{_libdir}/dart-sass/