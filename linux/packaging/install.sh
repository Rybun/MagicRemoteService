#!/usr/bin/env bash
# Installs the MagicRemoteService daemon as a systemd system service: dedicated system user/group,
# uinput udev rule, systemd unit, best-effort firewall rule, and (optionally) a GUI autostart entry.
#
# Must run as root (it creates a system user, writes to /etc, reloads udev/systemd). Safe to re-run
# for upgrades — every step checks whether it's already done before doing it again.
#
# Usage:
#   sudo ./install.sh [--port N] [--no-enable] [--gui-appimage /path/to/MagicRemoteService.AppImage] [--gui-user someuser]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DAEMON_SRC="$SCRIPT_DIR/out/daemon/MagicRemote.Daemon"
INSTALL_DIR="/opt/magicremoteservice"
SHARED_CONFIG_DIR="/var/lib/magicremoteservice"
SERVICE_USER="magicremote"
SERVICE_GROUP="magicremote"
PORT=41230
ENABLE_SERVICE=1
GUI_APPIMAGE=""
GUI_USER=""

while [[ $# -gt 0 ]]; do
	case "$1" in
		--port)
			PORT="$2"
			shift 2
			;;
		--no-enable)
			ENABLE_SERVICE=0
			shift
			;;
		--gui-appimage)
			GUI_APPIMAGE="$2"
			shift 2
			;;
		--gui-user)
			GUI_USER="$2"
			shift 2
			;;
		*)
			echo "Unknown argument: $1" >&2
			exit 1
			;;
	esac
done

if [[ $EUID -ne 0 ]]; then
	echo "This installer needs root (it creates a system user and writes to /etc). Re-run with sudo." >&2
	exit 1
fi

if [[ ! -x "$DAEMON_SRC" ]]; then
	echo "Daemon binary not found at $DAEMON_SRC — run packaging/build.sh first." >&2
	exit 1
fi

echo "==> Creating system group/user '$SERVICE_USER' (if missing)"
if ! getent group "$SERVICE_GROUP" > /dev/null; then
	groupadd --system "$SERVICE_GROUP"
fi
if ! getent passwd "$SERVICE_USER" > /dev/null; then
	useradd --system --no-create-home --shell /usr/sbin/nologin --gid "$SERVICE_GROUP" "$SERVICE_USER"
fi

echo "==> Checking for the webOS CLI (ares-*) — needed by the GUI's TV pairing/install features"
if command -v ares-setup-device > /dev/null; then
	echo "    Already installed ($(command -v ares-setup-device))."
else
	if ! command -v npm > /dev/null; then
		echo "    Installing Node.js/npm (needed to install the webOS CLI)"
		if command -v dnf > /dev/null; then
			dnf install -y nodejs npm
		elif command -v apt-get > /dev/null; then
			apt-get update && apt-get install -y nodejs npm
		else
			echo "    Warning: no supported package manager found (dnf/apt-get) — install Node.js" >&2
			echo "    manually, then run: npm install -g @webos-tools/cli" >&2
		fi
	fi
	if command -v npm > /dev/null; then
		echo "    Installing @webos-tools/cli (ares-setup-device, ares-install, ares-package, ...) globally"
		npm install -g @webos-tools/cli
	fi
fi

echo "==> Creating shared config directory $SHARED_CONFIG_DIR"
mkdir -p "$SHARED_CONFIG_DIR"
chown root:"$SERVICE_GROUP" "$SHARED_CONFIG_DIR"
# setgid (2775): files the daemon creates here inherit group magicremote, so the GUI
# (running as a different, interactive user added to that group via --gui-user) can still write them.
chmod 2775 "$SHARED_CONFIG_DIR"

echo "==> Installing daemon binary to $INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
install -o root -g root -m 0755 "$DAEMON_SRC" "$INSTALL_DIR/MagicRemote.Daemon"

echo "==> Installing udev rule (uinput access for $SERVICE_GROUP)"
install -o root -g root -m 0644 "$SCRIPT_DIR/udev/71-magicremoteservice.rules" /etc/udev/rules.d/71-magicremoteservice.rules
udevadm control --reload-rules
udevadm trigger --subsystem-match=misc --sysname-match=uinput || true

echo "==> Installing systemd unit"
install -o root -g root -m 0644 "$SCRIPT_DIR/systemd/magicremoteservice.service" /etc/systemd/system/magicremoteservice.service
systemctl daemon-reload

if [[ "$ENABLE_SERVICE" -eq 1 ]]; then
	echo "==> Enabling and (re)starting magicremoteservice.service"
	systemctl enable magicremoteservice.service
	systemctl restart magicremoteservice.service
