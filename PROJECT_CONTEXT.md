# PROJECT CONTEXT --- Mặc

### Game 2D Action Platformer / Metroidvania

> **Mục đích:** Tài liệu này cung cấp bối cảnh kỹ thuật và kiến trúc cho
> các AI agent làm việc trên project Unity.
>
> **Ngày audit gần nhất:** 27/09/2026
>
> **Quy tắc cho AI agent:** Không tự suy đoán ngoài những thông tin đã
> được xác nhận bên dưới. Những mục có nhãn `[CHƯA XÁC NHẬN]` cần người
> dùng xác nhận trước khi triển khai quyết định thiết kế.

------------------------------------------------------------------------

# 1. Thông tin project đã xác nhận

## 1.1. Môi trường & Engine

  -----------------------------------------------------------------------
  Thành phần                          Thông tin
  ----------------------------------- -----------------------------------
  Unity                               `6000.0.71f1` --- Unity 6

  Build Target                        `StandaloneWindows64`

  Render Pipeline                     `URP 2D`

  Universal RP                        `17.0.4`

  Renderer                            `Renderer2D`

  Post Processing                     Global Volume Profile, Bloom/Post
                                      Processing đang hoạt động

  Input                               Code hiện tại dùng Legacy
                                      `UnityEngine.Input`

  Unity MCP                           `com.coplaydev.unity-mcp`

  UI                                  `com.unity.ugui 2.0.0`

  2D Feature                          `com.unity.feature.2d 2.0.1`

  Input System package                `com.unity.inputsystem 1.19.0`
  -----------------------------------------------------------------------

> **Lưu ý:** Package Input System đã được cài nhưng code hiện tại vẫn sử
> dụng Legacy Input.

------------------------------------------------------------------------

## 1.2. Cấu trúc thư mục

``` text
Assets/_Project/
│
├── Animations/
│   ├── Enemies/
│   │   └── Votri/
│   │       ├── Sprite.controller
│   │       └── Votri_Walk.anim
│   │
│   └── Player/
│       ├── Tail.controller
│       ├── Tail_Idle.anim
│       ├── noir.png
│       └── tail.png
│
├── Graphics/
│   ├── Enemies/
│   │   ├── hoi_cham.png
│   │   └── votri.png
│   ├── Backgrounds/      # hiện đang trống
│   ├── player/           # hiện đang trống
│   ├── Tilesets/         # hiện đang trống
│   └── UI/               # hiện đang trống
│
├── Prefabs/
│   ├── Characters/
│   │   ├── Player.prefab
│   │   ├── Sword.prefab
│   │   └── FlyingSword.prefab
│   ├── Enemies/
│   │   ├── VoTri.prefab
│   │   └── AnPor.prefab
│   ├── Environment/      # hiện đang trống
│   └── Slippery.physicsMaterial2D
│
├── Scenes/
│   └── SampleScene.unity
│
├── Scripts/
│   ├── Core/
│   │   ├── Entity.cs
│   │   └── Health.cs
│   ├── Enemies/
│   │   ├── IEnemy.cs
│   │   ├── EnemyBase.cs
│   │   ├── VoTri.cs
│   │   ├── AnchorPointEnemy.cs
│   │   └── EnemyDamage.cs
│   ├── Player/
│   │   ├── Player.cs
│   │   ├── PlayerMovement.cs
│   │   ├── PlayerCombat.cs
│   │   ├── PlayerSwordTech.cs
│   │   ├── PlayerHeal.cs
│   │   ├── PlayerAnima.cs
│   │   ├── Sword.cs
│   │   └── FlyingSword.cs
│   └── UI/               # hiện đang trống
│
└── Settings/
    ├── UniversalRP.asset
    ├── Renderer2D.asset
    ├── Lit2DSceneTemplate
    └── URP2DSceneTemplate.unity
```

------------------------------------------------------------------------

## 1.3. Layer & Tag

### Layer đã định nghĩa

    Index Layer
  ------- ------------------
        0 Default
        1 TransparentFX
        2 Ignore Raycast
        3 Player
        4 Water
        5 UI
        6 Ground
        7 Enemy
        8 Wall
        9 PlayerInvincible

### Tag đã định nghĩa

`Untagged`, `Respawn`, `Finish`, `EditorOnly`, `MainCamera`, `Player`,
`GameController`, `Sword`, `Enemy`

------------------------------------------------------------------------

# 2. Kiến trúc & hệ thống hiện tại

## 2.1. Core

### `Entity.cs`

Base class của `Player` và `EnemyBase`.

Quản lý:

-   `Rigidbody2D`
-   `Animator`
-   `SpriteRenderer`
-   `Collider2D`
-   `Health`

Ngoài ra:

