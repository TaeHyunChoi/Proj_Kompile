# KompileMapEditorWindow 참고 문서

> 이 문서는 `KompileMapEditorWindow.cs` 및 직접 참조하는 소스 파일들을 전량 직접 읽어 작성하였습니다.  
> 추론이 포함된 항목에는 **[추론]** 표기를 합니다.

---

## 1. 파일 개요

| 항목 | 내용 |
|------|------|
| 파일 | `KompileMapEditorWindow.cs` |
| 네임스페이스 | `Kompile.Map.Editor.Tools` |
| 클래스 | `KompileMapEditorWindow : EditorWindow` |
| 메뉴 경로 | `Tools > Map > Map Editor` |
| 컴파일 조건 | `#if UNITY_EDITOR` |
| 역할 | 멀티 아틀라스 팔레트, 스포이드, Focus Mode를 지원하는 통합 맵 에디터 (클래스 summary 원문 기준) |

---

## 2. using 선언 (직접 확인)

```csharp
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using Kompile.Map.Data;
using Kompile.Map.Entity;
using Kompile.Map.Editor.Provider;
```

---

## 3. 열거형

```csharp
private enum EditMode    { None, Paint, Erase, Add, Height, Navi }
private enum SelectionMode { Vertex, Face }
```

- `Navi` 는 열거형에 존재하나, `OnSceneGUI` 의 switch 문에 case 가 없음 → 씬 조작 미구현 상태.

---

## 4. 상태 변수 전체 목록

### 4-1. 편집 제어

| 변수 | 타입 | 초기값 | 설명 |
|------|------|--------|------|
| `_currentMode` | `EditMode` | `None` | 현재 편집 모드 |
| `_currentSelection` | `SelectionMode` | `Vertex` | Height 모드 내 선택 단위 |
| `_isEditingEnabled` | `bool` | `false` | 씬 GUI 이벤트 처리 활성화 여부 |
| `_isAltPressed` | `bool` | `false` | Alt 키 눌림 상태 (스포이드 모드 트리거) |

### 4-2. 레이어 / 타겟

| 변수 | 타입 | 초기값 | 설명 |
|------|------|--------|------|
| `_targetY` | `float` | `0f` | 작업 기준 Y 좌표 (층) |
| `_targetRenderLayer` | `ushort` | `0` | 작업 기준 렌더 레이어 |
| `_focusSelectedLayer` | `bool` | `false` | 대상 레이어 이외를 어둡게 표시하는 모드 |

### 4-3. 아틀라스 / 브러시

| 변수 | 타입 | 설명 |
|------|------|------|
| `_atlasPages` | `List<AtlasPage>` | 로드된 아틀라스 페이지 목록 |
| `_selectedAtlasPageIndex` | `int` | 팔레트 UI에서 선택된 페이지 인덱스 |
| `_brushTopIndex` | `int` | 현재 선택된 Top 텍스처의 GlobalIndex |
| `_brushTopAtlas` | `Texture2D` | 현재 선택된 Top 텍스처가 속한 아틀라스 |
| `_brushSideIndex` | `int` | 현재 선택된 Side 텍스처의 GlobalIndex |
| `_brushSideAtlas` | `Texture2D` | 현재 선택된 Side 텍스처가 속한 아틀라스 |

### 4-4. 타일 관리

| 변수 | 타입 | 설명 |
|------|------|------|
| `_cachedTiles` | `HashSet<EditMapTileComponent>` | 씬 내 전체 타일 캐시 |
| `_lastHoveredTile` | `EditMapTileComponent` | 직전 프레임에 마우스가 호버된 타일 |
| `_samplingRoot` | `EditMapSamplingComponent` | 타일 생성 부모 오브젝트 |
| `_tilePrefab` | `GameObject` | Add 모드에서 생성할 타일 프리팹 |

### 4-5. UI 보조

| 변수 | 타입 | 설명 |
|------|------|------|
| `_mainScrollPos` | `Vector2` | 에디터 윈도우 스크롤 위치 |

---

## 5. 상수

```csharp
private const string ROOT_INPUT_PATH = "Assets/Rcs/Map";
```

아틀라스 텍스처 소스가 위치하는 루트 경로.

---

## 6. PointOffsets (정적 배열, 13개)

13개 정점의 XZ 평면 오프셋 (Vector2). `EditMapMeshUtil.PointCoords` 와 동일한 좌표계.

