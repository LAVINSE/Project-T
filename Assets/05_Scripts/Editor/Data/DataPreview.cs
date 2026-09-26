using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using SW.EditorTools;
using SW.Base;
using SW.Stat;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 편집값의 외형·애니메이션·경로·전투 비용을 원본 변경 없이 표시합니다.
    /// </summary>
    public static class DataPreview
    {
        #region 미리보기
        /// <summary>
        /// 현재 데이터에 맞는 미리보기를 만듭니다. 참조가 없으면 안내만 표시합니다.
        /// </summary>
        public static VisualElement Create(ScriptableObject asset)
        {
            var root = new VisualElement();
            root.Add(Description("편집 중인 값의 미리보기입니다. 실제 반영은 저장 후 사용하는 스테이지에서 확인하세요."));
            UnitData appearance = asset as UnitData;
            switch (asset)
            {
                case UnitClassData ally:
                    AddClass(root, ally);
                    break;
                case UnitEnemyData enemy:
                    AddEnemy(root, enemy);
                    break;
                case StageData stage:
                    AddStage(root, stage);
                    break;
                case EquipmentData equipment:
                    AddEquipment(root, equipment);
                    break;
                case EquipmentEffectData effect:
                    root.Add(Description(effect.EffectDescription));
                    root.Add(Description("고정 증가량 설정입니다. 실제 장착·전투 반영은 후속 기능입니다."));
                    break;
                case RewardData reward:
                    AddReward(root, reward);
                    break;
                case EnemyRouteData route:
                    AddRoute(root, route);
                    break;
                case SWIdentifiedObject identified:
                    AddIdentified(root, identified);
                    break;
            }

            if (appearance != null)
            {
                AddAppearance(root, appearance);
            }

            return root;
        }

        /// <summary>
        /// 캐릭터의 배치 비용과 주요 전투 수치를 표시합니다.
        /// </summary>
        private static void AddClass(VisualElement root, UnitClassData ally)
        {
            root.Add(Description("배치 비용 " + ally.DeploymentCost + " · 체력 " + ally.MaximumHealth
                + " · 이동속도 " + ally.MoveSpeed + " · 공격력 " + ally.AttackDamage
                + " · 공격 속도 " + ally.AttackSpeed.ToString("0.###") + "회/초"));
        }

        /// <summary>
        /// 적의 처치 보상 목록과 공방 공격 수치를 표시합니다.
        /// </summary>
        private static void AddEnemy(VisualElement root, UnitEnemyData enemy)
        {
            foreach (RewardEntry reward in enemy.Rewards)
            {
                if (reward != null)
                {
                    root.Add(Description((reward.Definition == null ? "보상 미연결" : reward.Definition.DisplayName)
                        + " · 수량 " + reward.Amount + " · 독립 확률 " + reward.AcquisitionProbability + "%"));
                }
            }

            root.Add(Description("체력 " + enemy.MaximumHealth + " · 공방 피해 " + enemy.WorkshopAttackDamage
                + " · 공방 공격 간격 " + enemy.WorkshopAttackInterval + "초"));
        }

        /// <summary>
        /// 스테이지의 시작 설정과 라운드별 적 수, 이동 경로를 표시합니다.
        /// </summary>
        private static void AddStage(VisualElement root, StageData stage)
        {
            root.Add(Description("시작 재화 " + stage.StartingCurrency + " · 공방 체력 " + stage.WorkshopMaximumHealth
                + " · 라운드 " + stage.RoundCount + "개 · 생성 간격 " + stage.SpawnInterval + "초"));
            for (int index = 0; index < stage.RoundCount; index++)
            {
                root.Add(Description((index + 1) + "라운드: 적 " + stage.GetEnemyCount(index) + "명"));
            }

            AddRoute(root, stage.EnemyRoute);
        }

        /// <summary>
        /// 보상의 기본 수량과 장비 연결, 아이콘을 표시합니다.
        /// </summary>
        private static void AddReward(VisualElement root, RewardData reward)
        {
            root.Add(Description(reward.DisplayName + " · 기본 수량 " + reward.DefaultAmount));
            if (reward is ItemData pricedItem)
            {
                root.Add(Description("판매가 (1개): " + ((double?)pricedItem.SellPrice).ExToSellPriceText(pricedItem.SellCurrency)));
            }
            root.Add(Description("적별 보상에서 수량을 덮어쓸 수 있습니다. 소울·아이템은 기존 자동 지급과 영구 저장을 사용합니다."));
            if (reward is ItemData item && item.Equipment != null)
            {
                root.Add(Description("장비: " + item.Equipment.DisplayName + " · 성능 등급: "
                    + (item.PerformanceGrade != null ? item.PerformanceGrade.DisplayName : "미연결")));
                if (item.Equipment.TryGetPerformanceGrade(item.PerformanceGrade, out EquipmentPerformanceGrade grade)
                    && grade.Effect != null)
                {
                    root.Add(Description(grade.EffectDescription));
                }

                root.Add(Description("이 아이템의 수량을 합산 보관합니다. 장착과 전투 효과 적용은 아직 연결하지 않았습니다."));
            }

            if (reward.Icon != null)
            {
                var icon = new Image
                {
                    sprite = reward.Icon,
                    scaleMode = ScaleMode.ScaleToFit
                };
                icon.AddToClassList("project-data-preview-image");
                root.Add(icon);
            }
        }

        /// <summary>
        /// 분류·스탯 자산의 이름과 값 범위를 표시합니다.
        /// </summary>
        private static void AddIdentified(VisualElement root, SWIdentifiedObject identified)
        {
            root.Add(Description(identified.DisplayName + " · 코드명: " + identified.CodeName));
            if (identified is SWStat stat)
            {
                root.Add(Description("기본값 " + stat.DefaultValue + " · 범위 " + stat.MinValue + " ~ " + stat.MaxValue));
                root.Add(Description(stat.IsPercentType ? "백분율 표시 스탯입니다. 1은 100%입니다." : "일반 수치 스탯입니다."));
            }
        }

        /// <summary>
        /// 사용자 지정 순서대로 성능 등급과 고정 효과를 표시합니다. 없는 참조는 안내로 대신합니다.
        /// </summary>
        private static void AddEquipment(VisualElement root, EquipmentData equipment)
        {
            root.Add(Description("판매가 (1개): " + ((double?)equipment.SellPrice).ExToSellPriceText(equipment.SellCurrency)));
            if (equipment.Icon != null)
            {
                var icon = new Image { sprite = equipment.Icon, scaleMode = ScaleMode.ScaleToFit };
                icon.AddToClassList("project-data-preview-image");
                root.Add(icon);
            }
            else
            {
                root.Add(Description("장비 아이콘 미등록 · 인벤토리에 빈 아이콘으로 표시합니다."));
            }
            root.Add(Description(equipment.DisplayName + " · 희귀도: "
                + (equipment.Rarity != null ? equipment.Rarity.DisplayName : "미연결")));
            if (equipment.PerformanceGrades == null || equipment.PerformanceGrades.Count == 0)
            {
                root.Add(Description("성능 등급을 추가하고 등급별 효과를 연결하세요."));
                return;
            }

            for (int index = 0; index < equipment.PerformanceGrades.Count; index++)
            {
                EquipmentPerformanceGrade grade = equipment.PerformanceGrades[index];
                string gradeName = grade != null && grade.PerformanceGrade != null
                    ? grade.PerformanceGrade.DisplayName : "등급 미연결";
                string effectDescription = grade != null && grade.Effect != null
                    ? grade.EffectDescription : "효과 미연결";
                double total = equipment.GetTotalSelectionWeight();
                double probability = total > 0d && grade != null ? grade.SelectionWeight / total * 100d : 0d;
                root.Add(Description((index + 1) + ". " + gradeName + " · " + probability.ToString("0.###") + "% · " + effectDescription));
            }

            root.Add(Description("추첨 대상 등급마다 아이템을 연결하면 적 보상에서 등급을 추첨해 자동 보관합니다. 장착 기능은 별도입니다."));
        }

        /// <summary>
        /// 연결된 클립의 스프라이트 키를 읽어 미리봅니다. 원본 프레임 목록을 별도로 저장하지 않습니다.
        /// </summary>
        private static void AddAppearance(VisualElement root, UnitData data)
        {
            var state = new DropdownField("동작", new List<string> { "대기", "이동", "공격", "사망" }, 0);
            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList("project-data-preview-image");
            var information = Description(string.Empty);
            root.Add(state);
            root.Add(image);
            root.Add(information);
            double start = EditorApplication.timeSinceStartup;
            root.schedule.Execute(() =>
            {
                if (data == null || data.Animation == null)
                {
                    image.sprite = null;
                    information.text = "애니메이션이 설정된 유닛 프리팹을 연결하세요.";
                    return;
                }

                AnimationClip clip = state.value == "공격"
                    ? data.Animation.GetClip(data.AttackAction)
                    : data.Animation.GetClip(state.value == "사망"
                        ? ProjectDefine.UnitAction.Death
                        : state.value == "이동" ? ProjectDefine.UnitAction.Walk : ProjectDefine.UnitAction.Idle);
                if (clip == null || clip.length <= 0f)
                {
                    image.sprite = null;
                    information.text = "선택한 동작의 클립을 확인하세요.";
                    return;
                }

                double time = (EditorApplication.timeSinceStartup - start) % clip.length;
                foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
                {
                    if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite")
                    {
                        continue;
                    }

                    foreach (var key in AnimationUtility.GetObjectReferenceCurve(clip, binding))
                    {
                        if (key.time > time)
                        {
                            break;
                        }

                        image.sprite = key.value as Sprite;
                    }

                    break;
                }

                image.tintColor = data.Tint;
                information.text = clip.name + " · 원본 길이 " + clip.length.ToString("0.##") + "초";
            }).Every(50);
            root.Add(Description("클립을 반복 미리봅니다. 실제 공격 속도는 공격 간격에 맞추고 피해 시점은 클립 이벤트를 사용합니다."));
        }

        /// <summary>
        /// 경유점 좌표를 읽어 입구·공방 방향과 순서를 그립니다.
        /// </summary>
        private static void AddRoute(VisualElement root, EnemyRouteData route)
        {
            if (route == null)
            {
                root.Add(Description("경로 참조를 연결하세요."));
                return;
            }

            using (var serialized = new SerializedObject(route))
            {
                var property = serialized.FindProperty("points");
                var points = new Vector2[property.arraySize];
                for (int index = 0; index < points.Length; index++)
                {
                    points[index] = property.GetArrayElementAtIndex(index).vector2Value;
                }

                root.Add(new RoutePreview(points));
                root.Add(Description("경유점 " + points.Length + "개 · 숫자는 이동 순서 · 첫 점이 입구, 마지막 점이 공방 방향입니다."));
            }
        }

        /// <summary>
        /// 미리보기의 줄바꿈 가능한 설명을 만듭니다.
        /// </summary>
        private static Label Description(string value)
        {
            var label = new Label(value);
            label.AddToClassList("project-data-description");
            return label;
        }

        #endregion // 미리보기
    }

    /// <summary>
    /// 고정 경로의 순서를 편집 화면 안에 표시합니다. 잘못된 좌표는 그리지 않습니다.
    /// </summary>
    public sealed class RoutePreview : VisualElement
    {
        #region 필드
        private Vector2[] points;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 표시할 좌표의 복사본을 받아 경로 그리기를 연결합니다.
        /// </summary>
        public RoutePreview(Vector2[] source)
        {
            points = source == null ? Array.Empty<Vector2>() : (Vector2[])source.Clone();
            AddToClassList("project-data-route");
            generateVisualContent += DrawRoute;
        }

        /// <summary>
        /// 표시할 좌표를 바꾸고 그림을 다시 그립니다.
        /// </summary>
        public void Present(Vector2[] source)
        {
            points = source == null ? Array.Empty<Vector2>() : (Vector2[])source.Clone();
            MarkDirtyRepaint();
        }

        #endregion // 초기화

        #region 표시
        /// <summary>
        /// 유한한 좌표만 화면 범위에 맞춰 선과 순서 번호로 표시합니다.
        /// </summary>
        private void DrawRoute(MeshGenerationContext context)
        {
            if (points.Length < 2 || contentRect.width < 40f || contentRect.height < 40f)
            {
                return;
            }

            Vector2 minimum = points[0];
            Vector2 maximum = points[0];
            foreach (Vector2 point in points)
            {
                if (float.IsNaN(point.x) || float.IsInfinity(point.x) || float.IsNaN(point.y) || float.IsInfinity(point.y))
                {
                    return;
                }

                minimum = Vector2.Min(minimum, point);
                maximum = Vector2.Max(maximum, point);
            }

            Vector2 span = maximum - minimum;
            float scale = Mathf.Min((contentRect.width - 40f) / Mathf.Max(1f, span.x), (contentRect.height - 40f) / Mathf.Max(1f, span.y));
            var painter = context.painter2D;
            painter.strokeColor = SWEditorTheme.Accent;
            painter.lineWidth = 2f;
            painter.BeginPath();
            for (int index = 0; index < points.Length; index++)
            {
                Vector2 relative = (points[index] - minimum) * scale;
                Vector2 position = new Vector2(relative.x + 20f, contentRect.height - relative.y - 20f);
                if (index == 0)
                {
                    painter.MoveTo(position);
                }
                else
                {
                    painter.LineTo(position);
                }

                context.DrawText((index + 1).ToString(), position + new Vector2(3f, -14f), 12f, SWEditorTheme.Text, null);
            }

            painter.Stroke();
        }

        #endregion // 표시
    }
}
