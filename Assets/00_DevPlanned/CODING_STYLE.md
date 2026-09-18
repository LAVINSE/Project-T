# 프로젝트 코드 작성 규칙

`Assets/05_Scripts`의 런타임·편집기·테스트 코드에 공통 적용합니다. SWUtils의 필드 배치, 영역 구분, 로그 API와 한글 XML 주석을 기준으로 합니다.

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

`--check`는 파일을 수정하지 않고 정렬이 필요한 경우 종료 코드 1을 반환합니다. `--audit`는 XML 주석이 없는 타입·프로퍼티·함수를 표시합니다. 실패 처리 변경은 Unity 컴파일과 편집기·플레이 모드 테스트로 함께 검증합니다.
