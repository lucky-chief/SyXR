using UnityEngine;
using UnityEngine.InputSystem;

using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// 把 <see cref="XRRayInteractor"/> 的命中状态可视化，三种状态：命中可交互物体、命中 UI、什么都没命中。
    ///
    /// 3D 那一路有两种可视化方式，用 <see cref="ReticleType"/> 切换：<c>MeshReticle</c> 显示
    /// <c>reticleFor3D</c> 模型；<c>LightReticle</c> 改为沿射线在交互点前方架一盏 spot light 把交互点照亮。
    /// UI 和未命中两路固定走 mesh：UI 不吃光照，而未命中时压根没有可照的面。
    ///
    /// 未命中时的 reticleIdle 优先沿用上一次交互的景深（沿射线的距离），而不是直接回到 0 视差面：
    /// 从 3D 物体或 UI 上滑出来那一帧，深度会从交互点突变到 0 视差面，看上去就像闪了一下。
    /// 一次都没交互过才落到 0 视差面上。
    ///
    /// 状态每帧在 <c>LateUpdate</c> 里重新判定，而不是只挂在 hoverEntered / hoverExited 上：
    /// 「什么都没命中」没有事件可听（开局的初始状态就是它，射线从空白处扫回来也不会触发任何事件），
    /// 而且命中点每帧都在动，reticle 本来就得跟着走。XRI 在 Update 阶段刷新命中数据，
    /// 所以 LateUpdate 里读到的就是本帧的射线结果。
    ///
    /// 朝向约定：reticle 的 +Z 轴对准命中面的法线（打到 UI 用画布法线，落到 0 视差面用屏幕法线），
    /// 按「模型建在 XY 平面、朝 +Z」做的 reticle 会平贴在命中面上。
    ///
    /// 前提：UI 那一路走的是 <c>TryGetCurrentUIRaycastResult</c>，这份数据由 EventSystem 上的
    /// XRUIInputModule 提供（本包 EventSystem.prefab 已带），并且 XRRayInteractor 的
    /// Enable UI Interaction 要打开，否则第二个状态永远不会出现。
    /// </summary>
    [RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor))]
    public class SyXRInteractorVisual : MonoBehaviour
    {
        public enum ReticleType
        {
            /// <summary>用具体模型可视化（reticleFor3D）。</summary>
            MeshReticle,

            /// <summary>3D 命中改用 spot light 照亮交互点。</summary>
            LightReticle,
        }

        [Tooltip("3D 命中的可视化方式；UI / 未命中两路固定用 mesh")]
        [SerializeField] ReticleType reticleType = ReticleType.MeshReticle;

        [Tooltip("MeshReticle：命中可交互物体时显示的模型")]
        [SerializeField] GameObject reticleFor3D;

        [Tooltip("命中 UI 时显示的模型（UI 不吃光照，LightReticle 模式下也用它）")]
        [SerializeField] GameObject reticleForUI;

        [Tooltip("什么都没命中时显示的模型")]
        [SerializeField] GameObject reticleIdle;
        [Tooltip("什么都没命中时显示的模型渲染器，材质球需要有一个 _Color 的属性用于修改颜色")]
        [SerializeField] Renderer reticleIdleRenderer;

        [Tooltip("LightReticle：命中可交互物体时用的 spot light")]
        [SerializeField] Light spotLightFor3D;

        [Tooltip("LightReticle：spot light 的颜色循环表，按帧数取模循环。")]
        [SerializeField] Color[] lightColors;

        [Tooltip("LightReticle：spot light 颜色变更触发action")]
        [SerializeField] InputActionProperty lightChangeAction;

        [Tooltip("LightReticle：spot light 沿射线架在交互点前方多远（米）")]
        [SerializeField] float lightDistance = 0.2f;

        [Tooltip("LightReticle：光锥在交互点处的半径（米）。锥角由它和 lightDistance 反算，所以改架设距离不会让光锥跟着变形")]
        [SerializeField] float spotRadius = 0.05f;

        [Tooltip("LightReticle：spot light 的衰减距离。要明显长于 lightDistance，否则交互点落在衰减末端会几乎不亮")]
        [SerializeField] float lightRange = 0.5f;

        [Tooltip("reticleForUI 沿命中面法线朝观察者方向的偏移（米）。reticle 和 UI 共面时深度分不出先后，会被 UI 盖住，挪一点就露出来了；太大会出现视差重影")]
        [SerializeField] float depthBias = 0.005f;

        [Tooltip("0视差面（屏幕平面）的 Transform，例如 ShiyanXR-Setup 里代表屏幕的对象；只在没有可沿用的景深（一次都还没交互过）时才会用到")]
        [SerializeField] Transform zeroParallaxPlane;

        [Tooltip("没有沿用景深、也没有指定 0 视差面时，reticleIdle 落在射线起点前方多远")]
        [SerializeField] float idleDistance = 2f;

        UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor m_Interactor;
        Light m_SpotLight;
        int m_lightColorIndex = 0;

        // lightChangeAction 上一帧的按住状态，用来在电平上自己取上升沿（见 Update）
        bool m_LightChangeWasPressed;
        bool m_WarnedDisabledAction;

        // 上一次命中 3D 物体 / UI 时的景深（沿射线到交互器起点的距离），供接下来的 reticleIdle 沿用
        float m_LastHitDepth;
        bool m_HasLastHitDepth;

        void Awake()
        {
            m_Interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRRayInteractor>();
            m_SpotLight = spotLightFor3D;

            // spotAngle / range 只对 Spot 类型的灯有效，其他类型会让 LightReticle 静默地照成另一种形状
            if (m_SpotLight != null && m_SpotLight.type != LightType.Spot)
                Debug.LogWarning($"{nameof(spotLightFor3D)} 要一盏 Spot 类型的 light，当前是 {m_SpotLight.type}。", this);

            HideAll();
        }

        // 内联 action 的启用权归本脚本（跟 XRI 的 ActionBasedController 一样）；引用模式的启用权归它所在的
        // asset，EnableDirectAction 只在 reference 为空时才动手，所以两种模式都安全。
        void OnEnable() => lightChangeAction.EnableDirectAction();

        // 交互器被关掉后 LateUpdate 不再跑，这里收尾一次，免得 reticle 停在被禁用那一帧的位置
        void OnDisable()
        {
            lightChangeAction.DisableDirectAction();
            HideAll();
        }

        private void Update()
        {
            var action = lightChangeAction.action;

            // 字段没配或引用丢了时 action 是 null，原先的守卫拦不住，会每帧空引用
            if (action == null)
                return;

            // 引用模式的启用权在 action 所在的 asset（或 XRUIInputModule 这类组件）手上，没启用就读不到输入。
            // 只说一次，免得又变成「什么都不发生」
            if (Utils.IsDisabledReferenceAction(lightChangeAction))
            {
                if (!m_WarnedDisabledAction)
                {
                    Debug.LogWarning($"{nameof(lightChangeAction)} 引用的 action 当前处于禁用状态，颜色切换不会触发。", this);
                    m_WarnedDisabledAction = true;
                }
                return;
            }

            // 读 IsPressed() 的电平自己取上升沿，不用 WasPressedThisFrame：后者比的是 update step 计数
            // （pressedInUpdate == InputUpdate.s_UpdateStepCount），而 XRI UI/RightClick 是 PassThrough 类型，
            // 按下、抬起都会各走一轮 Performed，press 语义对它本来就不成立（XRI 读这几个 UI action 也用 IsPressed）。
            var pressed =
                #if !UNITY_EDITOR && UNITY_ANDROID
                    MouseButtonBridge.IsRightPressed();
                #else
                    action.IsPressed();
                #endif
            if (pressed && !m_LightChangeWasPressed && lightColors.Length > 0 && m_SpotLight != null)
            {
                int index = ++m_lightColorIndex % lightColors.Length;
                m_SpotLight.color = lightColors[index];
                reticleIdleRenderer?.sharedMaterial.SetColor("_Color", lightColors[index]);
            }

            m_LightChangeWasPressed = pressed;
        }

        void LateUpdate()
        {
            // 交互器被关掉、或被交互组挡下时，XRI 缓存的命中数据会停在上一帧，
            // 这里直接全收起来，别让 reticle 冻在原地（判法跟 XRI 自己的 line / reticle visual 一致）
            if (m_Interactor == null ||
                !m_Interactor.isActiveAndEnabled ||
                (m_Interactor.disableVisualsWhenBlockedInGroup && m_Interactor.IsBlockedByInteractionWithinGroup()))
            {
                HideAll();
                return;
            }

            if (TryGetInteractableHit(out var point, out var normal))
            {
                RememberHitDepth(point);

                if (reticleType == ReticleType.LightReticle)
                    ShowLightReticle(point);
                else
                    ShowReticleAt(reticleFor3D, point, normal);
            }
            else if (TryGetUIHit(out point, out normal))
            {
                RememberHitDepth(point);
                ShowReticleAt(reticleForUI, point, normal);
            }
            else
            {
                GetIdlePose(out point, out normal);
                ShowReticleAt(reticleIdle, point, normal);
            }
        }

        /// <summary>射线正 hover 着可交互物体：reticle 落在实体命中点上。</summary>
        bool TryGetInteractableHit(out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = default;

            // UI 挡在前面时 XRI 不会 hover 它后面的可交互物体，这里的 isUIHitClosest 只是兜底
            if (!m_Interactor.hasHover ||
                !m_Interactor.TryGetCurrentRaycast(out var raycastHit, out _, out _, out _, out var isUIHitClosest) ||
                isUIHitClosest || !raycastHit.HasValue)
                return false;

            point = raycastHit.Value.point;
            normal = raycastHit.Value.normal;
            return true;
        }

        /// <summary>射线当前打到了 UI：reticle 落在 UI 命中点上。</summary>
        bool TryGetUIHit(out Vector3 point, out Vector3 normal)
        {
            point = default;
            normal = default;

            // 这里只问「当前有没有有效的 UI 命中」，不用 TryGetCurrentRaycast 的 isUIHitClosest。
            // 那个标志位是 XRI 在「UI 命中」和「物理命中」之间比远近（见 XRRayInteractor.CacheRaycastHit）：
            // 它要求命中所在射线段序号 > 0，并且 UI 必须更近；直线射线只有一段，只要后面压着任何碰撞体
            // 就会走到比距离那一步，UI 被判成更远时它是 false——此时 UI 明明打着、照样能交互，
            // 只是拿不到 UI 的 reticle。
            if (!m_Interactor.TryGetCurrentUIRaycastResult(out var hit) || !hit.isValid)
                return false;

            // 画布正面朝观察者，所以这里的 worldNormal 是从观察者看过去的那一面；从画布背面打过去时翻过来，reticle 才贴在背面
            normal = Vector3.Dot(m_Interactor.rayOriginTransform.forward, hit.worldNormal) > 0f ? -hit.worldNormal : hit.worldNormal;

            // 沿法线朝观察者挪出 depthBias，和 UI 拉开一点深度
            point = hit.worldPosition + normal * depthBias;
            return true;
        }

        /// <summary>
        /// 记下这一帧命中点的景深，供接下来的 reticleIdle 沿用。
        /// 存的是沿射线到交互器起点的距离（而不是世界坐标或到眼睛的直线距离），
        /// 这样 idle 时用 origin + direction * depth 摆回去，位置和刚离开时是同一处，深度不会跳。
        /// </summary>
        void RememberHitDepth(Vector3 point)
        {
            m_Interactor.GetLineOriginAndDirection(out var origin, out var direction);
            m_LastHitDepth = Vector3.Dot(point - origin, direction);
            m_HasLastHitDepth = true;
        }

        /// <summary>什么都没命中：优先沿用上一次交互的景深，一次都没交互过才落在 0 视差面上。</summary>
        void GetIdlePose(out Vector3 point, out Vector3 normal)
        {
            m_Interactor.GetLineOriginAndDirection(out var origin, out var direction);

            var distance = idleDistance;
            // 没指定 0 视差面时，让 reticle 正对着交互器
            normal = -direction;

            if (m_HasLastHitDepth)
            {
                // 沿用上一次交互的景深：从 3D 物体 / UI 上滑出来时不会突然跳到 0 视差面
                distance = m_LastHitDepth;
            }
            else if (zeroParallaxPlane != null)
            {
                var plane = new Plane(zeroParallaxPlane.forward, zeroParallaxPlane.position);
                if (plane.Raycast(new Ray(origin, direction), out var planeDistance))
                {
                    distance = planeDistance;
                    // 法线取朝向观察者的那一面
                    normal = Vector3.Dot(plane.normal, direction) < 0f ? plane.normal : -plane.normal;
                }
            }

            // 前面若已经有实体挡着（只是它不是可交互物体），reticle 会被它遮住或穿到它后面，所以退到该命中面上
            if (m_Interactor.TryGetCurrent3DRaycastHit(out var blocker) && blocker.distance < distance)
            {
                distance = blocker.distance;
                normal = blocker.normal;
            }

            point = origin + direction * distance;
        }

        /// <summary>只显示 <paramref name="reticle"/> 这个 mesh，另外两个 mesh 和 spot light 都收起来
        /// （都在同一帧内完成，切换时不会两个 reticle 同时出现一帧）。</summary>
        void ShowReticleAt(GameObject reticle, Vector3 point, Vector3 normal)
        {
            HideReticle(reticleFor3D, reticle);
            HideReticle(reticleForUI, reticle);
            HideReticle(reticleIdle, reticle);
            SetLightActive(false);

            if (reticle == null)
                return;

            reticle.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            if (!reticle.activeSelf)
                reticle.SetActive(true);
        }

        /// <summary>
        /// 光沿射线架在交互点前方，朝射线方向照；锥角由 <c>spotRadius</c> 反算，
        /// 于是交互点处光锥的半径恒为 spotRadius。
        /// </summary>
        void ShowLightReticle(Vector3 point)
        {
            HideReticle(reticleFor3D, null);
            HideReticle(reticleForUI, null);
            HideReticle(reticleIdle, null);

            if (m_SpotLight == null)
                return;

            var distance = Mathf.Max(lightDistance, 1e-4f);

            // 半角 = atan(光锥半径 / 架设距离)，而 Unity 的 spotAngle 是整个锥角
            m_SpotLight.spotAngle = 2f * Mathf.Atan2(spotRadius, distance) * Mathf.Rad2Deg;
            m_SpotLight.range = lightRange;

            // 沿射线退回到交互点前方；forward 取射线方向（从灯指向交互点）
            m_Interactor.GetLineOriginAndDirection(out _, out var direction);

            // 射线正好竖直时 LookRotation 的参考上方向会退化，换一个不共线的
            var up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up;

            m_SpotLight.transform.SetPositionAndRotation(point - direction * distance, Quaternion.LookRotation(direction, up));
            SetLightActive(true);
        }

        void SetLightActive(bool active)
        {
            if (m_SpotLight != null && m_SpotLight.gameObject.activeSelf != active)
                m_SpotLight.gameObject.SetActive(active);
        }

        static void HideReticle(GameObject reticle, GameObject keep)
        {
            if (reticle != null && reticle != keep && reticle.activeSelf)
                reticle.SetActive(false);
        }

        void HideAll()
        {
            HideReticle(reticleFor3D, null);
            HideReticle(reticleForUI, null);
            HideReticle(reticleIdle, null);
            SetLightActive(false);
        }
    }
}
