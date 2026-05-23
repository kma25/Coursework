-- Выполнять в pgAdmin только если базы еще не созданы.
-- Важно: CREATE DATABASE нельзя запускать внутри открытой транзакции.

create database "Coursework tester"
    with encoding 'UTF8'
    template template0;

create database "Coursework test bsses"
    with encoding 'UTF8'
    template template0;
