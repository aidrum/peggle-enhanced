# Shared by the build_*.sh scripts (sourced, not run): the edition's names and the port's version.
#   edition_info HD|iPhone
# sets APP_NAME (display name), EXE (executable / file name prefix), APP_ID (bundle / application id)
# and VERSION (Directory.Build.props <Version>). Run from the repository root.

edition_info() {
  case "$1" in
    HD)     APP_NAME="Peggle HD"; EXE="PeggleHD"; APP_ID="com.aidrum.peggle.hd" ;;
    iPhone) APP_NAME="Peggle";    EXE="Peggle";   APP_ID="com.aidrum.peggle" ;;
    *) echo "Edition must be HD or iPhone (got '$1')" >&2; return 1 ;;
  esac
  VERSION="$(sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' Directory.Build.props | head -n1)"
}
