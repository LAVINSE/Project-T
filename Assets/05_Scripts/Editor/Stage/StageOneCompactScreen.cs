using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;

namespace ProjectT.Editor
{
    /// <summary>
    /// 기존 화면 연결을 유지하면서 전장을 덜 가리는 작은 상태 표시와 명령 창을 적용합니다.
    /// </summary>
    public static class StageOneCompactScreen
    {
        #region 함수
        /// <summary>
        /// 현재 장면의 실제 화면 객체를 확인하고 크기와 간격만 조정합니다.
        /// </summary>
        public static void Apply()
        {
            Transform[] objects = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
            RectTransform Find(string name)
            {
                return (RectTransform)objects.Single(value => value.name == name);
            }

            var header = Find("BattleHeader");
            header.offsetMin = new Vector2(0, -64);
            var headerLayout = header.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(24, 24, 7, 7);
            headerLayout.spacing = 24;
            header.GetComponent<UnityEngine.UI.Image>().color = new Color32(21, 34, 33, 225);
            foreach (string name in new[]
            {
                "StageLabel",
                "RoundLabel",
                "DefenseLabel",
                "CurrencyLabel"
            })
            {
                Find(name).GetComponent<TMP_Text>().fontSize = 23;
            }

            SetSize(Find("StageLabel"), 300, 48);
            SetSize(Find("RoundLabel"), 230, 48);
            SetSize(Find("DefenseLabel"), 260, 48);
            SetSize(Find("CurrencyLabel"), 240, 48);
            SetSize(Find("MenuButton"), 110, 48);
            Find("MenuButton").GetComponentInChildren<TMP_Text>().fontSize = 22;
            var footer = Find("CommandPanel");
            footer.anchorMin = footer.anchorMax = new Vector2(0.5f, 0);
            footer.pivot = new Vector2(0.5f, 0);
            footer.anchoredPosition = new Vector2(0, 12);
            footer.sizeDelta = new Vector2(1180, 128);
            var footerLayout = footer.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            footerLayout.padding = new RectOffset(18, 18, 10, 10);
            footerLayout.spacing = 6;
            var hint = Find("CommandHint");
            hint.GetComponent<TMP_Text>().fontSize = 17;
            hint.GetComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 23;
            Find("CommandControls").GetComponent<UnityEngine.UI.HorizontalLayoutGroup>().spacing = 14;
            foreach (string name in new[]
            {
                "PurchaseWarriorButton",
                "PurchaseMageButton"
            })
            {
                var button = Find(name);
                SetSize(button, 215, 70);
                var label = button.GetComponentInChildren<TMP_Text>();
                label.fontSize = 22;
                label.text = label.text.Replace("size=19", "size=16");
            }

            SetSize(Find("SelectedUnitLabel"), 350, 70);
            Find("SelectedUnitLabel").GetComponent<TMP_Text>().fontSize = 20;
            SetSize(Find("StartBattleButton"), 190, 70);
            Find("StartBattleButton").GetComponentInChildren<TMP_Text>().fontSize = 22;
            var roster = Find("AllyRoster");
            roster.anchorMin = roster.anchorMax = new Vector2(0, 1);
            roster.pivot = new Vector2(0, 1);
            roster.anchoredPosition = new Vector2(24, -72);
            roster.sizeDelta = new Vector2(650, 44);
            var template = Find("AllyRosterButtonTemplate");
            SetSize(template, 155, 36);
            template.GetComponentInChildren<TMP_Text>(true).fontSize = 19;
        }

        /// <summary>
        /// 화면 요소의 가로·세로 크기를 설정합니다.
        /// </summary>
        private static void SetSize(RectTransform target, float width, float height)
        {
            var element = target.GetComponent<UnityEngine.UI.LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        #endregion // 함수
    }
}
