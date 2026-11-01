# Shared by the build scripts and the Build workflow (sourced, not run). Run from the repository root.
#
#   port_version         prints the port's version: Directory.Build.props <Version>, plus ".<n>" when the
#                        BUILD_NUMBER environment variable is set (CI sets it to the workflow run number), so
#                        every CI build has a new version (1.0.8 for build 8), which AltStore / SideStore need
#                        to see an update.
#   edition_info HD|iPhone
#                        sets APP_NAME (display name), EXE (executable / file name prefix), APP_ID (bundle /
#                        application id) and VERSION (port_version).

port_version() {
  local base
  base="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n1)"
  echo "$base${BUILD_NUMBER:+.$BUILD_NUMBER}"
}

edition_info() {
  case "$1" in
    HD)     APP_NAME="Peggle HD"; EXE="PeggleHD"; APP_ID="com.aidrum.peggle.hd" ;;
    iPhone) APP_NAME="Peggle";    EXE="Peggle";   APP_ID="com.aidrum.peggle" ;;
    *) echo "Edition must be HD or iPhone (got '$1')" >&2; return 1 ;;
  esac
  VERSION="$(port_version)"
}
