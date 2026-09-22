using UnityEngine;

namespace ProjectT.Data
{
    /// <summary>
    /// 아이템 종류의 이름·그림·기본 지급량입니다. 보관·사용·설계도 기능은 별도 모듈에서 확장합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/데이터/아이템", fileName = "ItemData")]
    public sealed class ItemData : RewardData
    {
    }
}
