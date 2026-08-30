# Ouch Browser !bangs Parity

Ouch Browser's !bangs are from Kagi, and as Ouch Browser implements !bangs
differently, there are some minor differences in how it handles and expands
!bangs.

To reduce redundancy and decouple from Kagi Search, most of Kagi's internal
!bangs (Regional search, News in [country], and Search with [lens] lens) are
removed or renamed during processing, which occurs on launch. Non-internal
!bangs are unmodified during processing.

An !bang may be utilized with the "!" prefix inside of the command palette.
Ouch Browser does not support any other

!bangs are updated via a CI workflow that runs every Saturday at around 8 P.M.
(Eastern Daylight Time).

> **Note**
> 
> You may be interested in reading [Kagi's !bangs overview](https://help.kagi.com/kagi/features/bangs.html#custom-bangs)
> for comparison.

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

## Custom !bangs

There is currently no option in Ouch Browser to create custom !bangs.
