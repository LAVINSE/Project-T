using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

using SW.Util;

using ProjectT.Data;
using ProjectT.Navigation;

namespace ProjectT.Editor
{
    /// <summary>
    /// 공방으로 이어지는 넓은 초원의 경로와 화면 범위를 한 곳에서 정의합니다.
    /// </summary>
    public static class StageOneBattlefieldLayout
    {
        #region 필드
        /// <summary>
        /// 화면에 표시하는 월드 영역의 가로 길이입니다.
        /// </summary>
        public const float ViewWidth = 48f;

        /// <summary>
        /// 화면에 표시하는 월드 영역의 세로 길이입니다.
        /// </summary>
        public const float ViewHeight = 27f;

        /// <summary>
        /// 단위당 32픽셀로 전장을 그리는 내부 기준 화면의 가로 픽셀 수입니다.
        /// </summary>
        public const int ReferenceWidth = 1536;

        /// <summary>
        /// 단위당 32픽셀로 전장을 그리는 내부 기준 화면의 세로 픽셀 수입니다.
        /// </summary>
        public const int ReferenceHeight = 864;

        /// <summary>
        /// 타일과 캐릭터의 최종 월드 크기에 대응하는 단위당 픽셀 수입니다.
        /// </summary>
        public const int PixelsPerUnit = 32;

        #endregion // 필드

        #region 프로퍼티
        /// <summary>
        /// 화면 메뉴를 고려한 아군 통행 범위입니다.
        /// </summary>
        public static Rect WalkableArea => new Rect(-23, -10.5f, 46, 21);

        /// <summary>
        /// 적이 도착한 뒤 공방 공격을 시작하는 경로 끝 위치입니다.
        /// </summary>
        public static Vector2 ObjectiveEntrance => new Vector2(18, 4);

        #endregion // 프로퍼티

        #region 함수
        /// <summary>
        /// 모서리를 사선으로 연결한 하나의 고정 경로입니다.
        /// </summary>
        public static Vector2[] CreateRoutePoints()
        {
            return new[]
            {
                new Vector2(-22, 6),
                new Vector2(-18, 6),
                new Vector2(-16, 4),
                new Vector2(-16, -4),
                new Vector2(-13, -6),
                new Vector2(-8, -6),
                new Vector2(-5.5f, -3.5f),
                new Vector2(-5.5f, 0.5f),
                new Vector2(-3, 3),
                new Vector2(0, 3),
                new Vector2(3, 0.5f),
                new Vector2(3, -2),
                new Vector2(5.5f, -4.5f),
                new Vector2(11, -4.5f),
                new Vector2(13.5f, -2),
                new Vector2(13.5f, 3),
                new Vector2(16, 5),
                ObjectiveEntrance
            };
        }

        /// <summary>
        /// 현재 장면의 경로·생성 위치·카메라에 같은 좌표계를 적용합니다.
        /// </summary>
        public static void Apply()
        {
            Vector2[] points = CreateRoutePoints();
            var serialized = new SerializedObject(AssetDatabase.LoadAssetAtPath<EnemyRouteDefinition>("Assets/02_Res/Data/Navigation/Stage01EnemyRoute.asset"));
            var property = serialized.FindProperty("points");
            property.arraySize = points.Length;
            for (int index = 0; index < points.Length; index++)
            {
                property.GetArrayElementAtIndex(index).vector2Value = points[index];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            GameObject.Find("EnemyEntrance").transform.position = points[0];
            GameObject.Find("WorkshopAttackPoint").transform.position = ObjectiveEntrance;
            ConfigureCamera(Camera.main);
        }

        /// <summary>
        /// 첨부된 참고 화면에 가까운 전장 크기를 설정하고 내부 픽셀 격자를 유지합니다.
        /// </summary>
        public static bool ConfigureCamera(Camera camera)
        {
            if (camera == null)
            {
                SWLog.LogWarning("[StageOneBattlefieldLayout] 카메라 설정 실패: 카메라가 없습니다.");
                return false;
            }

            camera.orthographic = true;
            camera.orthographicSize = ViewHeight / 2f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.allowDynamicResolution = false;
            var pixelCamera = camera.GetComponent<PixelPerfectCamera>() ?? camera.gameObject.AddComponent<PixelPerfectCamera>();
            pixelCamera.assetsPPU = PixelsPerUnit;
            pixelCamera.refResolutionX = ReferenceWidth;
            pixelCamera.refResolutionY = ReferenceHeight;
            pixelCamera.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
            pixelCamera.cropFrame = PixelPerfectCamera.CropFrame.StretchFill;
            if (!StageOneGameplayBuilder.Set(pixelCamera, "m_FilterMode", (int)PixelPerfectCamera.PixelPerfectFilterMode.Point))
            {
                return false;
            }

            var cameraData = camera.GetUniversalAdditionalCameraData();
            cameraData.antialiasing = AntialiasingMode.None;
            cameraData.renderPostProcessing = false;
            return true;
        }

        #endregion // 함수
    }
}
