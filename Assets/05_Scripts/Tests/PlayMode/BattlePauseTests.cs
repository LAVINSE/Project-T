using System;
using UnityEngine;

using NUnit.Framework;

using ProjectT.Timing;

namespace ProjectT.Tests
{
    /// <summary>
    /// 중첩 팝업과 장면 정리 시 전투가 조기 재개되거나 계속 정지하지 않는지 검증합니다.
    /// </summary>
    public sealed class BattlePauseTests
    {
        #region 필드
        private float previousTimeScale;
        private GameObject owner;
        private BattlePauseController controller;

        #endregion // 필드

        #region 함수
        /// <summary>
        /// 현재 시간 배율을 보존하고 독립 정지 관리자를 준비합니다.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            previousTimeScale = Time.timeScale;
            owner = new GameObject("BattlePauseTests");
            controller = owner.AddComponent<BattlePauseController>();
        }

        /// <summary>
        /// 테스트 객체와 실행 전 시간 배율을 정리합니다.
        /// </summary>
        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(owner);
            Time.timeScale = previousTimeScale;
        }

        /// <summary>
        /// 마지막 팝업까지 닫아야 재개하고 중복 해제는 다른 팝업에 영향을 주지 않습니다.
        /// </summary>
        [Test]
        public void NestedOwnersRestorePreviousSpeedOnlyAfterLastRelease()
        {
            Time.timeScale = 1.5f;
            IDisposable first = controller.Pause();
            IDisposable second = controller.Pause();
            first.Dispose();
            first.Dispose();
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(controller.IsPaused, Is.True);
            second.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
            Assert.That(controller.IsPaused, Is.False);
        }

        /// <summary>
        /// 이미 정지된 전투 위에 팝업을 열었다 닫아도 임의로 재개하지 않습니다.
        /// </summary>
        [Test]
        public void ExistingPauseIsPreserved()
        {
            Time.timeScale = 0f;
            controller.Pause().Dispose();
            Assert.That(Time.timeScale, Is.Zero);
        }

        /// <summary>
        /// 관리자 정리 후 이전 팝업의 늦은 해제가 새 팝업의 정지를 해제하지 않습니다.
        /// </summary>
        [Test]
        public void StaleLeaseCannotReleaseNewPauseAfterReactivation()
        {
            Time.timeScale = 1f;
            IDisposable previous = controller.Pause();
            owner.SetActive(false);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            owner.SetActive(true);
            IDisposable current = controller.Pause();
            previous.Dispose();
            Assert.That(Time.timeScale, Is.Zero);
            current.Dispose();
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        /// <summary>
        /// 팝업 활성·비활성 수명이 정지 소유권을 자동으로 관리하고 반복 연결이 누적되지 않습니다.
        /// </summary>
        [Test]
        public void PopupLifecycleAcquiresAndReleasesOnce()
        {
            var popup = new GameObject("PopupPause");
            try
            {
                Time.timeScale = 1f;
                popup.SetActive(false);
                var pause = popup.AddComponent<BattlePopupPause>();
                pause.Configure(controller);
                Assert.That(controller.IsPaused, Is.False);
                popup.SetActive(true);
                pause.Configure(controller);
                Assert.That(controller.IsPaused, Is.True);
                popup.SetActive(false);
                Assert.That(controller.IsPaused, Is.False);
                Assert.That(Time.timeScale, Is.EqualTo(1f));
                popup.SetActive(true);
                UnityEngine.Object.DestroyImmediate(popup);
                Assert.That(controller.IsPaused, Is.False);
            }
            finally
            {
                if (popup != null)
                {
                    UnityEngine.Object.DestroyImmediate(popup);
                }
            }
        }

        /// <summary>
        /// 비활성 관리자의 정지 요청은 실패하며 기존 시간 배율을 보존합니다.
        /// </summary>
        [Test]
        public void InactiveControllerRejectsPauseWithoutChangingTime()
        {
            Time.timeScale = 1.5f;
            owner.SetActive(false);
            Assert.That(controller.Pause(), Is.Null);
            Assert.That(controller.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1.5f));
        }

        #endregion // 함수
    }
}
