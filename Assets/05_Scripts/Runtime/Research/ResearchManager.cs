using System.Collections.Generic;
using System.Text;
using UnityEngine;

using SW.Attributes;
using SW.SkillTree;
using SW.Util;

using ProjectT.Progression;

namespace ProjectT.Research
{
    /// <summary>
    /// Bootstrap에서 공통 연구 트리를 준비하고 장면 전환에도 유지합니다. 소울로 구매하며 진행이 바뀔 때만 연구 저장을 기록합니다.
    /// </summary>
    public sealed class ResearchManager : SWSingleton<ResearchManager>
    {
        #region 필드
        [SWGroup("공통 연구")]
        [SerializeField] private SWSkillTreeDefinition tree;
        private readonly ResearchSaveStore store = new ResearchSaveStore();
        private ResearchSoulWallet wallet;
        private SWSkillTreeEffectBinding binding;
        private string savedProgress = string.Empty;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 연구 구매·환불·초기화를 처리하는 트리입니다. 준비에 실패하면 null입니다.
        /// </summary>
        public SWSkillTreeSystem System { get; private set; }

        /// <summary>
        /// 습득한 연구의 능력치 증가량입니다. 준비 전이나 실패 시에도 빈 모음이라 전투는 그대로 진행합니다.
        /// </summary>
        public ResearchBonuses Bonuses { get; } = new ResearchBonuses();

        /// <summary>
        /// 준비에 실패한 이유입니다. 성공하면 빈 문자열입니다.
        /// </summary>
        public string InitializationError { get; private set; } = string.Empty;

        /// <summary>
        /// 마지막 진행 저장에 실패한 이유입니다. 다음 진행 변경 때 다시 저장하며 성공하면 빈 문자열입니다.
        /// </summary>
        public string SaveIssue { get; private set; } = string.Empty;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 소울 지갑이 준비된 뒤 트리를 검사하고 저장된 진행과 효과를 복원합니다. 실패하면 연구를 막고 원인을 남깁니다.
        /// </summary>
        private void Start()
        {
            if (Instance != this)
            {
                return;
            }

            SoulWallet souls = SoulManager.HasInstance ? SoulManager.Instance.Wallet : null;
            if (souls == null)
            {
                Fail("소울 저장을 불러오지 못해 연구를 사용할 수 없습니다.");
                return;
            }

            if (!TryValidateTree(out string reason))
            {
                Fail(reason);
                return;
            }

            if (!store.TryLoad(out ResearchSaveData data, out reason))
            {
                Fail(reason);
                return;
            }

            wallet = new ResearchSoulWallet(souls);
            System = new SWSkillTreeSystem(tree, wallet, Bonuses);
            if (data.Tree != null && !System.Restore(data.Tree, out reason))
            {
                Dispose();
                Fail("연구 저장이 현재 트리와 맞지 않습니다. 기존 파일을 보존합니다. " + reason);
                return;
            }

            savedProgress = DescribeProgress();
            binding = new SWSkillTreeEffectBinding(System);
            System.Changed += SaveProgress;
        }

        /// <summary>
        /// SWSkillTree 기본 검사와 이 프로젝트 규칙(모든 노드 1회, 비용은 소울)을 확인합니다.
        /// </summary>
        private bool TryValidateTree(out string reason)
        {
            var errors = new List<string>(SWSkillTreeDefinitionValidator.Validate(tree));
            if (errors.Count == 0)
            {
                foreach (SWSkillTreeNode node in tree.Nodes)
                {
                    if (node.Skill.MaximumLevel != 1)
                    {
                        errors.Add("연구 노드는 최대 레벨 1이어야 합니다: " + node.Skill.name);
                    }

                    foreach (SWSkillTreeCost cost in node.Skill.Costs)
                    {
                        if (cost.Evaluate(0).currency != ProjectDefine.Research.SoulCurrency)
                        {
                            errors.Add("연구 비용 재화는 " + ProjectDefine.Research.SoulCurrency + "여야 합니다: " + node.Skill.name);
                        }
                    }
                }
            }

            reason = string.Join("\n", errors);
            return errors.Count == 0;
        }

        /// <summary>
        /// 준비 실패를 기록합니다. 연구 효과 없이 게임은 계속 진행합니다.
        /// </summary>
        private void Fail(string reason)
        {
            InitializationError = reason;
            SWLog.LogWarning("[ResearchManager] 연구 준비 실패: " + reason);
        }

        #endregion // 초기화

        #region 저장
        /// <summary>
        /// 노드 레벨이 바뀐 경우에만 저장합니다. 소울 잔액만 바뀐 알림은 건너뜁니다. 실패하면 다음 변경 때 다시 시도합니다.
        /// </summary>
        private void SaveProgress()
        {
            string progress = DescribeProgress();
            if (progress == savedProgress && string.IsNullOrEmpty(SaveIssue))
            {
                return;
            }

            if (!store.TrySave(new ResearchSaveData(System.CaptureSaveData())))
            {
                SaveIssue = "연구 진행을 저장하지 못했습니다. 다음 연구 때 다시 저장합니다.";
                SWLog.LogError("[ResearchManager] " + SaveIssue);
                return;
            }

            savedProgress = progress;
            SaveIssue = string.Empty;
        }

        /// <summary>
        /// 저장 여부 판단용으로 노드별 레벨을 한 문자열로 요약합니다.
        /// </summary>
        private string DescribeProgress()
        {
            var text = new StringBuilder();
            foreach (SWSkillTreeNode node in tree.Nodes)
            {
                text.Append(System.GetLevel(node.Identifier)).Append(',');
            }

            return text.ToString();
        }

        #endregion // 저장

        #region 정리
        /// <summary>
        /// 효과 연결을 먼저 해제한 뒤 트리와 지갑 구독을 정리합니다.
        /// </summary>
        private void Dispose()
        {
            binding?.Dispose();
            binding = null;
            if (System != null)
            {
                System.Changed -= SaveProgress;
                System.Dispose();
                System = null;
            }

            wallet?.Dispose();
            wallet = null;
        }

        /// <summary>
        /// 종료 시 구독을 정리하고 SWUtils 싱글톤 참조를 해제합니다.
        /// </summary>
        public override void OnDestroy()
        {
            if (Instance == this)
            {
                Dispose();
            }

            base.OnDestroy();
        }

        #endregion // 정리
    }
}