```
인덱스  (X,     Z)
 0    (0.00, 0.00)   1    (0.50, 0.00)   2    (1.00, 0.00)
 3    (0.25, 0.25)   4    (0.75, 0.25)
 5    (0.00, 0.50)   6    (0.50, 0.50)   7    (1.00, 0.50)
 8    (0.25, 0.75)   9    (0.75, 0.75)
10    (0.00, 1.00)  11    (0.50, 1.00)  12    (1.00, 1.00)
```

씬 GUI에서 타일의 감지 포인트 및 Height 모드 핸들 위치로 사용됨.

---

## 7. AtlasPage 내부 클래스

```csharp
private class AtlasPage
{
    public string   PageName;
    public Texture2D Texture;
    public int[]    GlobalIndices = new int[64]; // 슬롯별 GlobalIndex (-1이면 빈 슬롯)
}
```

- 아틀라스 1장은 **8×8 = 64 슬롯** 구조.
- `GlobalIndices[localIndex]` 값이 `-1`이면 해당 슬롯에 텍스처 없음.

---

## 8. 아틀라스 로딩 시스템

### 8-1. LoadAllAtlases() 흐름

1. `Assets/Rcs/Map` 하위 **폴더** 전체 순회.
2. 각 폴더에서 `MapTextureTable.asset` 로드. 없으면 경고 로그 후 skip.
3. 폴더 내 `.png` 파일 중 파일명이 `"merged-"`로 시작하지 않는 것만 수집.
4. 수집된 파일과 `MapTextureTable.TextureList` 를 **대소문자 무시** 비교 → `GlobalIndex` 획득.
5. 유효한 파일들을 `GetGroupFiles()` 로 그룹핑.

**아틀라스 파일명 규칙:**
```
groupId == 0 → merged-{folderName}.png
groupId  > 0 → merged-{folderName}-{groupId}.png
```

**인덱스 분해 공식:**
```
groupId    = GlobalIndex >> 6      (== GlobalIndex / 64)
localIndex = GlobalIndex & 0b111111 (== GlobalIndex % 64)
```

6. 아틀라스 로드 성공 시 `AtlasPage` 생성:
   - `GlobalIndices` 64개 전부 `-1` 로 초기화.
   - 각 파일의 `localIndex` 위치에 `GlobalIndex` 기록.

7. 초기 브러시 설정: `_brushTopAtlas`, `_brushSideAtlas` 가 null일 때만 첫 번째 페이지로 초기화. 첫 번째 유효 GlobalIndex(`!= -1`) 로 Top/SideIndex 초기화.

### 8-2. FileGroup 구조체 및 GetGroupFiles()

LINQ `GroupBy / OrderBy / ToList` 를 수동 반복문으로 대체. 코드 내 주석에 명시됨.

```csharp
public struct FileGroup
{
    public int GroupId;
    public List<KeyValuePair<int, string>> Files;
}
```

내부 처리 순서:
1. `Dictionary<int, List<...>> buckets` 로 groupId 기준 버킷팅 (`kvp.Key >> 6`).
2. 키 목록 추출 후 `List.Sort()` 로 정렬.
3. 정렬된 순서대로 `FileGroup` 리스트 생성.

---

## 9. 편집 모드별 동작 상세

### 9-1. 공통 - 타일 호버 감지 (OnSceneGUI)

레이캐스트를 사용하지 않고, **GUI 화면 좌표 거리**로 타일을 감지함.

```
허용 거리: 40px (minDistanceToVertex 초기값)
```

각 `EditMapTileComponent` 의 13개 정점에 대해:
1. `EditMapTileOperator.GetPointLocalY(tile, i)` → 정점의 로컬 Y값 계산.
2. `tile.transform.TransformPoint()` 로 월드 좌표 변환.
3. `HandleUtility.WorldToGUIPoint()` 로 GUI 좌표 변환.
4. `Vector2.Distance(guiPoint, e.mousePosition)` 로 거리 비교.

**타일 스킵 조건:**
- `_focusSelectedLayer == true` 이고 `tile.RenderLayer != _targetRenderLayer` 인 경우.
- `|tile.transform.position.y - _targetY| > 0.1f` 인 타일. Alt(스포이드) 모드 포함, 모든 모드에서 동일하게 적용됨.

