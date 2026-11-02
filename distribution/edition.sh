# Shared by the build scripts and the Build workflow (sourced, not run). Run from the repository root.
#
#   port_version         prints the port's version: Directory.Build.props <Version>, plus VERSION_SUFFIX
#                        when set. CI sets it to ".<run number>" for automatic builds, so each one has a new
#                        version (1.1.17 for build 17), which AltStore / SideStore need to see an update; a
#                        version tag build leaves it empty, so its version is exactly <Version> (1.2).
#   edition_info HD|iPhone
#                        sets APP_NAME (display name), EXE (executable / file name prefix), APP_ID (bundle /
#                        application id) and VERSION (port_version).

port_version() {
  local base
  base="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n1)"
  echo "$base${VERSION_SUFFIX:-}"
}

edition_info() {
  case "$1" in
    HD)     APP_NAME="Peggle HD"; EXE="PeggleHD"; APP_ID="com.aidrum.peggle.hd" ;;
    iPhone) APP_NAME="Peggle";    EXE="Peggle";   APP_ID="com.aidrum.peggle" ;;
    *) echo "Edition must be HD or iPhone (got '$1')" >&2; return 1 ;;
  esac
  VERSION="$(port_version)"
}
