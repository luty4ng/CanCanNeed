# 地面模式模块 (Gravity Modules)

## 📋 **模块概述**

地面模式模块为宇航员控制器提供了在重力环境下的传统第一人称控制功能，包括重力检测、地面移动、跳跃、蹲伏等特性。

## 🏗️ **架构设计**

### **核心设计原则**

1. **模块化设计**: 每个功能独立成模块，便于维护和扩展
2. **事件驱动**: 模块间通过事件系统通信，保持松耦合
3. **状态管理**: 统一的数据容器管理所有状态
4. **平滑过渡**: 支持太空模式和地面模式之间的平滑切换

### **模块组成**

```
Gravity/
├── DetectionModule.cs    # 重力检测组件
├── GravityPhysicsModule.cs      # 重力物理组件
├── GroundMovementModule.cs      # 地面移动组件
├── GravitySource.cs                # 重力源组件
├── GravityPlate.cs                 # 重力板组件
└── README.md                       # 说明文档
```

## 🎯 **模块详解**

### **1. DetectionModule (重力检测组件)**

**职责**: 检测重力场和地面状态

**核心功能**:
- 检测星球重力源 (GravitySource)
- 检测人造重力板 (GravityPlate)
- 地面接触检测
- 重力方向计算
- 状态变化事件触发

**检测逻辑**:
```csharp
// 星球重力检测
Physics.Raycast(position, Vector3.down, out hit, distance, gravityLayers)

// 重力板检测
Physics.OverlapSphere(position, distance, gravityLayers)

// 地面检测
Physics.Raycast(position, gravityDirection, out hit, groundDistance, groundLayers)
```

### **2. GravityPhysicsModule (重力物理组件)**

**职责**: 处理重力应用和物理属性调整

**核心功能**:
- 自定义重力应用
- 物理属性动态调整 (阻力、角阻力)
- 重力过渡处理
- 地面碰撞处理

**物理模式**:
- **太空模式**: 无重力、无阻力
- **空中模式**: 有重力、低阻力
- **地面模式**: 有重力、高阻力

### **3. GroundMovementModule (地面移动组件)**

**职责**: 处理地面移动和交互

**核心功能**:
- 地面行走/奔跑
- 跳跃系统
- 蹲伏系统
- 碰撞体动态调整

**移动模式**:
- **行走**: 基础移动速度
- **奔跑**: 按住Shift加速
- **蹲伏**: 按住Ctrl减速并降低高度

### **4. GravitySource (重力源组件)**

**职责**: 星球表面重力场

**特性**:
- 球形重力场
- 支持平方反比定律
- 可配置重力强度和半径
- 可视化调试

### **5. GravityPlate (重力板组件)**

**职责**: 人造重力场

**特性**:
- 可激活/停用
- 自定义重力方向
- 音效和视觉效果
- 触发器支持

## 🔄 **数据流设计**

```
重力检测 → 状态更新 → 物理调整 → 移动控制 → 事件触发 → UI更新
```

### **状态变化流程**

1. **进入重力场**:
   ```
   检测到重力源 → 更新重力状态 → 开始物理过渡 → 切换控制模式
   ```

2. **着地**:
   ```
   检测到地面 → 更新地面状态 → 调整物理属性 → 启用地面移动
   ```

3. **离开重力场**:
   ```
   离开重力范围 → 清除重力状态 → 恢复太空物理 → 切换控制模式
   ```

## 🎮 **控制映射**

### **地面模式输入**:
- **WASD**: 地面移动
- **Shift**: 奔跑
- **Ctrl**: 蹲伏
- **空格**: 跳跃
- **鼠标**: 视角控制

### **模式切换**:
- **太空 → 地面**: 自动检测重力场
- **地面 → 太空**: 自动离开重力场

## 📊 **事件系统**

### **重力场事件**:
- `OnEnterGravityField`: 进入重力场
- `OnExitGravityField`: 离开重力场
- `OnGravityTransitionStarted`: 重力过渡开始
- `OnGravityTransitionCompleted`: 重力过渡完成

### **地面事件**:
- `OnLandOnGround`: 着地
- `OnLeaveGround`: 离地
- `OnLanded`: 着地完成
- `OnLeftGround`: 离地完成
- `OnGroundMovement`: 地面移动
- `OnJump`: 跳跃
- `OnCrouchStarted`: 蹲伏开始
- `OnCrouchEnded`: 蹲伏结束

## ⚙️ **配置参数**

### **重力检测设置**:
- `gravityDetectionDistance`: 重力检测距离
- `groundCheckDistance`: 地面检测距离
- `gravityLayers`: 重力层掩码
- `groundLayers`: 地面层掩码

### **物理设置**:
- `groundDrag`: 地面阻力
- `groundAngularDrag`: 地面角阻力
- `airDrag`: 空中阻力
- `gravityTransitionTime`: 重力过渡时间

### **移动设置**:
- `walkSpeed`: 行走速度
- `runSpeed`: 奔跑速度
- `crouchSpeed`: 蹲伏速度
- `jumpForce`: 跳跃力度

## 🔧 **使用方法**

### **1. 设置重力源**:
```csharp
// 在星球上添加GravitySource组件
GravitySource gravitySource = planet.AddComponent<GravitySource>();
gravitySource.gravityStrength = 9.8f;
gravitySource.gravityRadius = 100f;
```

### **2. 设置重力板**:
```csharp
// 在空间站添加GravityPlate组件
GravityPlate gravityPlate = station.AddComponent<GravityPlate>();
gravityPlate.gravityDirection = Vector3.down;
gravityPlate.gravityStrength = 9.8f;
```

### **3. 集成到控制器**:
```csharp
// 在AstronautController中添加重力模块
DetectionModule gravityDetection = new DetectionModule(data);
GravityPhysicsModule gravityPhysics = new GravityPhysicsModule(data);
GroundMovementModule groundMovement = new GroundMovementModule(data);

modules.Add(gravityDetection);
modules.Add(gravityPhysics);
modules.Add(groundMovement);
```

## 🎨 **视觉效果**

### **调试可视化**:
- 重力场范围显示
- 重力方向指示器
- 地面检测射线
- 移动方向指示器

### **状态指示**:
- 重力场状态颜色
- 地面接触状态
- 移动模式指示

## 🚀 **扩展性**

### **支持的功能扩展**:
- 多重力源支持
- 重力梯度变化
- 特殊重力效果
- 攀爬系统
- 滑行系统

### **性能优化**:
- 检测频率控制
- 缓存机制
- LOD系统
- 对象池

## 📝 **注意事项**

1. **层设置**: 确保重力源和地面物体在正确的层上
2. **碰撞体**: 重力板需要Collider组件
3. **物理材质**: 地面物体需要适当的物理材质
4. **性能**: 大量重力源可能影响性能，需要优化检测频率

## 🔗 **依赖关系**

- **AstronautData**: 共享数据容器
- **AstronautEvents**: 事件系统
- **IAstronautModule**: 模块接口
- **Unity Physics**: 物理系统

这个地面模式模块为宇航员控制器提供了完整的地面控制功能，支持从太空到地面的无缝切换，同时保持了模块化架构的优势。 