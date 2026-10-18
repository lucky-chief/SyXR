using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using com.unity.shiyantech.xr;

namespace com.shiyanxr.editor
{
    /// <summary>
    /// Hierarchy 右键里的两个画布工具：把 world space canvas 摆到当前会聚面（零视差面）上。
    ///
    /// 贴合规则：像素尺寸取项目设置里的单 view 像素宽高，物理尺寸取项目设置里的屏幕物理尺寸，
    /// 两者的差值全部由 canvas 的 scale 吸收；位置与朝向与会聚面完全一致。
    /// 朝向即 Main Camera 的朝向 —— 这也是会聚面的朝向，且画布正面正对相机，文字不会镜像。
    ///
    /// 射线器用 XRI 的 TrackedDeviceGraphicRaycaster 而不是 UI 自带的 GraphicRaycaster：
    /// 前者把 XR 射线 / 手柄的交互事件接进 UI，后者只处理鼠标、触摸这类屏幕输入。
    /// </summary>
    static class ShiyanXRMenuTools
    {
        const string k_MenuRoot = "GameObject/XR/视延XR/";
        const string k_CreateItem = k_MenuRoot + "Create Fitted World Canvas";
        const string k_FitItem = k_MenuRoot + "Fit to Convergence plane";
        const int k_MenuPriority = 50;
        const string k_CanvasName = "Canvas";
        const string k_CreateUndoName = "Create Fitted World Canvas";
        const string k_FitUndoName = "Fit Canvas to Convergence Plane";

        // 预制体路径按包名（package.json 里的 name）拼：Unity 用包名而不是文件夹名挂载包，
        // 所以 Packages/ 下的文件夹叫什么无所谓，只有包改名时才需要改这里。
        const string k_PrefabsFolder = "Packages/com.unity.shiyanxr/Editor/Prefabs/";
        const string k_XROriginPrefabName = "XR Origin (SY)";
        const string k_EventSystemPrefabName = "EventSystem";
        const string k_InteractionsPrefabName = "Interactions";

        static readonly Vector2 k_CenterAnchor = new Vector2(0.5f, 0.5f);

        [MenuItem(k_CreateItem, false, k_MenuPriority)]
        static void CreateFittedWorldCanvas()
        {
            if (!TryResolveConvergencePlane(out var camera, out var settings)) return;

            var canvasObject = new GameObject(k_CanvasName);
            Undo.RegisterCreatedObjectUndo(canvasObject, k_CreateUndoName);

            // WorldSpace 是贴合的前提：位置 / 朝向 / 缩放都要在世界空间里生效
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // CanvasScaler 是工程既有约定（OverlayUI 缺它会报错）；射线器按 XR 交互用 XRI 的实现
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<TrackedDeviceGraphicRaycaster>();

            ApplyFit((RectTransform)canvasObject.transform, camera, settings);

            Selection.activeGameObject = canvasObject;
        }

        [MenuItem(k_FitItem, true, k_MenuPriority)]
        static bool ValidateFitToConvergencePlane()
        {
            foreach (var gameObject in Selection.gameObjects)
            {
                if (IsWorldSpaceCanvas(gameObject)) return true;
            }

            return false;
        }

        [MenuItem(k_FitItem, false, k_MenuPriority)]
        static void FitToConvergencePlane()
        {
            if (!TryResolveConvergencePlane(out var camera, out var settings)) return;

            foreach (var gameObject in Selection.gameObjects)
            {
                if (!IsWorldSpaceCanvas(gameObject)) continue;

                var rectTransform = (RectTransform)gameObject.transform;
                Undo.RecordObject(rectTransform, k_FitUndoName);
                ApplyFit(rectTransform, camera, settings);
            }
        }

        [MenuItem(k_MenuRoot + k_XROriginPrefabName, false, k_MenuPriority)]
        static void PlaceXROrigin() => PlacePrefab(k_XROriginPrefabName);

