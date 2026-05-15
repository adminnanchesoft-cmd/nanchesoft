-- Detalle de tablas físicas con tamaño estimado
select n.nspname as schema_name,
       c.relname as table_name,
       pg_size_pretty(pg_total_relation_size(c.oid)) as total_size,
       cast(coalesce(c.reltuples, 0) as bigint) as estimated_rows
from pg_class c
inner join pg_namespace n on n.oid = c.relnamespace
where c.relkind = 'r'
  and n.nspname not in ('pg_catalog', 'information_schema')
order by n.nspname, c.relname;