else
	echo "==> Skipping enable/start (--no-enable given). Run 'systemctl enable --now magicremoteservice' when ready."
fi

echo "==> Opening TCP port $PORT in the firewall (best effort)"
if command -v firewall-cmd > /dev/null && systemctl is-active --quiet firewalld 2> /dev/null; then
	firewall-cmd --permanent --add-port="${PORT}/tcp"
	firewall-cmd --reload
	echo "    firewalld: opened ${PORT}/tcp"
elif command -v ufw > /dev/null && ufw status | grep -q "Status: active"; then
	ufw allow "${PORT}/tcp"
	echo "    ufw: opened ${PORT}/tcp"
else
	echo "    No supported firewall manager detected (or none active) — if you use one, open TCP port $PORT manually."
fi

if [[ -n "$GUI_APPIMAGE" ]]; then
	echo "==> Installing GUI systemd --user unit for $GUI_APPIMAGE"
	if [[ ! -f "$GUI_APPIMAGE" ]]; then
		echo "    Warning: $GUI_APPIMAGE does not exist yet; the unit will still be installed but won't work until it does." >&2
	fi
	# A --user unit, not a system one: it needs the invoking user's own graphical session
	# (DISPLAY/WAYLAND_DISPLAY), which a root-owned system service doesn't have. Installed under
	# /usr/lib/systemd/user/ so it's available to any user, but each user enables it for themselves
	# (root can't reliably reach another user's D-Bus session bus to do this on their behalf).
	# Restart=on-failure in the unit auto-recovers from Avalonia's known intermittent tray D-Bus
	# crash (see the unit file's comment) without restarting on a clean, intentional Quit.
	mkdir -p /usr/lib/systemd/user
	sed "s#__APPIMAGE_PATH__#$GUI_APPIMAGE#" "$SCRIPT_DIR/systemd/magicremoteservice-gui.service" \
		> /usr/lib/systemd/user/magicremoteservice-gui.service
	chmod 0644 /usr/lib/systemd/user/magicremoteservice-gui.service
	systemctl daemon-reload
	echo "    Installed. Each user who wants the GUI/tray at login should run (as themselves, not root):"
	echo "      systemctl --user enable --now magicremoteservice-gui"

	echo "==> Installing desktop menu entry (so the app shows up in the system's application launcher)"
	ICON_SRC="$(dirname "$GUI_APPIMAGE")/magicremoteservice.png"
	ICONS_DIR="/usr/share/icons/hicolor/256x256/apps"
	mkdir -p /usr/share/applications "$ICONS_DIR"
	if [[ -f "$ICON_SRC" ]]; then
		install -o root -g root -m 0644 "$ICON_SRC" "$ICONS_DIR/magicremoteservice.png"
	else
		echo "    Warning: no icon found at $ICON_SRC — menu entry will use a generic icon." >&2
	fi
	cat > /usr/share/applications/magicremoteservice.desktop <<EOF
[Desktop Entry]
Type=Application
Name=MagicRemoteService
Comment=Configure and control MagicRemoteService
Exec=$GUI_APPIMAGE
Icon=magicremoteservice
Terminal=false
Categories=Utility;Settings;
EOF
	chmod 0644 /usr/share/applications/magicremoteservice.desktop
	command -v update-desktop-database > /dev/null && update-desktop-database /usr/share/applications || true
	command -v gtk-update-icon-cache > /dev/null && gtk-update-icon-cache -f -t /usr/share/icons/hicolor 2> /dev/null || true
	echo "    Installed — the app should now appear in the system's application menu/launcher."
fi

if [[ -n "$GUI_USER" ]]; then
	echo "==> Adding '$GUI_USER' to group '$SERVICE_GROUP' (write access to $SHARED_CONFIG_DIR)"
	if ! id "$GUI_USER" > /dev/null 2>&1; then
		echo "    Warning: user '$GUI_USER' does not exist — skipping." >&2
	else
		usermod -aG "$SERVICE_GROUP" "$GUI_USER"
		echo "    Done. '$GUI_USER' must log out and back in (new group membership only applies to new"
		echo "    sessions) before the GUI can write to $SHARED_CONFIG_DIR. To test without logging out,"
		echo "    run the GUI via: sg $SERVICE_GROUP -c '<path-to-appimage>'"
	fi
fi

echo "==> Done."
echo "    Check status with: systemctl status magicremoteservice"
echo "    Check logs with:   journalctl -u magicremoteservice -f"
