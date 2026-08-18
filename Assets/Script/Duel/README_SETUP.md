# Duel module — cách lắp prototype offline

Module nằm trong namespace `AOG.Duel` và không phụ thuộc vào gameplay đánh quái cũ.

## 1. Tạo Arrow prefab

1. Tạo GameObject `ArrowProjectile`.
2. Thêm `SpriteRenderer`, gán sprite mũi tên, để đầu tên hướng theo trục X dương.
3. Thêm `Rigidbody2D` và một `Collider2D` ôm sát sprite.
4. Thêm `ArrowProjectile`.
5. Kéo GameObject thành prefab.
6. Tạo asset bằng `Create > JS Club > Duel > Projectile Definition`.
7. Gán prefab và chỉnh damage, flight duration, arc height.

## 2. Tạo Player prefab

Component cần có trên GameObject gốc:

- `Rigidbody2D` (Gravity Scale 0, Freeze Rotation Z)
- `Collider2D`
- `DuelCharacter`
- `DuelCharacterMotor`
- `DuelCharacterActionController`
- `DuelAutoAttackController`
- `DuelHealth`
- `DuelCharacterAnimator`
- một `DuelInputSource`: `DuelKeyboardInput`, `DuelMobileInput` hoặc `DuelBotInput`

Hierarchy tối thiểu:

```text
Player
├── VisualRoot (Animator)
│   └── RigRoot
│       └── ... bộ xương ...
│           └── Hand_L
│               └── BowSocket
│                   └── BowRoot
│                       ├── BowSprite
│                       ├── LoadedArrowVisual
│                       └── ArrowSpawnPoint
├── AimTarget
└── Collider
```

Trong `DuelCharacter`, gán:

- Input Source
- Motor
- Action Controller
- Auto Attack Controller
- Health
- Animation Controller
- Arrow Spawn Point
- Aim Target
- Loaded Arrow Visual
- Basic Arrow definition

`AimTarget` nên đứng ngang đầu nhưng không làm con của xương đầu để animation không làm rung đích gameplay.

## 3. Tạo skill

Tạo asset bằng:

`Create > JS Club > Duel > Skills > Projectile Volley`

Chỉnh:

- Action Duration: thời gian khóa skill/dash/khiên khác
- Active Time: thời điểm tạo projectile
- Cooldown: bắt đầu sau khi action kết thúc
- Allow Movement: bật để vẫn chạy trong lúc dùng chiêu
- Projectile Count, delay và Arc Height Step

Kéo tối đa bốn asset skill vào `Equipped Skills` của `DuelCharacterActionController`.

## 4. Tạo trận đấu

1. Tạo `DuelSystems`.
2. Thêm `ProjectilePool` và kéo các projectile definition vào danh sách prewarm.
3. Thêm `GameController`.
4. Gán Player One, Player Two và Projectile Pool.
5. Đặt giới hạn X riêng cho từng player trong `DuelCharacterMotor` nếu hai bên không được vượt sân.

Player 1 mặc định dùng A/D, Space, Left Shift và phím 1–4. Player 2 có thể gắn `DuelBotInput`; hoặc thêm `DuelKeyboardInput` và đổi bộ phím trong Inspector.

## 5. Animator parameters

Tạo các parameter đúng kiểu:

```text
Bool: IsMoving
Float: MoveSpeed
Trigger: AutoAttack
Trigger: Dash
Trigger: Shield
Trigger: Skill1
Trigger: Skill2
Trigger: Skill3
Trigger: Skill4
Trigger: Hit
Trigger: Stun
Trigger: Die
```

Thiếu parameter không làm gameplay lỗi; animation controller sẽ bỏ qua trigger chưa tồn tại.

## 6. UI

- Thanh máu: thêm `DuelHealthBar`, gán `DuelHealth` và `Slider`.
- Cooldown: thêm `DuelCooldownDisplay` vào từng nút và chọn đúng slot.
- Mobile: thêm `DuelMobileButtonRelay` vào từng nút. Nút trái/phải sử dụng pointer down/up; các nút hành động phát lệnh một lần.
- Kết quả/timer: thêm `DuelMatchHUD`, gán match manager, text và result panel.

## Luật đã được mã hóa

- Di chuyển không chiếm khóa hành động.
- Skill, dash, khiên và auto attack khóa lẫn nhau.
- Cooldown bắt đầu sau khi hành động kết thúc.
- Tự bắn chỉ khởi động khi đứng yên và action controller đang Ready.
- Projectile khóa tọa độ AimTarget ở thời điểm bắn rồi bay theo Bézier.
- Khiên chặn projectile, trừ projectile bật Ignore Shield.
- Hết máu hoặc hết giờ sẽ kết thúc trận.
