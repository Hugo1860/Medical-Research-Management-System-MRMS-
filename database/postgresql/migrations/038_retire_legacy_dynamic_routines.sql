-- Close the SQL Server compatibility gap without carrying unsafe dynamic SQL into PostgreSQL.
-- The old procedures either generated SQL Server DDL or accepted arbitrary table/where fragments.
-- No executable .NET call site remains; current queries use parameterized, allow-listed functions.
BEGIN;

CREATE OR REPLACE FUNCTION "sp_GetList"(
    p_tb_name varchar,
    p_tb_fields varchar DEFAULT '*',
    p_key_fields varchar DEFAULT '编号',
    p_order_type boolean DEFAULT false,
    p_order_str varchar DEFAULT '',
    p_str_where varchar DEFAULT ''
) RETURNS jsonb
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server sp_GetList 已退休；请使用具体的 PostgreSQL 白名单查询函数'
        USING ERRCODE = '0A000', HINT = '不要传入任意表名、字段或 where 片段';
END;
$$;

CREATE OR REPLACE FUNCTION "sp_GetPagingList"(
    p_tb_name varchar,
    p_tb_fields varchar DEFAULT '*',
    p_key_fields varchar DEFAULT '编号',
    p_order_type boolean DEFAULT false,
    p_page_size integer DEFAULT 10,
    p_page_index integer DEFAULT 1,
    p_order_str varchar DEFAULT '',
    p_str_where varchar DEFAULT ''
) RETURNS jsonb
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server sp_GetPagingList 已退休；请使用具体的 PostgreSQL 白名单分页函数'
        USING ERRCODE = '0A000', HINT = '不要传入任意表名、字段或 where 片段';
END;
$$;

CREATE OR REPLACE FUNCTION "sp_GetPagingList_作废180601"(
    p_tb_name varchar,
    p_tb_fields varchar DEFAULT '*',
    p_key_fields varchar DEFAULT '编号',
    p_order_type boolean DEFAULT false,
    p_page_size integer DEFAULT 10,
    p_page_index integer DEFAULT 1,
    p_order_str varchar DEFAULT '',
    p_str_where varchar DEFAULT ''
) RETURNS jsonb
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server 作废分页过程已退休；请使用具体的 PostgreSQL 白名单分页函数'
        USING ERRCODE = '0A000', HINT = '该对象仅为历史兼容标记';
END;
$$;

CREATE OR REPLACE FUNCTION "GET_TTScript_MSSQL"(
    p_dbname varchar, p_tbname varchar
) RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server 表值参数脚本生成器已退休；PostgreSQL 使用 jsonb_to_recordset'
        USING ERRCODE = '0A000';
END;
$$;

CREATE OR REPLACE FUNCTION "GET_MergeScript_MSSQL"(
    p_dbname varchar, p_tbname varchar
) RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server MERGE 脚本生成器已退休；PostgreSQL CRUD 使用 ON CONFLICT 函数'
        USING ERRCODE = '0A000';
END;
$$;

CREATE OR REPLACE FUNCTION "GET_TrigerScript_MSSQL"(
    p_dbname varchar, p_tbname varchar, p_unique_field_name varchar, p_unique_field_declare varchar
) RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server 触发器脚本生成器已退休；重复校验已由 PostgreSQL trigger function 实现'
        USING ERRCODE = '0A000';
END;
$$;

CREATE OR REPLACE FUNCTION "GET_EnableDisableScript_MSSQL"(
    p_dbname varchar, p_tbname varchar, p_is_enabled_field_name varchar
) RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server 启禁用脚本生成器已退休；请调用具体表的启禁用函数'
        USING ERRCODE = '0A000';
END;
$$;

CREATE OR REPLACE FUNCTION "GET_DeleteScript_MSSQL"(
    p_dbname varchar, p_tbname varchar
) RETURNS text
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'SQL Server 删除脚本生成器已退休；请调用具体表的 jsonb 删除函数'
        USING ERRCODE = '0A000';
END;
$$;

-- A machine-readable audit surface records that the eight source objects were
-- deliberately retired, rather than silently omitted from the migration.
CREATE OR REPLACE FUNCTION "fn_迁移遗留例程状态"()
RETURNS TABLE("名称" text, "状态" text, "替代实现" text, "说明" text)
LANGUAGE sql
IMMUTABLE
AS $$
    VALUES
      ('sp_GetList', '已退休', '具体 PostgreSQL 白名单查询函数', '拒绝任意表名、字段和 where 片段'),
      ('sp_GetPagingList', '已退休', '具体 PostgreSQL 白名单分页函数', '拒绝任意表名、字段和 where 片段'),
      ('sp_GetPagingList_作废180601', '已退休', '无', '历史作废分页过程，不再作为运行时依赖'),
      ('GET_TTScript_MSSQL', '已退休', 'jsonb_to_recordset', 'PostgreSQL 不生成 SQL Server 表值参数脚本'),
      ('GET_MergeScript_MSSQL', '已退休', 'ON CONFLICT CRUD 函数', 'PostgreSQL 不生成 SQL Server MERGE 脚本'),
      ('GET_TrigerScript_MSSQL', '已退休', 'PostgreSQL trigger function', '35 个重复校验触发器已迁移'),
      ('GET_EnableDisableScript_MSSQL', '已退休', '具体表启禁用函数', '不再拼接任意表名和字段'),
      ('GET_DeleteScript_MSSQL', '已退休', '具体表 jsonb 删除函数', '不再拼接任意表名和字段');
$$;

DO $$
DECLARE
    v_count integer;
BEGIN
    SELECT count(*) INTO v_count
      FROM "fn_迁移遗留例程状态"()
     WHERE "状态" = '已退休';
    IF v_count <> 8 THEN
        RAISE EXCEPTION '遗留动态过程退休登记不完整：%', v_count;
    END IF;
END;
$$;

REVOKE ALL ON FUNCTION "sp_GetList"(varchar,varchar,varchar,boolean,varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "sp_GetPagingList"(varchar,varchar,varchar,boolean,integer,integer,varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "sp_GetPagingList_作废180601"(varchar,varchar,varchar,boolean,integer,integer,varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "GET_TTScript_MSSQL"(varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "GET_MergeScript_MSSQL"(varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "GET_TrigerScript_MSSQL"(varchar,varchar,varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "GET_EnableDisableScript_MSSQL"(varchar,varchar,varchar) FROM PUBLIC;
REVOKE ALL ON FUNCTION "GET_DeleteScript_MSSQL"(varchar,varchar) FROM PUBLIC;

UPDATE "系统迁移版本"
   SET "版本" = 38,
       "脚本" = '001-038',
       "检查摘要" = '62 张表、52 个视图、74 个过程已转换，8 个 SQL Server 动态过程已安全退休'
 WHERE "编号" = 1;

COMMIT;
