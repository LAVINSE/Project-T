using System;
using UnityEngine;

using SW.SkillTree;

namespace ProjectT.Research
{
    /// <summary>
    /// 공통 연구의 노드별 지불 기록을 저장합니다. 트리 일치와 선행 조건은 불러온 뒤 SWSkillTree가 다시 검사합니다.
    /// </summary>
    [Serializable]
    public sealed class ResearchSaveData : IProjectSaveData
    {
        #region 필드
        [SerializeField] private int version;
        [SerializeField] private SWSkillTreeSaveData tree;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 연구 트리 진행입니다. 아직 연구하지 않았으면 null입니다.
        /// </summary>
        public SWSkillTreeSaveData Tree => tree;

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 현재 형식으로 진행 사본을 보관합니다.
        /// </summary>
        public ResearchSaveData(SWSkillTreeSaveData tree)
        {
            version = ProjectDefine.Save.ResearchVersion;
            this.tree = tree;
        }

        #endregion // 초기화

        #region 검사
        /// <summary>
        /// 저장 형식 버전을 확인합니다. 지원하지 않는 형식이면 기존 파일을 보존하도록 false입니다.
        /// </summary>
        public bool Validate(out string reason)
        {
            reason = version == ProjectDefine.Save.ResearchVersion ? string.Empty
                : "연구 저장 형식을 읽을 수 없습니다. 기존 파일을 보존합니다.";
            return string.IsNullOrEmpty(reason);
        }

        #endregion // 검사
    }
}
