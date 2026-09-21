using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using NUnit.Framework;

using ProjectT.Data;
using ProjectT.Economy;
using ProjectT.Rewards;

namespace ProjectT.Tests
{
    /// <summary>
    /// 독립 확률·원본 보존·개별 수량·배치 재화의 원자적 지급을 검증합니다.
    /// </summary>
    public sealed class RewardCalculationTests
    {
        #region 필드
        private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
        #endregion // 필드

        #region 정리
        /// <summary>
        /// 테스트가 만든 메모리 데이터만 제거합니다.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var asset in temporary)
            {
                Object.DestroyImmediate(asset);
            }

            temporary.Clear();
        }
        #endregion // 정리

        #region 검증
        /// <summary>
        /// 같은 자산의 기본 수량과 개별 수량을 동시에 사용해도 원본은 바뀌지 않습니다.
        /// </summary>
        [Test]
        public void AmountOverridesPreserveSharedDefinition()
        {
            var coin = Definition<CurrencyDefinition>(10d);
            var entries = new[] { RewardEntry.Create(coin), RewardEntry.Create(coin, 100f, true, 27d) };
            Assert.That(RewardCalculator.TryCalculate(entries, () => 1d, out var result, out string reason), Is.True, reason);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Amount, Is.EqualTo(10d));
            Assert.That(result[1].Amount, Is.EqualTo(27d));
            Assert.That(coin.DefaultAmount, Is.EqualTo(10d));
        }

        /// <summary>
        /// 보장·제외 항목은 난수 없이 결정되고 나머지 항목은 각각 별도 난수로 판정됩니다.
        /// </summary>
        [Test]
        public void IndependentProbabilitiesAllowSeveralRewardsAndPreserveGuaranteedCoin()
        {
            var coin = Definition<CurrencyDefinition>(10d);
            var item = Definition<ItemDefinition>(1d);
            var entries = new[]
            {
                RewardEntry.Create(coin, 100f),
                RewardEntry.Create(item, 0f),
                RewardEntry.Create(item, 30f, true, 2d),
                RewardEntry.Create(item, 5f, true, 3d)
            };
            var samples = new Queue<double>(new[] { 0.29d, 0.04d });
            Assert.That(RewardCalculator.TryCalculate(entries, () => samples.Dequeue(), out var result, out _), Is.True);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(samples, Is.Empty);
            Assert.That(result[0].Definition, Is.SameAs(coin));
            samples = new Queue<double>(new[] { 0.3d, 0.05d });
            Assert.That(RewardCalculator.TryCalculate(entries, () => samples.Dequeue(), out result, out _), Is.True);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Definition, Is.SameAs(coin));
        }

        /// <summary>
        /// 확률 100%는 난수의 최댓값에서도 지급되고 0%는 최솟값에서도 지급되지 않습니다.
        /// </summary>
        [Test]
        public void ProbabilityEndpointsDoNotConsumeRandomValues()
        {
            var coin = Definition<CurrencyDefinition>(10d);
            int calls = 0;
            Assert.That(RewardCalculator.TryCalculate(
                new[] { RewardEntry.Create(coin, 0), RewardEntry.Create(coin, 100) },
                () =>
                {
                    calls++;
                    return 1d;
                }, out var result, out _), Is.True);
            Assert.That(calls, Is.Zero);
            Assert.That(result.Count, Is.EqualTo(1));
        }

        /// <summary>
        /// 마지막 항목의 잘못된 수량도 판정 전에 발견하므로 앞 항목만 지급되지 않습니다.
        /// </summary>
        [Test]
        public void InvalidEntriesPreserveWalletAndReturnNoPartialResult()
        {
            var coin = Definition<CurrencyDefinition>(10d);
            var invalid = Definition<ItemDefinition>(-1d);
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            temporary.Add(enemy);
            using (var serialized = new SerializedObject(enemy))
            {
                var entries = serialized.FindProperty("rewards");
                entries.arraySize = 1;
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("definition").objectReferenceValue = invalid;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            var wallet = BattleDeploymentWallet.Create(100d);
            Assert.That(BattleRewardService.TryProcess(
                new[] { RewardEntry.Create(coin), enemy.Rewards[0] }, coin, wallet, () => 0d,
                out var result, out _), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100d));
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// 같은 재화가 여러 항목에서 나와도 합산 후 한 번 지급하고 아이템은 계산 결과에만 포함합니다.
        /// </summary>
        [Test]
        public void MultipleRewardsCreditOnlyConfiguredBattleCurrencyOnce()
        {
            var coin = Definition<CurrencyDefinition>(10d);
            var otherCurrency = Definition<CurrencyDefinition>(90d);
            var item = Definition<ItemDefinition>(2d);
            var wallet = BattleDeploymentWallet.Create(100d);
            int notifications = 0;
            wallet.Changed += () => notifications++;
            Assert.That(BattleRewardService.TryProcess(new[]
            {
                RewardEntry.Create(coin), RewardEntry.Create(item),
                RewardEntry.Create(otherCurrency), RewardEntry.Create(coin, 100, true, 5d)
            }, coin, wallet, () => 0d, out var result, out string reason), Is.True, reason);
            Assert.That(wallet.Balance, Is.EqualTo(115d));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(result.Count, Is.EqualTo(4));
        }

        /// <summary>
        /// 계산 중 잘못된 난수가 나오면 앞의 보장 보상도 부분 지급하지 않습니다.
        /// </summary>
        [Test]
        public void InvalidRandomValueRejectsEntirePayout()
        {
            var coin = Definition<CurrencyDefinition>(10d);
            var wallet = BattleDeploymentWallet.Create(100d);
            Assert.That(BattleRewardService.TryProcess(
                new[] { RewardEntry.Create(coin), RewardEntry.Create(coin, 30) }, coin, wallet,
                () => double.NaN, out var result, out _), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100d));
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// 보상 합계가 표현 범위를 벗어나면 잔액·변경 알림·반환 결과를 보존합니다.
        /// </summary>
        [Test]
        public void OverflowDoesNotPartiallyCreditWallet()
        {
            var coin = Definition<CurrencyDefinition>(double.MaxValue);
            var wallet = BattleDeploymentWallet.Create(100d);
            int notifications = 0;
            wallet.Changed += () => notifications++;
            Assert.That(BattleRewardService.TryProcess(
                new[] { RewardEntry.Create(coin), RewardEntry.Create(coin) }, coin, wallet,
                () => 0d, out var result, out _), Is.False);
            Assert.That(wallet.Balance, Is.EqualTo(100d));
            Assert.That(notifications, Is.Zero);
            Assert.That(result, Is.Empty);
        }

        /// <summary>
        /// 기존 스테이지의 처치 보상은 새 구조에서도 코인 10을 항상 지급합니다.
        /// </summary>
        [Test]
        public void MigratedStageKeepsExistingCoinPayout()
        {
            var stage = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/02_Res/Data/Stage/Stage01.asset");
            string before = EditorJsonUtility.ToJson(stage.Enemy);
            var wallet = BattleDeploymentWallet.Create(stage.StartingCurrency);
            Assert.That(BattleRewardService.TryProcess(stage.Enemy.Rewards, stage.DeploymentCurrency, wallet,
                () => 1d, out var result, out string reason), Is.True, reason);
            Assert.That(wallet.Balance, Is.EqualTo(stage.StartingCurrency + 10d));
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Definition, Is.SameAs(stage.DeploymentCurrency));
            Assert.That(EditorJsonUtility.ToJson(stage.Enemy), Is.EqualTo(before));
        }
        #endregion // 검증

        #region 데이터 준비
        /// <summary>
        /// 실제 원본에 영향을 주지 않는 수량 지정 메모리 자산을 생성합니다.
        /// </summary>
        private T Definition<T>(double amount) where T : RewardDefinition
        {
            var asset = ScriptableObject.CreateInstance<T>();
            temporary.Add(asset);
            using (var serialized = new SerializedObject(asset))
            {
                serialized.FindProperty("defaultAmount").doubleValue = amount;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            return asset;
        }
        #endregion // 데이터 준비
    }
}