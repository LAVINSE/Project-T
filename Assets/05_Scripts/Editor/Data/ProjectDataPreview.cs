using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using SW.EditorTools;

using ProjectT.Data;

namespace ProjectT.Editor.Data
{
    /// <summary>
    /// 편집값의 외형·애니메이션·경로·전투 비용을 원본 변경 없이 표시합니다.
    /// </summary>
    public static class ProjectDataPreview
    {
        #region 미리보기
        /// <summary>
        /// 현재 데이터에 맞는 미리보기를 만듭니다. 참조가 없으면 안내만 표시합니다.
        /// </summary>
        public static VisualElement Create(ScriptableObject asset)
        {
            var root = new VisualElement();
            root.Add(Description("편집 중인 값의 미리보기입니다. 실제 전투는 적용·저장 후 시험 전투에서 확인하세요."));
            UnitAppearance appearance = asset as UnitAppearance;
            if (asset is AllyClassDefinition ally)
            {
                appearance = ally.Appearance;
                root.Add(Description("배치 비용 " + ally.DeploymentCost + " · 체력 " + ally.MaximumHealth + " · 이동속도 " + ally.MoveSpeed + " · 피해 " + ally.AttackDamage + " · 공격 간격 " + ally.AttackInterval + "초"));
            }
            else if (asset is EnemyDefinition enemy)
            {
                appearance = enemy.Appearance;
                foreach (var reward in enemy.Rewards)
                {
                    if (reward != null)
                    {
                        root.Add(Description((reward.Definition == null ? "보상 미연결" : reward.Definition.DisplayName)
                            + " · 수량 " + reward.Amount + " · 독립 확률 " + reward.AcquisitionProbability + "%"));
                    }
                }
                root.Add(Description("체력 " + enemy.MaximumHealth + " · 공방 피해 " + enemy.WorkshopAttackDamage + " · 공방 공격 간격 " + enemy.WorkshopAttackInterval + "초"));
            }
            else if (asset is StageDefinition stage)
            {
                root.Add(Description("시작 재화 " + stage.StartingCurrency + " · 공방 체력 " + stage.WorkshopMaximumHealth + " · 라운드 " + stage.RoundCount + "개 · 생성 간격 " + stage.SpawnInterval + "초"));
                for (int index = 0; index < stage.RoundCount; index++)
                {
                    root.Add(Description((index + 1) + "라운드: 적 " + stage.GetEnemyCount(index) + "명"));
                }

                AddRoute(root, stage.EnemyRoute);
            }
            else if (asset is RewardDefinition reward)
            {
                root.Add(Description(reward.DisplayName + " · 기본 수량 " + reward.DefaultAmount));
                root.Add(Description("적별 보상에서 수량을 덮어쓸 수 있습니다. 현재 배치 재화만 지급되며 다른 재화·아이템의 보관은 후속 기능입니다."));
                if (reward.Icon != null)
                {
                    var icon = new Image { sprite = reward.Icon, scaleMode = ScaleMode.ScaleToFit };
                    icon.AddToClassList("project-data-preview-image");
                    root.Add(icon);
                }
            }
            else if (asset is EnemyRouteDefinition route)
            {
                AddRoute(root, route);
            }

            if (appearance != null)
            {
                AddAppearance(root, appearance);
            }

            return root;
        }

        /// <summary>
        /// 외형의 대체 프레임 규칙에 맞춰 선택한 동작을 반복 재생합니다.
        /// </summary>
        private static void AddAppearance(VisualElement root, UnitAppearance appearance)
        {
            var choices = new List<string>
            {
                "대기",
                "이동",
                "공격",
                "사망"
            };
            var state = new DropdownField("동작", choices, 0);
            root.Add(state);
            var image = new Image
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            image.AddToClassList("project-data-preview-image");
            root.Add(image);
            var information = Description(string.Empty);
            root.Add(information);
            double start = EditorApplication.timeSinceStartup;
            root.schedule.Execute(() =>
            {
                if (appearance == null)
                {
                    return;
                }

                Sprite[] frames = appearance.GetFrames(state.value != "사망", state.value == "이동", state.value == "공격");
                float speed = appearance.FramesPerSecond;
                if (frames == null || frames.Length == 0 || float.IsNaN(speed) || float.IsInfinity(speed) || speed <= 0f)
                {
                    image.sprite = null;
                    information.text = "표시 가능한 프레임과 재생 속도가 필요합니다.";
                    return;
                }

                int index = (int)(((EditorApplication.timeSinceStartup - start) * speed) % frames.Length);
                image.sprite = frames[index];
                image.tintColor = appearance.Tint;
                information.text = state.value + " · 프레임 " + index + " / " + (frames.Length - 1) + " · 초당 " + speed + "프레임";
            }).Every(50);
            root.Add(Description("일반 동작은 초당 프레임으로 재생합니다. 실제 공격은 공격 간격에 맞춰 전체 동작 길이가 조정됩니다."));
        }

        /// <summary>
        /// 경유점 좌표를 읽어 입구·공방 방향과 순서를 그립니다.
        /// </summary>
        private static void AddRoute(VisualElement root, EnemyRouteDefinition route)
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

                root.Add(new ProjectRoutePreview(points));
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
    public sealed class ProjectRoutePreview : VisualElement
    {
        #region 필드
        private readonly Vector2[] points;

        #endregion // 필드

        #region 초기화
        /// <summary>
        /// 표시할 좌표의 복사본을 받아 경로 그리기를 연결합니다.
        /// </summary>
        public ProjectRoutePreview(Vector2[] source)
        {
            points = source == null ? Array.Empty<Vector2>() : (Vector2[])source.Clone();
            AddToClassList("project-data-route");
            generateVisualContent += DrawRoute;
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