`Layout`, `Repaint` 이벤트에서는 탐지를 실행하지 않고 `_lastHoveredTile` 을 재사용함.

---

### 9-2. Paint 모드

**하이라이트 색상:** 일반 = 청록(Cyan), 스포이드(Alt) = 마젠타(Magenta)

**일반 페인트 (Alt 없음):**
- `MouseDown` 또는 `MouseDrag`(hotControl 일치) → `EditMapTileOperator.ApplyTextures()` 호출.
- `MouseDown` 시 `GUIUtility.hotControl = controlID` 세팅 → 드래그 중 연속 페인트 가능.
- `MouseUp` 시 `GUIUtility.hotControl = 0`.

**스포이드 (Alt + 클릭):**
- `MouseDown` 시 타일의 `TopTextureIndex`, `SideTextureIndex`, `TopAtlasTexture`, `SideAtlasTexture` 를 브러시에 복사.
- `_brushTopAtlas` 가 속한 아틀라스 페이지를 찾아 `_selectedAtlasPageIndex` 갱신 → 팔레트 UI 자동 동기화.

---

### 9-3. Erase 모드

**하이라이트 색상:** 빨강(Red)

- `MouseDown` → `GUIUtility.hotControl = controlID`, `_cachedTiles.Remove(tile)`, `Undo.DestroyObjectImmediate(tile.gameObject)`.
- `MouseDrag`(hotControl 일치) → 동일하게 타일 삭제.
- `MouseUp` → `GUIUtility.hotControl = 0`.
- Alt 키가 눌린 상태에서는 동작하지 않음 (`e.alt` 체크).

---

### 9-4. Add 모드

**대상 평면:** `Plane(Vector3.up, new Vector3(0, _targetY, 0))` 에 레이캐스트.

**스폰 위치 계산:**
```csharp
spawnPos = new Vector3(
    Mathf.Round(hitPoint.x),
    _targetY,
    Mathf.Round(hitPoint.z)
);
```

**Repaint 시 미리보기:**
- 점유 타일(`_cachedTiles` 내에 동일 위치 타일 존재) → 빨간 큐브.
- 빈 위치 → 초록 큐브.
- `Handles.CubeHandleCap(scale: 0.98f)` + `Handles.DrawWireCube`.

**MouseDown (좌클릭, Alt 없음) 처리:**
1. `Physics.OverlapBox(spawnPos + Vector3.one * 0.5f, Vector3.one * 0.4f).Length == 0` 으로 물리 충돌 확인. 충돌 시 생성 중단.
2. `_samplingRoot` 가 있으면 그 transform 하위에, 없으면 씬 루트에 `PrefabUtility.InstantiatePrefab()`.
3. 생성된 `EditMapTileComponent` 에 `SetRenderLayer(_targetRenderLayer)`, `SetVisualDimmed(false)`, `ApplyTextures`, `RefreshMesh` 적용.
4. `_cachedTiles.Add(comp)`.
5. `Undo.RegisterCreatedObjectUndo(newTile, "Add Tile")`.

---

### 9-5. Height 모드

**타겟 정점 선택:** 타일의 13개 정점 중 마우스 GUI 거리 40px 이내 가장 가까운 것.

**Repaint 시:**
- `DrawCustomGrid(tile.transform.position)` 으로 3x3 그리드 표시.
- `SelectionMode.Vertex` 일 때만 노란 구체(`Handles.SphereHandleCap(scale: 0.12f)`)로 최근접 정점 표시.

**MouseDown (좌클릭, Alt 없음, 유효 정점 있음):**
- `delta = e.shift ? -1 : 1` (Shift: 낮추기, 기본: 높이기).
- `Undo.SetCurrentGroupName("Adjust Height")` + `Undo.GetCurrentGroup()` → 조작 후 `Undo.CollapseUndoOperations(undoGroup)` 으로 Face 모드의 다중 RecordObject 를 단일 Undo 로 병합.

**SelectionMode 분기:**
- `Vertex`: `nearIdx` 한 개에 `ModifyHeightIndex` 직접 호출.
- `Face`: `for (int idx = 0; idx < 13; idx++)` 루프로 전체 13개 정점 수정.

수정은 `EditMapTileOperator.ModifyHeightIndex(tile, idx, delta)` 를 통해 수행.

---

### 9-6. Navi 모드

