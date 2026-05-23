-- SQL-скрипт схемы для курсового проекта Coursework.
-- Запускается отдельно в рабочей базе "Coursework tester" и тестовой базе "Coursework test bsses".

create table if not exists roles (
    id serial primary key,
    name varchar(32) not null unique
);

create table if not exists users (
    id serial primary key,
    login varchar(64) not null unique,
    password_hash text not null,
    role_id integer not null references roles(id),
    is_blocked boolean not null default false,
    created_at timestamp not null default now()
);

create table if not exists notes (
    id serial primary key,
    user_id integer not null references users(id) on delete cascade,
    text text not null check (length(trim(text)) > 0),
    created_at timestamp not null default now(),
    updated_at timestamp not null default now()
);

create table if not exists security_logs (
    id serial primary key,
    user_id integer null references users(id) on delete set null,
    event_type varchar(64) not null,
    details text not null,
    created_at timestamp not null default now()
);

create table if not exists monitored_devices (
    id serial primary key,
    device_key varchar(128) not null unique,
    device_name varchar(128) not null,
    last_seen_at timestamp not null default now()
);

create table if not exists system_metrics (
    id serial primary key,
    device_id integer not null references monitored_devices(id) on delete cascade,
    cpu_percent numeric(5, 2) not null check (cpu_percent >= 0 and cpu_percent <= 100),
    ram_percent numeric(5, 2) not null check (ram_percent >= 0 and ram_percent <= 100),
    hdd_percent numeric(5, 2) not null check (hdd_percent >= 0 and hdd_percent <= 100),
    created_at timestamp not null default now()
);

insert into roles(name)
values ('user'), ('admin'), ('statistician')
on conflict (name) do nothing;

do $$
begin
    if not exists (select 1 from pg_roles where rolname = 'app_auth') then
        create role app_auth login password 'change_me_auth_password';
    end if;
    if not exists (select 1 from pg_roles where rolname = 'app_user_role') then
        create role app_user_role login password 'change_me_user_password';
    end if;
    if not exists (select 1 from pg_roles where rolname = 'app_admin_role') then
        create role app_admin_role login password 'change_me_admin_password';
    end if;
    if not exists (select 1 from pg_roles where rolname = 'app_statistician_role') then
        create role app_statistician_role login password 'change_me_statistician_password';
    end if;
    if not exists (select 1 from pg_roles where rolname = 'app_watcher_role') then
        create role app_watcher_role login password 'change_me_watcher_password';
    end if;
end $$;

create or replace function get_role_connection(app_role text)
returns text
language plpgsql
security definer
as $$
begin
    -- Приложение получает только Username/Password роли.
    -- Host, Port и Database остаются в App.config, чтобы БД не управляла адресом подключения.
    case lower(app_role)
        when 'admin' then
            return 'Username=app_admin_role;Password=change_me_admin_password;';
        when 'statistician' then
            return 'Username=app_statistician_role;Password=change_me_statistician_password;';
        else
            return 'Username=app_user_role;Password=change_me_user_password;';
    end case;
end;
$$;

create or replace function save_system_metric(
    p_device_key text,
    p_device_name text,
    p_cpu numeric,
    p_ram numeric,
    p_hdd numeric)
returns void
language plpgsql
security definer
as $$
declare
    v_device_id integer;
begin
    -- Watcher не требует ручного добавления устройства: первая метрика создает запись,
    -- следующие только обновляют имя и время последнего сигнала.
    insert into monitored_devices(device_key, device_name, last_seen_at)
    values (p_device_key, p_device_name, now())
    on conflict (device_key)
    do update set device_name = excluded.device_name, last_seen_at = now()
    returning id into v_device_id;

    insert into system_metrics(device_id, cpu_percent, ram_percent, hdd_percent)
    values (v_device_id, p_cpu, p_ram, p_hdd);
end;
$$;

create index if not exists ix_notes_user_id on notes(user_id);
create index if not exists ix_users_role_id on users(role_id);
create index if not exists ix_security_logs_created_at on security_logs(created_at);
create index if not exists ix_system_metrics_device_created_at on system_metrics(device_id, created_at);
create index if not exists ix_monitored_devices_device_key on monitored_devices(device_key);

revoke all on all tables in schema public from public;
revoke all on all sequences in schema public from public;
revoke all on all functions in schema public from public;

grant usage on schema public to app_auth, app_user_role, app_admin_role, app_statistician_role, app_watcher_role;

grant select on roles to app_auth, app_admin_role;
grant select, insert on users to app_auth;
grant select, insert, update, delete on users to app_admin_role;
grant usage, select on sequence users_id_seq to app_auth, app_admin_role;

grant select, insert, update, delete on notes to app_user_role, app_admin_role;
grant usage, select on sequence notes_id_seq to app_user_role, app_admin_role;

grant insert on security_logs to app_auth, app_user_role, app_admin_role, app_statistician_role;
grant select on security_logs to app_admin_role;
grant usage, select on sequence security_logs_id_seq to app_auth, app_user_role, app_admin_role, app_statistician_role;

grant select on monitored_devices, system_metrics to app_admin_role, app_statistician_role;
grant insert, update on monitored_devices to app_watcher_role;
grant insert on system_metrics to app_watcher_role;
grant usage, select on sequence monitored_devices_id_seq, system_metrics_id_seq to app_watcher_role;

grant execute on function get_role_connection(text) to app_auth;
grant execute on function save_system_metric(text, text, numeric, numeric, numeric) to app_watcher_role;
