using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// 把 <see cref="XRRayInteractor"/> 的命中状态可视化，三种状态：命中可交互物体、命中 UI、什么都没命中，
    /// 分别显示 <c>reticleFor3D</c> / <c>reticleForUI</c> / <c>reticleIdle</c>。
    ///
    /// 未命中时的 reticleIdle 放在与 0 视差面（屏幕）平行的平面上，深度沿用上一次交互命中点的深度：
    /// 原来沿射线按固定距离摆放，射线一转就绕交互器扫出一个球面，深度一直在变；改成和屏幕平面求交后，
    /// 不管射线怎么转，idle 都只在同一个景深上移动。一次都没交互过就落在 0 视差面上。
    /// 没有指定 0 视差面时，依次退到场景里的 <see cref="ScreenPointToRayHelper"/> 屏幕、主相机。
    ///
    /// 状态每帧在 <c>LateUpdate</c> 里重新判定，而不是只挂在 hoverEntered / hoverExited 上：
    /// 「什么都没命中」没有事件可听（开局的初始状态就是它，射线从空白处扫回来也不会触发任何事件），
    /// 而且命中点每帧都在动，reticle 本来就得跟着走。XRI 在 Update 阶段刷新命中数据，
    /// 所以 LateUpdate 里读到的就是本帧的射线结果。
    ///
    /// 朝向约定：reticle 的 +Z 轴对准命中面的法线（打到 UI 用画布法线，idle 落到屏幕平面时用屏幕法线），
    /// 按「模型建在 XY 平面、朝 +Z」做的 reticle 会平贴在命中面上。
    ///
    /// 前提：UI 那一路走的是 <c>TryGetCurrentUIRaycastResult</c>，这份数据由 EventSystem 上的
    /// XRUIInputModule 提供（本包 EventSystem.prefab 已带），并且 XRRayInteractor 的
    /// Enable UI Interaction 要打开，否则第二个状态永远不会出现。
    /// </summary>
    [RequireComponent(typeof(XRRayInteractor))]
    public class SyXRInteractorVisual : MonoBehaviour
    {
        [Tooltip("命中可交互物体时显示的模型")]
        [SerializeField] GameObject reticleFor3D;

        [Tooltip("命中 UI 时显示的模型")]
        [SerializeField] GameObject reticleForUI;

        [Tooltip("什么都没命中时显示的模型")]
        [SerializeField] GameObject reticleIdle;

        [Tooltip("reticleForUI 沿命中面法线朝观察者方向的偏移（米）。reticle 和 UI 共面时深度分不出先后，会被 UI 盖住，挪一点就露出来了；太大会出现视差重影")]
        [SerializeField] float depthBias = 0.005f;

        [Tooltip("0视差面（屏幕平面）的 Transform，例如 ShiyanXR-Setup 里代表屏幕的对象。留空时自动用场景里的 ScreenPointToRayHelper，再退到主相机")]
        [SerializeField] Transform zeroParallaxPlane;

        [Tooltip("没有可用参考平面时，reticleIdle 落在射线起点前方多远")]
        [SerializeField] float idleDistance = 2f;

        XRRayInteractor m_Interactor;
        // reticleIdle 的深度参考面：与它平行的平面就是「同一景深」的平面
        Transform m_ScreenPlane;

        // 上一次命中 3D 物体 / UI 的位置与沿射线的距离：前者用来算相对屏幕的深度，后者是没有参考平面时的兜底
        Vector3 m_LastHitPoint;
        float m_LastHitDistance;
        bool m_HasLastHit;

        void Awake()
        {
            m_Interactor = GetComponent<XRRayInteractor>();
            m_ScreenPlane = ResolveScreenPlane();
            HideAll();
        }

        /// <summary>
        /// 取 0 视差面：优先用面板上指定的 Transform；没指定就用场景里的屏幕转换组件（它的 transform 就是屏幕中心与朝向）；
        /// 再没有就退回主相机，至少保证 idle 有一个稳定的景深参考。
        /// </summary>
        Transform ResolveScreenPlane()
        {
            if (zeroParallaxPlane != null)
                return zeroParallaxPlane;

            var helper = FindAnyObjectByType<ScreenPointToRayHelper>();
            if (helper != null)
                return helper.transform;

            var camera = Camera.main;
            return camera != null ? camera.transform : null;
        }

        // 交互器被关掉后 LateUpdate 不再跑，这里收尾一次，免得 reticle 停在被禁用那一帧的位置
        void OnDisable() => HideAll();

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
                RememberHit(point);
                ShowReticleAt(reticleFor3D, point, normal);
            }
            else if (TryGetUIHit(out point, out normal))
            {
                RememberHit(point);
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
        /// 记下这一帧的命中点，供接下来的 reticleIdle 沿用景深。
        /// 存世界坐标（用于算相对屏幕平面的深度）和沿射线的距离（没有参考平面时的兜底）。
        /// </summary>
        void RememberHit(Vector3 point)
        {
            m_Interactor.GetLineOriginAndDirection(out var origin, out var direction);
            m_LastHitDistance = Vector3.Dot(point - origin, direction);
            m_LastHitPoint = point;
            m_HasLastHit = true;
        }

        /// <summary>
        /// 什么都没命中：把 reticleIdle 放在与屏幕平面平行、深度等于上一次命中点的平面上。
        /// 与射线求交而不是 origin + direction * 固定距离，这样转动射线时 idle 只在同一个景深上平移，
        /// 不会绕交互器扫出一个球面。没有参考平面或射线与平面平行时，退回沿射线的固定距离。
        /// </summary>
        void GetIdlePose(out Vector3 point, out Vector3 normal)
        {
            m_Interactor.GetLineOriginAndDirection(out var origin, out var direction);

            if (m_ScreenPlane != null)
            {
                var screenPlane = new Plane(m_ScreenPlane.forward, m_ScreenPlane.position);
                // 偏移平面：法线与屏幕一致，穿过上一次命中的位置；没命中过就落在屏幕平面上
                var idlePlane = m_HasLastHit ? new Plane(screenPlane.normal, m_LastHitPoint) : screenPlane;

                if (idlePlane.Raycast(new Ray(origin, direction), out var planeDistance))
                {
                    // 掠射时交点会跑到很远，收到射线的最大长度上
                    planeDistance = Mathf.Min(planeDistance, m_Interactor.maxRaycastDistance);
                    point = origin + direction * planeDistance;

                    // 法线取朝向观察者的那一面
                    normal = Vector3.Dot(idlePlane.normal, direction) < 0f ? idlePlane.normal : -idlePlane.normal;

                    // 前面若已经有实体挡着（只是它不是可交互物体），reticle 会被它遮住或穿到它后面，所以退到该命中面上
                    if (m_Interactor.TryGetCurrent3DRaycastHit(out var blocker) && blocker.distance < planeDistance)
                    {
                        point = origin + direction * blocker.distance;
                        normal = blocker.normal;
                    }

                    return;
                }
            }

            // 没有参考平面，或射线与平面平行 / 朝向平面背面：退回沿射线的固定距离
            var distance = m_HasLastHit ? m_LastHitDistance : idleDistance;
            normal = -direction;

            if (m_Interactor.TryGetCurrent3DRaycastHit(out var surface) && surface.distance < distance)
            {
                distance = surface.distance;
                normal = surface.normal;
            }

            point = origin + direction * distance;
        }

        /// <summary>只显示 <paramref name="reticle"/> 这个 mesh，另外两个 mesh 都收起来
        /// （都在同一帧内完成，切换时不会两个 reticle 同时出现一帧）。</summary>
        void ShowReticleAt(GameObject reticle, Vector3 point, Vector3 normal)
        {
            HideReticle(reticleFor3D, reticle);
            HideReticle(reticleForUI, reticle);
            HideReticle(reticleIdle, reticle);

            if (reticle == null)
                return;

            reticle.transform.SetPositionAndRotation(point, Quaternion.LookRotation(normal));
            if (!reticle.activeSelf)
                reticle.SetActive(true);
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
        }
    }
}
