# Path Editor Specification / 패스 에디터 사양

## 한국어 (Korean)

### 1. 개요
- 본 도포 경로 편집기는 기준 이미지를 배경으로 도포 패스를 작성하고 WMX3 PathInterpolation 에 전달할 수 있는 레시피를 구성한다.
- 좌표 단위는 **밀리미터**이며, 모든 좌표 값은 반올림하여 소수점 셋째 자리까지(0.001mm) 저장한다.
- 기본 뷰는 7:3 비율의 레이아웃으로, 좌측에는 드로잉 캔버스, 우측에는 레시피 및 속성 편집 패널을 배치한다.

### 2. 드로잉 도구 동작
- **모드 기반 입력**: 포인터, 포인트 추가, 폴리라인 추가, 이미지 팬 등 모드를 전환 버튼으로 선택한다.
- **선/점 편집**: 포인트(단일 점)는 서로 연결되지 않으며, 폴리라인은 인접 좌표를 자동 연결한다.
- **드래그**: 모든 포인트와 폴리라인 꼭짓점은 드래그로 이동 가능하며, 이동 시 실시간으로 mm 좌표가 갱신된다.
- **센터 기준**: 기본 센터는 이미지 중심이다. 사용자가 새 센터를 지정하면 화면상의 위치는 그대로 유지하되, 레시피 좌표는 새 센터 기준으로 다시 계산된다.
- **Z-순서 및 강조**: 모든 그리기 요소는 배경 이미지 위에 렌더링되며 전역 투명도 값을 적용한다. 선택된 점/선은 강조 색상(불투명)으로 표시한다.
- **Undo / Redo**: 점 추가, 폴리라인 작성, 속성 변경, 센터 이동 등 편집 내역을 실행 취소/다시 실행으로 되돌릴 수 있다.
- **보조 기능**: 스냅과 축 정렬은 제공하지 않으며, Ctrl + 휠 줌과 팬을 지원한다.

### 3. 레시피 데이터 모델
- **전역 속성**
  - `schemaVersion`: 직렬화 스키마 버전(`"1.0.0"`).
  - `defaultSpeedMmPerSec`: 폴리라인에서 별도 속도를 지정하지 않을 때 사용하는 기본 속도.
  - `speedLimitMmPerSec` 및 `enforceSpeedLimit`: 속도 제한 옵션.
  - `overlayOpacity`: 드로잉 요소 투명도(0.0 ~ 1.0).
  - `centerOffsetMm`: 이미지 중심 대비 사용자 정의 중심 오프셋(mm).
  - `pixelSizeX`, `pixelSizeY`: 이미지 좌표 → mm 변환 계수.
- **포인트(Points)**
  - 드로잉용 픽셀 좌표와 mm 좌표 변환을 지원한다.
  - WMX3 전송 시 중심 보정이 적용된 mm 좌표로 내보낸다.
- **폴리라인(Lines)**
  - 좌표 목록과 함께 `useCustomSpeed`, `speedMmPerSec`, `accelerationMmPerSec2`, `decelerationMmPerSec2`, `dispenseEnabled`, `useAutoSmoothing`, `openTimeMs`, `closeTimeMs`, `numOfPulse`를 포함한다.
  - 시작점 선택 시 관련 파라미터를 우측 패널에서 편집한다.
- **통합 레시피**
  - 여러 도포 레시피의 조합으로 구성하며, PathInterpolation 전송용 세그먼트를 생성할 때 mm 좌표와 속도/도포 정보를 함께 제공한다.
- **JSON 저장**
  - 저장 시 mm 단위 좌표로 직렬화하며, 불러올 때는 픽셀 좌표로 환산하여 UI에 표시한다.

### 4. UI 및 워크플로우
- **레이아웃**: 7:3 비율의 스플릿 패널. 최소 폭을 보장해 좌표와 리스트가 잘리지 않도록 한다.
- **리스트 & 편집 패널**: 레시피 항목 리스트에서 선택 시, 하단 인라인 편집 패널에 속성 입력창이 표시된다. 멀티 선택은 미지원.
- **선택 동기화**: 리스트 선택 상태와 캔버스 강조 표시를 실시간으로 동기화한다.
- **속성 편집**: 단축키는 제공하지 않으며, 모든 편집은 인라인 UI 컨트롤로 수행한다.
- **다국어**: 한국어 UI만 지원한다.

