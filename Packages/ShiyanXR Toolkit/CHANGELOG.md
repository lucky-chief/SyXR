# Changelog

## [0.0.5] 2026.9.1
### 优化开发使用体验
- ShiyanDevicesSimulator 移除参考相机(referenceCamera)相关逻辑,眼位改由 Scene 视图 XYZ 三轴眉心手柄与 Inspector 眉心字段直接调节(含一键重置到 (0,0,0.5))。
- 生成的左右相机改为 HideFlags.DontSave,不再写入场景,构建时自动剔除,无需手动删除。
- 新增构建前自动清理(IPreprocessBuildWithReport),删除旧版本已序列化进构建场景的残留相机;并提供 Tools/ShiyanXR/清理当前场景残留的模拟器相机 菜单。
- Scene 视图新增 XYZ 三轴"眉心"手柄,可定向模拟人物头部移动,实时驱动左右眼位置与投影。

## [0.0.4] 2026.7.10
- 添加composition layer支持。
- 添加自定义 OpenXR UIQuadLayerFeature,用于overlay的UI开发。
- 添加 OverlayUI.cs 及其Editor，提供开发模式和产品模式切换。
- 添加2D UI Sample

## [0.0.3] 2026.4.15
### 优化开发使用体验
- 解决 shiyan virtual screen 和 ShiyanXR-Setup 在开发和build时需要禁用的操作，如今把 shiyan virtual screen去掉，合入 ShiyanXR-Setup，这样在开发的时候能直观看到Side By Side的视图，方便开发。同时打包的时候不会打入shiyan virtual screen的相关逻辑。
- 添加samples，方便学习和理解。

## [0.0.2] 2026.3.10
### 实现功能
- 接入视延眼追
- Unity openxr features 视延科技眼追 开关

## [0.0.1] 2026.1.2

### 实现功能
- 实现shiyanXR 插件基础设置菜单；
- 实现ui的交互；
- 接入和 xr interaction toolkit插件，能使用里面的各种组件