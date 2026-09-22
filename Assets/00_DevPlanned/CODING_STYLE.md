# 프로젝트 코드 작성 규칙

`Assets/05_Scripts`의 런타임·편집기 코드에 공통 적용합니다. SWUtils의 필드 배치, 영역 구분, 로그 API와 한글 XML 주석을 기준으로 합니다.

## 테스트·마이그레이션 코드 추가 전 확인

- 자동 테스트, 일회성 검증, 마이그레이션 코드를 새로 만들거나 추가하기 전에는 목적과 범위를 설명하고 사용자 확인을 받습니다. 확인 없이 미리 만들지 않습니다.
- 사용자가 Unity에서 지속적으로 사용하는 TestManager 인스펙터 테스트 버튼과 데이터 편집기는 유지합니다. 시험 장면·데이터 자동 복제와 시험 장면 열기는 2026-09-22 후속 요청으로 제거했으므로 다시 만들지 않습니다.
- 2026-09-22 사용자 요청으로 지속 사용하는 수동 테스트 기능은 TestManager에 통합합니다. 기존 버튼과 앞으로 승인한 테스트 기능의 진입점을 모으되 실제 기능 규칙은 각 모듈에 둡니다. 배치·이전 범위는 작업 전에 확인하며 임의의 새 테스트 기능을 추가하지 않습니다.
- 현재 자동 테스트 폴더와 과거 일회성 검증·마이그레이션 도구는 사용자 요청에 따라 제거했습니다. 임의로 다시 생성하지 않습니다.
- 작업 결과는 우선 Unity 컴파일과 기존 수동 확인 기능으로 검증합니다. 검증용 코드 추가가 필요하면 먼저 질문합니다.
- 개발은 작업 하나마다 확인받은 범위만 진행합니다. 추가 기획이나 변경을 질문한 뒤에는 구현·문서 수정·읽기 전용 조사·다른 작업을 모두 멈추고 사용자 답변 후 재개합니다.

## 코드 정렬

- 들여쓰기는 공백 4칸, 중괄호는 다음 줄에 작성합니다.
- `if`, `else`, 반복문은 본문이 한 줄이어도 중괄호로 감쌉니다.
- 생성자와 함수는 여러 줄 블록으로 작성합니다. 값을 읽는 단순 프로퍼티와 자동 프로퍼티는 간결한 형태를 허용합니다.
- 긴 조건과 매개변수는 줄을 나누며, 서로 다른 처리를 한 줄에 압축하지 않습니다.
- 필드·프로퍼티·초기화·기능별 함수는 `#region`으로 구분합니다. 타입·프로퍼티·함수에는 역할과 실패 결과를 설명하는 한글 XML 주석을 작성합니다.
- 임의 약어를 피하고 역할이 드러나는 이름을 사용합니다. Unity 직렬화 필드 이름 변경은 기존 자산 참조 보존을 함께 검토합니다.
- `System`·Unity, 외부 패키지, `SW`, `ProjectT` 순서로 using을 묶습니다.

```csharp
/// <summary>
/// 전달받은 프리팹을 검증하고 배치를 준비합니다.
/// </summary>
private bool Prepare(GameObject prefab)
{
    if (prefab == null)
    {
        SWLog.LogWarning("[클래스명] 준비 실패: 프리팹이 없습니다.");
        return false;
    }

    return true;
}
```

## 구조와 갱신 규칙

- `Update`·`LateUpdate`는 피할 수 있으면 피합니다. 상태가 바뀔 때만 필요한 처리는 알림(event)을 보내고 받는 쪽이 구독해 그때만 갱신합니다.
- 매 프레임 진행이 꼭 필요한 경우(이동·시간 진행·연속 입력 등)에는 `Update`를 사용합니다. 같은 규칙으로 여러 개체를 진행해야 하면 개별 `Update` 대신 관리하는 쪽의 `Update` 하나에서 `Tick`을 호출하는 방식을 우선 검토합니다(예: `BattleManager`가 유닛 이동·부활을 진행).
- 입력은 Input System의 `InputAction` 신호로 처리하고, 짧은 연출만 코루틴을 사용합니다.
- `DefaultExecutionOrder`로 실행 순서를 지정하지 않습니다. 자기 초기화는 `Awake`, 다른 객체 참조는 `Start`부터 사용하고, 공통 관리자는 Main 장면에 두며 편집기 Play도 Main에서 시작합니다. `Resources.Load`로 관리자를 만들지 않습니다.
- 기획자가 종류를 늘려 가는 분류(유닛 동작 등)는 enum 대신 SWUtils `SWCategory` 자산으로 만들고 코드는 코드명으로 비교합니다. 코드가 값마다 다르게 처리하는 값만 enum으로 둡니다.
- 열거형은 `Runtime/Util/ProjectEnum.cs`, 애니메이터 파라미터 해시·공용 상수·색상은 `ProjectDefine.cs`, 공용 검사·표시 확장 메서드는 `ProjectExtension.cs`(SWUtils처럼 `Ex` 접두사)에 모읍니다. 스크립트마다 같은 상수·검사 함수를 따로 만들지 않고, 상태를 가진 static 필드를 새로 만들지 않습니다.
- 데이터 검사 규칙은 각 데이터의 `Validate`에 둡니다. 별도 검사 스크립트를 만들지 않습니다.
- 컴포넌트는 조립 지점 역할만 하고 규칙은 일반 클래스(서비스·컨트롤러)에 나눕니다. 싱글톤·공통 데이터는 조립 지점(`BattleManager`, `BattleUI`)에서만 읽고 하위 객체에는 `Initialize` 인자로 전달합니다.
- 시간 대기는 SWUtils `SWTimer`(Manual 모드, 전투 관리자의 Tick)로 처리합니다.
- 유닛 애니메이션은 유닛별 Animator Controller를 쓰고, 코드가 재생할 동작은 `UnitAnimation` 동작 목록에서 상태 이름으로 연결합니다. 코드에 상태 이름 문자열을 쓰지 않습니다.
- 스크립트 이름을 바꿀 때는 `.cs`와 `.meta`를 함께 옮겨 GUID를 유지합니다. `MovedFrom`·`FormerlySerializedAs` 같은 호환 코드 대신 바뀐 참조를 편집기에서 직접 다시 연결합니다.