`EditMode` 열거형에 `Navi` 항목이 존재하나, `OnSceneGUI` 의 switch 문과 `OnGUI` 의 switch 문 어디에도 `case EditMode.Navi:` 가 없음. UI 툴바에는 표시되며 Layer Settings 패널도 표시되지 않음 (`if (_currentMode != EditMode.None && _currentMode != EditMode.Navi)` 조건).

---

## 10. UI 구조 (OnGUI)

```
[Editing ON/OFF 토글 버튼]
─────────────────────────────
View Options (box)
  └ Focus Target Layer 체크박스
─────────────────────────────
EditMode 툴바: None | Paint | Erase | Add | Height | Navi
─────────────────────────────
Layer Settings (box) ← None, Navi 제외한 모드에서 표시
  ├ Target Base Y (float)
  └ Target Render Layer (int, 0~ushort.MaxValue)
─────────────────────────────
모드별 추가 UI:
  Paint  → DrawAtlasPaletteUI()
  Add    → Tile Prefab (ObjectField) + Sampling Root (ObjectField)
  Height → SelectionMode 툴바: Vertex | Face
─────────────────────────────
[Optimize Visible Sides 버튼]
[Bake Map (Combine Meshes) 버튼]
```

---

## 11. 아틀라스 팔레트 UI (DrawAtlasPaletteUI)

**아틀라스 표시 크기:** `Mathf.Min(윈도우 너비 - 30f, 300f)`, 윈도우 중앙 정렬.

**테마 선택 드롭다운:** `string[] pageNames` 를 `for` 루프로 생성 (`$"[{_atlasPages[i].PageName}] Atlas"`).

**슬롯 클릭:**
- 좌클릭 (`e.button == 0`): `_brushTopIndex`, `_brushTopAtlas` 갱신.
- 우클릭 (`e.button == 1`): `_brushSideIndex`, `_brushSideAtlas` 갱신.
- `hoveredGlobalIndex == -1` 이면 클릭 무시.

**슬롯 오버레이 (8×8 그리드 루프에서 직접 그림):**

| 조건 | 오버레이 |
|------|----------|
| `GlobalIndices[localIndex] == -1` | 반투명 검정 (`0,0,0,0.5`) 전체 |
| Top 브러시 슬롯 (현재 아틀라스 일치) | 녹색 (`0,1,0,0.4`) 전체 + `"T"` 라벨 |
| Side 브러시 슬롯 (현재 아틀라스 일치) | 파란 (`0,0.5,1,0.4`) 하단 50% + `"S"` 라벨 |
| 호버 슬롯 (유효 슬롯) | 흰색 반투명 (`1,1,1,0.2`) |

Top 과 Side 브러시가 같은 슬롯인 경우 두 오버레이 모두 표시됨.

---

## 12. 브러시 미리보기 (DrawBrushPreview)

64×64 크기의 어두운 배경(`0.15, 0.15, 0.15`) 위에 UV 슬라이싱으로 텍스처 표시.

**UV 계산:**
```csharp
localIndex = index & 63;       // index % 64 (비트 연산)
col        = localIndex & 7;   // localIndex % 8
row        = localIndex >> 3;  // localIndex / 8
uvX = col * 0.125f;
uvY = 1f - (row + 1) * 0.125f;
uvRect = new Rect(uvX, uvY, 0.125f, 0.125f);
```

---

## 13. Focus Mode

- `_focusSelectedLayer = true`: 씬 내 타일 중 `RenderLayer != _targetRenderLayer` 인 타일에 `SetVisualDimmed(true)` 적용.
- `SetVisualDimmed()` 내부: `_isVisualDimmed` 값이 바뀐 경우에만 `UpdateMaterialProperties()` 호출 (중복 갱신 방지).
- `UpdateMaterialProperties()` 내부: dim 이면 `_Color = (0.2, 0.2, 0.2, 1)`, 아니면 `Color.white`.

**트리거 시점:**
- `OnEnable`: `UpdateTilesFocusState()` 호출.
- `_focusSelectedLayer` 토글 변경: `EditorGUI.EndChangeCheck()` 로 감지 후 호출.
- `_targetRenderLayer` 변경 (`_focusSelectedLayer == true` 인 경우에만): 동일.
- `OnDisable`: `ClearAllTilesFocusState()` (전체 dim 해제).

---

## 14. Undo / Redo 처리

