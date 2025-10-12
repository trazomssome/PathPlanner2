# Path Editor Specification / 도포 경로 편집기 사양

## 한글

### 1. 개요
- 기준 이미지를 배경으로 도포 경로를 작성하고 WMX3 PathInterpolation에 필요한 데이터를 JSON으로 직렬화한다.
- 좌표 단위는 밀리미터(mm)이며 모든 좌표값은 소수점 셋째 자리까지 보존한다.
- 캔버스상의 mm/px 스케일은 설정으로 관리하며 변경 시 레이아웃이 즉시 반영된다.

### 2. 드로잉 도구
- **모드**: 이동/선/점/센터 설정 모드를 버튼으로 전환한다. 모드에 따라 클릭·드래그 동작이 달라진다.
- **선 모드**: 좌클릭으로 선분 꼭지점을 추가하며 인접한 Line 좌표끼리는 자동 연결된다. 우클릭으로 현재 선 그리기를 종료한다.
- **점 모드**: 좌클릭으로 Point 항목을 추가한다. 포인트는 서로 연결되지 않는다.
- **드래그**: 모든 좌표 요소(점, 선 꼭지점)는 드래그로 이동 가능하다. 이동 중에도 좌표는 mm 기준으로 반올림 규칙을 지킨다.
- **센터 조정**: 센터 설정 모드에서 클릭한 위치를 새 센터로 지정한다. 화면상의 위치는 그대로 유지되지만 레시피 좌표는 센터 이동량만큼 오프셋된다.
- **표시**: 모든 도형은 기준 이미지 위에 오버레이되며 투명도는 레시피 전체 설정값으로 제어한다. 선택된 요소는 강조 색상을 사용한다.
- **Undo/Redo**: 좌표 추가·삭제·이동 및 속성 변경은 Undo/Redo 스택을 통해 되돌릴 수 있다.

### 3. 레시피 데이터 모델
- **통합 레시피**: 여러 도포 레시피의 목록으로 구성되는 상위 객체. 각 도포 레시피는 동일한 구조를 가진다.
- **도포 레시피**:
  - 기본 속도(Default Speed)와 속도 제한 옵션을 가진다.
  - `Coordinates`: 순서가 있는 좌표 리스트로, 각 항목은 아래 필드를 가진다.
    - `Id`, `Name`
    - `Type`: `Line` 또는 `Point`
    - `X`, `Y` (mm, 소수점 셋째 자리)
    - `UseCustomSpeed`, `Speed`
    - `Acceleration`, `Deceleration`
    - `DispenseEnabled`, `UseAutoSmoothing`
- **직렬화**: Newtonsoft Json.NET을 사용해 JSON 포맷으로 저장·불러오기를 지원한다. 스키마 버전(`schemaVersion`) 필드를 포함한다.

### 4. UI 구성
- **레이아웃**: 좌측 70%(드로잉 캔버스), 우측 30%(리스트 및 속성 편집)를 유지한다. 최소 창 너비를 지정해 편집 UI가 잘리지 않게 한다.
- **레시피 리스트**: 우측 패널에 통합 레시피와 각 도포 레시피, 좌표 항목을 트리 형태로 표시하고 선택·추가·삭제·순서 변경을 지원한다. (현재 구현 범위는 단일 도포 레시피 편집으로 제한 가능)
- **선택 동기화**: 리스트에서 선택한 항목이 캔버스에서도 강조되고, 반대로 캔버스 선택이 리스트에 반영된다.
- **편집**: 우측 패널 하단에 선택한 좌표의 상세 속성을 인라인 편집(텍스트 박스, 토글)으로 제공한다.
- **단축키**: 제공하지 않으며 버튼 중심의 인터랙션을 사용한다.
- **다국어**: 한국어 UI 고정, 다국어는 지원하지 않는다.

