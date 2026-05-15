-- Verifica tablas por schema en PostgreSQL
select table_schema,
       count(*) as total_tables
from information_schema.tables
where table_type = 'BASE TABLE'
  and table_schema not in ('pg_catalog', 'information_schema')
group by table_schema
order by table_schema;
