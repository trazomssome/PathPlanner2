# Path Editor Specification / 패스 에디터 사양

## 한글 (Korean)

### 1. 드로잉 도구 동작
- **좌표 단위**: 밀리미터(mm) 기준. 표시 및 저장 시 소수점 셋째 자리까지 유지하며, 필요 시 픽셀-밀리미터 환산 비율(픽셀/mm)을 옵션으로 지정한다.
- **이미지 및 Z-순서**: 모든 점/라인은 참조 이미지 위에 렌더링된다. 레시피 전체 설정으로 투명도를 제어하며, 선택된 요소는 강조 색상으로 표시한다.
- **모드 기반 입력**: 이동, 선, 점 모드를 버튼으로 전환한다. 선 모드에서는 좌클릭으로 선분을 추가하고 우클릭으로 현재 선을 종료한다. 점 모드에서는 좌클릭마다 독립된 점 요소를 생성한다.
- **드래그 편집**: 이동 모드에서 요소를 선택한 뒤 드래그하여 점이나 선 꼭짓점을 이동할 수 있다. 이미지 경계를 벗어날 경우 경고 없이 그대로 좌표를 저장한다.
- **센터 조정**: 기본 센터는 이미지 중심이다. 센터 좌표를 변경하면 화면상의 위치는 유지되지만 레시피 저장 좌표는 센터 이동량만큼 자동으로 오프셋된다.
- **Undo/Redo**: 레시피 및 드로잉 조작 전 상태를 저장하여 실행 취소/다시 실행 기능을 지원한다.

### 2. 레시피 구조 및 데이터 모델
- **레시피 계층**: 통합 레시피는 여러 도포 레시피를 묶은 상위 개념이며, 본 에디터는 단일 도포 레시피 편집에 초점을 둔다.
- **도포 레시피 항목**: 각 항목은 `Point` 또는 `Line` 타입을 가지며, 좌표(X, Y), 속도, 가속도, 감속도, 도포 여부, 자동 스무딩 사용 여부, 개별 속도 사용 여부를 포함한다.
- **기본 설정**: 레시피 전체에 적용되는 기본 속도(Default Speed)와 오버레이 투명도(Opacity), 픽셀-밀리미터 비율을 유지한다. 개별 항목이 개별 속도 사용 옵션을 활성화하면 해당 속도를 우선 적용한다.
- **제약 조건**: 속도와 가속/감속은 구성 가능한 범위 내 값으로 검증하며, 범위를 벗어나면 메시지를 통해 사용자에게 안내한다. 스냅, 축 정렬 제약은 제공하지 않는다.
- **JSON 직렬화**: 레시피는 Newtonsoft Json.NET을 사용해 JSON으로 import/export 하며, `schemaVersion` 필드를 포함한다.

### 3. UI 구성
- **레이아웃**: 창은 좌측 드로잉 영역과 우측 편집 패널을 7:3 비율로 유지한다. 최소 폭을 지정하여 목록과 편집 필드가 잘리지 않도록 한다.
- **리스트 및 편집기**: 우측 패널에는 레시피 항목 리스트와 요약 정보가 표시된다. 항목 선택 시 하단 인라인 편집 패널에서 속성 값을 변경할 수 있다. 다중 선택은 지원하지 않는다.
- **연동**: 리스트 선택과 캔버스 강조 상태가 동기화되며, 편집 내용은 즉시 드로잉 요소와 레시피 데이터에 반영된다.
- **다국어/단축키**: 다국어 지원과 단축키는 제공하지 않는다.

### 4. WMX3 통합 및 기타 고려사항
- **PathInterpolation 파라미터**: 내보내기 시 X, Y 위치, 속도, 가속도, 감속도, 도포기 분사 여부, 자동 스무딩 여부, 개별 속도 사용 여부를 JSON에 포함한다.
- **검증 및 오류 처리**: 내보내기 전에 필수 필드를 확인하고 누락/범위 초과 시 사용자에게 메시지로 안내한다. WMX3 통신은 포함하지 않으며, 레시피 저장/불러오기 실패에 대한 오류 메시지만 제공한다.
- **성능**: 세그먼트 수 제한은 없으나, 대량 편집 시 렌더링은 배치 갱신으로 묶어 처리하여 화면 갱신 빈도를 낮춘다.
- **테스트**: 기능 테스트는 수동 검증을 기준으로 하며 별도 자동화 테스트 요구 사항은 없다.

## English

### 1. Drawing Tool Behaviour
- **Coordinate unit**: Values are stored in millimetres with precision up to three decimal places. A millimetres-per-pixel ratio can be configured to map the canvas to physical units.
- **Image & Z-order**: Points and lines are rendered above the reference image. Global opacity is controlled at the recipe level, and selected elements are highlighted with a distinct colour.
- **Mode-based input**: Buttons switch between Move, Line, and Point modes. In Line mode, left-click adds vertices and right-click finishes the active polyline. In Point mode, each left-click creates an independent point element.
- **Drag editing**: With Move mode active, vertices and standalone points can be dragged to new positions. Coordinates are allowed to extend beyond the image bounds.
- **Center adjustment**: The default centre aligns with the image centre. Changing the centre keeps on-screen geometry stationary while shifting stored coordinates by the centre offset.
- **Undo/Redo**: Drawing and recipe mutations capture their previous state to support undo and redo operations.

### 2. Recipe Structure & Data Model
- **Hierarchy**: An integration recipe aggregates multiple dispense recipes. The editor focuses on one dispense recipe at a time.
- **Dispense recipe items**: Each item is either `Point` or `Line`, containing coordinates (X, Y), speed, acceleration, deceleration, dispense flag, auto-smoothing flag, and a toggle indicating whether a custom speed overrides the default.
- **Global defaults**: The recipe defines a Default Speed, overlay opacity, and the millimetres-per-pixel ratio. When an item enables custom speed, its own speed value overrides the default.
- **Constraints**: Speed and acceleration/deceleration values are validated against configurable bounds. Snap-to-grid and axis-alignment constraints are not provided.
- **JSON serialisation**: Recipes are imported/exported as JSON via Newtonsoft Json.NET, including a `schemaVersion` field for compatibility.

### 3. UI Composition
- **Layout**: The window maintains a 7:3 split between the drawing surface and the recipe inspector, enforcing a minimum width to avoid clipping list content.
- **Lists & editor**: The inspector shows recipe items and summary values. Selecting an item reveals inline editing controls beneath the list. Multi-select is not supported.
- **Synchronisation**: List selection stays in sync with canvas highlighting, and edits update both the visuals and underlying recipe data.
- **Localization/shortcuts**: No multi-language UI or keyboard shortcuts are provided.

### 4. WMX3 Integration & Additional Considerations
- **PathInterpolation parameters**: Exported JSON includes X/Y positions, speed, acceleration, deceleration, dispense flag, auto-smoothing flag, and the per-item custom speed flag required by PathInterpolation.
- **Validation & errors**: Prior to export, required fields and value ranges are validated. Failures present error messages to the user. WMX3 communication is out of scope; only recipe load/save errors are handled.
- **Performance**: There is no hard cap on segment count, but render updates batch changes to minimise refresh churn for large edits.
- **Testing**: Manual validation is expected; automated tests are not mandated.

