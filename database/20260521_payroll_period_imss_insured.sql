alter table payroll.payroll_periods
    add column if not exists is_imss_insured boolean not null default true;

update payroll.payroll_periods
set is_imss_insured = true
where is_imss_insured is null;