### 5. WMX3 통합 및 검증
- PathInterpolation 전송 전에 다음 필드를 검증한다: `X`, `Y`, `Speed`, `Acceleration`, `Deceleration`, `DispenseEnabled`, `UseAutoSmoothing`.
- 속도/가속도 파라미터는 속도 제한 설정을 넘지 않도록 검사하며, 오류 시 사용자에게 메시지로 알린다.
- JSON 내보내기 전에 스키마 버전과 필수 필드 누락 여부를 검사한다.

### 6. 성능 및 오류 처리
- 세그먼트 개수에는 제한이 없으나 다수의 업데이트는 배치 렌더링으로 묶어 성능을 확보한다.
- WMX3 통신 기능은 포함하지 않으며, 직렬화/검증 단계에서 오류가 발생하면 사용자에게 알림 창을 통해 안내한다.

## English

### 1. Overview
- Uses a reference image as the background to design dispense paths and serializes WMX3 PathInterpolation data to JSON.
- Coordinate unit is millimeters (mm) and every coordinate preserves three decimal places.
- The mm-per-pixel scale is configurable and updates the overlay immediately when changed.

### 2. Drawing Tool
- **Modes**: Switch between Move, Line, Point, and Center modes via buttons; the active mode controls click/drag behavior.
- **Line Mode**: Left-click appends vertices for a polyline. Adjacent coordinates marked as `Line` are automatically connected. Right-click finishes the current polyline.
- **Point Mode**: Left-click creates a `Point` entry. Points are not connected to neighbors.
- **Dragging**: All graphical elements (points and line vertices) are draggable and store coordinates rounded to three decimals in mm.
- **Center Adjustment**: In center mode, clicking defines the new center. Visual positions stay fixed while recipe coordinates are offset by the center delta.
- **Rendering**: Shapes are drawn above the reference image. Global opacity comes from the recipe setting, and selected items are highlighted.
- **Undo/Redo**: Adding, removing, moving, and editing coordinates is tracked with undo/redo stacks.

### 3. Recipe Data Model
- **Integrated Recipe**: Parent object that can hold multiple dispense recipes; each dispense recipe uses the same schema.
- **Dispense Recipe**:
  - Provides `DefaultSpeed` and optional speed limit settings.
  - `Coordinates`: ordered list with the fields below.
    - `Id`, `Name`
    - `Type`: `Line` or `Point`
    - `X`, `Y` (mm, three decimal places)
    - `UseCustomSpeed`, `Speed`
    - `Acceleration`, `Deceleration`
    - `DispenseEnabled`, `UseAutoSmoothing`
- **Serialization**: Use Newtonsoft Json.NET for import/export in JSON format, including a `schemaVersion` marker.

### 4. UI Composition
- **Layout**: Maintain a 70/30 split between the drawing canvas and the inspector panel, enforcing a minimum window width.
- **Recipe List**: Display integrated recipe, dispense recipes, and coordinate entries on the right. Support selection, add/remove, and reordering. (Initial implementation may limit editing to a single dispense recipe.)
- **Selection Sync**: Selections in the list highlight shapes on the canvas and vice versa.
- **Editing**: Inline editors (text boxes, toggles) below the list expose the properties of the selected coordinate.
- **Shortcuts**: None required; rely on buttons.
- **Localization**: Single-language UI (Korean only) with no localization layer.

### 5. WMX3 Integration & Validation
- Validate the following fields before exporting to PathInterpolation: `X`, `Y`, `Speed`, `Acceleration`, `Deceleration`, `DispenseEnabled`, `UseAutoSmoothing`.
- Enforce speed/acceleration limits using the configured speed limit option. Notify the user when validation fails.
- Ensure schema version and mandatory fields are present before exporting JSON.

### 6. Performance & Error Handling
- No hard limit on segments, but batch re-rendering to maintain responsiveness when many updates occur.
- WMX3 communication is out of scope. Report serialization/validation errors via dialog messages.

