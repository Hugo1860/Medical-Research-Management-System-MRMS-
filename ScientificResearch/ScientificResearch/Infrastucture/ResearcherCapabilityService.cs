using System;
using System.Collections.Generic;
using System.Linq;
using ScientificResearch.Models;

namespace ScientificResearch.Infrastucture
{
    public class CapabilityScores
    {
        public int 人员编号 { get; set; }
        public decimal 课题主持得分 { get; set; }
        public decimal 经费规模得分 { get; set; }
        public decimal 论文能力得分 { get; set; }
        public decimal 成果转化得分 { get; set; }
        public decimal 团队指导得分 { get; set; }
        public decimal 综合科研指数 { get; set; }
        public string 评级称号 { get; set; }
    }

    public class YearTrendItem
    {
        public string 年度 { get; set; }
        public int 纵向课题数 { get; set; }
        public decimal 纵向经费 { get; set; }
        public int 横向合同数 { get; set; }
        public decimal 横向经费 { get; set; }
        public int 论文数 { get; set; }
        public int 专利数 { get; set; }
        public int 著作数 { get; set; }
        public int 获奖数 { get; set; }
    }

    public static class ResearcherCapabilityEvaluator
    {
        public static CapabilityScores Evaluate(
            int 人员编号,
            IEnumerable<v4_纵向项目总览> verticalList,
            IEnumerable<v4_横向项目总览> horizontalList,
            IEnumerable<v4_成果总览> fruitList,
            int mentorCount,
            int teamCount)
        {
            var verticals = verticalList?.ToList() ?? new List<v4_纵向项目总览>();
            var horizontals = horizontalList?.ToList() ?? new List<v4_横向项目总览>();
            var fruits = fruitList?.ToList() ?? new List<v4_成果总览>();

            // 1. 课题主持能力 (0-100)
            decimal projectScore = 15;
            foreach (var v in verticals)
            {
                var level = v.项目级别 ?? "";
                if (level.Contains("国家")) projectScore += 30;
                else if (level.Contains("省") || level.Contains("部")) projectScore += 18;
                else if (level.Contains("市") || level.Contains("厅")) projectScore += 12;
                else projectScore += 8;
            }
            projectScore += horizontals.Count * 10;
            projectScore = Math.Min(100, Math.Max(15, projectScore));

            // 2. 经费规模体量 (0-100)
            decimal totalFunding = verticals.Sum(x => x.批准经费 ?? 0) + horizontals.Sum(x => x.合同金额);
            decimal totalFundingWan = totalFunding / 10000m;
            decimal fundScore = 15;
            if (totalFundingWan >= 200) fundScore = 100;
            else if (totalFundingWan >= 100) fundScore = 90;
            else if (totalFundingWan >= 50) fundScore = 80;
            else if (totalFundingWan >= 20) fundScore = 70;
            else if (totalFundingWan >= 10) fundScore = 55;
            else if (totalFundingWan >= 5) fundScore = 40;
            else if (totalFundingWan > 0) fundScore = 25;

            // 3. 论文发表能力 (0-100)
            int paperCount = fruits.Count(x => (x.成果类型 ?? "").Contains("论文"));
            int bookCount = fruits.Count(x => (x.成果类型 ?? "").Contains("著作"));
            decimal paperScore = 15;
            if (paperCount >= 10) paperScore = 100;
            else if (paperCount >= 6) paperScore = 85;
            else if (paperCount >= 3) paperScore = 70;
            else if (paperCount >= 1) paperScore = 50 + paperCount * 5;
            paperScore += bookCount * 10;
            paperScore = Math.Min(100, Math.Max(15, paperScore));

            // 4. 成果转化与创新 (0-100)
            int patentCount = fruits.Count(x => (x.成果类型 ?? "").Contains("专利"));
            int awardCount = fruits.Count(x => (x.成果类型 ?? "").Contains("获奖"));
            decimal transferScore = 15 + (patentCount * 25) + (awardCount * 30) + (horizontals.Count * 15);
            transferScore = Math.Min(100, Math.Max(15, transferScore));

            // 5. 团队引领与指导 (0-100)
            decimal teamScore = 20 + (mentorCount * 20) + (teamCount * 20);
            teamScore = Math.Min(100, Math.Max(20, teamScore));

            // 综合科研指数
            decimal overallIndex = Math.Round(projectScore * 0.25m + fundScore * 0.25m + paperScore * 0.20m + transferScore * 0.15m + teamScore * 0.15m, 1);

            string rankTitle;
            if (overallIndex >= 90) rankTitle = "卓越学术领军人才 (S级)";
            else if (overallIndex >= 75) rankTitle = "杰出科研骨干 (A级)";
            else if (overallIndex >= 60) rankTitle = "优秀中坚学者 (B级)";
            else if (overallIndex >= 40) rankTitle = "青年科研新星 (C级)";
            else rankTitle = "学术起步人才 (D级)";

            return new CapabilityScores
            {
                人员编号 = 人员编号,
                课题主持得分 = projectScore,
                经费规模得分 = fundScore,
                论文能力得分 = paperScore,
                成果转化得分 = transferScore,
                团队指导得分 = teamScore,
                综合科研指数 = overallIndex,
                评级称号 = rankTitle
            };
        }
    }
}
