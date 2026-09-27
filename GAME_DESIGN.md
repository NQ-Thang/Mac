# GAME DESIGN DOCUMENT --- MẶC

### 2D Action Platformer / Metroidvania

> **Mục đích:** Tài liệu mô tả thiết kế gameplay, cơ chế, phạm vi demo
> và trải nghiệm người chơi.
>
> **Quy tắc:** Đây là tài liệu thiết kế. Những mục chưa được chốt sẽ
> được đánh dấu `[CHƯA CHỐT]`. AI không được tự biến đề xuất thành quyết
> định chính thức.

------------------------------------------------------------------------

# 1. Tổng quan game

## 1.1. Thể loại

-   2D Action Platformer
-   Có định hướng Metroidvania.
-   Gameplay tập trung vào:
    -   Di chuyển.
    -   Cận chiến.
    -   Ném kiếm.
    -   Dash tới kiếm.
    -   Tận dụng kiếm để di chuyển trong môi trường.
    -   Khám phá và vượt qua các khu vực.

## 1.2. Nhân vật chính

**Mặc**

-   Tên tiếng Anh: **Noir**
-   Mặc và Noir là cùng một nhân vật.
-   Nhân vật có hình dáng linh hồn mèo nhỏ.
-   Có áo choàng.
-   Có đuôi.
-   Cơ thể có liên quan đến Void.

> **Lưu ý:** Không tự đặt tên cho thanh kiếm của Mặc.

------------------------------------------------------------------------

# 2. Triết lý gameplay

Gameplay nên tạo cảm giác:

-   Nhanh.
-   Linh hoạt.
-   Có sự kết hợp giữa combat và movement.
-   Người chơi có thể dùng kiếm vừa để tấn công vừa để di chuyển.
-   Các cơ chế nên hỗ trợ lẫn nhau thay vì tồn tại tách biệt.

## Core Loop

``` text
Di chuyển
   ↓
Tấn công enemy
   ↓
Tạo Anima
   ↓
Sử dụng Anima
   ↓
Hồi phục / tiếp tục chiến đấu
```

Với cơ chế kiếm:

``` text
Ném kiếm
   ↓
Kiếm cắm vào vị trí / wall / Xuyên qua enemy
   ↓
Dash tới kiếm
   ↓
Tận dụng vị trí mới
   ↓
Tiếp tục di chuyển / chiến đấu
```

------------------------------------------------------------------------

# 3. Điều khiển

| Input          | Chức năng                       |
|----------------|---------------------------------|
| A / D / Arrow  | Di chuyển                       |
| Space          | Nhảy                            |
| Chuột trái     | Tấn công cận chiến              |
| Chuột phải     | Ném kiếm / Dash tới kiếm        |
| Giữ S          | Focus Heal                      |
| R              | Recall kiếm                     |

> **Lưu ý:** Đây là control scheme hiện tại. Thay đổi sau này cần được xác nhận.

------------------------------------------------------------------------

# 4. Player Movement

## 4.1. Di chuyển cơ bản

Mặc có khả năng:

-   Đi trái / phải.
-   Nhảy.
-   Điều chỉnh độ cao cú nhảy.
-   Wall Slide.
-   Wall Jump.

## 4.2. Jump

Các cơ chế hỗ trợ:

-   Jump Buffer.
-   Coyote Time.
-   Variable Jump Height.
-   Apex Hang Time.
-   Custom gravity khi rơi.

Mục tiêu là tạo cảm giác điều khiển responsive thay vì chuyển động cứng.

## 4.3. Wall Movement

Mặc có:

-   Wall Slide.
-   Wall Jump.

Wall movement là một phần của bộ movement hiện tại.

------------------------------------------------------------------------

# 5. Combat

## 5.1. Melee Attack

-   Tấn công bằng chuột trái.
-   Hướng đánh dựa trên vị trí chuột.
-   Có thể đánh enemy trong phạm vi gần.
-   Damage hiện tại: `20 HP`.

## 5.2. Anima

Anima là tài nguyên được tạo ra thông qua combat.

### Quy tắc hiện tại

-   Max Anima: `100`.
-   Melee hit enemy: `+20 Anima`.

### Mục đích

Anima được sử dụng cho Focus Heal.

> Các kỹ năng khác tiêu thụ Anima: `[CHƯA CHỐT]`

------------------------------------------------------------------------

# 6. Focus Heal

Focus Heal là cơ chế hồi máu dựa trên Anima.

## Cách sử dụng

``` text
Đứng yên trên mặt đất
        ↓
Giữ S
        ↓
Chờ 1 giây
        ↓
Tiêu thụ 33 Anima
        ↓
Hồi 20 HP
```

## Heal bị hủy khi

-   Player di chuyển.
-   Thả phím S.
-   Player nhận damage.

