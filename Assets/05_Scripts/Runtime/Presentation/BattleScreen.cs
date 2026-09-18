using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;

using SW.Base;
using SW.Popup;
using SW.Util;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Units;

namespace ProjectT.Presentation
{
    /// <summary>
    /// 전투 상태를 한국어 화면에 표시하고 구매·시작·메뉴 버튼을 연결합니다.
    /// </summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, "ProjectT.Defense.Presentation", "ProjectT.Defense.Runtime", "BattleScreen")]
    public sealed class BattleScreen : SWMonoBehaviour
    {
        #region 필드
        [SerializeField] private BattleSession session;
        [SerializeField] private BattleMouseCommand commands;
        [SerializeField] private BattlePlacementCommand placement;
        [SerializeField] private TMP_Text stageLabel;
        [SerializeField] private TMP_Text currencyLabel;
        [SerializeField] private TMP_Text roundLabel;
        [SerializeField] private TMP_Text defenseLabel;
        [SerializeField] private TMP_Text commandLabel;
        [SerializeField] private TMP_Text selectedLabel;
        [SerializeField] private UnityEngine.UI.Button[] purchaseButtons;
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private TMP_Text startLabel;
        [SerializeField] private UnityEngine.UI.Button menuButton;
        [SerializeField] private SWPopupBase menuPopup;
        [SerializeField] private UnityEngine.UI.Button resumeButton;
        [SerializeField] private UnityEngine.UI.Button restartButton;
        [SerializeField] private SWPopupBase resultPopup;
        [SerializeField] private TMP_Text resultTitle;
        [SerializeField] private TMP_Text resultDescription;
        [SerializeField] private UnityEngine.UI.Button retryButton;
        [SerializeField] private Transform rosterParent;
        [SerializeField] private UnityEngine.UI.Button rosterButtonPrefab;
        private readonly List<UnityEngine.UI.Button> rosterButtons = new List<UnityEngine.UI.Button>();
        private bool resultShown;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 준비된 전투에 화면 이벤트와 조작 버튼을 연결합니다.
        /// </summary>
        private void Start()
        {
            if (session == null || session.Wallet == null || session.Workshop == null)
            {
                SWLog.LogWarning("[BattleScreen] 화면 초기화 중단: 전투 초기화 상태를 확인해 주세요.");
                enabled = false;
                return;
            }

            for (int index = 0; index < purchaseButtons.Length; index++)
            {
                AllyClassDefinition definition = session.Definition.Classes[index];
                purchaseButtons[index].GetComponent<ClassDeploymentButton>().Configure(placement, definition);
                purchaseButtons[index].onClick.AddListener(() => commands.PurchaseClass(definition));
            }

            startButton.onClick.AddListener(AdvanceBattle);
            menuButton.onClick.AddListener(menuPopup.Show);
            resumeButton.onClick.AddListener(menuPopup.Hide);
            restartButton.onClick.AddListener(Restart);
            retryButton.onClick.AddListener(Restart);
            stageLabel.text = "01  /  " + session.Definition.DisplayName;
        }

