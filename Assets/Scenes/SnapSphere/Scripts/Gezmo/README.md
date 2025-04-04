# 简单变换控制系统

这是一个简单的3D物体变换控制系统，支持触摸和鼠标输入，可以用于移动、旋转和缩放3D物体。

## 功能特点

- 支持触摸和鼠标输入
- 支持移动、旋转和缩放操作
- 支持UI界面切换操作模式
- 支持长按取消选择
- 支持UI元素检测，避免与UI交互冲突

## 使用方法

### 1. 设置预制体

1. 创建一个空物体，命名为"TransformController"
2. 将`SimpleTransformController`脚本添加到该物体上
3. 在TransformController下创建三个子物体：
   - PositionHandles：包含三个箭头，表示X、Y、Z轴的移动
   - RotationHandles：包含三个圆环，表示绕X、Y、Z轴的旋转
   - ScaleHandles：包含三个缩放手柄，表示沿X、Y、Z轴的缩放

### 2. 设置UI界面

1. 创建一个Canvas
2. 在Canvas下创建三个按钮：Position、Rotation、Scale
3. 创建一个空物体，命名为"UIController"
4. 将`TransformUIController`脚本添加到UIController上
5. 在Inspector中设置按钮引用和颜色

### 3. 设置层

1. 创建一个新的层，命名为"Handle"
2. 将所有手柄物体设置为"Handle"层
3. 在SimpleTransformController的Inspector中设置handleLayer和objectLayer

### 4. 操作方式

#### 触摸操作：
- 单指点击物体：选择物体
- 单指拖动箭头：移动物体
- 单指拖动圆环：旋转物体
- 单指拖动缩放手柄：缩放物体
- 长按物体：取消选择

#### 鼠标操作：
- 左键点击物体：选择物体
- 左键拖动箭头：移动物体
- 左键拖动圆环：旋转物体
- 左键拖动缩放手柄：缩放物体
- 右键点击：取消选择

#### 按钮操作：
- 点击Position按钮：切换到移动模式
- 点击Rotation按钮：切换到旋转模式
- 点击Scale按钮：切换到缩放模式

## 注意事项

1. 确保场景中有EventSystem组件，否则UI检测功能将无法工作
2. 确保所有手柄物体都有Collider组件，否则无法检测点击
3. 确保物体在正确的层上，否则无法检测点击
4. 如果需要支持多物体选择，需要修改代码以支持多物体操作 