**구독 시점:**
- `OnEnable`: `SceneView.duringSceneGui += OnSceneGUI` + `LoadAllAtlases` + `RefreshTileCache` + `UpdateTilesFocusState`.
- `OnDisable`: `SceneView.duringSceneGui -= OnSceneGUI` + `Undo.undoRedoPerformed -= OnUndoRedo` + `ClearAllTilesFocusState`.

> **⚠️ 코드 확인 사항:** `OnDisable` 에서 `Undo.undoRedoPerformed -= OnUndoRedo` 를 해제하는 코드가 있으나, `OnEnable` 에 `Undo.undoRedoPerformed += OnUndoRedo` 구독 코드가 없음. 코드 주석에 `// [추가] 이벤트 감지 해제` 라고 되어 있어, 구독 추가 코드가 누락된 것으로 보임. 단, **이 부분은 소스 직접 확인 사항이며 동작 여부에 대한 판단은 [추론]** 임.

**OnUndoRedo() 처리 순서:**
1. `RefreshTileCache()` → `_cachedTiles` 재수집 (Undo로 타일이 살아나거나 사라질 수 있으므로).
2. 캐시의 모든 타일에 `EditMapTileOperator.RefreshMesh(tile)` + `tile.UpdateMaterialProperties()`.
3. `UpdateTilesFocusState()` → 포커스 상태 재적용.
4. `SceneView.RepaintAll()` + `Repaint()`.

**각 모드별 Undo 등록 방식:**
- `Add`: `Undo.RegisterCreatedObjectUndo(newTile, "Add Tile")`.
- `Erase`: `Undo.DestroyObjectImmediate(tile.gameObject)` (내부적으로 Undo 등록 포함).
- `Paint`: `EditMapTileOperator.ApplyTextures()` 내부에서 `Undo.RecordObject(tile, "Apply Textures")`.
- `Height (Vertex)`: `Undo.SetCurrentGroupName("Adjust Height")` + `CollapseUndoOperations`.
- `Height (Face)`: 동일, 13개의 `RecordObject` 를 단일 그룹으로 병합.

---

## 15. 씬 그리드 (DrawCustomGrid)

```csharp
private void DrawCustomGrid(Vector3 pos)
{
    float y  = _targetY;
    float sX = Mathf.Floor(pos.x) - 1f;
    float sZ = Mathf.Floor(pos.z) - 1f;
    Handles.color = new Color(1, 1, 1, 0.2f);
    for (float i = -1; i <= 2; i++)
    {
        Handles.DrawLine(new Vector3(sX, y, sZ + i), new Vector3(sX + 3f, y, sZ + i));
        Handles.DrawLine(new Vector3(sX + i, y, sZ), new Vector3(sX + i, y, sZ + 3f));
    }
}
```

3×3 타일 범위 격자를 `_targetY` 높이에 흰색 반투명으로 표시. Add 모드와 Height 모드 Repaint 시 호출됨.

---

## 16. 메쉬 최적화 (ExecuteOptimizeMesh)

1. `_cachedTiles` 를 `Dictionary<Vector2Int, EditMapTileComponent>` 로 변환 (`TryAdd` 사용, 중복 위치 무시).
2. 각 타일에 `Undo.RecordObject(t, "Optimize Side Mesh")` 후 `EditMapTileOperator.OptimizeSides(t, tileMap)` 호출.
3. 완료 후 `Debug.Log` 출력.

`ExecuteOptimizeMesh()` 는 Bake 버튼 클릭 시 `ExecuteBake()` 직전에도 자동 호출됨.

---

## 17. Bake (ExecuteBake)

1. `FindFirstObjectByType<EditMapSamplingComponent>()` 로 루트 찾기. 없으면 즉시 반환.
2. `new EditMapSamplingProvider().Bake()` 호출.
3. 예외 발생 시 `Debug.LogError` 출력.

Bake 버튼에는 `EditorUtility.DisplayDialog("Bake Map", ...)` 확인 다이얼로그가 있음.

---

## 18. 참조 클래스 요약

### 18-1. EditMapTileComponent (`Kompile.Map.Entity`)

파일: `Assets/Script/Map/Entity/EditMapTileComponent.cs`

