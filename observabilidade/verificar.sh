#!/usr/bin/env bash
# Verifica, de ponta a ponta, que os logs da API chegam ao Loki e podem ser consultados pelo Grafana.
# Uso: ./observabilidade/verificar.sh   (com a stack no ar: docker compose up -d)
set -euo pipefail

API_URL="${API_URL:-http://localhost:8080}"
GRAFANA_URL="${GRAFANA_URL:-http://localhost:3000}"
LIMITE_SEGUNDOS="${LIMITE_SEGUNDOS:-30}"

# Credenciais do Grafana: variáveis de ambiente ou .env na raiz do projeto.
if [[ -z "${GRAFANA_ADMIN_USUARIO:-}" || -z "${GRAFANA_ADMIN_SENHA:-}" ]] && [[ -f .env ]]; then
  GRAFANA_ADMIN_USUARIO="$(grep -E '^GRAFANA_ADMIN_USUARIO=' .env | cut -d= -f2-)"
  GRAFANA_ADMIN_SENHA="$(grep -E '^GRAFANA_ADMIN_SENHA=' .env | cut -d= -f2-)"
fi
: "${GRAFANA_ADMIN_USUARIO:?defina GRAFANA_ADMIN_USUARIO (ou rode na raiz do projeto, com .env)}"
: "${GRAFANA_ADMIN_SENHA:?defina GRAFANA_ADMIN_SENHA (ou rode na raiz do projeto, com .env)}"

# Logo após subir (principalmente com volume novo), o Loki leva alguns segundos para aceitar logs.
# Os 30 s da verificação contam a partir do Loki pronto.
echo "0) Aguardando o Loki ficar pronto (até 90s)"
for ((segundos = 0; ; segundos += 3)); do
  if curl -sf -u "$GRAFANA_ADMIN_USUARIO:$GRAFANA_ADMIN_SENHA" \
      "$GRAFANA_URL/api/datasources/proxy/uid/loki/ready" 2>/dev/null | grep -q ready; then
    break
  fi
  (( segundos < 90 )) || { echo "FALHA: Loki não ficou pronto em 90s"; exit 1; }
  sleep 3
done

# O log de requisição registra o caminho sem query string; por isso a busca é pelo log de
# GET /info emitido a partir do instante da requisição (1 s de folga para relógios).
inicio_ns="$(( $(date +%s) - 1 ))000000000"

echo "1) Gerando requisição: GET /info"
status="$(curl -s -o /dev/null -w '%{http_code}' "$API_URL/info")"
[[ "$status" == "200" ]] || { echo "FALHA: a API respondeu $status"; exit 1; }

echo "2) Consultando o Loki pelo Grafana (limite: ${LIMITE_SEGUNDOS}s)"
consulta='{servico="api"} | json | Properties_RequestPath="/info"'
marca='"Properties_RequestPath":"/info"'
for ((segundos = 0; segundos < LIMITE_SEGUNDOS; segundos += 2)); do
  resposta="$(curl -s -u "$GRAFANA_ADMIN_USUARIO:$GRAFANA_ADMIN_SENHA" -G \
    "$GRAFANA_URL/api/datasources/proxy/uid/loki/loki/api/v1/query_range" \
    --data-urlencode "query=$consulta" --data-urlencode "start=$inicio_ns" --data-urlencode "limit=5" || true)"
  if grep -q "$marca" <<< "$resposta"; then
    echo "OK: log da requisição encontrado no Loki após ~${segundos}s"
    exit 0
  fi
  sleep 2
done

echo "FALHA: log da requisição não apareceu no Loki em ${LIMITE_SEGUNDOS}s"
echo "Dica: docker compose ps; docker compose logs alloy loki"
exit 1
