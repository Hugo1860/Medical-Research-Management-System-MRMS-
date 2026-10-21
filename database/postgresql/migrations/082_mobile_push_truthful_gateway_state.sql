-- P0: 移动协同通道真实状态与失败闭环。
-- 073 的数据库函数只写入“已发送/模拟成功”，但没有调用外部网关，
-- 这会把演示记录误报成真实通知。本迁移让通道先经过真实适配器验证；
-- Worker 未运行时只能排队或明确失败，禁止再产生模拟成功。
BEGIN;

ALTER TABLE "移动协同消息配置"
    ADD COLUMN IF NOT EXISTS "配置状态" varchar(20) NOT NULL DEFAULT '待配置',
    ADD COLUMN IF NOT EXISTS "最后测试时间" timestamp without time zone,
    ADD COLUMN IF NOT EXISTS "最后测试状态" varchar(30) NOT NULL DEFAULT '未执行',
    ADD COLUMN IF NOT EXISTS "最后测试提示" text;

ALTER TABLE "移动协同消息配置"
    DROP CONSTRAINT IF EXISTS "ck_移动消息_配置状态";
ALTER TABLE "移动协同消息配置"
    ADD CONSTRAINT "ck_移动消息_配置状态"
    CHECK ("配置状态" IN ('待配置','待验证','已验证','验证失败','已停用'));

ALTER TABLE "移动待办推送日志"
    ADD COLUMN IF NOT EXISTS "回执来源" varchar(20) NOT NULL DEFAULT '未核验';

-- 先把旧版本留下的模拟状态改为明确失败，再收紧状态约束。
UPDATE "移动待办推送日志"
   SET "推送状态" = '发送失败'
       ,"回执来源" = '历史迁移'
       ,"响应报文" = COALESCE("响应报文", '') || E'\n迁移 082：历史记录未被真实移动网关确认。'
 WHERE "推送状态" = '模拟成功';

-- 073 的固定演示调用使用了“已发送”状态；该条记录同样没有真实网关回执。
UPDATE "移动待办推送日志"
   SET "推送状态" = '发送失败',
       "回执来源" = '历史迁移',
       "响应报文" = COALESCE("响应报文", '') || E'\n迁移 082：073 演示调用未被真实移动网关确认。'
 WHERE "推送状态" = '已发送'
   AND "业务编号" = 'PRJ-2026-001'
   AND "消息标题" LIKE '【企业微信待办】%';

UPDATE "移动待办推送日志" AS l
   SET "推送状态" = '发送失败',
       "回执来源" = '历史迁移',
       "响应报文" = COALESCE(l."响应报文", '') || E'\n迁移 082：示例移动通道没有真实网关回执。'
 WHERE l."推送状态" = '已发送'
   AND l."通道编号" IN (
       SELECT "编号" FROM "移动协同消息配置"
        WHERE "通道名称" IN ('北京清华长庚移动协同工作台-待办推送通道', '医院科研管理系统-钉钉移动待办网关')
   );

-- 其他历史“已发送”记录如果没有真实网关回执来源，也只能按未核验处理。
UPDATE "移动待办推送日志"
   SET "推送状态" = '发送失败',
       "回执来源" = '历史迁移',
       "响应报文" = COALESCE("响应报文", '') || E'\n迁移 082：历史已发送状态缺少真实网关回执来源。'
 WHERE "推送状态" = '已发送'
   AND COALESCE("回执来源", '未核验') <> '真实网关';

ALTER TABLE "移动待办推送日志"
    DROP CONSTRAINT IF EXISTS "ck_移动推送_状态";
ALTER TABLE "移动待办推送日志"
    ADD CONSTRAINT "ck_移动推送_状态"
    CHECK ("推送状态" IN ('待发送', '已发送', '发送失败', '未配置', '通道未验证'));

ALTER TABLE "移动待办推送日志"
    DROP CONSTRAINT IF EXISTS "ck_移动推送_回执来源";
