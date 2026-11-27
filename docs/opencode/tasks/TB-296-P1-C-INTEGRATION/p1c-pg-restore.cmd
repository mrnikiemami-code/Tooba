@echo off
docker cp %2 postgres-db:/tmp/p1c-restore-list.dump >nul
docker exec postgres-db pg_restore --list /tmp/p1c-restore-list.dump
