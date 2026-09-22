using UnityEngine;

namespace ProjectT.Data
{
    /// <summary>
    /// 재화 종류의 이름·그림·기본 지급량입니다. 잔액은 지갑에서 별도로 관리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "Project T/데이터/재화", fileName = "CurrencyData")]
    public sealed class CurrencyData : RewardData
    {
    }
}