ALTER TABLE "移动待办推送日志"
    ADD CONSTRAINT "ck_移动推送_回执来源"
    CHECK ("回执来源" IN ('未核验','真实网关','系统排队','配置检查','历史迁移'));

-- 073 的示例配置包含演示地址、应用 ID 和密钥。清除它们并停用，
-- 避免把示例凭据当成医院真实通道，也避免启动后向公网发起请求。
UPDATE "移动协同消息配置"
   SET "Webhook地址" = NULL,
       "应用ID" = NULL,
       "应用密钥" = NULL,
       "基础配置JSON" = NULL,
       "是否启用" = FALSE,
       "配置状态" = '待配置',
       "最后测试状态" = '未执行',
       "最后测试提示" = '迁移 082 已清理示例通道；请登记真实网关地址和凭据后再验收。',
       "更新时间" = CURRENT_TIMESTAMP
 WHERE "通道名称" IN ('北京清华长庚移动协同工作台-待办推送通道', '医院科研管理系统-钉钉移动待办网关');

CREATE INDEX IF NOT EXISTS "ix_移动消息_状态启用"
    ON "移动协同消息配置" ("配置状态", "是否启用", "编号");

-- 发送函数只负责生成待发送队列。只有外部适配器拿到真实 HTTP 回执后，
-- 才允许把日志更新为“已发送”；数据库函数本身绝不伪造成功。
CREATE OR REPLACE FUNCTION sp_发送企业微信待办通知(
    p_消息类型 character varying,
    p_业务模块 character varying,
    p_业务编号 character varying,
    p_接收人编号 integer,
    p_消息标题 character varying,
    p_消息摘要 text,
    p_卡片链接 character varying DEFAULT NULL,
    OUT p_日志编号 integer,
    OUT p_通道名称 character varying,
    OUT p_推送状态 character varying,
    OUT p_提示消息 character varying
)
RETURNS record
LANGUAGE plpgsql
AS $$
DECLARE
    v_channel_id integer;
    v_channel_name varchar(100);
    v_channel_type varchar(50);
    v_channel_enabled boolean;
    v_channel_status varchar(20);
    v_webhook varchar(500);
    v_app_id varchar(100);
    v_secret varchar(200);
    v_user_name varchar(50);
    v_user_account varchar(100);
    v_status varchar(30);
    v_source varchar(20);
    v_response text;
BEGIN
    SELECT "编号", "通道名称", "通道类型", "是否启用", "配置状态",
           NULLIF(BTRIM("Webhook地址"), ''), NULLIF(BTRIM("应用ID"), ''),
           NULLIF(BTRIM("应用密钥"), '')
      INTO v_channel_id, v_channel_name, v_channel_type, v_channel_enabled,
           v_channel_status, v_webhook, v_app_id, v_secret
      FROM "移动协同消息配置"
     ORDER BY CASE WHEN "是否启用" THEN 0 ELSE 1 END, "编号" ASC
     LIMIT 1;

    SELECT "姓名", "工号"
      INTO v_user_name, v_user_account
      FROM "人员"
     WHERE "编号" = p_接收人编号;
    IF v_user_name IS NULL THEN
        v_user_name := '科研学者';
        v_user_account := 'EMP-' || COALESCE(p_接收人编号, 0)::varchar;
    END IF;

    IF v_channel_id IS NULL OR NOT COALESCE(v_channel_enabled, FALSE)
       OR v_channel_status IN ('待配置', '已停用')
       OR v_webhook IS NULL OR v_app_id IS NULL OR v_secret IS NULL THEN
        v_status := '未配置';
        v_source := '配置检查';
        v_response := '未检测到可发送的真实移动通道：需要启用通道、完整凭据，并完成外部适配器验证。';
        v_channel_name := COALESCE(v_channel_name, '未配置真实通道');
    ELSIF v_channel_status <> '已验证' THEN
        v_status := '通道未验证';
        v_source := '配置检查';
        v_response := '通道配置字段完整，但尚未取得真实网关验证回执；当前请求未发送到外部系统。';
    ELSE
        v_status := '待发送';
        v_source := '系统排队';
        v_response := '已进入移动外部适配器队列，等待真实网关处理；本系统未伪造已发送回执。';
    END IF;

    INSERT INTO "移动待办推送日志" (
        "通道编号", "消息类型", "业务模块", "业务编号",
        "接收人编号", "接收人姓名", "接收人账号",
        "消息标题", "消息摘要", "卡片跳转链接",
        "推送状态", "回执来源", "响应报文", "推送时间"
    ) VALUES (
        v_channel_id, COALESCE(NULLIF(BTRIM(p_消息类型), ''), '待办审批'),
        COALESCE(NULLIF(BTRIM(p_业务模块), ''), '工作流'), p_业务编号,
        p_接收人编号, v_user_name, v_user_account,
        p_消息标题, p_消息摘要, COALESCE(p_卡片链接, '/office/mobile-workbench'),
        v_status, v_source, v_response, CURRENT_TIMESTAMP
    ) RETURNING "编号" INTO p_日志编号;

    p_通道名称 := v_channel_name;
    p_推送状态 := v_status;
    p_提示消息 := v_response;
