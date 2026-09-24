using SW.Data;
using SW.Util;

namespace ProjectT
{
    /// <summary>
    /// 영구 저장 데이터가 스스로 형식과 내용을 검사하는 규칙입니다.
    /// </summary>
    public interface IProjectSaveData
    {
        /// <summary>
        /// 저장 형식과 내용을 검사합니다. 손상된 저장이면 사유와 함께 false입니다.
        /// </summary>
        bool Validate(out string reason);
    }

    /// <summary>
    /// SWUtils 로컬 저장 슬롯 하나를 원자적으로 읽고 기록하는 공용 저장소입니다. 클라우드와 백업 파일은 만들지 않습니다.
    /// </summary>
    public abstract class ProjectSaveStore<TData> where TData : class, IProjectSaveData
    {
        #region 필드
        private TData committed;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 이 저장소가 사용하는 로컬 저장 슬롯입니다.
        /// </summary>
        protected abstract string Slot { get; }

        /// <summary>
        /// 저장을 읽지 못했을 때 호출자에게 전달할 사유입니다.
        /// </summary>
        protected abstract string LoadFailureReason { get; }

        #endregion // 프로퍼티

        #region 저장과 불러오기
        /// <summary>
        /// 저장이 없을 때만 빈 상태로 시작합니다. 기존 파일 읽기·검사 실패는 false이며 덮어쓰지 않습니다.
        /// </summary>
        public bool TryLoad(out TData data, out string reason)
        {
            data = null;
            reason = string.Empty;
            if (!SWSaveDataManager.HasSave(Slot))
            {
                committed = CreateEmpty();
                data = committed;
                return true;
            }

            SWSaveDataManager.SetData(CreateEmpty());
            bool loaded = false;
            SWSaveDataManager.LoadAll(
                success => loaded = success,
                slot: Slot,
                cloudFirst: false,
                createBackup: false);
            TData candidate = SWSaveDataManager.GetData<TData>();
            if (!loaded || candidate == null)
            {
                SWSaveDataManager.ClearData();
                reason = LoadFailureReason;
                return false;
            }

            if (!candidate.Validate(out reason))
            {
                SWSaveDataManager.ClearData();
                return false;
            }

            committed = candidate;
            data = committed;
            return true;
        }

        /// <summary>
        /// 검사를 통과한 후보만 한 파일에 저장합니다. 실패하면 직전 성공 상태를 유지하고 false입니다.
        /// </summary>
        public bool TrySave(TData candidate)
        {
            if (committed == null || candidate == null || !candidate.Validate(out _))
            {
                SWLog.LogWarning("[" + GetType().Name + "] 저장 실패: 초기화된 저장소와 올바른 후보가 필요합니다.");
                return false;
            }

            SWSaveDataManager.SetData(candidate);
            if (!SWSaveDataManager.SaveAll(
                slot: Slot,
                createBackup: false,
                backupToCloud: false))
            {
                SWSaveDataManager.SetData(committed);
                return false;
            }

            committed = candidate;
            return true;
        }

        #endregion // 저장과 불러오기

        #region 확장
        /// <summary>
        /// 저장이 없을 때 사용할 빈 상태를 만듭니다.
        /// </summary>
        protected abstract TData CreateEmpty();

        #endregion // 확장
    }
}