## 데이터와 유닛 명명

- 프로젝트 ScriptableObject 스크립트와 자산 이름은 `Data`로 끝냅니다. 예: `UnitClassData`, `UnitEnemyData`, `OrcMageRedData`, `BattleCoinData`.
- `UnitData`는 공통 능력치·프리팹·표시 정보의 부모입니다. 캐릭터와 적은 각각 `UnitClassData`, `UnitEnemyData` 한 자산으로 관리합니다.
- 데이터 자산은 `02_Res/Data/Character`, `02_Res/Data/Enemy`에 구분합니다. 종류별 프리팹은 `04_Prefabs/Units/Character`, `04_Prefabs/Units/Enemy`에 `<종류>Unit` 이름으로 둡니다.
- 공통 동작은 `CharacterUnitBase`, `EnemyUnitBase`에 두고 종류별 프리팹은 이를 상속하는 변형으로 만듭니다. 타입마다 동작 스크립트를 복제하지 않습니다.
- 애니메이션 프레임 배열은 데이터에 중복 저장하지 않습니다. 프리팹의 `UnitAnimation`과 Animator가 원본 클립을 재생하고 공격 클립의 `AttackImpact` 이벤트가 피해 시점을 전달합니다.

## 로그와 실패 처리

- 프로젝트 코드에서 잘못된 입력을 `throw`로 전달하지 않습니다. SWUtils의 실제 API인 `SW.Util.SWLog`로 원인을 기록합니다.
- 복구 가능한 입력·참조 문제는 `SWLog.LogWarning`과 `null` 또는 `false`로 반환합니다. 생성 실패가 가능한 일반 객체는 검증하는 `Create` 함수와 비공개 생성자를 사용합니다.
- 호출자는 성공 여부를 확인한 뒤 상태를 반영합니다. 초기화 실패는 기존 상태를 보존하고, 실패한 배치에서는 비용을 차감하지 않으며 생성된 임시 객체를 풀로 반환합니다.
- 재화 부족·배치 불가 등 정상적인 사용자 입력 거절은 `Try` 반환값과 안내 문구로 처리해 반복 로그를 피합니다.
- 편집기 작업은 필수 입력을 먼저 검증합니다. 직렬화 속성 묶음은 모든 항목을 확인한 뒤 한 번에 적용합니다.
- 외부 이벤트 구독자에서 발생한 예외를 격리해야 할 때는 필요한 범위에서만 잡고 `SWLog.LogError`로 기록합니다.
- `SWLog`는 `SW_DEBUG_MODE`가 있을 때 출력됩니다. 현재 개발 타겟인 Standalone에 활성화했으며 다른 타겟은 각 타겟의 설정을 확인합니다.

## 정렬 실행과 확인

편집기는 이 폴더의 `.editorconfig`를 사용합니다. 문법 구조를 기준으로 정렬하는 도구도 제공하며, Unity 패키지나 다른 Assets 폴더는 수정하지 않습니다. 도구 실행에는 .NET 10 SDK가 필요합니다.

```powershell
dotnet build Tools/CodeStyle/ProjectCodeStyle.csproj --configfile Tools/CodeStyle/NuGet.Config
dotnet Tools/CodeStyle/bin/Debug/net10.0/ProjectCodeStyle.dll Assets/05_Scripts
dotnet Tools/CodeStyle/bin/Debug/net10.0/ProjectCodeStyle.dll Assets/05_Scripts --check
dotnet Tools/CodeStyle/bin/Debug/net10.0/ProjectCodeStyle.dll Assets/05_Scripts --audit
```

`--check`는 파일을 수정하지 않고 정렬이 필요한 경우 종료 코드 1을 반환합니다. `--audit`는 XML 주석이 없는 타입·프로퍼티·함수를 표시합니다. 코드 변경은 Unity 컴파일과 사용자가 유지하는 수동 확인 기능으로 검증하며, 자동 테스트 코드를 추가하려면 사전 확인을 받습니다.
