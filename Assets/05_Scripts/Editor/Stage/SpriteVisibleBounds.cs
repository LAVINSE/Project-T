using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectT.Editor
{
    /// <summary>
    /// 원본 임포트 설정을 바꾸지 않고 불투명 픽셀의 영역을 읽어 발과 소품 바닥을 맞춥니다.
    /// </summary>
    public static class SpriteVisibleBounds
    {
        #region 함수
        /// <summary>
        /// 투명 여백을 제외한 스프라이트 내부 좌표 영역을 반환합니다.
        /// </summary>
        public static Bounds Read(Sprite sprite)
        {
            var texture = new Texture2D(2, 2);
            try
            {
                ImageConversion.LoadImage(texture, File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite)));
                Color32[] pixels = texture.GetPixels32();
                Rect rectangle = sprite.rect;
                var minimum = new Vector2(rectangle.xMax, rectangle.yMax);
                var maximum = new Vector2(rectangle.xMin, rectangle.yMin);
                for (int vertical = (int)rectangle.yMin; vertical < rectangle.yMax; vertical++)
                {
                    for (int horizontal = (int)rectangle.xMin; horizontal < rectangle.xMax; horizontal++)
                    {
                        if (pixels[vertical * texture.width + horizontal].a <= 127)
                        {
                            continue;
                        }

                        minimum = Vector2.Min(minimum, new Vector2(horizontal, vertical));
                        maximum = Vector2.Max(maximum, new Vector2(horizontal + 1, vertical + 1));
                    }
                }

                if (maximum.x < minimum.x)
                {
                    return sprite.bounds;
                }

                minimum = (minimum - rectangle.min - sprite.pivot) / sprite.pixelsPerUnit;
                maximum = (maximum - rectangle.min - sprite.pivot) / sprite.pixelsPerUnit;
                return new Bounds((minimum + maximum) / 2f, maximum - minimum);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        #endregion // 함수
    }
}
