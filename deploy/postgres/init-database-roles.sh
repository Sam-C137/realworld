#!/bin/sh
set -eu

for required in APP_DB_USER APP_DB_PASSWORD GATEWAY_DB_USER GATEWAY_DB_PASSWORD; do
    value=$(printenv "$required" || true)
    if [ -z "$value" ]; then
        echo "$required is required" >&2
        exit 1
    fi
done

psql \
    --set ON_ERROR_STOP=1 \
    --username "$POSTGRES_USER" \
    --dbname "$POSTGRES_DB" \
    --set app_user="$APP_DB_USER" \
    --set app_password="$APP_DB_PASSWORD" \
    --set gateway_user="$GATEWAY_DB_USER" \
    --set gateway_password="$GATEWAY_DB_PASSWORD" \
    --set database_name="$POSTGRES_DB" <<-'EOSQL'
SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'app_user', :'app_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'app_user') \gexec
SELECT format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', :'app_user', :'app_password') \gexec
SELECT format('ALTER DATABASE %I OWNER TO %I', :'database_name', :'app_user') \gexec
SELECT format('ALTER SCHEMA public OWNER TO %I', :'app_user') \gexec

-- These ownership updates also make the script safe to run against a volume
-- that was initialized before the dedicated application role existed.
SELECT format('ALTER TABLE %I.%I OWNER TO %I', schemaname, tablename, :'app_user')
FROM pg_tables
WHERE schemaname = 'public' \gexec
SELECT format('ALTER SEQUENCE %I.%I OWNER TO %I', sequence_schema, sequence_name, :'app_user')
FROM information_schema.sequences
WHERE sequence_schema = 'public' \gexec

SELECT format('CREATE ROLE %I LOGIN PASSWORD %L', :'gateway_user', :'gateway_password')
WHERE NOT EXISTS (SELECT FROM pg_roles WHERE rolname = :'gateway_user') \gexec
SELECT format('ALTER ROLE %I WITH LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD %L', :'gateway_user', :'gateway_password') \gexec
SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'database_name', :'gateway_user') \gexec
SELECT format('GRANT USAGE ON SCHEMA public TO %I', :'gateway_user') \gexec
SELECT format('GRANT SELECT ON ALL TABLES IN SCHEMA public TO %I', :'gateway_user') \gexec
SELECT format('GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO %I', :'gateway_user') \gexec
SELECT format('ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA public GRANT SELECT ON TABLES TO %I', :'app_user', :'gateway_user') \gexec
SELECT format('ALTER DEFAULT PRIVILEGES FOR ROLE %I IN SCHEMA public GRANT SELECT ON SEQUENCES TO %I', :'app_user', :'gateway_user') \gexec
EOSQL