------------------------------------------------------------------------

# 7. Cơ chế kiếm

Đây là một trong những cơ chế gameplay quan trọng nhất của game.

## 7.1. Trạng thái chờ

Khi chưa ném:

-   Kiếm bay/lơ lửng bên cạnh Mặc.
-   Không giữ một khoảng cách cố định.
-   Có chuyển động nhẹ tự nhiên.
-   Có thể hơi gần / hơi xa Mặc.
-   Có thể hơi cao / hơi thấp.
-   Kiếm luôn giữ tư thế **dọc**.
-   Không tự xoay theo chuột khi đang chờ.

> Đây là quyết định thiết kế quan trọng. Không tự thay đổi hành vi này.

------------------------------------------------------------------------

# 8. Throw Sword

## Cách sử dụng

Nhấn chuột phải lần đầu.

Khi ném:

1.  Lấy vị trí chuột tại thời điểm ném.
2.  Kiếm lập tức xoay theo hướng chuột.
3.  Kiếm bay theo hướng đó.
4.  Trong khi bay, kiếm xoay.

### Thông số hiện tại

| Thuộc tính          | Giá trị        |
|---------------------|----------------|
| Flying Speed        | `30`           |
| Max Fly Distance    | `8m`           |
| Damage              | `30 HP`        |

------------------------------------------------------------------------

# 9. Kiếm va chạm

Kiếm có thể:

### Đâm xuyên enemy

-   Có thể pierce enemy.
-   Không nhất thiết dừng lại ngay khi đánh trúng enemy.

### Cắm vào enemy

Một số loại enemy có thể tương tác đặc biệt với kiếm.

### Cắm vào tường / môi trường

-   Kiếm có thể dừng tại wall.
-   Kiếm có thể ở trạng thái stuck.
-   Player có thể recall kiếm bằng `R`.

### Khi đạt giới hạn

Nếu không va chạm trước:

``` text
Đạt Max Range
      ↓
Kiếm dừng
      ↓
Bắt đầu quay về
```

------------------------------------------------------------------------

# 10. Dash To Sword

Khi kiếm đang tồn tại trong thế giới:

``` text
Chuột phải lần 1
      ↓
Ném kiếm
      ↓
Chuột phải lần 2
      ↓
Dash tới kiếm
```

## Dash

-   Dash trực tiếp tới vị trí kiếm.
-   Dash Speed hiện tại: `30`.
-   Player có i-frame trong quá trình dash.
-   Player có thể gây damage cho enemy trên đường dash.
-   Damage hiện tại: `20 HP`.

Dash là một cơ chế movement quan trọng, không chỉ là combat skill.

------------------------------------------------------------------------

# 11. Recall

Nhấn `R` để gọi kiếm trở lại.

Ngoài ra:

-   Nếu Player đi tới gần kiếm trong phạm vi `1m`, kiếm có thể tự được
    thu hồi / nhặt lại.

------------------------------------------------------------------------

# 12. Anchor Point Enemy

Một loại enemy / object đặc biệt có thể trở thành điểm neo.

## Flow

``` text
Ném kiếm
    ↓
Kiếm cắm vào Anchor
    ↓
Dash tới Anchor
    ↓
Anchor bị tiêu diệt
    ↓
Player bật lên
```

Bounce hiện tại:

-   `bounceForceY = 5`

Cơ chế này có thể được dùng để:

-   Vượt địa hình.
-   Tạo route movement.
-   Kết hợp Throw → Dash → Bounce.

------------------------------------------------------------------------

# 13. Enemy
`[CHƯA CHỐT]`


# 14. Level Design

Game định hướng Metroidvania, vì vậy level có thể được xây dựng xoay
quanh:

-   Platforming.
-   Combat.
-   Khám phá.
-   Những vị trí cần sử dụng sword mobility.
-   Các route khác nhau.

## Các thành phần chưa chốt

-   Biome: `[CHƯA CHỐT]`
-   Tileset: `[CHƯA CHỐT]`
-   Hazard: `[CHƯA CHỐT]`
-   Room transition: `[CHƯA CHỐT]`
-   Checkpoint: `[CHƯA CHỐT]`
-   Map structure: `[CHƯA CHỐT]`

------------------------------------------------------------------------

# 15. Demo

> **Trạng thái:** `[CHƯA CHỐT HOÀN TOÀN]`

Demo dự kiến tập trung vào việc cho người chơi trải nghiệm:

1.  Movement.
2.  Melee attack.
3.  Throw sword.
4.  Recall sword.
5.  Dash tới sword.
6.  Kết hợp sword mobility với level.

## Boss

`[CHƯA CHỐT]`

Demo có thể không cần boss nếu scope không phù hợp.

## Thời lượng

`[CHƯA CHỐT]`

## Số lượng enemy

`[CHƯA CHỐT]`

