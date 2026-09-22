using System.Collections.Generic;
using UnityEngine;

using SW.Base;

namespace ProjectT.Data
{
    /// <summary>
    /// 데이터 검사에서 발견한 문제의 위치와 수정 방법입니다.
    /// </summary>
    public readonly struct DataIssue
    {
        #region 프로퍼티
        /// <summary>
        /// 문제가 있는 데이터입니다.
        /// </summary>
        public ProjectData Asset { get; }

        /// <summary>
        /// 문제가 있는 항목의 직렬화 경로입니다.
        /// </summary>
        public string PropertyPath { get; }

        /// <summary>
        /// 수정 방법을 담은 한글 설명입니다.
        /// </summary>
        public string Message { get; }

        #endregion // 프로퍼티

        #region 초기화
        /// <summary>
        /// 검사 결과의 위치와 설명을 보관합니다.
        /// </summary>
        public DataIssue(ProjectData asset, string propertyPath, string message)
        {
            Asset = asset;
            PropertyPath = propertyPath;
            Message = message;
        }

        #endregion // 초기화
    }

    /// <summary>
    /// 프로젝트 ScriptableObject 데이터의 공통 부모입니다. 실행 검사와 편집기 검사가 같은 규칙을 사용합니다.
    /// </summary>
    public abstract class ProjectData : SWScriptableObject
    {
        #region 프로퍼티
        /// <summary>
        /// 모든 검사 규칙을 통과했는지 반환합니다. 문제 목록을 만들지 않습니다.
        /// </summary>
        public bool IsValid => Validate(null);

        #endregion // 프로퍼티

        #region 검사
        /// <summary>
        /// 데이터를 검사합니다. 목록이 있으면 발견한 문제를 모두 추가하며 원본 값은 바꾸지 않습니다.
        /// </summary>
        public abstract bool Validate(List<DataIssue> issues);

        /// <summary>
        /// 조건이 거짓이면 문제를 기록하고 조건 결과를 그대로 반환합니다.
        /// </summary>
        protected bool Check(bool condition, string propertyPath, string message, List<DataIssue> issues)
        {
            if (!condition)
            {
                issues?.Add(new DataIssue(this, propertyPath, message));
            }

            return condition;
        }

        /// <summary>
        /// 0보다 큰 유한한 값인지 검사합니다.
        /// </summary>
        protected bool CheckPositive(float value, string propertyPath, List<DataIssue> issues)
        {
            return Check(value.ExIsPositive(), propertyPath, "0보다 큰 유한한 수를 입력하세요.", issues);
        }

        /// <summary>
        /// 0 이상의 유한한 값인지 검사합니다.
        /// </summary>
        protected bool CheckNonNegative(double value, string propertyPath, List<DataIssue> issues)
        {
            return Check(value.ExIsNonNegative(), propertyPath, "0 이상의 유한한 수를 입력하세요.", issues);
        }

        /// <summary>
        /// 필수 참조가 연결되었는지 검사합니다.
        /// </summary>
        protected bool CheckRequired(Object reference, string propertyPath, List<DataIssue> issues)
        {
            return Check(reference != null, propertyPath, "필수 참조를 연결하세요.", issues);
        }

        /// <summary>
        /// 표시 이름이 비어 있지 않은지 검사합니다.
        /// </summary>
        protected bool CheckName(string value, string propertyPath, List<DataIssue> issues)
        {
            return Check(!string.IsNullOrWhiteSpace(value), propertyPath, "표시 이름을 입력하세요.", issues);
        }

        #endregion // 검사
    }
}
