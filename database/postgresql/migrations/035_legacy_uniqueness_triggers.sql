-- P1: preserve the 35 SQL Server duplicate guards without rewriting data.
-- Existing historical duplicates remain visible for cleanup; new duplicate
-- inserts/key changes are rejected with a transaction-safe PostgreSQL trigger.
BEGIN;

CREATE OR REPLACE FUNCTION "trg_唯一性校验"()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    v_pk text := COALESCE(TG_ARGV[0], '编号');
    v_message text := COALESCE(TG_ARGV[1], '数据重复');
    v_sql text;
    v_duplicate boolean;
    v_same_key boolean;
    i integer;
    v_column text;
BEGIN
    IF TG_OP = 'UPDATE' AND TG_NARGS > 2 THEN
        -- The key columns are trigger arguments from position 2 onward.
        v_sql := 'SELECT ';
        FOR i IN 2..(TG_NARGS - 1) LOOP
            IF i > 2 THEN v_sql := v_sql || ' AND '; END IF;
            v_sql := v_sql || format('($1).%I IS NOT DISTINCT FROM ($2).%I', TG_ARGV[i], TG_ARGV[i]);
        END LOOP;
        v_sql := v_sql || ' FROM (SELECT 1) AS one';
        EXECUTE v_sql INTO v_same_key USING NEW, OLD;
        IF v_same_key THEN RETURN NEW; END IF;
    END IF;

    v_sql := format('SELECT EXISTS (SELECT 1 FROM %I.%I t WHERE t.%I <> ($1).%I',
        TG_TABLE_SCHEMA, TG_TABLE_NAME, v_pk, v_pk);
    FOR i IN 2..(TG_NARGS - 1) LOOP
        v_column := TG_ARGV[i];
        v_sql := v_sql || format(
            ' AND t.%I IS NOT NULL AND ($1).%I IS NOT NULL AND t.%I = ($1).%I',
            v_column, v_column, v_column, v_column);
    END LOOP;
    v_sql := v_sql || ')';
    EXECUTE v_sql INTO v_duplicate USING NEW;
    IF v_duplicate THEN RAISE EXCEPTION '%', v_message; END IF;
    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS "tr_pg_区域_唯一" ON "区域";