END;
$$;

-- 测试接口只能进行本地配置检查；真实握手必须由外部适配器执行。
CREATE OR REPLACE FUNCTION sp_测试移动推送通道(
    p_通道编号 integer,
    p_测试人编号 integer,
    OUT p_成功 boolean,
    OUT p_提示消息 character varying
)
RETURNS record
LANGUAGE plpgsql
AS $$
DECLARE
    v_enabled boolean;
    v_status varchar(20);
    v_name varchar(100);
    v_webhook varchar(500);
    v_app_id varchar(100);
    v_secret varchar(200);
    v_message varchar(500);
BEGIN
    SELECT "通道名称", "是否启用", "配置状态",
           NULLIF(BTRIM("Webhook地址"), ''), NULLIF(BTRIM("应用ID"), ''),
           NULLIF(BTRIM("应用密钥"), '')
      INTO v_name, v_enabled, v_status, v_webhook, v_app_id, v_secret
      FROM "移动协同消息配置"
     WHERE "编号" = p_通道编号;

    IF v_name IS NULL THEN
        p_成功 := FALSE;
        p_提示消息 := '指定的移动协同推送通道不存在';
        RETURN;
    END IF;

    IF NOT COALESCE(v_enabled, FALSE) OR v_webhook IS NULL OR v_app_id IS NULL OR v_secret IS NULL THEN
        v_message := '配置不完整或通道已停用，未执行外部握手。';
        UPDATE "移动协同消息配置"
           SET "配置状态" = CASE WHEN COALESCE(v_enabled, FALSE) THEN '待配置' ELSE '已停用' END,
               "最后测试时间" = CURRENT_TIMESTAMP,
               "最后测试状态" = '配置不完整',
               "最后测试提示" = v_message,
               "更新时间" = CURRENT_TIMESTAMP
         WHERE "编号" = p_通道编号;
        p_成功 := FALSE;
        p_提示消息 := v_message;
        RETURN;
    END IF;

    v_message := '配置字段完整，但本地测试未访问外部网关；Worker 当前不会模拟成功。请启用真实适配器后重新验收。';
    UPDATE "移动协同消息配置"
       SET "配置状态" = '待验证',
           "最后测试时间" = CURRENT_TIMESTAMP,
           "最后测试状态" = '未执行真实握手',
           "最后测试提示" = v_message,
           "更新时间" = CURRENT_TIMESTAMP
     WHERE "编号" = p_通道编号;
    p_成功 := FALSE;
    p_提示消息 := v_message;
END;
$$;

UPDATE "系统迁移版本"
   SET "版本" = 82,
       "脚本" = '082_mobile_push_truthful_gateway_state.sql',
       "检查摘要" = '移动推送改为真实通道验证、排队与失败状态，清除示例凭据并禁止模拟成功',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