-   `IsGrounded()` và `IsTouchingWall()` cache kết quả theo
    `Time.frameCount` để tránh lặp physics query trong cùng một frame.
-   Xử lý hướng nhân vật bằng `transform.Rotate(0, 180, 0)`.

### `Health.cs`

Quản lý:

-   `currentHealth`
-   `maxHealth`
-   `TakeDamage(amount, attackerPos)`
-   `Heal(amount)`
-   Knockback
-   I-Frames

Player sử dụng layer `PlayerInvincible` trong `1.5s` để xử lý trạng thái
bất tử.

Khi Player nhận damage, `PlayerHeal` cũng bị ngắt.

Khi chết: - Enemy → `Destroy()` - Player hiện tại → reset full HP để
phục vụ test.

------------------------------------------------------------------------

# 3. Hệ thống Player

## 3.1. `Player.cs`

Đây là **Component Hub** trung tâm của Player, kế thừa từ `Entity`.

Các chức năng được tách thành những component riêng:

### `PlayerMovement.cs`

-   Variable Jump Height
-   Jump Cut Multiplier: `2.5`
-   Jump Buffer: `0.15s`
-   Coyote Time: `0.12s`
-   Apex Hang Time:
    -   `apexThreshold = 1.5`
    -   `apexGravityMultiplier = 0.5`
    -   `apexBonusSpeedMultiplier = 1.2`
-   Wall Slide:
    -   `wallSlideSpeed = 2.0`
-   Wall Jump
-   Wall Catch Delay: `0.05s`
-   Fall Multiplier: `2.5`
-   Có logic Corner Correction, nhưng cần `headCheckPosition`.

### `PlayerCombat.cs`

-   Tấn công cận chiến bằng chuột trái.
-   Hướng đánh theo vị trí chuột.
-   Damage: `20 HP`
-   Range: `1.0m`
-   Dùng `ContactFilter2D` và mảng `Collider2D[10]` được cache để tránh
    allocation.
-   Đánh trúng enemy sẽ tạo Anima.

### `PlayerAnima.cs`

-   `maxAnima = 100`
-   Mỗi melee hit thành công: `+20 Anima`
-   Có event: `OnAnimaChanged`

Event này có thể được dùng để kết nối UI sau này.

### `PlayerHeal.cs`

Cơ chế Focus Heal:

1.  Player đứng yên trên mặt đất.
2.  Giữ `S`.
3.  Sau `1.0s`, tiêu thụ `33 Anima`.
4.  Hồi `20 HP`.

Bị hủy nếu:

-   Di chuyển.
-   Thả phím.
-   Nhận damage.

------------------------------------------------------------------------

# 4. Cơ chế kiếm

## 4.1. Kiếm ném --- `Sword.cs`

### Ném kiếm

-   Nhấn chuột phải lần đầu.
-   Kiếm bay về phía con trỏ chuột.
-   Tốc độ: `30`
-   Tầm bay tối đa: `8m`
-   Damage: `30 HP`

### Khi va chạm

-   Có thể xuyên qua enemy.
-   Có thể cắm vào ground/wall.
-   Layer ground hiện dùng: `320`.
-   Khi đạt khoảng cách tối đa, kiếm dừng rồi quay về.

### Thu hồi

-   Nhấn `R` để recall thủ công.
-   Nếu Player đi vào phạm vi `1.0m`, kiếm sẽ tự được nhặt lại.

------------------------------------------------------------------------

## 4.2. Dash tới kiếm --- `PlayerSwordTech.cs`

Khi kiếm đang tồn tại:

-   Nhấn chuột phải lần thứ hai.
-   Player dash trực tiếp tới vị trí kiếm.
-   Dash Speed: `30`.

Trong lúc dash:

-   Gây `20 HP` AoE damage cho enemy trên đường đi.
-   Player có trạng thái bất tử khi dash.
-   Sprite Player được ẩn.
-   Khi tới nơi, trạng thái được reset.

------------------------------------------------------------------------

## 4.3. Anchor Point --- `AnchorPointEnemy.cs`

Một enemy đặc biệt có thể được sử dụng như điểm neo:

1.  Ném kiếm vào Anchor.
2.  Kiếm cắm vào Anchor và được `SetParent`.
3.  Dash tới Anchor.
4.  Anchor bị tiêu diệt.
5.  Player được bật lên trên.

`bounceForceY = 5.0`

------------------------------------------------------------------------

## 4.4. Kiếm bay cạnh Player --- `FlyingSword.cs`

Đây là visual companion sword.

-   Bay theo Player bằng `SmoothDamp`.
-   Có chuyển động bobbing bằng `Mathf.Sin`.
-   Tự đổi phía dựa trên vận tốc ngang của Player.

------------------------------------------------------------------------

# 5. Hệ thống Enemy

## 5.1. `EnemyBase.cs`