CREATE TRIGGER "tr_pg_区域_唯一" BEFORE INSERT OR UPDATE ON "区域" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '区域名称重复', '上级编号', '名称');
DROP TRIGGER IF EXISTS "tr_pg_技术领域_唯一" ON "技术领域";
CREATE TRIGGER "tr_pg_技术领域_唯一" BEFORE INSERT OR UPDATE ON "技术领域" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '技术领域名称重复', '上级编号', '名称');
DROP TRIGGER IF EXISTS "tr_pg_步骤指定人_唯一" ON "步骤指定人";
CREATE TRIGGER "tr_pg_步骤指定人_唯一" BEFORE INSERT OR UPDATE ON "步骤指定人" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '步骤指定人重复', '步骤编号', '指定人编号');
DROP TRIGGER IF EXISTS "tr_pg_通知公告_唯一" ON "通知公告";
CREATE TRIGGER "tr_pg_通知公告_唯一" BEFORE INSERT OR UPDATE ON "通知公告" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '通知公告名称重复', '通知名称');
DROP TRIGGER IF EXISTS "tr_pg_主办讲座_唯一" ON "主办讲座";
CREATE TRIGGER "tr_pg_主办讲座_唯一" BEFORE INSERT OR UPDATE ON "主办讲座" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '活动名称重复', '活动名称');
DROP TRIGGER IF EXISTS "tr_pg_国民行业代码_唯一" ON "国民行业代码";
CREATE TRIGGER "tr_pg_国民行业代码_唯一" BEFORE INSERT OR UPDATE ON "国民行业代码" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '代码名称重复', '上级编号', '名称');
DROP TRIGGER IF EXISTS "tr_pg_项目参与人_唯一" ON "项目参与人";
CREATE TRIGGER "tr_pg_项目参与人_唯一" BEFORE INSERT OR UPDATE ON "项目参与人" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '参与人重复', '流程模板编号', '项目编号', '参与人编号');
DROP TRIGGER IF EXISTS "tr_pg_纵向项目申报_唯一" ON "纵向项目申报";
CREATE TRIGGER "tr_pg_纵向项目申报_唯一" BEFORE INSERT OR UPDATE ON "纵向项目申报" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '纵向项目申报名称重复', '项目名称');
DROP TRIGGER IF EXISTS "tr_pg_下载中心_唯一" ON "下载中心";
CREATE TRIGGER "tr_pg_下载中心_唯一" BEFORE INSERT OR UPDATE ON "下载中心" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '文件名称重复', '文件名称');
DROP TRIGGER IF EXISTS "tr_pg_讲座反馈_唯一" ON "讲座反馈";
CREATE TRIGGER "tr_pg_讲座反馈_唯一" BEFORE INSERT OR UPDATE ON "讲座反馈" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '该讲座反馈已存在', '讲座编号');
DROP TRIGGER IF EXISTS "tr_pg_经费模版_唯一" ON "经费模版";
CREATE TRIGGER "tr_pg_经费模版_唯一" BEFORE INSERT OR UPDATE ON "经费模版" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '经费模版名称重复', '名称');
DROP TRIGGER IF EXISTS "tr_pg_经费模版项目支出类型_唯一" ON "经费模版项目支出类型";
CREATE TRIGGER "tr_pg_经费模版项目支出类型_唯一" BEFORE INSERT OR UPDATE ON "经费模版项目支出类型" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '项目支出类型重复', '所属经费模版编号', '项目支出类型');
DROP TRIGGER IF EXISTS "tr_pg_参加会议_唯一" ON "参加会议";
CREATE TRIGGER "tr_pg_参加会议_唯一" BEFORE INSERT OR UPDATE ON "参加会议" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '会议名称重复', '会议名称');
DROP TRIGGER IF EXISTS "tr_pg_经费模版项目支出内容_唯一" ON "经费模版项目支出内容";
CREATE TRIGGER "tr_pg_经费模版项目支出内容_唯一" BEFORE INSERT OR UPDATE ON "经费模版项目支出内容" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '项目支出内容重复', '所属经费模版项目支出类型编号', '项目支出内容');
DROP TRIGGER IF EXISTS "tr_pg_专家评分_唯一" ON "专家评分";
CREATE TRIGGER "tr_pg_专家评分_唯一" BEFORE INSERT OR UPDATE ON "专家评分" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '评分项重复', '流程模板编号', '项目编号', '评分项');
DROP TRIGGER IF EXISTS "tr_pg_参加会议反馈_唯一" ON "参加会议反馈";
CREATE TRIGGER "tr_pg_参加会议反馈_唯一" BEFORE INSERT OR UPDATE ON "参加会议反馈" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '该参加会议反馈已存在', '参加会议编号');
DROP TRIGGER IF EXISTS "tr_pg_部门_唯一" ON "部门";
CREATE TRIGGER "tr_pg_部门_唯一" BEFORE INSERT OR UPDATE ON "部门" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '部门名称重复', '名称');
DROP TRIGGER IF EXISTS "tr_pg_成果项目_唯一" ON "成果项目";
CREATE TRIGGER "tr_pg_成果项目_唯一" BEFORE INSERT OR UPDATE ON "成果项目" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '流程已存在重复', '项目编号');
DROP TRIGGER IF EXISTS "tr_pg_角色_唯一" ON "角色";
CREATE TRIGGER "tr_pg_角色_唯一" BEFORE INSERT OR UPDATE ON "角色" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '角色名称重复', '名称');
DROP TRIGGER IF EXISTS "tr_pg_成果作者_唯一" ON "成果作者";
CREATE TRIGGER "tr_pg_成果作者_唯一" BEFORE INSERT OR UPDATE ON "成果作者" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '同一成果下作者重复', '成果类型', '成果编号', '作者编号');
DROP TRIGGER IF EXISTS "tr_pg_期刊名称_唯一" ON "期刊";
CREATE TRIGGER "tr_pg_期刊名称_唯一" BEFORE INSERT OR UPDATE ON "期刊" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '中文刊名重复', '名称');
DROP TRIGGER IF EXISTS "tr_pg_期刊英文_唯一" ON "期刊";
CREATE TRIGGER "tr_pg_期刊英文_唯一" BEFORE INSERT OR UPDATE ON "期刊" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '英文刊名重复', '英文刊名');
DROP TRIGGER IF EXISTS "tr_pg_字典_唯一" ON "字典";
CREATE TRIGGER "tr_pg_字典_唯一" BEFORE INSERT OR UPDATE ON "字典" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '字典数据重复', '上级编号', '分类', '值');
DROP TRIGGER IF EXISTS "tr_pg_纵向项目结题_唯一" ON "纵向项目结题";
CREATE TRIGGER "tr_pg_纵向项目结题_唯一" BEFORE INSERT OR UPDATE ON "纵向项目结题" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '该纵向项目已开始结题', '纵向项目编号');
DROP TRIGGER IF EXISTS "tr_pg_成果著作_唯一" ON "成果著作";
CREATE TRIGGER "tr_pg_成果著作_唯一" BEFORE INSERT OR UPDATE ON "成果著作" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '著作名称重复', '著作名称');
DROP TRIGGER IF EXISTS "tr_pg_人员_唯一" ON "人员";
CREATE TRIGGER "tr_pg_人员_唯一" BEFORE INSERT OR UPDATE ON "人员" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '工号重复', '工号');
DROP TRIGGER IF EXISTS "tr_pg_成果专利_唯一" ON "成果专利";
CREATE TRIGGER "tr_pg_成果专利_唯一" BEFORE INSERT OR UPDATE ON "成果专利" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '专利名称重复', '专利名称');
DROP TRIGGER IF EXISTS "tr_pg_横向项目_唯一" ON "横向项目";
CREATE TRIGGER "tr_pg_横向项目_唯一" BEFORE INSERT OR UPDATE ON "横向项目" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '合同编号重复', '合同编号');
DROP TRIGGER IF EXISTS "tr_pg_成果获奖_唯一" ON "成果获奖";
CREATE TRIGGER "tr_pg_成果获奖_唯一" BEFORE INSERT OR UPDATE ON "成果获奖" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '获奖名称重复', '获奖名称');
DROP TRIGGER IF EXISTS "tr_pg_成果论文_唯一" ON "成果论文";
CREATE TRIGGER "tr_pg_成果论文_唯一" BEFORE INSERT OR UPDATE ON "成果论文" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '论文标题重复', '论文标题');
DROP TRIGGER IF EXISTS "tr_pg_流程_唯一" ON "流程";
CREATE TRIGGER "tr_pg_流程_唯一" BEFORE INSERT OR UPDATE ON "流程" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '流程已存在', '项目编号', '流程模板编号');
DROP TRIGGER IF EXISTS "tr_pg_横向项目结题_唯一" ON "横向项目结题";
CREATE TRIGGER "tr_pg_横向项目结题_唯一" BEFORE INSERT OR UPDATE ON "横向项目结题" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '该项目已有结题', '横向项目编号');
DROP TRIGGER IF EXISTS "tr_pg_合同买方性质_唯一" ON "合同买方性质";
CREATE TRIGGER "tr_pg_合同买方性质_唯一" BEFORE INSERT OR UPDATE ON "合同买方性质" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '合同买方性质名称重复', '上级编号', '名称');
DROP TRIGGER IF EXISTS "tr_pg_学科_唯一" ON "学科";
CREATE TRIGGER "tr_pg_学科_唯一" BEFORE INSERT OR UPDATE ON "学科" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '学科名称重复', '上级编号', '名称');
DROP TRIGGER IF EXISTS "tr_pg_项目分类_唯一" ON "项目分类";
CREATE TRIGGER "tr_pg_项目分类_唯一" BEFORE INSERT OR UPDATE ON "项目分类" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '项目分类名称重复', '上级编号', '名称');
DROP TRIGGER IF EXISTS "tr_pg_通知公告接收条件_唯一" ON "通知公告接收条件";
CREATE TRIGGER "tr_pg_通知公告接收条件_唯一" BEFORE INSERT OR UPDATE ON "通知公告接收条件" FOR EACH ROW
EXECUTE FUNCTION "trg_唯一性校验"('编号', '通知接收者重复', '通知公告编号', '接收者类型', '接收者编号');

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'ck_期刊_至少一个名称'
    ) THEN
        ALTER TABLE "期刊" ADD CONSTRAINT "ck_期刊_至少一个名称"
            CHECK ("名称" IS NOT NULL OR "英文刊名" IS NOT NULL);
    END IF;
END;
$$;

COMMIT;
