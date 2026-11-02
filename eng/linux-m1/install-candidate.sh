#!/bin/sh
set -eu
umask 077
exec /usr/bin/python3 -I /usr/local/libexec/gagamba-install/install_common.py install "$@"