        /// <summary>
        /// 전투 진행 상태·재화·공방 체력·선택 정보를 화면에 반영합니다.
        /// </summary>
        private void Update()
        {
            if (session.Wallet == null)
            {
                return;
            }

            currencyLabel.text = "배치 재화  <b>" + session.Wallet.Balance.ToString("0") + "</b>";
            roundLabel.text = session.CurrentPhase == BattleSession.Phase.Preparation
                ? "전투 준비  ·  " + session.Definition.RoundCount + " 라운드"
                : session.CurrentPhase == BattleSession.Phase.RoundBreak
                    ? "라운드 " + session.RoundNumber + " 완료 · 휴식"
                    : session.CurrentPhase == BattleSession.Phase.FinalRest
                        ? "방어 성공 · 최종 휴식"
                        : "라운드  " + session.RoundNumber + " / " + session.Definition.RoundCount;
            defenseLabel.text = "공방 체력  " + Mathf.CeilToInt(session.Workshop.Health.Current) + " / " + session.Workshop.Health.Maximum.ToString("0");
            commandLabel.text = commands.Message;
            for (int index = 0; index < purchaseButtons.Length; index++)
            {
                purchaseButtons[index].interactable = session.CanCommand && session.Wallet.Balance >= session.Definition.Classes[index].DeploymentCost;
            }

            bool resting = session.CurrentPhase == BattleSession.Phase.RoundBreak
                || session.CurrentPhase == BattleSession.Phase.FinalRest;
            startButton.interactable = session.CanCommand && (session.CurrentPhase == BattleSession.Phase.Preparation || resting);
            startLabel.text = session.CurrentPhase == BattleSession.Phase.Preparation
                ? "전투 시작"
                : session.CurrentPhase == BattleSession.Phase.RoundBreak
                    ? "다음 라운드 시작"
                    : session.CurrentPhase == BattleSession.Phase.FinalRest
                        ? "결과 보기"
                        : "전투 진행 중";
            AllyUnit selected = commands.SelectedUnit;
            selectedLabel.text = selected == null
                ? "아군을 클릭해 선택하세요."
                : selected.Definition.DisplayName + (selected.Health.IsAlive
                ? "  ·  체력 " + Mathf.CeilToInt(selected.Health.Current) + " / " + selected.Health.Maximum
                : "  ·  부활까지 " + Mathf.CeilToInt(selected.RevivalRemaining) + "초") + "\n" + (selected.Health.IsAlive ? selected.Movement.IsMoving ? "이동 중" : "교전 대기" : "쓰러진 자리에서 무료 부활");
            UpdateRoster();
            if (!resultShown
                && (session.CurrentPhase == BattleSession.Phase.Victory || session.CurrentPhase == BattleSession.Phase.Defeat))
            {
                resultShown = true;
                bool victory = session.CurrentPhase == BattleSession.Phase.Victory;
                resultTitle.text = victory ? "마법 공방을 지켰습니다" : "마법 공방이 파손되었습니다";
                resultDescription.text = "처치한 적  " + session.KilledCount + "명\n남은 공방 체력  " + Mathf.CeilToInt(session.Workshop.Health.Current) + " / " + session.Workshop.Health.Maximum.ToString("0");
                resultPopup.Show();
            }
        }

        /// <summary>
        /// 현재 단계에 맞춰 라운드를 시작하거나 최종 결과를 확정합니다.
        /// </summary>
        private void AdvanceBattle()
        {
            if (session.CurrentPhase == BattleSession.Phase.FinalRest)
            {
                session.CompleteBattle();
            }
            else
            {
                session.StartBattle();
            }
        }

        /// <summary>
        /// 구매한 아군 목록과 생존·부활 상태를 표시합니다.
        /// </summary>
        private void UpdateRoster()
        {
            rosterParent.parent.gameObject.SetActive(session.Allies.Count > 0);
            while (rosterButtons.Count < session.Allies.Count)
            {
                AllyUnit ally = session.Allies[rosterButtons.Count];
                var button = Instantiate(rosterButtonPrefab, rosterParent);
                button.gameObject.SetActive(true);
                button.onClick.AddListener(() => commands.SelectUnit(ally));
                rosterButtons.Add(button);
            }

            for (int index = 0; index < rosterButtons.Count; index++)
            {
                AllyUnit ally = session.Allies[index];
                rosterButtons[index].GetComponentInChildren<TMP_Text>().text = (index + 1) + "  " + ally.Definition.DisplayName + (ally.Health.IsAlive ? "" : " · " + Mathf.CeilToInt(ally.RevivalRemaining) + "초");
                rosterButtons[index].interactable = session.CanCommand;
            }
        }

        /// <summary>
        /// 현재 스테이지를 다시 불러와 새 전투를 시작합니다.
        /// </summary>
        private void Restart()
        {
            menuPopup.Hide();
            resultPopup.Hide();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        #endregion // 함수
    }
}
