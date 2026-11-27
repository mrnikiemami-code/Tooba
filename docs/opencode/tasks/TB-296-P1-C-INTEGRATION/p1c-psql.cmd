@echo off
docker exec -i postgres-db psql -U admin -d tooba_alpha -X -qAt -v ON_ERROR_STOP=1 -c %*
