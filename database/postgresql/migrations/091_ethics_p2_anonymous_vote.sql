-- 迁移 091：伦理 P2 匿名表决和可审计的投票汇总入口。
-- 委员意见仍保留受控审计身份；对外/委员查看只返回汇总票数，不返回委员身份。
BEGIN;

ALTER TABLE "伦理审查会议"
    ADD COLUMN IF NOT EXISTS "是否匿名表决" boolean NOT NULL DEFAULT true;

COMMENT ON COLUMN "伦理审查会议"."是否匿名表决" IS '伦理表决向委员和申请人展示时只返回汇总票数；审计身份仍保留在受控表中';

UPDATE "系统迁移版本"
   SET "版本" = 91,
       "脚本" = '091_ethics_p2_anonymous_vote.sql',
       "检查摘要" = '伦理匿名表决汇总、严重不良事件与逾期提醒 P2 支持',
       "应用时间" = CURRENT_TIMESTAMP
 WHERE "编号" = 1;

COMMIT;