Base class cho enemy.

Có State Machine:

``` text
Patrol
   ↓
Chase
   ↓
Attack

Surprised
   ↓
Die
```

Đồng thời xử lý target detection.

------------------------------------------------------------------------

## 5.2. `VoTri.cs`

Enemy hiện tại / dummy / wanderer.

Trong scene hiện tại có một dummy với:

-   `100,000 HP`

Mục đích chính là test damage.

### Patrol

VoTri:

-   Di chuyển tuần tra.
-   Dùng raycast để phát hiện tường và mép vực.
-   Đổi hướng khi gặp vật cản hoặc mép vực.

### Khi bị đánh

Chuỗi hành vi:

``` text
Bị đánh
  ↓
Knockback (0.15s)
  ↓
Surprised (0.4s)
  ↓
Hiện dấu ?
  ↓
1.0s
  ↓
Bỏ chạy khỏi Player (3.0s)
  ↓
Quay lại Patrol
```

------------------------------------------------------------------------

## 5.3. `EnemyDamage.cs`

Component gây damage khi enemy tiếp xúc với Player.

Thông số hiện tại:

-   Damage: `20`
-   Cooldown: `1.0s`

**Trạng thái:** Chưa được gắn vào `VoTri.prefab` hoặc enemy nào trong
scene.

→ Vì vậy hiện tại enemy chưa gây contact damage cho Player.

------------------------------------------------------------------------

# 6. Input hiện tại

Project đang dùng Legacy `UnityEngine.Input`.

  Input           Chức năng
  --------------- --------------------
  A / D / Arrow   Di chuyển
  Space           Jump / Wall Jump
  Chuột trái      Melee Attack
  Chuột phải      Throw Sword / Dash
  S               Focus Heal
  R               Recall Sword

------------------------------------------------------------------------

# 7. Camera & UI

## Camera

Hiện tại chỉ có một Camera tĩnh trong `SampleScene`.

-   Position: `(-0.1, 0.05, -10)`
-   Orthographic Size: `7.0`
-   Chưa có Camera Follow.
-   Chưa có Cinemachine.

## UI

Hiện tại project **chưa có**:

-   Canvas
-   HUD
-   HP Bar
-   Anima Bar

------------------------------------------------------------------------

# 8. Các vấn đề đã phát hiện trong audit

## 8.1. Tên prefab Sword / FlyingSword đang bị đảo

Hiện tại:

``` text
FlyingSword.prefab
└── chứa Sword.cs
    → kiếm ném

Sword.prefab
└── chứa FlyingSword.cs
    → kiếm bay cạnh Player
```

Code vẫn hoạt động vì reference đang trỏ đúng Asset GUID.

**Vấn đề:** Tên asset gây nhầm lẫn cho việc phát triển sau này.

------------------------------------------------------------------------

## 8.2. `Health.cs` không lấy được SpriteRenderer của Player

`Health.Start()` đang dùng:

``` csharp
GetComponent<SpriteRenderer>()
```

Nhưng SpriteRenderer của Player nằm ở:

``` text
Player
└── noir
    └── SpriteRenderer
```

Do đó `spriteRenderer` trên Player đang là `null`.

**Ảnh hưởng:** hiệu ứng nhấp nháy sprite khi Player nhận damage không
hoạt động.

------------------------------------------------------------------------

## 8.3. `headCheckPosition` chưa được gán

`PlayerMovement.headCheckPosition` đang `null`.

**Ảnh hưởng:** Corner Correction thoát sớm và hiện không hoạt động trong
gameplay.

------------------------------------------------------------------------

## 8.4. `EnemyDamage.cs` chưa được sử dụng

`EnemyDamage.cs` chưa được gắn vào `VoTri.prefab` hoặc GameObject enemy
nào.

**Ảnh hưởng:** enemy hiện không gây contact damage cho Player.

------------------------------------------------------------------------

## 8.5. Rigidbody2D của VoTri

`VoTri.prefab` hiện có:

``` text
constraints = 0
```

Tuy nhiên `EnemyBase.Start()` sẽ freeze rotation khi chạy game.

→ Runtime vẫn xử lý, nhưng prefab chưa phản ánh trực tiếp trạng thái
này.

------------------------------------------------------------------------

# 9. Các quyết định thiết kế đã xác nhận

Những điều dưới đây đã được xác nhận tại thời điểm audit.

### Gameplay

-   Core gameplay xoay quanh platforming 2D tốc độ cao.
-   Cơ chế di chuyển đặc trưng:

``` text
Throw Sword
      ↓
Dash to Sword
      ↓
Bounce
```

### Anima & Heal

-   Focus Heal lấy cảm hứng từ cơ chế Focus của Hollow Knight.
-   Đánh enemy bằng melee tạo Anima.
-   Anima được sử dụng để hồi máu.

