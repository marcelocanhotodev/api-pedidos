#!/usr/bin/env bash
# Confirma que a versão implantada está no ar: GET /info com o commit esperado e GET /health/ready = 200.
# Tolera o "despertar" do plano gratuito (Render + Neon) repetindo até o tempo limite.
#
# Uso: scripts/verificar-implantacao.sh <url-base> <commit> [limite-em-segundos]
#   ex.: scripts/verificar-implantacao.sh https://api-pedidos.onrender.com 0123456789abcdef 600
set -euo pipefail

URL="${1:?informe a URL base, ex.: https://api-pedidos.onrender.com}"
COMMIT="${2:?informe o commit implantado}"
LIMITE="${3:-600}"
INTERVALO="${INTERVALO:-10}"

URL="${URL%/}"
CURTO="${COMMIT:0:7}"
inicio=$(date +%s)

echo "Aguardando ${URL} responder com o commit ${CURTO} (limite: ${LIMITE}s)"
while true; do
  decorrido=$(( $(date +%s) - inicio ))

  versao="$(curl -s --max-time 60 "${URL}/info" | sed -n 's/.*"versao":"\([^"]*\)".*/\1/p' || true)"
  if [[ "$versao" == *"+${CURTO}" ]]; then
    pronto="$(curl -s -o /dev/null --max-time 60 -w '%{http_code}' "${URL}/health/ready" || true)"
    if [[ "$pronto" == "200" ]]; then
      echo "OK: versão ${versao} no ar e /health/ready = 200 após ~${decorrido}s"
      exit 0
    fi
    echo "  versão ${versao} no ar, /health/ready = ${pronto:-sem resposta} (${decorrido}s)"
  else
    echo "  ainda não: versão atual '${versao:-sem resposta}' (${decorrido}s)"
  fi

  if (( decorrido >= LIMITE )); then
    echo "::error::A verificação pós-deploy não confirmou o commit ${CURTO} em ${LIMITE}s"
    exit 1
  fi
  sleep "$INTERVALO"
done