## Số lượng room

`[CHƯA CHỐT]`

------------------------------------------------------------------------

# 16. Animation

## Nguyên tắc hiện tại

Không ưu tiên xây dựng một hệ thống animation nhân vật phức tạp cho
demo.

Character animation được đơn giản hóa để tập trung vào:

-   Gameplay.
-   Void.
-   Sword mechanic.
-   Level.
-   VFX.

### Animation hiện có / dự kiến

-   Tail animation.
-   Các animation khác: `[CHƯA CHỐT]`

> Không tự mở rộng scope animation nếu chưa được yêu cầu.

------------------------------------------------------------------------

# 17. Void

Void là một thành phần quan trọng trong hình ảnh và narrative của Mặc.

## Vai trò hiện tại

-   Void có thể được dùng để che phần cơ thể của Mặc.
-   Giúp giảm nhu cầu animation cơ thể trong demo.
-   Void có thể trở thành một visual effect quan trọng.

## Thiết kế chi tiết

`[CHƯA CHỐT]`

Ví dụ các vấn đề cần quyết định:

-   Hình dạng.
-   Chuyển động.
-   Shader.
-   Particle.
-   Màu sắc / độ sáng.
-   Cách Void xuất hiện trong gameplay.

> Không tự quyết định visual cuối cùng nếu chưa có art direction.

------------------------------------------------------------------------

# 18. Story / Narrative

Tài liệu này **không phải tài liệu lore chính thức**.

Các chi tiết về:

-   Linh Thụ.
-   Void.
-   Nghịch Quang.
-   Nguồn gốc Mặc.
-   Cốt truyện Part 1.
-   Ending.
-   Dialogue.

→ sẽ được lưu trong `LORE.md` / `STORY.md`.

------------------------------------------------------------------------

# 19. Art Direction

Art direction chi tiết sẽ được lưu riêng trong:

`ART_DIRECTION.md`

Tài liệu này chỉ xác định gameplay cần những asset nào.

Ví dụ:

-   Player.
-   Enemy.
-   Environment.
-   Tileset.
-   Sword VFX.
-   Dash VFX.
-   Void VFX.
-   UI.

------------------------------------------------------------------------

# 20. Ưu tiên trải nghiệm người chơi

Khi thêm hoặc chỉnh sửa mechanic, ưu tiên:

### 1. Cảm giác điều khiển

Player phải phản hồi nhanh và dễ kiểm soát.

### 2. Sword mobility

Throw → Dash → Bounce phải có cảm giác liền mạch.

### 3. Combat

Combat và movement nên có thể hỗ trợ lẫn nhau.

### 4. Readability

Người chơi phải dễ hiểu:

-   Kiếm đang ở đâu.
-   Có thể dash tới đâu.
-   Enemy nào có thể tương tác.
-   Những vật thể nào có thể sử dụng.

### 5. Scope

Không thêm hệ thống chỉ vì "game Metroidvania nên có".

Mọi tính năng mới phải phục vụ trải nghiệm game hoặc mục tiêu demo.

------------------------------------------------------------------------

# 21. Quy tắc cho AI khi làm Gameplay

AI agent cần:

1.  Đọc `PROJECT_CONTEXT.md` trước.
2.  Đọc `GAME_DESIGN.md` khi task liên quan gameplay.
3.  Không tự thay đổi mechanic đã được đánh dấu là quyết định.
4.  Không tự thêm mechanic mới chỉ vì cho rằng nó phù hợp.
5.  Nếu design chưa chốt → hỏi người dùng.
6.  Không tự mở rộng scope demo.
7.  Khi sửa gameplay:
    -   Nêu file sẽ sửa.
    -   Nêu logic sẽ thay đổi.
    -   Giải thích ảnh hưởng tới hệ thống hiện tại.
    -   Sau khi sửa, hướng dẫn cách test.

------------------------------------------------------------------------

# 22. Những thứ cần tiếp tục quyết định

Danh sách này sẽ được cập nhật trong quá trình phát triển.

-   [ ] Demo scope
-   [ ] Map structure
-   [ ] Biome / environment
-   [ ] Enemy roster
-   [ ] Hazard
-   [ ] Checkpoint
-   [ ] Camera behavior
-   [ ] UI
-   [ ] Audio
-   [ ] VFX
-   [ ] Void visual
-   [ ] Story / lore
-   [ ] Art direction

------------------------------------------------------------------------

> **Ghi chú cuối:** `GAME_DESIGN.md` mô tả game sẽ chơi như thế nào.\
> `PROJECT_CONTEXT.md` mô tả project Unity hiện tại được xây dựng như
> thế nào.\
> `LORE.md` / `STORY.md` mô tả thế giới và câu chuyện.\
> `ART_DIRECTION.md` mô tả game sẽ trông như thế nào.
