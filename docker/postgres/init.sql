CREATE ROLE planboard_owner LOGIN BYPASSRLS PASSWORD 'planboard_owner_dev';
CREATE ROLE planboard_app LOGIN NOBYPASSRLS PASSWORD 'planboard_app_dev';

ALTER DATABASE planboard OWNER TO planboard_owner;
ALTER SCHEMA public OWNER TO planboard_owner;

GRANT CONNECT ON DATABASE planboard TO planboard_app;
GRANT USAGE ON SCHEMA public TO planboard_app;

ALTER DEFAULT PRIVILEGES FOR ROLE planboard_owner IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO planboard_app;
