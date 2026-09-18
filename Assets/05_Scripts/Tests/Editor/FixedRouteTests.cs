using System;
using UnityEngine;

using NUnit.Framework;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Tests
{
    /// <summary>
    /// 꺾인 경로의 이동, 공유 정의의 독립성, 도착 중복과 정지 경계를 검증합니다.
    /// </summary>
    public sealed class FixedRouteTests
    {
        #region 함수
        /// <summary>
        /// 꺾인 경로의 이동 거리를 검증할 테스트 경로를 만듭니다.
        /// </summary>
        private static FixedRoute CreateCornerRoute()
        {
            return FixedRoute.Create(new[] { Vector2.zero, new Vector2(3f, 0f), new Vector2(3f, 4f) });
        }

        /// <summary>
        /// 한 번에 멀리 이동해도 모서리를 가로지르지 않고 경로상의 남은 거리만큼 진행합니다.
        /// </summary>
        [Test]
        public void AdvanceUsesPathLengthAcrossCorner()
        {
            FixedRoute route = CreateCornerRoute();
            var progress = RouteProgress.Create(route);
            Assert.That(route.Length, Is.EqualTo(7f));
            Assert.That(progress.Advance(5f), Is.False);
            Assert.That(progress.Position, Is.EqualTo(new Vector2(3f, 2f)));
            Assert.That(progress.RemainingDistance, Is.EqualTo(2f));
        }

        /// <summary>
        /// 입력 배열이나 다른 개체의 진행이 공유 경로를 변경하지 않습니다.
        /// </summary>
        [Test]
        public void SourceAndIndividualProgressCannotChangeSharedRoute()
        {
            Vector2[] points =
            {
                Vector2.zero,
                new Vector2(4f, 0f)
            };
            var route = FixedRoute.Create(points);
            var first = RouteProgress.Create(route);
            var second = RouteProgress.Create(route);
            points[1] = new Vector2(100f, 0f);
            first.Advance(3f);
            Assert.That(route.Length, Is.EqualTo(4f));
            Assert.That(second.Position, Is.EqualTo(Vector2.zero));
            Assert.That(first.Position, Is.EqualTo(new Vector2(3f, 0f)));
        }

        /// <summary>
        /// 출구를 넘는 이동과 반복 갱신에서도 최초 도착만 알림 대상으로 인정합니다.
        /// </summary>
        [Test]
        public void OvershootArrivesOnceAndStaysAtExit()
        {
            var progress = RouteProgress.Create(CreateCornerRoute());
            Assert.That(progress.Advance(float.MaxValue), Is.True);
            Assert.That(progress.Advance(1f), Is.False);
            Assert.That(progress.Position, Is.EqualTo(new Vector2(3f, 4f)));
            Assert.That(progress.RemainingDistance, Is.Zero);
        }

        /// <summary>
        /// 잘못된 경로와 거리를 거부하면서 현재 진행 위치를 보존합니다.
        /// </summary>
        [Test]
        public void InvalidInputCannotCorruptProgress()
        {
            Assert.That(FixedRoute.Create(new[] { Vector2.zero }), Is.Null);
            Assert.That(FixedRoute.Create(new[] { Vector2.zero, Vector2.zero }), Is.Null);
            Assert.That(FixedRoute.Create(new[] { Vector2.zero, new Vector2(float.NaN, 1f) }), Is.Null);
            var progress = RouteProgress.Create(CreateCornerRoute());
            progress.Advance(2f);
            Assert.That(progress.Advance(-1f), Is.False);
            Assert.That(progress.Advance(float.PositiveInfinity), Is.False);
            Assert.That(progress.Distance, Is.EqualTo(2f));
        }

        /// <summary>
        /// 같은 경로에서 속도가 두 배인 개체는 절반의 시간에 도착합니다.
        /// </summary>
        [Test]
        public void FasterMovementArrivesInHalfTheTime()
        {
            var firstOwner = new GameObject("FirstRouteMovement");
            var secondOwner = new GameObject("SecondRouteMovement");
            try
            {
                var first = firstOwner.AddComponent<EnemyRouteMovement>();
                var second = secondOwner.AddComponent<EnemyRouteMovement>();
                FixedRoute route = CreateCornerRoute();
                first.Initialize(route, 1f);
                second.Initialize(route, 2f);
                first.Advance(3.5f);
                second.Advance(3.5f);
                Assert.That(first.HasArrived, Is.False);
                Assert.That(first.RemainingDistance, Is.EqualTo(3.5f));
                Assert.That(second.HasArrived, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(firstOwner);
                UnityEngine.Object.DestroyImmediate(secondOwner);
            }
        }

        /// <summary>
        /// 정지와 재개는 경로를 유지하고 재초기화는 이전 개체의 도착 상태를 제거합니다.
        /// </summary>
        [Test]
        public void StopResumeAndReusePreserveSingleArrivalPerInitialization()
        {
            var owner = new GameObject("RouteMovement");
            try
            {
                var movement = owner.AddComponent<EnemyRouteMovement>();
                FixedRoute route = CreateCornerRoute();
                int arrivals = 0;
                movement.Arrived += _ => arrivals++;
                movement.Initialize(route, 2f);
                movement.Advance(1f);
                movement.SetStopped(true);
                movement.Advance(100f);
                Assert.That(owner.transform.position, Is.EqualTo(new Vector3(2f, 0f, 0f)));
                movement.SetStopped(false);
                movement.Advance(0f);
                Assert.That(owner.transform.position, Is.EqualTo(new Vector3(2f, 0f, 0f)));
                movement.Advance(2.5f);
                movement.Advance(10f);
                Assert.That(arrivals, Is.EqualTo(1));
                movement.Initialize(route, 1f);
                Assert.That(movement.HasArrived, Is.False);
                Assert.That(owner.transform.position, Is.EqualTo(Vector3.zero));
                movement.Advance(7f);
                Assert.That(arrivals, Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        /// <summary>
        /// 잘못된 새 설정이 기존 이동을 지우지 않고 큰 시간 증가도 출구에서 멈춥니다.
        /// </summary>
        [Test]
        public void InvalidReinitializationPreservesMovementAndLargeStepCannotOverflow()
        {
            var owner = new GameObject("ValidatedRouteMovement");
            try
            {
                var movement = owner.AddComponent<EnemyRouteMovement>();
                FixedRoute route = CreateCornerRoute();
                movement.Initialize(route, 2f);
                movement.Advance(1f);
                Assert.That(movement.Initialize(route, float.NaN), Is.False);
                Assert.That(movement.Initialize(null, 1f), Is.False);
                Assert.That(movement.RemainingDistance, Is.EqualTo(5f));
                Assert.That(movement.MoveSpeed, Is.EqualTo(2f));
                movement.Advance(float.MaxValue);
                Assert.That(movement.HasArrived, Is.True);
                Assert.That(owner.transform.position, Is.EqualTo(new Vector3(3f, 4f, 0f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        /// <summary>
        /// 경유점을 입력하지 않은 데이터는 이유를 제공하고 실행 경로 생성을 거부합니다.
        /// </summary>
        [Test]
        public void EmptyDefinitionReturnsValidationReason()
        {
            var definition = ScriptableObject.CreateInstance<EnemyRouteDefinition>();
            try
            {
                Assert.That(definition.TryCreateRoute(out FixedRoute route, out string reason), Is.False);
                Assert.That(route, Is.Null);
                Assert.That(reason, Is.Not.Empty);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
            }
        }

        #endregion // 함수
    }
}
