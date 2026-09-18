using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

using NUnit.Framework;

using SW.Pooling;
using SW.Popup;

using ProjectT.Battle;
using ProjectT.Data;
using ProjectT.Presentation;
using ProjectT.Units;

namespace ProjectT.Tests
{
    /// <summary>
    /// 실제 스테이지 자산으로 구매·이동·교전·부활·팝업·라운드 종료와 재시작을 검증합니다.
    /// </summary>
    public sealed class StageOneBattleTests
    {
        #region 필드
        private BattleSession session;
        private Mouse testMouse;
        private InputSettings originalInputSettings;
        private InputSettings testInputSettings;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 각 검증을 독립된 새 전투로 시작합니다.
        /// </summary>
        [UnitySetUp]
        public IEnumerator OpenBattle()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Stage01_Grassland");
            yield return null;
            session = Object.FindFirstObjectByType<BattleSession>();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
        }

        /// <summary>
        /// 입력 장치와 시간 배율을 검증 전 상태로 정리합니다.
        /// </summary>
        [UnityTearDown]
        public IEnumerator CloseBattle()
        {
            if (testMouse != null)
            {
                InputSystem.RemoveDevice(testMouse);
            }

            testMouse = null;
            if (testInputSettings != null)
            {
                InputSystem.settings = originalInputSettings;
                Object.Destroy(testInputSettings);
                testInputSettings = null;
            }

            var empty = SceneManager.CreateScene("BattleTestCleanup");
            SceneManager.SetActiveScene(empty);
            if (session != null)
            {
                yield return SceneManager.UnloadSceneAsync(session.gameObject.scene);
            }

            Time.timeScale = 1f;
        }