### 5. WMX3 통합 및 검증
- **PathInterpolation 전송 파라미터**: X, Y(mm), 속도, 가속도, 감속도, 도포기 On/Off, 자동 스무딩 플래그.
- **검증 로직**: 속도 제한 초과, 좌표 누락 등 오류를 사전에 검출하고 메시지로 제공한다.
- **직렬화 관리**: JSON 직렬화는 Newtonsoft Json.NET과 System.Text.Json을 혼용 가능하며, 스키마 버전과 속성 변경 이력 관리(자동 저장 포함)를 고려한다.
- **성능 고려**: 세그먼트 수에 제한은 없으나, 리렌더링은 배치 업데이트로 묶어 성능을 확보한다.
- **테스트**: 단위 테스트/통합 테스트는 선택 사항이나, WMX3 전송 전에 “전송 검증(Export Validation)” 절차를 거치는 것을 권장한다.

## English

### 1. Overview
- The dispense path editor overlays a reference image and produces recipes that can be consumed by WMX3 PathInterpolation.
- Coordinate units are **millimetres** and every value is rounded to three decimal places (0.001 mm).
- The default layout keeps a 70/30 split between the drawing canvas and the recipe/property panel.

### 2. Drawing Tool Behaviour
- **Mode driven input**: pointer, add point, add polyline, and pan modes are toggled through explicit buttons.
- **Line/point authoring**: point items remain unconnected while adjacent polyline vertices are connected automatically.
- **Dragging**: every point or vertex is draggable; live updates recompute the millimetre coordinates.
- **Centre handling**: the default centre is the image midpoint. When the user re-centres, on-screen positions stay unchanged while the stored recipe coordinates are recomputed relative to the new centre.
- **Z-order & emphasis**: drawing elements render above the background image and honour a recipe-level opacity. Selected items are rendered with fully opaque highlight colours.
- **Undo/Redo**: adding points, completing polylines, editing properties, or moving the centre is tracked through undo/redo stacks.
- **Assists**: snapping and axis constraints are intentionally omitted. Ctrl + mouse wheel zoom and panning are supported.

### 3. Recipe & Data Model
- **Global settings**
  - `schemaVersion` (`"1.0.0"`), `defaultSpeedMmPerSec`, `speedLimitMmPerSec`, `enforceSpeedLimit`, `overlayOpacity`, `centerOffsetMm`, `pixelSizeX`, `pixelSizeY`.
- **Points**
  - Maintain pixel coordinates for drawing but convert to millimetres on export. Conversion honours the current centre offset.
- **Polylines**
  - Store vertex lists and process attributes such as `useCustomSpeed`, `speedMmPerSec`, `accelerationMmPerSec2`, `decelerationMmPerSec2`, `dispenseEnabled`, `useAutoSmoothing`, `openTimeMs`, `closeTimeMs`, and `numOfPulse`.
  - Selecting the first vertex surfaces its configurable parameters in the property panel.
- **Integrated recipe**
  - Combines multiple dispense recipes and exposes a segment-oriented export structure ready for PathInterpolation.
- **JSON persistence**
  - Persists coordinates as millimetres and restores pixel coordinates on load; schema versioning enables backward compatibility.

### 4. UI & Workflow
- **Layout**: split view (70/30) with minimum width guarantees to keep coordinate tables readable.
- **List & property panel**: single-selection list synchronised with an inline property editor; multi-selection is not available yet.
- **Selection sync**: list selection and canvas highlighting are always in sync.
- **Editing approach**: no keyboard shortcuts; edits happen inline.
- **Localisation**: Korean only, no multilingual resources.

### 5. WMX3 Integration & Validation
- **Required fields**: PathInterpolation expects X/Y (mm), speed, acceleration, deceleration, dispense nozzle enable, and auto-smoothing flags.
- **Validation**: speed limit enforcement and missing coordinate checks run before export and surface actionable messages.
- **Serialisation**: JSON serialisation uses Json.NET/System.Text.Json with schema versioning and optional autosave/version history.
- **Performance**: no hard segment limit; redraw requests are batched to minimise latency.
- **Testing**: automated tests are optional, yet an explicit “export validation” pass before WMX3 transmission is recommended.

