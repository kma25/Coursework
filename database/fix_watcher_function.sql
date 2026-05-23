-- Быстрый фикс для уже созданных баз, если watcher пишет:
-- function save_system_metric(text, text, double precision, double precision, double precision) does not exist

create or replace function save_system_metric(
    p_device_key text,
    p_device_name text,
    p_cpu double precision,
    p_ram double precision,
    p_hdd double precision)
returns void
language plpgsql
security definer
as $$
declare
    v_device_id integer;
begin
    insert into monitored_devices(device_key, device_name, last_seen_at)
    values (p_device_key, p_device_name, now())
    on conflict (device_key)
    do update set device_name = excluded.device_name, last_seen_at = now()
    returning id into v_device_id;

    insert into system_metrics(device_id, cpu_percent, ram_percent, hdd_percent)
    values (v_device_id, round(p_cpu::numeric, 2), round(p_ram::numeric, 2), round(p_hdd::numeric, 2));
end;
$$;

grant execute on function save_system_metric(text, text, double precision, double precision, double precision) to app_watcher_role;