        [MenuItem(k_MenuRoot + k_EventSystemPrefabName, false, k_MenuPriority)]
        static void PlaceEventSystem() => PlacePrefab(k_EventSystemPrefabName);

        [MenuItem(k_MenuRoot + k_InteractionsPrefabName, false, k_MenuPriority)]
        static void PlaceInteractions() => PlacePrefab(k_InteractionsPrefabName);

        /// <summary>
        /// 用 PrefabUtility.InstantiatePrefab 而不是 Object.Instantiate：前者留下的实例仍与预制体保持链接，
        /// 之后改预制体的改动才推得下来（后者会直接烘成一份没有来源的物体）。
        /// </summary>
        static void PlacePrefab(string prefabName)
        {
            string path = k_PrefabsFolder + prefabName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                Debug.LogWarning("[ShiyanXR] 找不到预制体 " + path + "。路径按包名 com.unity.shiyanxr 拼出，包被改名后需同步更新。");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create " + prefabName);
            Selection.activeGameObject = instance;
        }

        static bool IsWorldSpaceCanvas(GameObject gameObject) =>
            gameObject.TryGetComponent(out Canvas canvas) && canvas.renderMode == RenderMode.WorldSpace;

        /// <summary>贴合只依赖两件外部输入：会聚面所在的相机、以及屏幕尺寸配置。缺一即放弃并说明原因。</summary>
        static bool TryResolveConvergencePlane(out Camera camera, out ShiyanXRProjectSettings settings)
        {
            camera = Camera.main;
            settings = null;

            if (camera == null)
            {
                Debug.LogWarning("[ShiyanXR] 场景里没有 Main Camera（Tag 需为 MainCamera），无法确定会聚面的位置与朝向。");
                return false;
            }

            settings = ShiyanXRProjectSettings.Instance;
            if (settings.ViewPixelWidth <= 0 || settings.ViewPixelHeight <= 0)
            {
                Debug.LogWarning("[ShiyanXR] 项目设置里的单 view 像素宽 / 高必须为正数，否则不能拿它当 canvas 的像素尺寸。");
                return false;
            }

            return true;
        }

        /// <summary>把 canvas 摆到会聚面上：像素尺寸 = 单 view 像素宽高，物理尺寸 = 屏幕物理尺寸（差值由 scale 吸收）。</summary>
        static void ApplyFit(RectTransform rectTransform, Camera camera, ShiyanXRProjectSettings settings)
        {
            // 锚点与轴心都居中：sizeDelta 才是矩形的真实尺寸，矩形也正好以中心落在会聚面中心上
            rectTransform.anchorMin = k_CenterAnchor;
            rectTransform.anchorMax = k_CenterAnchor;
            rectTransform.pivot = k_CenterAnchor;
            rectTransform.sizeDelta = new Vector2(settings.ViewPixelWidth, settings.ViewPixelHeight);

            // 像素尺寸保持不变，物理尺寸靠 scale 贴合；再除以父级缩放，否则父级有缩放时世界尺寸会跟着变
            Vector3 parentScale = rectTransform.parent != null ? rectTransform.parent.lossyScale : Vector3.one;
            rectTransform.localScale = new Vector3(
                LocalScaleFor(settings.ScreenPhysicalWidth, settings.ViewPixelWidth, parentScale.x),
                LocalScaleFor(settings.ScreenPhysicalHeight, settings.ViewPixelHeight, parentScale.y),
                1f);

            rectTransform.position = ShiyanXREyeStereoPreview.ConvergencePlaneCenter(camera);
            rectTransform.rotation = camera.transform.rotation;
        }

        static float LocalScaleFor(float physicalSize, int pixelSize, float parentScale)
        {
            float scale = physicalSize / pixelSize;
            return Mathf.Abs(parentScale) < 1e-6f ? scale : scale / parentScale;
        }
    }
}
