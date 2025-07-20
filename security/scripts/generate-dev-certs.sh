#!/bin/bash

# ADHD 生產力系統 - 開發環境 SSL 證書生成腳本
# 此腳本用於生成自簽名 SSL 證書，僅適用於開發環境

set -e

CERT_DIR="/certs"
COUNTRY="TW"
STATE="Taiwan"
CITY="Taipei"
ORGANIZATION="ADHD Productivity System"
COMMON_NAME="localhost"
EMAIL="admin@adhd-productivity.dev"
DAYS=365

# 確保證書目錄存在
mkdir -p "$CERT_DIR"

echo "🔐 開始生成 ADHD 生產力系統開發環境 SSL 證書..."
echo "   證書目錄: $CERT_DIR"
echo "   有效期限: $DAYS 天"
echo "   通用名稱: $COMMON_NAME"

# 生成私鑰 (4096位元，更安全)
echo "📝 步驟 1/4: 生成 RSA 私鑰..."
openssl genrsa -out "$CERT_DIR/adhd-dev.key" 4096

# 創建證書請求配置
echo "📝 步驟 2/4: 創建證書簽名請求配置..."
cat > "$CERT_DIR/adhd-dev.conf" << EOF
[req]
default_bits = 4096
prompt = no
default_md = sha256
distinguished_name = dn
req_extensions = v3_req

[dn]
C=$COUNTRY
ST=$STATE
L=$CITY
O=$ORGANIZATION
emailAddress=$EMAIL
CN=$COMMON_NAME

[v3_req]
basicConstraints = CA:FALSE
keyUsage = keyEncipherment, dataEncipherment
subjectAltName = @alt_names

[alt_names]
DNS.1 = localhost
DNS.2 = *.localhost
DNS.3 = adhd-nginx
DNS.4 = *.adhd-nginx
DNS.5 = 127.0.0.1
DNS.6 = ::1
IP.1 = 127.0.0.1
IP.2 = ::1
IP.3 = 172.20.0.0/16
EOF

# 生成證書簽名請求
echo "📝 步驟 3/4: 生成證書簽名請求..."
openssl req -new -key "$CERT_DIR/adhd-dev.key" -out "$CERT_DIR/adhd-dev.csr" -config "$CERT_DIR/adhd-dev.conf"

# 生成自簽名證書
echo "📝 步驟 4/4: 生成自簽名證書..."
openssl x509 -req -in "$CERT_DIR/adhd-dev.csr" -signkey "$CERT_DIR/adhd-dev.key" -out "$CERT_DIR/adhd-dev.crt" -days $DAYS -extensions v3_req -extfile "$CERT_DIR/adhd-dev.conf"

# 生成 PEM 格式的全鏈證書
echo "🔗 生成 PEM 格式全鏈證書..."
cat "$CERT_DIR/adhd-dev.crt" > "$CERT_DIR/adhd-dev-fullchain.pem"

# 生成 DH 參數 (提高安全性)
echo "🔒 生成 Diffie-Hellman 參數..."
openssl dhparam -out "$CERT_DIR/dhparam.pem" 2048

# 設置適當的文件權限
echo "🔐 設置證書文件權限..."
chmod 600 "$CERT_DIR"/adhd-dev.key
chmod 644 "$CERT_DIR"/adhd-dev.crt
chmod 644 "$CERT_DIR"/adhd-dev-fullchain.pem
chmod 644 "$CERT_DIR"/dhparam.pem
chmod 600 "$CERT_DIR"/adhd-dev.conf

# 顯示證書資訊
echo "📋 證書資訊:"
openssl x509 -in "$CERT_DIR/adhd-dev.crt" -text -noout | grep -E "(Subject:|Issuer:|Not Before|Not After|DNS:|IP Address:)"

echo ""
echo "✅ SSL 證書生成完成！"
echo ""
echo "📁 生成的文件:"
echo "   🔑 私鑰: $CERT_DIR/adhd-dev.key"
echo "   📜 證書: $CERT_DIR/adhd-dev.crt"
echo "   🔗 全鏈證書: $CERT_DIR/adhd-dev-fullchain.pem"
echo "   🔒 DH 參數: $CERT_DIR/dhparam.pem"
echo ""
echo "⚠️  注意事項:"
echo "   • 此為開發環境專用的自簽名證書"
echo "   • 瀏覽器會顯示「不安全」警告，這是正常的"
echo "   • 生產環境請使用有效的 CA 簽名證書"
echo "   • 請妥善保管私鑰文件，不要提交到版本控制系統"
echo ""
echo "🌐 要信任此證書，請將 adhd-dev.crt 添加到系統的受信任根證書頒發機構"

# 清理臨時文件
rm -f "$CERT_DIR/adhd-dev.csr"

echo "🧹 清理完成"