- `[ExecuteInEditMode]`, `[RequireComponent(MeshFilter, MeshRenderer)]`.
- **직렬화 필드:** `meshFilter`, `meshRenderer`, `renderLayer(ushort)`, `topAtlasTexture`, `sideAtlasTexture`, `topTextureIndex(int)`, `sideTextureIndex(int)`, `heightMask(ulong)`, `heightData(MapTileHeightsData)`.
- **비직렬화:** `_isVisualDimmed(bool)`.
- **공개 프로퍼티:** `MeshFilter`, `MeshRenderer`, `RenderLayer`, `TopTextureIndex`, `SideTextureIndex`, `TopAtlasTexture`, `SideAtlasTexture`, `HeightMask`, `HeightData`.
- `UpdateMaterialProperties()`: `MaterialPropertyBlock` 으로 셰이더 프로퍼티 주입.
  - `_TopUVOffset`, `_TopUVScale`, `_SideUVOffset`, `_SideUVScale` (값: `1/8 = 0.125`).
  - `_IsBaked = 0f` (에디터 프리뷰용).
  - `_TopAtlas`, `_MainTex`, `_BaseMap`, `_SideAtlas`.
  - `_Color`: dim 시 `(0.2, 0.2, 0.2, 1)`, 일반 시 `Color.white`.
- `OnValidate()`: `EditorApplication.delayCall` 으로 1프레임 뒤 `UpdateMaterialProperties()` + `OnEditorDataChanged?.Invoke(this)`.
- `static Action<EditMapTileComponent> OnEditorDataChanged`: 에디터 측에서 구독하는 정적 대리자.

### 18-2. EditMapTileOperator (`Kompile.Map.Editor.Provider`)

파일: `Assets/Script.Editor/Map/Provider/EditMapTileOperator.cs`

- `[InitializeOnLoad]` 정적 클래스.
- 생성자에서 `EditMapTileComponent.OnEditorDataChanged` 구독 → `EditorApplication.delayCall` 으로 `RefreshMesh + UpdateMaterialProperties + SetDirty`.
- `RefreshMesh(tile, neighborHeights)`: `EditMapMeshUtil.GenerateMesh()` 호출. 기존 `"Generated3DBlockMesh"` 이름의 메쉬는 `DestroyImmediate` 후 교체. `MeshCollider` 있으면 함께 갱신.
- `ApplyTextures(tile, tIdx, tAtlas, sIdx, sAtlas)`: `Undo.RecordObject` 후 `System.Reflection` 으로 private 필드 직접 설정. `UpdateMaterialProperties` + `SetDirty`.
- `ModifyHeightIndex(tile, pointIndex, delta)`: `Undo.RecordObject` 후 `data.PointHeights.Clone()` 으로 새 배열 생성 → Undo 호환성 확보. 높이 범위: `-1~8`.
- `OptimizeSides(tile, tileMap)`: 4방향(`down, right, up, left`) 이웃 타일 높이 16바이트(`sbyte[16]`) 수집 → `RefreshMesh(tile, neighborHeights)`.
- `GetPointLocalY(tile, index)`: `height == -1` 이면 `0f`, 아니면 `height * EditMapMeshUtil.HeightStep(0.125f)`.

### 18-3. EditMapSamplingComponent (`Kompile.Map.Entity`)

파일: `Assets/Script/Map/Entity/EditMapSamplingComponent.cs`

- `MonoBehaviour`. 직렬화 필드: `sceneIndex(byte)`. 공개 프로퍼티: `SceneIndex`.
- 타일들의 부모 오브젝트로 사용됨. Bake 시 `GetComponentsInChildren<EditMapTileComponent>()` 의 루트.

### 18-4. MapTextureTable / MapTextureData (`Kompile.Map.Data`)

파일: `Assets/Script/Map/Data/Table/MapTextureTable.cs`

- `MapTextureData`: `[Serializable]` 클래스. 필드: `GlobalIndex(int)`, `TextureName(string)`.
- `MapTextureTable`: `ScriptableObject`. 필드: `List<MapTextureData> TextureList`.
- `GetOrAssignIndex(textureName)`: 대소문자 무시 검색 → 없으면 `maxIndex + 1` 로 신규 발급 + `Undo.RecordObject` + `SetDirty`.

### 18-5. MapTileHeightsData (`Kompile.Map.Data`)

파일: `Assets/Script/Map/Data/Definition/MapTileHeightsData.cs`

