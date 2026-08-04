# Ouch Browser !bangs Parity

Ouch Browser's !bangs are from Kagi, and as Ouch Browser implements !bangs
differently, there are some minor differences in how it handles and expands
!bangs.

## !bang Expansion

- [x] Support for `{{{s}}}` placeholder
- [x] Support for regexes (`$1`, `$2`, etc.)
  - For example, "!rsr cats calico"

## Format Flags

- [x] All format flags enabled by default
- [x] `open_base_path`
  - For example: "!ghrepo" 
  - *Is always enabled*
- [x] `open_snap_domain`
  - For example: "!nixpkgs"
  - *Is always enabled*
- [x] `url_encode_placeholder`
  - For example: "!hn claude code"
- [x] `url_encode_space_to_plus`
