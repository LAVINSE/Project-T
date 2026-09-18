using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

using ProjectT.Battle;
using ProjectT.Presentation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 사용자가 제작한 공방 프리팹의 애니메이션과 체력 표시를 전투에 연결합니다.
    /// </summary>
    public static class ArcaneWorkshopBuilder
    {
        #region 함수
        /// <summary>
        /// 공방 원본의 그림과 애니메이터를 보존하며 참조를 연결합니다.
        /// </summary>
        public static void ConfigurePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ProjectAssetPaths.Workshop);
            try
            {
                Configure(root, null);
                PrefabUtility.SaveAsPrefabAsset(root, ProjectAssetPaths.Workshop);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/02_Res/Animator/ArcaneWorkshop/ArcaneWorkshop_Animator.controller");
            foreach (var layer in controller.layers)
            {
                foreach (var state in layer.stateMachine.states)
                {
                    if (state.state.name == "Hit_Animation")
                    {
                        foreach (var transition in state.state.transitions)
                        {
                            if (transition.isExit && transition.hasExitTime && transition.exitTime == 0f)
                            {
                                transition.exitTime = 1f;
                                EditorUtility.SetDirty(transition);
                                EditorUtility.SetDirty(controller);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 현재 스테이지의 공방을 연결합니다. 이미 배치된 새 공방은 재생성하지 않습니다.
        /// </summary>
        public static void Apply()
        {
            var session = Object.FindFirstObjectByType<BattleSession>();
            if (session == null)
            {
                return;
            }

            GameObject workshop = GameObject.Find("ArcaneWorkshop");
            if (workshop == null)
            {
                workshop = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ProjectAssetPaths.Workshop));
                workshop.transform.position = StageOneBattlefieldLayout.ObjectiveEntrance + Vector2.up * 0.5f;
            }

            GameObject attackPoint = GameObject.Find("WorkshopAttackPoint") ?? GameObject.Find("DefenseExit") ?? new GameObject("WorkshopAttackPoint");
            attackPoint.name = "WorkshopAttackPoint";
            attackPoint.transform.position = StageOneBattlefieldLayout.ObjectiveEntrance + Vector2.up * 0.5f;
            Vector3 workshopPosition = workshop.transform.position;
            workshopPosition.x = attackPoint.transform.position.x;
            workshop.transform.position = workshopPosition;
            SpriteRenderer sprite = workshop.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
            Vector3 visibleBottom = sprite.transform.TransformPoint(SpriteVisibleBounds.Read(sprite.sprite).min);
            workshop.transform.position += Vector3.up * (attackPoint.transform.position.y - visibleBottom.y);
            workshop.GetComponent<WorldDepthSorting>().Configure(attackPoint.transform.position);
            Configure(workshop, session);
            StageOneGameplayBuilder.Set(session, "workshopTarget", attackPoint.transform);
        }

        /// <summary>
        /// 공방 프리팹에 애니메이터·공격 위치·체력바 표시 참조를 연결합니다.
        /// </summary>
        private static void Configure(GameObject workshop, BattleSession session)
        {
            var presentation = workshop.GetComponent<WorkshopObjectivePresentation>();
            var animator = workshop.GetComponentInChildren<Animator>();
            StageOneGameplayBuilder.Set(
                presentation,
                "session",
                session,
                "animator",
                animator,
                "workshopSprite",
                animator.GetComponent<SpriteRenderer>(),
                "healthBar",
                workshop.GetComponentInChildren<HealthBarPresentation>());
            if (PrefabUtility.IsPartOfPrefabInstance(presentation))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(presentation);
            }
        }

        #endregion // 함수
    }
}
