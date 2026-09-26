#!/usr/bin/env bash
# Seralyth Menu API - Oracle Cloud Always Free VM setup (ARM Ampere A1 / aarch64)
#
# Usage on the VM (after creating it in the OCI console):
#   git clone https://github.com/1x1x1x1736/api.git
#   cd api/Seralyth-Menu-4.8.5
#   sudo bash deploy/oracle-setup.sh
#
# The script resolves its own location, so any working directory works as long
# as this file keeps its place next to Server/. It publishes the ASP.NET Core
# project from source, so the .NET SDK is installed, not just the runtime.
#
# Safe to re-run: it is idempotent.

set -euo pipefail

APP_DIR=/opt/seralyth-api
DATA_DIR=/var/lib/seralyth
SERVICE=seralyth-api
DOTNET_DIR=/usr/local/dotnet
APP_USER=seralyth

log() { printf '\033[1;34m[setup]\033[0m %s\n' "$*"; }
die() { printf '\033[1;31m[error]\033[0m %s\n' "$*" >&2; exit 1; }

[ "$(id -u)" -eq 0 ] || die "run with sudo: sudo bash deploy/oracle-setup.sh"

SRC_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
log "source directory: $SRC_DIR"
[ -f "$SRC_DIR/Server/Server.csproj" ] || die "Server/Server.csproj not found next to deploy/ - run this from the Seralyth-Menu-4.8.5 source folder"

# ---------------------------------------------------------------- .NET 9
if [ ! -x "$DOTNET_DIR/dotnet" ]; then
  # The SDK is required: this script publishes the project from source.
  # The ASP.NET Core runtime ships with it.
  log "installing .NET 9 SDK"
  command -v curl >/dev/null || die "curl is required"
  command -v tar  >/dev/null || die "tar is required"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel 9.0 --install-dir "$DOTNET_DIR"
else
  log ".NET already present: $("$DOTNET_DIR/dotnet" --version)"
fi
ln -sf "$DOTNET_DIR/dotnet" /usr/local/bin/dotnet

# ---------------------------------------------------------------- build & install
log "publishing application"
install -d -m 0755 "$APP_DIR"
dotnet publish "$SRC_DIR/Server/Server.csproj" -c Release -o "$APP_DIR" --nologo

id -u "$APP_USER" >/dev/null 2>&1 || useradd --system --home "$APP_DIR" --shell /usr/sbin/nologin "$APP_USER"
install -d -o "$APP_USER" -g "$APP_USER" -m 0750 "$DATA_DIR"

# ---------------------------------------------------------------- systemd
log "installing systemd service"
cat > /etc/systemd/system/$SERVICE.service <<EOF
[Unit]
Description=Seralyth Menu API
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=$APP_USER
Group=$APP_USER
WorkingDirectory=$APP_DIR
Environment=PORT=8080
Environment=DATA_DIR=$DATA_DIR
Environment=DOTNET_ROOT=$DOTNET_DIR
Environment=ASPNETCORE_ENVIRONMENT=Production
ExecStart=$DOTNET_DIR/dotnet $APP_DIR/Seralyth.Menu.Server.dll
Restart=always
RestartSec=5
# The server only needs to write to its own data directory.
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=$DATA_DIR

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now $SERVICE
sleep 4

# ---------------------------------------------------------------- firewall
# Oracle Linux uses firewalld, Ubuntu uses ufw. Open 80/443 for the public IP.
if command -v firewall-cmd >/dev/null; then
  log "configuring firewalld"
  firewall-cmd --permanent --add-port=8080/tcp >/dev/null
  firewall-cmd --permanent --add-service=http >/dev/null
  firewall-cmd --permanent --add-service=https >/dev/null
  firewall-cmd --reload >/dev/null
elif command -v ufw >/dev/null; then
  log "configuring ufw"
  ufw allow 8080/tcp >/dev/null || true
  ufw allow 80/tcp  >/dev/null || true
  ufw allow 443/tcp >/dev/null || true
fi

# ---------------------------------------------------------------- verify
log "service status:"
systemctl --no-pager --lines=0 status $SERVICE || true

if curl -fsS http://127.0.0.1:8080/health >/dev/null 2>&1; then
  log "local health check OK:"
  curl -fsS http://127.0.0.1:8080/health; echo
else
  log "local health check FAILED - recent logs:"
  journalctl -u $SERVICE --no-pager --lines=30 || true
  exit 1
fi

cat <<EOF

done.

  Test URL   : http://<your-vm-public-ip>:8080/health
  Logs       : journalctl -u $SERVICE -f
  Data dir   : $DATA_DIR   (back this up - it holds friend lists and votes)
  Restart    : systemctl restart $SERVICE

  OCI security list: make sure ingress TCP 8080 (and 80/443 if you add a
  load balancer or domain) is allowed in the VCN's Security Rules.
EOF