### Kiến trúc code

-   Player sử dụng Component Hub Pattern.
-   `Player.cs` đóng vai trò facade / hub.
-   Chức năng mới nên được tách thành component riêng thay vì làm
    `Player.cs` quá lớn.

### Hiệu năng

Project đang hướng tới Zero-GC runtime:

-   Cache physics arrays.
-   Cache ContactFilter.
-   Tránh allocation trong `Update()` / `FixedUpdate()`.
-   Cache các đối tượng cần thiết thay vì tạo mới liên tục.

------------------------------------------------------------------------

# 10. Những phần CHƯA được xác nhận

> **AI không được tự quyết định các mục này nếu chưa có xác nhận của
> người dùng.**

### Demo

`[CHƯA XÁC NHẬN]`

-   Thời lượng demo.
-   Số lượng room.
-   Điều kiện thắng/thua.
-   Có boss hay không.

### Level Design

`[CHƯA XÁC NHẬN]`

-   Biome.
-   Tileset.
-   Hazard như spike / pit.
-   Cách chuyển room.

### Input

`[CHƯA XÁC NHẬN]`

Có chuyển từ Legacy Input sang New Input System hay không.

### Camera

`[CHƯA XÁC NHẬN]`

-   Cinemachine 2D.
-   Custom smooth follow.
-   Camera confiner.

### Audio

`[CHƯA XÁC NHẬN]`

-   Audio Manager.
-   SFX.
-   BGM.

### Save / Checkpoint

`[CHƯA XÁC NHẬN]`

-   Checkpoint trigger.
-   Respawn persistence.
-   Scene management.

### UI / Art Style

`[CHƯA XÁC NHẬN]`

-   HUD layout.
-   Art assets.
-   Font.
-   HP / Anima gauge.

### VFX

`[CHƯA XÁC NHẬN]`

-   Particle System.
-   Shader Graph.
-   Sprite-based VFX.

------------------------------------------------------------------------

# 11. Quy tắc khi AI chỉnh sửa project

## 11.1. Không tự suy đoán thiết kế

Nếu một vấn đề liên quan đến gameplay, art direction, level design,
story hoặc scope mà tài liệu không xác nhận:

> **Hỏi người dùng trước khi tự quyết định.**

Không biến một đề xuất của AI thành design chính thức.

------------------------------------------------------------------------

## 11.2. Giữ kiến trúc Player

Khi thêm chức năng mới:

``` text
Player.cs
    │
    ├── PlayerMovement.cs
    ├── PlayerCombat.cs
    ├── PlayerSwordTech.cs
    ├── PlayerHeal.cs
    ├── PlayerAnima.cs
    └── [Component mới]
```

Không nhồi toàn bộ logic mới vào `Player.cs` nếu có thể tách thành
component riêng.

------------------------------------------------------------------------

## 11.3. Giữ chuẩn Zero-GC

Không tự ý đưa các allocation lặp lại vào:

-   `Update()`
-   `FixedUpdate()`

Đặc biệt tránh:

-   `new WaitForSeconds` tạo liên tục.
-   `new ContactFilter2D` tạo liên tục.
-   LINQ trong gameplay loop.
-   Physics query không cache khi có thể cache.

------------------------------------------------------------------------

## 11.4. Giữ Layer & Mask

Không tự ý đổi Layer hiện tại.

Đặc biệt:

``` text
Player            = 3
Ground            = 6
Enemy             = 7
Wall              = 8
PlayerInvincible  = 9
```

Ưu tiên LayerMask trong physics query thay vì phụ thuộc vào string tag
khi hệ thống hiện tại đã dùng Layer.

------------------------------------------------------------------------

## 11.5. Không phá Unity MCP

Không xóa hoặc thay đổi cấu hình:

``` text
com.coplaydev.unity-mcp
```

------------------------------------------------------------------------

# 12. Nguyên tắc làm việc với AI Agent

Trước khi thực hiện một task:

1.  Đọc `PROJECT_CONTEXT.md`.
2.  Xác định task đang liên quan đến hệ thống nào.
3.  Chỉ đọc thêm những file cần thiết cho task.
4.  Không sửa các hệ thống không liên quan.
5.  Nếu gặp quyết định thiết kế chưa được xác nhận → hỏi người dùng.
6.  Sau khi sửa → báo rõ:
    -   File nào đã sửa.
    -   Đã thay đổi gì.
    -   Cách test.
    -   Có ảnh hưởng hệ thống nào khác hay không.

> **PROJECT_CONTEXT.md là tài liệu tham chiếu kỹ thuật, không phải tài
> liệu lore/cốt truyện.**
>
> Lore, story, worldbuilding, art direction và game design chi tiết nên
> được lưu trong các tài liệu riêng.
