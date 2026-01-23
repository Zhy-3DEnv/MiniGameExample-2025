# 怪物Y轴锁定系统说明

## 问题背景

当同屏有大量怪物（例如2000个）时，怪物会在高度（Y轴）上越叠越多，脱离地面，影响游戏体验。

## 解决方案

使用 **Y轴强制锁定**机制，确保所有怪物始终保持在固定的地面高度。

### 实现方案

1. **EnemyController 锁定Y轴**：
   - 在 `Update()` 中移动时，强制设置 `position.y = groundHeight`
   - 在 `LateUpdate()` 中再次锁定Y轴，确保即使其他系统修改了位置，Y轴也会被锁定

2. **EnemySeparation 只在XZ平面应用分离力**：
   - 分离力计算时，将 `separationVector.y = 0`
   - 确保分离力不会改变怪物的Y轴位置

3. **EnemySpawner 生成时设置Y轴**：
   - 生成怪物时，强制设置 `spawnPosition.y = groundHeight`
   - 确保新生成的怪物也在正确的高度

---

## 设置步骤

### 步骤1：配置 EnemyController（怪物 Prefab）

1. 选中怪物 Prefab
2. 在 `Enemy Controller` 组件中配置：
   - `Ground Height`: **0**（根据你的场景调整，如果地面在Y=0就设为0）
   - `Lock Y Axis`: **勾选**（强制锁定Y轴）

### 步骤2：配置 EnemySpawner

1. 在 GameScene 中，找到 **EnemySpawner** 对象
2. 在 `Enemy Spawner` 组件中配置：
   - `Ground Height`: **0**（应该和 `EnemyController` 的 `Ground Height` 一致）

### 步骤3：验证设置

1. 运行游戏，生成大量怪物
2. 观察怪物是否都在同一高度（不会在Y轴上重叠）
3. 如果还有问题，检查：
   - `EnemyController` 的 `Lock Y Axis` 是否勾选
   - `Ground Height` 是否设置正确

---

## 工作原理

### 1. EnemyController 的 Y 轴锁定

```csharp
// Update() 中：移动时锁定Y轴
Vector3 newPosition = transform.position + movement;
if (lockYAxis)
{
    newPosition.y = groundHeight;
}
transform.position = newPosition;

// LateUpdate() 中：确保Y轴锁定不被覆盖
if (lockYAxis)
{
    Vector3 pos = transform.position;
    pos.y = groundHeight;
    transform.position = pos;
}
```

**为什么需要 LateUpdate？**
- `LateUpdate()` 在所有 `Update()` 之后执行
- 即使其他系统（如 `EnemySeparation`）在 `Update()` 中修改了位置，`LateUpdate()` 也会强制锁定Y轴
- 确保Y轴锁定不会被覆盖

### 2. EnemySeparation 的 XZ 平面约束

```csharp
// 分离力只在XZ平面上应用
separationVector.y = 0f;
Vector3 movement = separationVector * separationForce * updateInterval * 0.5f;
enemy.transform.position += movement;
```

**为什么需要这个？**
- 分离力计算时，可能会因为浮点误差或其他原因产生Y轴分量
- 强制将Y轴设为0，确保分离力不会改变怪物的高度

### 3. EnemySpawner 的生成高度

```csharp
// 生成时强制设置Y轴
basePosition.y = groundHeight;
```

**为什么需要这个？**
- 确保新生成的怪物从一开始就在正确的高度
- 避免生成时Y轴位置不正确

---

## 性能说明

### 性能开销

- **Y轴锁定**：每帧对每个怪物执行一次简单的赋值操作（`pos.y = groundHeight`）
- **性能影响**：几乎可以忽略不计（2000个怪物 = 2000次简单赋值）

### 优化建议

如果性能还是有问题，可以考虑：

1. **降低 LateUpdate 频率**：
   - 不是每帧都执行，而是按间隔执行（例如每0.1秒）
   - 但这样可能会导致Y轴锁定不及时

2. **使用对象池**：
   - 复用怪物对象，减少创建/销毁开销
   - 适合大量怪物场景

3. **LOD 系统**：
   - 距离玩家远的怪物不执行Y轴锁定（如果它们不会重叠）
   - 只对距离玩家近的怪物执行

---

## 常见问题

### Q: 怪物还是会脱离地面？
A: 
- 检查 `EnemyController` 的 `Lock Y Axis` 是否勾选
- 检查 `Ground Height` 是否设置正确（应该和场景的地面高度一致）
- 检查是否有其他脚本在修改怪物的Y轴位置

### Q: 怪物生成时高度不对？
A:
- 检查 `EnemySpawner` 的 `Ground Height` 是否设置正确
- 检查刷怪点的Y轴位置是否正确

### Q: 性能有问题？
A:
- Y轴锁定本身性能开销很小
- 如果性能有问题，可能是其他系统（如分离系统）造成的
- 可以尝试降低 `EnemySeparation` 的更新频率

### Q: 如果场景有不同高度的地面怎么办？
A:
- 可以使用 Raycast 检测地面高度（但会增加性能开销）
- 或者为不同区域设置不同的 `groundHeight`
- 对于俯视角肉鸽游戏，通常地面是平的，使用固定高度即可

---

## 肉鸽游戏海量怪物优化最佳实践

### 1. **2D/俯视角游戏：直接锁定Y轴**
- ✅ 最简单、最有效的方案
- ✅ 性能开销最小
- ✅ 适合2000+怪物同屏

### 2. **使用对象池**
- 复用怪物对象，减少创建/销毁开销
- 适合频繁生成/销毁的场景

### 3. **空间分区优化**
- 只检测附近怪物，不检测所有怪物
- 大幅减少碰撞检测次数

### 4. **分帧处理**
- 每帧只处理一部分怪物
- 避免单帧卡顿

### 5. **LOD 系统**
- 距离远的怪物使用简化逻辑
- 距离近的怪物使用完整逻辑

---

完成设置后，怪物应该不会再在Y轴上重叠，始终保持在固定的地面高度！