        /// <summary>
        /// 같은 클래스를 여러 명 배치하며 실패한 배치에서는 비용이 차감되지 않습니다.
        /// </summary>
        [UnityTest]
        public IEnumerator DeploymentIsIndependentAndInvalidDestinationDoesNotSpend()
        {
            AllyClassDefinition warrior = session.Definition.Classes[0];
            Assert.That(session.TryDeploy(warrior, new Vector2(-21, -2), out _, out _), Is.False, "연못 위에는 배치할 수 없어야 합니다.");
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            Assert.That(session.TryDeploy(warrior, new Vector2(-9.5f, 3.5f), out AllyUnit first, out _), Is.True);
            Assert.That(session.TryDeploy(warrior, new Vector2(-2.5f, -2.5f), out AllyUnit second, out _), Is.True);
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Health, Is.Not.SameAs(second.Health));
            Assert.That(session.Wallet.Balance, Is.EqualTo(40));
            Assert.That(first.Movement.IsMoving || second.Movement.IsMoving, Is.False);
            Assert.That((Vector2)first.transform.position, Is.EqualTo(new Vector2(-9.5f, 3.5f)));
            Assert.That((Vector2)second.transform.position, Is.EqualTo(new Vector2(-2.5f, -2.5f)));
            Assert.That(first.CanFight, Is.True);
            Assert.That(second.CanFight, Is.True);
            Assert.That(session.TryDeploy(warrior, new Vector2(5.5f, 0.5f), out _, out _), Is.True);
            Assert.That(session.TryDeploy(warrior, new Vector2(5.5f, 1.5f), out _, out _), Is.False);
            Assert.That(session.Wallet.Balance, Is.EqualTo(10));
            yield return null;
        }

        /// <summary>
        /// 명령 즉시 저지가 해제되고 이동 중에는 공격하지 않으며 사망 지점 부활과 예약 목적지가 유지됩니다.
        /// </summary>
        [UnityTest]
        public IEnumerator MoveReleasesBlockingAndRevivalUsesDeathPositionAndLatestOrder()
        {
            Assert.That(session.Definition.EnemyRoute.TryCreateRoute(out var route, out _), Is.True);
            Assert.That(session.TryDeploy(session.Definition.Classes[0], route.GetPoint(2), out AllyUnit ally, out _), Is.True);
            ally.Movement.Advance(100f);
            session.StartBattle();
            yield return null;
            Assert.That(session.Enemies.Count, Is.GreaterThan(0));
            EnemyUnit enemy = session.Enemies[0];
            float interceptionDistance = Vector2.Distance(route.GetPoint(0), route.GetPoint(1)) + Vector2.Distance(route.GetPoint(1), route.GetPoint(2));
            float travelledDistance = route.Length - enemy.Movement.RemainingDistance;
            enemy.Movement.Advance(Mathf.Max(0, interceptionDistance - travelledDistance) / enemy.Movement.MoveSpeed);
            yield return null;
            Assert.That(enemy.Blocker, Is.EqualTo(ally));
            Assert.That(session.TryMove(ally, new Vector2(-9.5f, -2.5f)), Is.True);
            Assert.That(enemy.Blocker, Is.Null);
            Assert.That(ally.CanFight, Is.False);
            float enemyHealth = enemy.Health.Current;
            yield return new WaitForSeconds(0.25f);
            Assert.That(enemy.Health.Current, Is.EqualTo(enemyHealth));
            Vector2 deathPosition = ally.transform.position;
            ally.Health.TakeDamage(10000f);
            Assert.That(ally.Health.IsAlive, Is.False);
            Vector2 requested = new Vector2(-4.5f, -2.5f);
            Assert.That(session.TryMove(ally, requested), Is.True);
            Assert.That(ally.RequestedDestination, Is.EqualTo(requested));
            double balance = session.Wallet.Balance;
            ally.AdvanceRevival(ally.Definition.RevivalSeconds);
            Assert.That(ally.Health.IsAlive, Is.True);
            Assert.That((Vector2)ally.transform.position, Is.EqualTo(deathPosition));
            Assert.That(ally.Movement.IsMoving, Is.True);
            Assert.That(session.Wallet.Balance, Is.EqualTo(balance));
            ally.Movement.Advance(100f);
            Assert.That((Vector2)ally.transform.position, Is.EqualTo(requested));
        }

        /// <summary>
        /// 팝업을 열면 이동과 부활이 멈추고 닫으면 이어집니다.
        /// </summary>
        [UnityTest]
        public IEnumerator PopupPausesMovementRevivalAndCommands()
        {
            session.TryDeploy(session.Definition.Classes[0], new Vector2(-2.5f, 3.5f), out AllyUnit moving, out _);
            session.TryDeploy(session.Definition.Classes[1], new Vector2(5.5f, 2.5f), out AllyUnit dead, out _);
            session.TryMove(moving, new Vector2(6, -1));
            dead.Health.TakeDamage(10000);
            var popups = Object.FindObjectsByType<SWPopupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SWPopupBase menu = System.Array.Find(popups, value => value.name == "PauseMenuPopup");
            menu.Show();
            Vector3 before = moving.transform.position;
            float remaining = dead.RevivalRemaining;
            Assert.That(session.TryMove(moving, Vector2.zero), Is.False);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(moving.transform.position, Is.EqualTo(before));
            Assert.That(dead.RevivalRemaining, Is.EqualTo(remaining));
            menu.Hide();
            yield return new WaitForSeconds(0.15f);
            Assert.That(moving.transform.position, Is.Not.EqualTo(before));
            Assert.That(dead.RevivalRemaining, Is.LessThan(remaining));
        }

        /// <summary>
        /// 가상 마우스로 클릭 위치 배치, 왼쪽 선택과 오른쪽 이동을 확인합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickPlacementWaitsForPositionThenAllowsSelectionAndRightMovement()
        {
            CreateTestMouse();
            var button = GameObject.Find("PurchaseWarriorButton").GetComponent<RectTransform>();
            Canvas.ForceUpdateCanvases();
            Vector2 buttonPosition = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            yield return Click(buttonPosition);
            Assert.That(session.Allies.Count, Is.Zero);
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            Vector2 placementPosition = new Vector2(0, 1);
            yield return Click(Camera.main.WorldToScreenPoint(placementPosition));
            var commands = Object.FindFirstObjectByType<BattleMouseCommand>();
            Assert.That(session.Allies.Count, Is.EqualTo(1));
            Assert.That(session.Wallet.Balance, Is.EqualTo(70));
            AllyUnit purchased = session.Allies[0];
            Assert.That(commands.SelectedUnit, Is.Null);
            Assert.That(purchased.Movement.IsMoving, Is.False);
            Assert.That(Vector2.Distance(purchased.transform.position, placementPosition), Is.LessThan(0.1f));
            var arrow = purchased.transform.Find("NewUnitArrow").GetComponent<LineRenderer>();
            Assert.That(arrow.enabled, Is.True);
            Vector2 destination = new Vector2(-3.75f, -3.75f);
            yield return Click(Camera.main.WorldToScreenPoint(destination), MouseButton.Right);
            Assert.That(purchased.Movement.IsMoving, Is.False);
            yield return Click(Camera.main.WorldToScreenPoint(purchased.transform.position + Vector3.up));
            Assert.That(commands.SelectedUnit, Is.EqualTo(purchased));
            Assert.That(arrow.enabled, Is.False);
            yield return Click(Camera.main.WorldToScreenPoint(destination));
            Assert.That(purchased.Movement.IsMoving, Is.False);
            yield return Click(Camera.main.WorldToScreenPoint(purchased.transform.position + Vector3.up));
            yield return Click(Camera.main.WorldToScreenPoint(destination), MouseButton.Right);
            Assert.That(Vector2.Distance(purchased.RequestedDestination, destination), Is.LessThan(0.1f));
            Assert.That(purchased.Movement.IsMoving, Is.True);
        }

        /// <summary>
        /// 실제 화면 버튼 드래그가 미리보기만 표시하다가 놓은 위치에 한 명을 생성하고 한 번 결제합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator DragFromPurchaseButtonPreviewsThenDeploysExactlyOnce()
        {
            CreateTestMouse();
            Vector2 button = ButtonPosition("PurchaseMageButton");
            Vector2 destination = new Vector2(-8, 2);
            yield return DragStart(button, Camera.main.WorldToScreenPoint(destination));
            var placement = Object.FindFirstObjectByType<BattlePlacementCommand>();
            Assert.That(placement.IsDragging, Is.True);
            Assert.That(session.Allies.Count, Is.Zero);
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            var range = GameObject.Find("PreviewAttackRange").GetComponent<LineRenderer>();
            Assert.That(range.enabled, Is.True);
            Assert.That(range.startColor.g, Is.GreaterThan(range.startColor.r));
            Assert.That(
                Vector2.Distance(range.GetPosition(0), destination),
                Is.EqualTo(session.Definition.Classes[1].AttackRange).Within(0.1f));
            Vector2 screenPosition = Camera.main.WorldToScreenPoint(destination);
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = screenPosition });
            yield return null;
            yield return null;
            Assert.That(session.Allies.Count, Is.EqualTo(1));
            Assert.That(session.Wallet.Balance, Is.EqualTo(60));
            Assert.That(Vector2.Distance(session.Allies[0].transform.position, destination), Is.LessThan(0.1f));
            Assert.That(session.Allies[0].CanFight, Is.True);
            Assert.That(range.enabled, Is.False);
            placement.EndDrag(screenPosition);
            Assert.That(session.Allies.Count, Is.EqualTo(1));
            Assert.That(session.Wallet.Balance, Is.EqualTo(60));
        }

        /// <summary>
        /// 연못·화면 버튼·화면 밖에 드롭해도 결제하지 않고 취소합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator InvalidDragDestinationsCancelWithoutSpending()
        {
            CreateTestMouse();
            Vector2 button = ButtonPosition("PurchaseWarriorButton");
            Vector2 lake = Camera.main.WorldToScreenPoint(new Vector2(-21, -2));
            foreach (Vector2 destination in new[]
            {
                lake,
                ButtonPosition("PurchaseMageButton"),
                new Vector2(-40, -40)
            })
            {
                yield return DragStart(button, lake);
                var range = GameObject.Find("PreviewAttackRange").GetComponent<LineRenderer>();
                Assert.That(range.enabled, Is.True);
                Assert.That(range.startColor.r, Is.GreaterThan(range.startColor.g));
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = destination }.WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(testMouse, new MouseState { position = destination });
                yield return null;
                yield return null;
                Assert.That(session.Allies.Count, Is.Zero);
                Assert.That(session.Wallet.Balance, Is.EqualTo(100));
                Assert.That(Object.FindFirstObjectByType<BattlePlacementCommand>().IsPlacing, Is.False);
                Assert.That(range.enabled, Is.False);
            }
        }

        /// <summary>
        /// 클릭 배치 중 잘못된 지면·오른쪽 취소·팝업 진입과 잔액 변경을 안전하게 처리합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator ClickPlacementCancellationPauseAndBalanceChangeDoNotCreateUnits()
        {
            CreateTestMouse();
            Vector2 button = ButtonPosition("PurchaseWarriorButton");
            var placement = Object.FindFirstObjectByType<BattlePlacementCommand>();
            yield return Click(button);
            yield return Click(Camera.main.WorldToScreenPoint(new Vector2(-21, -2)));
            Assert.That(placement.IsPlacing, Is.False);
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            yield return Click(button);
            yield return Click(Camera.main.WorldToScreenPoint(Vector2.zero), MouseButton.Right);
            Assert.That(placement.IsPlacing, Is.False);
            yield return Click(button);
            var popups = Object.FindObjectsByType<SWPopupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SWPopupBase menu = System.Array.Find(popups, value => value.name == "PauseMenuPopup");
            menu.Show();
            yield return null;
            yield return null;
            Assert.That(placement.IsPlacing, Is.False);
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            menu.Hide();
            yield return Click(button);
            Assert.That(session.Wallet.TrySpend(100), Is.True);
            yield return Click(Camera.main.WorldToScreenPoint(Vector2.zero));
            Assert.That(session.Allies.Count, Is.Zero);
            Assert.That(session.Wallet.Balance, Is.Zero);
            Assert.That(placement.IsPlacing, Is.False);
        }

        /// <summary>
        /// 선택 클래스의 실제 사거리와 최신 목적지를 표시하고 선택 변경·도착·부활 예약을 반영합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator SelectionRangeAndDestinationFollowCommandsAndArrival()
        {
            session.TryDeploy(session.Definition.Classes[1], new Vector2(0, 1), out AllyUnit mage, out _);
            session.TryDeploy(session.Definition.Classes[0], new Vector2(5, 1), out AllyUnit warrior, out _);
            var commands = Object.FindFirstObjectByType<BattleMouseCommand>();
            var range = GameObject.Find("SelectedAttackRange").GetComponent<LineRenderer>();
            var destination = GameObject.Find("MoveDestinationRing").GetComponent<LineRenderer>();
            commands.SelectUnit(mage);
            yield return null;
            yield return null;
            Assert.That(range.enabled, Is.True);
            Assert.That(
                Vector2.Distance(range.GetPosition(0), mage.transform.position),
                Is.EqualTo(mage.Definition.AttackRange).Within(0.001f));
            Assert.That(destination.enabled, Is.False);
            Vector2 firstDestination = new Vector2(6, -1);
            commands.ExecuteWorldClick(firstDestination, true);
            yield return null;
            yield return null;
            Assert.That(destination.enabled, Is.True);
            Assert.That((Vector2)destination.GetPosition(0), Is.EqualTo(firstDestination + Vector2.right * 0.48f));
            commands.ExecuteWorldClick(new Vector2(-21, -2), true);
            Assert.That(mage.RequestedDestination, Is.EqualTo(firstDestination));
            commands.SelectUnit(warrior);
            yield return null;
            yield return null;
            Assert.That(destination.enabled, Is.False);
            Assert.That(
                Vector2.Distance(range.GetPosition(0), warrior.transform.position),
                Is.EqualTo(warrior.Definition.AttackRange).Within(0.001f));
            commands.SelectUnit(mage);
            mage.Health.TakeDamage(10000);
            Vector2 changedDestination = new Vector2(3, -3);
            commands.ExecuteWorldClick(changedDestination, true);
            yield return null;
            yield return null;
            Assert.That(destination.enabled, Is.True);
            Assert.That((Vector2)destination.GetPosition(0), Is.EqualTo(changedDestination + Vector2.right * 0.48f));
            mage.AdvanceRevival(mage.Definition.RevivalSeconds);
            mage.Movement.Advance(1000);
            yield return null;
            yield return null;
            Assert.That(destination.enabled, Is.False);
            commands.SelectUnit(null);
            yield return null;
            yield return null;
            Assert.That(range.enabled, Is.False);
        }

        /// <summary>
        /// 공방 체력바 길이가 실제 피해 비율을 반영하고 파괴 시 빈 바만 남습니다.
        /// </summary>
        [UnityTest]
        public IEnumerator WorkshopHealthBarReflectsDamageAndDestruction()
        {
            var bar = Object.FindFirstObjectByType<WorkshopObjectivePresentation>().GetComponentInChildren<HealthBarPresentation>();
            var fill = bar.transform.Find("HpFill_Sprite").GetComponent<SpriteRenderer>();
            var background = bar.transform.Find("HpBackground_Sprite").GetComponent<SpriteRenderer>();
            float height = fill.transform.localScale.y;
            float bottom = fill.bounds.min.y;
            session.Workshop.Health.TakeDamage(session.Workshop.Health.Maximum * 0.6f);
            yield return null;
            yield return null;
            Assert.That(fill.enabled, Is.True);
            Assert.That(fill.transform.localScale.y, Is.EqualTo(height * 0.4f).Within(0.001f));
            Assert.That(fill.bounds.min.y, Is.EqualTo(bottom).Within(0.001f));
            Assert.That(fill.color, Is.EqualTo(DataManager.Instance.ColorData.ArcaneFillColorGradient.Evaluate(0.4f)));
            Assert.That(background.color, Is.EqualTo(DataManager.Instance.ColorData.ArcaneHpBackgroundColor));
            session.Workshop.Health.TakeDamage(10000);
            yield return null;
            yield return null;
            Assert.That(fill.enabled, Is.False);
            Assert.That(background.enabled, Is.True);
        }

        /// <summary>
        /// Main 진입과 재시작에서 관리자 중복 없이 장면별 풀 등록과 공통 데이터가 유지됩니다.
        /// </summary>
        [UnityTest]
        public IEnumerator MainEntryAndStageReloadKeepOneConfiguredManagerAndPool()
        {
            var pool = SWPool.Instance;
            var data = DataManager.Instance;
            var colors = data.ColorData;
            yield return SceneManager.LoadSceneAsync("Main");
            float deadline = Time.realtimeSinceStartup + 10f;
            do
            {
                yield return null;
            }
            while ((SceneManager.GetActiveScene().name != "Stage01_Grassland"
                || Object.FindFirstObjectByType<BattleSession>()?.Wallet == null)
                && Time.realtimeSinceStartup < deadline);
            session = Object.FindFirstObjectByType<BattleSession>();
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Wallet, Is.Not.Null);
            Assert.That(SWPool.Instance, Is.SameAs(pool));
            Assert.That(DataManager.Instance, Is.SameAs(data));
            Assert.That(data.ColorData, Is.SameAs(colors));
            for (int restart = 0; restart < 2; restart++)
            {
                Assert.That(Object.FindObjectsByType<SWPool>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<DataManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(Object.FindObjectsByType<SWPoolRegistry>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
                Assert.That(pool.CountInPool("EnemyUnit"), Is.GreaterThanOrEqualTo(12));
                Assert.That(session.TryDeploy(session.Definition.Classes[0], Vector2.zero, out _, out _), Is.True);
                yield return SceneManager.LoadSceneAsync("Stage01_Grassland");
                yield return null;
                session = Object.FindFirstObjectByType<BattleSession>();
                Assert.That(session.Allies.Count, Is.Zero);
                Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            }
        }

        /// <summary>
        /// 가로 체력바가 아군·적 색상과 사망·부활·풀 재사용 후 체력을 표시합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator CommonHealthBarsUseSharedColorsAndRefreshAfterRevivalAndReuse()
        {
            session.TryDeploy(session.Definition.Classes[0], Vector2.zero, out AllyUnit ally, out _);
            yield return null;
            yield return null;
            var bar = ally.GetComponentInChildren<HealthBarPresentation>();
            var fill = bar.transform.Find("HpFill_Sprite").GetComponent<SpriteRenderer>();
            float width = fill.transform.localScale.x;
            float left = fill.bounds.min.x;
            ally.Health.TakeDamage(ally.Health.Maximum * 0.5f);
            yield return null;
            yield return null;
            Assert.That(fill.transform.localScale.x, Is.EqualTo(width * 0.5f).Within(0.001f));
            Assert.That(fill.bounds.min.x, Is.EqualTo(left).Within(0.001f));
            Assert.That(fill.color, Is.EqualTo(DataManager.Instance.ColorData.CharacterFillColorGradient.Evaluate(0.5f)));
            ally.Health.TakeDamage(10000);
            yield return null;
            yield return null;
            Assert.That(fill.enabled, Is.False);
            ally.AdvanceRevival(ally.Definition.RevivalSeconds);
            yield return null;
            yield return null;
            Assert.That(fill.enabled, Is.True);
            Assert.That(fill.transform.localScale.x, Is.EqualTo(width).Within(0.001f));
            session.StartBattle();
            yield return null;
            yield return null;
            EnemyUnit enemy = session.Enemies[0];
            var enemyBar = enemy.GetComponentInChildren<HealthBarPresentation>();
            var enemyFill = enemyBar.transform.Find("HpFill_Sprite").GetComponent<SpriteRenderer>();
            enemy.Health.TakeDamage(enemy.Health.Maximum * 0.6f);
            yield return null;
            yield return null;
            Assert.That(
                Vector4.Distance(enemyFill.color, DataManager.Instance.ColorData.EnemyFillColorGradient.Evaluate(0.4f)),
                Is.LessThan(0.00001f));
            // 같은 풀 개체를 다른 체력 인스턴스로 초기화하는 실제 재대여 경로를 검증합니다.
            GameObject reused = SWPool.Instance.Spawn("AllyUnit");
            var reusedAlly = reused.GetComponent<AllyUnit>();
            reusedAlly.Initialize(
                session.Definition.Classes[1],
                Object.FindFirstObjectByType<ProjectT.Navigation.WalkableBattlefield>(),
                new Vector2(2, 0));
            yield return null;
            yield return null;
            reusedAlly.Health.TakeDamage(60);
            yield return null;
            SWPool.Instance.Release(reused);
            Assert.That(SWPool.Instance.Spawn("AllyUnit"), Is.SameAs(reused));
            reusedAlly.Initialize(
                session.Definition.Classes[0],
                Object.FindFirstObjectByType<ProjectT.Navigation.WalkableBattlefield>(),
                new Vector2(2, 0));
            yield return null;
            yield return null;
            Assert.That(reused.GetComponentInChildren<HealthBarPresentation>().Fraction, Is.EqualTo(1f));
            SWPool.Instance.Release(reused);
        }

        /// <summary>
        /// 공방 피격은 Hit를 재생하며 선택지 유무만 Alert를 변경합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator WorkshopHitAndChoicesAlertRemainIndependent()
        {
            var workshop = Object.FindFirstObjectByType<WorkshopObjectivePresentation>();
            var animator = workshop.GetComponentInChildren<Animator>();
            Assert.That(animator.GetBool("Alert"), Is.False);
            workshop.SetChoicesAvailable(true);
            yield return null;
            yield return null;
            Assert.That(animator.GetBool("Alert"), Is.True);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Alert_Animation"), Is.True);
            workshop.SetChoicesAvailable(false);
            yield return null;
            session.Workshop.Health.TakeDamage(250);
            yield return null;
            yield return null;
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("Hit_Animation"), Is.True);
            Assert.That(animator.GetBool("Alert"), Is.False, "체력 저하는 선택지 알림을 켜지 않습니다.");
            yield return new WaitForSeconds(1.5f);
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("ArcaneWorkshop_Idle_Animation"), Is.True);
        }

        /// <summary>
        /// 입력 검증에 사용할 가상 마우스를 준비합니다.
        /// </summary>
        private void CreateTestMouse()
        {
            originalInputSettings = InputSystem.settings;
            testInputSettings = Object.Instantiate(originalInputSettings);
            InputSystem.settings = testInputSettings;
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            testMouse = InputSystem.AddDevice<Mouse>();
        }

        /// <summary>
        /// 버튼의 화면 좌표를 계산합니다.
        /// </summary>
        private static Vector2 ButtonPosition(string name)
        {
            Canvas.ForceUpdateCanvases();
            var button = GameObject.Find(name).GetComponent<RectTransform>();
            return RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
        }

        /// <summary>
        /// 가상 마우스로 구매 버튼을 누르고 드래그를 시작합니다.
        /// </summary>
        private IEnumerator DragStart(Vector2 start, Vector2 destination)
        {
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = start });
            yield return null;
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = start }.WithButton(MouseButton.Left));
            yield return null;
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = destination }.WithButton(MouseButton.Left));
            yield return null;
            yield return null;
        }

        /// <summary>
        /// 정지 상태 공격도 대상 방향을 바라보며 실제 소품의 바닥 앞뒤로 정렬이 바뀝니다.
        /// </summary>
        [UnityTest]
        public IEnumerator StationaryAttackFacesTargetAndGroundDepthOrdersCharacters()
        {
            Assert.That(session.TryDeploy(session.Definition.Classes[0], new Vector2(0, 1), out AllyUnit ally, out _), Is.True);
            ally.Movement.Advance(100f);
            Vector3 position = ally.transform.position;
            var renderer = ally.transform.Find("CharacterVisual").GetComponent<SpriteRenderer>();
            ally.Attack.Begin(Time.time, 5, 5, 0.5f, position + Vector3.right, CombatHealth.Create(100));
            yield return null;
            Assert.That(renderer.flipX, Is.False);
            ally.Attack.AimAt(position + Vector3.left);
            yield return null;
            Assert.That(renderer.flipX, Is.True);
            Assert.That(ally.transform.position, Is.EqualTo(position));
            foreach (WorldDepthSorting scenery in Object.FindObjectsByType<WorldDepthSorting>(FindObjectsSortMode.None))
            {
                ally.transform.position = scenery.GroundPosition + Vector3.down * 0.2f;
                yield return null;
                Assert.That(
                    renderer.sortingOrder,
                    Is.GreaterThan(scenery.GetComponent<SortingGroup>().sortingOrder),
                    scenery.name + " 앞");
                ally.transform.position = scenery.GroundPosition + Vector3.up * 0.2f;
                yield return null;
                Assert.That(
                    renderer.sortingOrder,
                    Is.LessThan(scenery.GetComponent<SortingGroup>().sortingOrder),
                    scenery.name + " 뒤");
            }
        }

        /// <summary>
        /// 백그라운드 실행을 활성화하고 실제 시간 동안 전투 시계와 이동이 계속 진행됩니다.
        /// </summary>
        [UnityTest]
        public IEnumerator BattleClockAndMovementContinueWithoutFocusRequirement()
        {
            Assert.That(Application.runInBackground, Is.True);
            Assert.That(session.TryDeploy(session.Definition.Classes[0], new Vector2(0, 1), out AllyUnit ally, out _), Is.True);
            Assert.That(session.TryMove(ally, new Vector2(6, -1)), Is.True);
            Vector3 before = ally.transform.position;
            float started = Time.time;
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(Time.time, Is.GreaterThan(started));
            Assert.That(ally.transform.position, Is.Not.EqualTo(before));
        }

        /// <summary>
        /// 시작 재화로 배치한 전사 두 명과 마법사 한 명이 실제 자동 교전만으로 첫 스테이지를 완료할 수 있습니다.
        /// </summary>
        [UnityTest]
        public IEnumerator InitialArmyCanWinThroughAutomaticCombat()
        {
            Assert.That(session.Definition.EnemyRoute.TryCreateRoute(out var route, out _), Is.True);
            Vector2 firstCorner = route.GetPoint(2);
            Assert.That(
                session.TryDeploy(session.Definition.Classes[0], firstCorner + Vector2.down, out AllyUnit first, out _),
                Is.True);
            Assert.That(
                session.TryDeploy(session.Definition.Classes[0], firstCorner + Vector2.down * 3, out AllyUnit second, out _),
                Is.True);
            Assert.That(
                session.TryDeploy(session.Definition.Classes[1], firstCorner + new Vector2(3, -1), out AllyUnit mage, out string reason),
                Is.True,
                reason);
            first.Movement.Advance(100f);
            second.Movement.Advance(100f);
            mage.Movement.Advance(100f);
            Time.timeScale = 12f;
            session.StartBattle();
            float deadline = Time.realtimeSinceStartup + 35f;
            while (session.CurrentPhase != BattleSession.Phase.FinalRest
                && session.CurrentPhase != BattleSession.Phase.Defeat
                && Time.realtimeSinceStartup < deadline)
            {
                if (session.CurrentPhase == BattleSession.Phase.RoundBreak)
                {
                    session.StartBattle();
                }

                yield return null;
            }

            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.FinalRest));
            Assert.That(session.KilledCount, Is.EqualTo(27));
            Assert.That(session.Workshop.Health.IsAlive, Is.True);
            session.CompleteBattle();
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Victory));
        }

        /// <summary>
        /// 모든 라운드 처치 후 승리하고 새 전투의 지갑과 풀 재사용이 정상인지 확인합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator ThreeRoundsRewardOnceAndRestartCreatesFreshBattle()
        {
            Time.timeScale = 30f;
            session.StartBattle();
            float deadline = Time.realtimeSinceStartup + 30f;
            while (session.CurrentPhase != BattleSession.Phase.FinalRest && Time.realtimeSinceStartup < deadline)
            {
                if (session.CurrentPhase == BattleSession.Phase.RoundBreak)
                {
                    session.StartBattle();
                }

                foreach (EnemyUnit enemy in session.Enemies)
                {
                    if (!enemy.IsActive)
                    {
                        continue;
                    }

                    enemy.Health.TakeDamage(10000f);
                    enemy.Health.TakeDamage(10000f);
                }

                yield return null;
            }

            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.FinalRest));
            Assert.That(session.IsPaused, Is.False);
            Assert.That(session.RoundNumber, Is.EqualTo(3));
            Assert.That(session.KilledCount, Is.EqualTo(27));
            Assert.That(session.Wallet.Balance, Is.EqualTo(370));
            session.CompleteBattle();
            session.CompleteBattle();
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Victory));
            Assert.That(session.Wallet.Balance, Is.EqualTo(370));
            yield return SceneManager.LoadSceneAsync("Stage01_Grassland");
            yield return null;
            session = Object.FindFirstObjectByType<BattleSession>();
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            Assert.That(session.Allies.Count, Is.Zero);
            Assert.That(session.Workshop.Health.Current, Is.EqualTo(session.Definition.WorkshopMaximumHealth));
            Assert.That(session.TryDeploy(session.Definition.Classes[0], new Vector2(5.5f, 0.5f), out _, out _), Is.True);
        }

        /// <summary>
        /// 공방에 도착한 적이 남아 실제 타격으로 공방을 파괴하며 처치 보상을 지급하지 않습니다.
        /// </summary>
        [UnityTest]
        public IEnumerator WorkshopAttacksCauseDefeatWithoutKillReward()
        {
            var workshop = Object.FindFirstObjectByType<WorkshopObjectivePresentation>();
            Assert.That(workshop, Is.Not.Null, "길 끝에 지킬 공방이 있어야 합니다.");
            Assert.That(session.Definition.EnemyRoute.TryCreateRoute(out var route, out _), Is.True);
            Assert.That(Vector2.Distance(session.Workshop.Position, route.GetPoint(route.PointCount - 1)), Is.LessThan(1f));
            Time.timeScale = 30f;
            session.StartBattle();
            float deadline = Time.realtimeSinceStartup + 25f;
            while (session.CurrentPhase != BattleSession.Phase.Defeat && Time.realtimeSinceStartup < deadline)
            {
                foreach (EnemyUnit enemy in session.Enemies)
                {
                    if (enemy.IsActive)
                    {
                        enemy.Movement.Advance(1000f);
                    }
                }

                yield return null;
            }

            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Defeat));
            Assert.That(session.Enemies.Count, Is.GreaterThan(0));
            Assert.That(session.KilledCount, Is.Zero);
            Assert.That(session.Wallet.Balance, Is.EqualTo(100));
            Assert.That(session.Workshop.Health.Current, Is.Zero);
            yield return null;
            Assert.That(workshop.GetComponentInChildren<Animator>().enabled, Is.False);
            Assert.That(workshop.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>().color.r, Is.LessThan(0.5f));
            Assert.That(workshop.ChoicesAvailable, Is.False);
        }

        /// <summary>
        /// 무제한 휴식에서 체력을 유지하고 부활과 재배치를 진행하며 시작 버튼 중복 입력을 거부합니다.
        /// </summary>
        [UnityTest]
        public IEnumerator RoundRestPreservesHealthAllowsRevivalAndStartsOnlyOnce()
        {
            session.TryDeploy(session.Definition.Classes[0], new Vector2(21, 3), out AllyUnit living, out _);
            session.TryDeploy(session.Definition.Classes[0], new Vector2(22, 3), out AllyUnit dead, out _);
            living.Health.TakeDamage(35);
            session.Workshop.Health.TakeDamage(40);
            Time.timeScale = 30;
            session.StartBattle();
            yield return ClearUntilPhase(BattleSession.Phase.RoundBreak);
            Time.timeScale = 15;
            dead.Health.TakeDamage(10000);
            Assert.That(session.TryMove(living, new Vector2(5, -3)), Is.True);
            Vector3 before = living.transform.position;
            float started = Time.time;
            while (Time.time - started < dead.Definition.RevivalSeconds + 1)
            {
                yield return null;
            }

            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.RoundBreak));
            Assert.That(session.RoundNumber, Is.EqualTo(1));
            Assert.That(session.Enemies.Count, Is.Zero);
            Assert.That(living.transform.position, Is.Not.EqualTo(before));
            Assert.That(living.Health.Current, Is.EqualTo(living.Health.Maximum - 35));
            Assert.That(session.Workshop.Health.Current, Is.EqualTo(session.Workshop.Health.Maximum - 40));
            Assert.That(dead.Health.IsAlive, Is.True);
            var button = GameObject.Find("StartBattleButton").GetComponent<UnityEngine.UI.Button>();
            Assert.That(button.interactable, Is.True);
            Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("다음 라운드 시작"));
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(session.RoundNumber, Is.EqualTo(2));
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Fighting));
        }

        /// <summary>
        /// 최종 휴식에는 이동과 부활이 가능하며 결과 버튼을 누르기 전에는 전투가 정지하지 않습니다.
        /// </summary>
        [UnityTest]
        public IEnumerator FinalRestWaitsForResultButtonAndPopupPausesRevival()
        {
            session.TryDeploy(session.Definition.Classes[0], new Vector2(21, 3), out AllyUnit living, out _);
            session.TryDeploy(session.Definition.Classes[1], new Vector2(22, 3), out AllyUnit dead, out _);
            Time.timeScale = 30;
            session.StartBattle();
            yield return ClearUntilPhase(BattleSession.Phase.FinalRest);
            Time.timeScale = 1;
            dead.Health.TakeDamage(10000);
            Assert.That(session.TryMove(living, new Vector2(5, -3)), Is.True);
            var popups = Object.FindObjectsByType<SWPopupBase>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            SWPopupBase menu = System.Array.Find(popups, value => value.name == "PauseMenuPopup");
            menu.Show();
            float remaining = dead.RevivalRemaining;
            session.CompleteBattle();
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.FinalRest));
            Assert.That(dead.RevivalRemaining, Is.EqualTo(remaining));
            menu.Hide();
            Vector3 before = living.transform.position;
            yield return new WaitForSeconds(0.15f);
            Assert.That(living.transform.position, Is.Not.EqualTo(before));
            Assert.That(dead.RevivalRemaining, Is.LessThan(remaining));
            Assert.That(session.IsPaused, Is.False);
            var button = GameObject.Find("StartBattleButton").GetComponent<UnityEngine.UI.Button>();
            Assert.That(button.GetComponentInChildren<TMPro.TMP_Text>().text, Is.EqualTo("결과 보기"));
            button.onClick.Invoke();
            button.onClick.Invoke();
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Victory));
            Assert.That(session.IsPaused, Is.True);
        }

        /// <summary>
        /// 공방 파괴와 마지막 처치가 겹쳐도 패배를 승리로 덮거나 파괴 뒤 보상을 추가하지 않습니다.
        /// </summary>
        [UnityTest]
        public IEnumerator WorkshopDestructionImmediatelyLocksDefeatBeforeRemainingKills()
        {
            Time.timeScale = 30;
            session.StartBattle();
            float deadline = Time.realtimeSinceStartup + 20;
            while ((session.RoundNumber < 3 || session.Enemies.Count < 12) && Time.realtimeSinceStartup < deadline)
            {
                if (session.CurrentPhase == BattleSession.Phase.RoundBreak)
                {
                    session.StartBattle();
                }

                if (session.RoundNumber < 3)
                {
                    foreach (EnemyUnit enemy in session.Enemies)
                    {
                        enemy.Health.TakeDamage(10000);
                    }
                }

                yield return null;
            }

            Assert.That(session.Enemies.Count, Is.EqualTo(12));
            double balance = session.Wallet.Balance;
            session.Workshop.Health.TakeDamage(session.Workshop.Health.Maximum);
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Defeat));
            Assert.That(session.IsPaused, Is.True);
            foreach (EnemyUnit enemy in session.Enemies)
            {
                enemy.Health.TakeDamage(10000);
            }

            session.StartBattle();
            session.CompleteBattle();
            yield return null;
            Assert.That(session.CurrentPhase, Is.EqualTo(BattleSession.Phase.Defeat));
            Assert.That(session.Wallet.Balance, Is.EqualTo(balance));
        }

        /// <summary>
        /// 목표 전투 단계까지 적을 정리하면서 진행을 기다립니다.
        /// </summary>
        private IEnumerator ClearUntilPhase(BattleSession.Phase desiredPhase)
        {
            float deadline = Time.realtimeSinceStartup + 25;
            while (session.CurrentPhase != desiredPhase
                && session.CurrentPhase != BattleSession.Phase.Defeat
                && Time.realtimeSinceStartup < deadline)
            {
                if (session.CurrentPhase == BattleSession.Phase.RoundBreak)
                {
                    session.StartBattle();
                }

                foreach (EnemyUnit enemy in session.Enemies)
                {
                    enemy.Health.TakeDamage(10000);
                }

                yield return null;
            }

            Assert.That(session.CurrentPhase, Is.EqualTo(desiredPhase));
        }

        /// <summary>
        /// 지정 화면 좌표에 마우스 클릭 입력을 전달합니다.
        /// </summary>
        private IEnumerator Click(Vector2 position, MouseButton button = MouseButton.Left)
        {
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = position });
            yield return null;
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = position }.WithButton(button));
            yield return null;
            InputSystem.QueueStateEvent(testMouse, new MouseState { position = position });
            yield return null;
            // 입력 처리 뒤 LateUpdate에서 갱신하는 선택 표시까지 완료한 다음 검사합니다.
            yield return null;
        }

        #endregion // 함수
    }
}
