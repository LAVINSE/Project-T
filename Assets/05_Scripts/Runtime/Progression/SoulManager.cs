using SW.Util;

namespace ProjectT.Progression
{
    /// <summary>
    /// Main에서 소울 저장을 불러오고 장면 전환에도 같은 지갑을 제공합니다. 규칙과 파일 처리는 지갑·저장소에 위임합니다.
    /// </summary>
    public sealed class SoulManager : SWSingleton<SoulManager>
    {
        #region 프로퍼티
        /// <summary>
        /// 영구 소울 지갑입니다. 저장을 불러오지 못한 경우 null이며 새 잔액으로 덮어쓰지 않습니다.
        /// </summary>
        public SoulWallet Wallet { get; private set; }

        /// <summary>
        /// 초기화에 실패한 이유입니다. 성공하면 빈 문자열입니다.
        /// </summary>
        public string InitializationError { get; private set; } = string.Empty;

        #endregion // 프로퍼티

        #region 초기화
        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            Wallet = SoulWallet.Create(new SoulSaveStore(), out string reason);
            InitializationError = reason;
        }

        #endregion // 초기화
    }
}