- `[Serializable] struct`. 필드: `sbyte[] PointHeights` (13개, 범위 -1~8).
- **-1 의미:** "없음" 또는 "기본" → 메쉬 생성 시 Y=0 바닥으로 처리, 해당 정점 포함 삼각형은 스킵(구멍).
- `EnsureInitialized()`: null 이거나 길이 != 13 이면 -1 로 초기화.
- 인덱서 `this[int index]`: get/set 시 `EnsureInitialized()` 자동 호출.

### 18-6. EditMapMeshUtil (`Kompile.Map.Editor.Utility`)

파일: `Assets/Script.Editor/Map/Utility/EditMapMeshUtil.cs`

- `HeightStep = 0.125f` (층간 고도 차이).
- `PointCoords`: 13개 정점 XZ 좌표 (float2). `KompileMapEditorWindow.PointOffsets` 와 동일 좌표.
- `TriangleIndices`: 16개 삼각형 × 3 = 48 인덱스. 윗면 삼각형 직조.
- `GenerateMesh(data, neighborHeights)`:
  1. 13개 Top 정점 생성 (Y = height * 0.125, height==-1 이면 0).
  2. 윗면 삼각형 직조. -1 정점 포함 삼각형 스킵.
  3. `AddDirectedEdge` 로 노출 엣지(절벽 면) 추출 (공유 엣지 상쇄).
  4. 노출 엣지 → 절벽 Quad 생성 (정점 4개, 삼각형 2개).
     - **외부 절벽 (perimeter 엣지):** `neighborHeights` 기반으로 floor Y 결정. 이웃 타일이 더 높으면 생성 스킵 (`floorY1 = float.MaxValue` 마커).
     - **내부 절벽 (구멍):** 0층 바닥으로 수직 강하.

### 18-7. MapConsts (`Kompile.Map.Data`)

파일: `Assets/Script/Map/Data/Definition/MapConsts.cs`

| 상수 | 값 | 설명 |
|------|----|------|
| `HEIGHT_MASK` | `0b_1111` | 4비트 마스크 |
| `HEIGHT_BITS` | `4` | 정점당 비트 수 |
| `GRID_SIZE` | `64` | 그리드 크기 |
| `TILE_BITS` | `6` | 타일 인덱스 비트 수 |

`heightMask(ulong)`: 13개 정점 × 4비트 = 52비트를 ulong(64비트)에 패킹.

---

## 19. LINQ 사용 현황

**방침:** LINQ / Lambda 미사용. `using System.Linq` 선언 없음.

**수동 구현으로 대체된 모든 위치 (직접 확인):**

| 위치 | 대체 방식 |
|------|-----------|
| `LoadAllAtlases()` — 초기 브러시 인덱스 | `for` 루프로 첫 번째 유효 GlobalIndex 탐색, 없으면 `0` |
| `DrawAtlasPaletteUI()` — 테마 드롭다운 | `for` 루프로 `string[]` 직접 생성 |
| `HandleAddMode()` — 물리 충돌 확인 | `Physics.OverlapBox(...).Length == 0` |
| `HandleHeightMode()` — Face 모드 순회 | `for (int idx = 0; idx < 13; idx++)` |
| `GetGroupFiles()` — 그룹핑/정렬 | Dictionary 버킷팅 + `List.Sort()` (코드 주석 명시) |

---

## 20. 비트 연산 최적화 목록

코드 내 주석에 명시된 최적화:

| 연산 | 수식 대체 | 비트 연산 |
|------|-----------|-----------|
| `index % 64` | `& 0b_0011_1111` 또는 `& 63` | localIndex 계산 |
| `index / 64` | `>> 6` | groupId 계산 |
| `localIndex % 8` | `& 7` | col 계산 |
| `localIndex / 8` | `>> 3` | row 계산 |
| `atlasSize / 8` | `* 0.125f` | cellSize 계산 |

---

## 21. 컨트롤 ID 및 이벤트 소비 패턴

```csharp
int controlID = GUIUtility.GetControlID("KompileMapEditor".GetHashCode(), FocusType.Passive);
if (e.type == EventType.Layout)
{
    HandleUtility.AddDefaultControl(controlID);
}
```

- `AddDefaultControl`: 씬 뷰에서 다른 Unity 기즈모/핸들보다 이 에디터가 우선적으로 이벤트를 처리하도록 등록.
- 드래그 중 타일 조작: `GUIUtility.hotControl == controlID` 조건으로 현재 조작 중인 컨트롤 식별.
- 이벤트 처리 후 반드시 `e.Use()` 호출.
