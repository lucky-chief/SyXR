using System.Collections;
using System.Collections.Generic;
using com.unity.shiyantech.xr;
using UnityEngine;

public enum RayProjectionProvider
{
    /// <summary>
    /// 眉心
    /// </summary>
    CenterEye,
    /// <summary>
    /// 左眼
    /// </summary>
    LeftEye,
    /// <summary>
    /// 右眼
    /// </summary>
    RightEye,
    /// <summary>
    /// 固定
    /// </summary>
    Constant
}

/// <summary>
/// 不依赖 Camera 的屏幕坐标转换工具：屏幕像素 → 世界坐标。
/// V / P 的来源由 <see cref="projectionProvider"/> 决定（编辑器取 <see cref="editorProjectionProvider"/>，构建里取 <see cref="runtimeProjectionProvider"/>）：
/// Constant 用组件上的固定参数，LeftEye / RightEye 用相机的立体矩阵，
/// CenterEye 由左右眼中点（眉心）与项目设置里的物理屏幕尺寸现算。
/// </summary>
public class ScreenPointToRayHelper : MonoBehaviour
{

    [Header("投影来源")]
    [Tooltip("Constant：用本组件上的固定参数；LeftEye / RightEye：取相机的左右眼立体矩阵；CenterEye：取左右眼中点（眉心）并按项目设置里的屏幕尺寸现算。")]
    public RayProjectionProvider editorProjectionProvider = RayProjectionProvider.Constant;
	[Tooltip("Constant：用本组件上的固定参数；LeftEye / RightEye：取相机的左右眼立体矩阵；CenterEye：取左右眼中点（眉心）并按项目设置里的屏幕尺寸现算。")]
	public RayProjectionProvider runtimeProjectionProvider = RayProjectionProvider.CenterEye;

	[Tooltip("提供左右眼立体矩阵的相机；留空时取 Camera.main。")]
    [SerializeField]
    Camera m_EyeCamera;

    /// <summary>
    /// 当前环境生效的投影来源：编辑器下（含 Play 模式）用 <see cref="editorProjectionProvider"/>，
    /// 构建运行时用 <see cref="runtimeProjectionProvider"/>。
    /// </summary>
    public RayProjectionProvider projectionProvider
    {
        get
        {
#if UNITY_EDITOR
            return editorProjectionProvider;
#else
            return runtimeProjectionProvider;
#endif
        }
    }

    /// <summary>屏幕像素空间：取自项目设置里的“单 view 像素宽高”，像素 → NDC 与鼠标映射都用它。</summary>
    public int screenWidth => ShiyanXRProjectSettings.Instance.ViewPixelWidth;
    public int screenHeight => ShiyanXRProjectSettings.Instance.ViewPixelHeight;
    public float zOffset = 1f;
    public float nearClip = 0.1f;
    public float farClip = 1000f;

    private Matrix4x4 viewMatrix;
    private Matrix4x4 projectionMatrix;
    private bool m_LoggedMissingCamera;

    /// <summary>
    /// 当前 provider 下的视点（眼睛）世界位置，也就是 <see cref="ScreenToWorldPoint"/> 里射线的起点。
    /// </summary>
    public Vector3 viewPosition { get; private set; }

    /// <summary>
    /// 当前 provider 下的 view 矩阵，由 <see cref="RefreshMatrices"/> 刷新。
    /// </summary>
    public Matrix4x4 currentViewMatrix => viewMatrix;

    /// <summary>
    /// 当前 provider 下的 projection 矩阵，由 <see cref="RefreshMatrices"/> 刷新。
    /// </summary>
    public Matrix4x4 currentProjectionMatrix => projectionMatrix;

    /// <summary>
    /// 按当前 provider 重算 V / P 与视点。取点前都会刷新，改完参数不会用到旧矩阵。
    /// </summary>
    public void RefreshMatrices()
    {
        switch (projectionProvider)
        {
            case RayProjectionProvider.LeftEye:
                if (TrySetStereoMatrices(Camera.StereoscopicEye.Left)) return;
                break;
            case RayProjectionProvider.RightEye:
                if (TrySetStereoMatrices(Camera.StereoscopicEye.Right)) return;
                break;
            case RayProjectionProvider.CenterEye:
                if (TrySetCenterEyeMatrices()) return;
                break;
        }

        // 兜底与 Constant：视点在屏幕中心前方 zOffset 处，
        // 视锥在 |zOffset| 处的截面正好等于物理屏幕尺寸，也就是屏幕所在的平面
        viewPosition = transform.position + transform.forward * zOffset;
        viewMatrix = CreateViewMatrix(viewPosition, transform.forward, transform.up);
        projectionMatrix = CreatePerspectiveMatrix();
    }

    /// <summary>
    /// 屏幕坐标转世界坐标点
    /// </summary>
    /// <param name="screenPoint">屏幕坐标（像素）</param>
    /// <param name="distance">从眼睛位置出发的距离</param>
    /// <returns>世界坐标点</returns>
    public Vector3 ScreenToWorldPoint(Vector3 screenPoint, float distance)
    {
        RefreshMatrices();

        // 屏幕坐标 → 标准化设备坐标 (NDC)，范围 [-1, 1]，-1 为近平面
        float ndcX = (screenPoint.x / screenWidth) * 2f - 1f;
        float ndcY = (screenPoint.y / screenHeight) * 2f - 1f;
        Vector4 ndcPoint = new Vector4(ndcX, ndcY, -1f, 1f);

        // 逆投影矩阵回到观察空间（含透视除法），再经逆视图矩阵回到世界空间
        Vector4 viewSpacePoint = projectionMatrix.inverse * ndcPoint;
        viewSpacePoint /= viewSpacePoint.w;
        Vector4 worldPoint = viewMatrix.inverse * viewSpacePoint;

        Vector3 direction = (worldPoint - new Vector4(viewPosition.x, viewPosition.y, viewPosition.z, 1f)).normalized;
        return viewPosition + direction * distance;
    }

    /// <summary>
    /// 透视投影矩阵构建（Constant 模型：屏幕中心在 transform 处，眼距为 |zOffset|，
    /// 屏幕物理尺寸取项目设置）
    /// </summary>
    public Matrix4x4 CreatePerspectiveMatrix()
    {
        var screenSize = ScreenSizeInMeters;
        float eyeToScreenDistance = Mathf.Max(Mathf.Abs(zOffset), 1e-4f);
        float f = nearClip / eyeToScreenDistance;
        float left = -screenSize.x / 2f * f;
        float right = -left;
        float up = screenSize.y / 2f * f;
        float bottom = -up;
        Matrix4x4 matrix = Matrix4x4.Frustum(left, right, bottom, up, nearClip, farClip);
        return matrix;
    }

    /// <summary>
    /// 视图矩阵构建
    /// </summary>
    public static Matrix4x4 CreateViewMatrix(Vector3 position, Vector3 forward, Vector3 up)
    {
        Vector3 z = -forward.normalized;
        // x 必须取 +right：写成 Cross(up, z) 会得到 -right，屏幕 x 就左右镜像了
        Vector3 x = Vector3.Cross(z, up).normalized;
        Vector3 y = Vector3.Cross(x, z);

        Matrix4x4 matrix = Matrix4x4.identity;
        matrix[0, 0] = x.x;
        matrix[0, 1] = x.y;
        matrix[0, 2] = x.z;
        matrix[0, 3] = -Vector3.Dot(x, position);

        matrix[1, 0] = y.x;
        matrix[1, 1] = y.y;
        matrix[1, 2] = y.z;
        matrix[1, 3] = -Vector3.Dot(y, position);

        matrix[2, 0] = z.x;
        matrix[2, 1] = z.y;
        matrix[2, 2] = z.z;
        matrix[2, 3] = -Vector3.Dot(z, position);

        matrix[3, 3] = 1f;

        return matrix;
    }

    /// <summary>
    /// 左眼 / 右眼：直接采用相机给出的立体矩阵，V 的原点就是该眼的世界位置。
    /// </summary>
    bool TrySetStereoMatrices(Camera.StereoscopicEye eye)
    {
        Camera eyeCamera = ResolveEyeCamera();
        if (eyeCamera == null) return false;

        viewMatrix = eyeCamera.GetStereoViewMatrix(eye);
        projectionMatrix = eyeCamera.GetStereoProjectionMatrix(eye);
        viewPosition = GetEyePosition(viewMatrix);
        return true;
    }

    /// <summary>
    /// 眉心：眼位取左右眼矩阵原点的中点，投影矩阵按“屏幕中心 + 项目设置里的物理屏幕尺寸”相对该眼位现算。
    /// 屏幕中心取 transform.position、屏幕朝向取 transform.forward，眼到屏幕的距离沿用 |zOffset|；
    /// 眉心落在屏幕中心轴上时就是对称视锥（与 Constant 的公式等价），偏轴时视锥随之变成非对称。
    /// </summary>
    bool TrySetCenterEyeMatrices()
    {
        Camera eyeCamera = ResolveEyeCamera();
        if (eyeCamera == null) return false;

        Vector3 leftEye = GetEyePosition(eyeCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left));
        Vector3 rightEye = GetEyePosition(eyeCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right));
        Vector3 centerEye = (leftEye + rightEye) * 0.5f;

        Vector3 screenCenter = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        // 只取眉心相对屏幕中心轴的横向偏移：屏幕距离由 zOffset 决定，不用眉心到屏幕平面的实测距离
        float offsetX = Vector3.Dot(centerEye - screenCenter, right);
        float offsetY = Vector3.Dot(centerEye - screenCenter, up);
        viewPosition = screenCenter + forward * zOffset + right * offsetX + up * offsetY;

        viewMatrix = CreateViewMatrix(viewPosition, forward, up);

        var screenSize = ScreenSizeInMeters;
        float eyeToScreenDistance = Mathf.Max(Mathf.Abs(zOffset), 1e-4f);
        float scale = nearClip / eyeToScreenDistance;
        float halfWidth = screenSize.x * 0.5f;
        float halfHeight = screenSize.y * 0.5f;
        projectionMatrix = Matrix4x4.Frustum(
            (-halfWidth - offsetX) * scale,
            (halfWidth - offsetX) * scale,
            (-halfHeight - offsetY) * scale,
            (halfHeight - offsetY) * scale,
            nearClip, farClip);
        return true;
    }

    /// <summary>
    /// 屏幕物理尺寸（米）：统一取自项目设置 Assets/Resources/ShiyanXR/ShiyanXRProjectSettings.asset，
    /// 本组件不再单独配置。
    /// </summary>
    static Vector2 ScreenSizeInMeters => new Vector2(
        ShiyanXRProjectSettings.Instance.ScreenPhysicalWidth,
        ShiyanXRProjectSettings.Instance.ScreenPhysicalHeight);

    /// <summary>取立体矩阵用的相机：优先用线上面指定的那台，否则退到 Camera.main。</summary>
    Camera ResolveEyeCamera()
    {
        if (m_EyeCamera != null) return m_EyeCamera;

        Camera mainCamera = Camera.main;
        if (mainCamera != null) return mainCamera;

        if (!m_LoggedMissingCamera)
        {
            m_LoggedMissingCamera = true;
            Debug.LogWarning($"[{nameof(ScreenPointToRayHelper)}] 场景里找不到可用于取立体矩阵的相机，暂时回退到 Constant 模型。", this);
        }

        return null;
    }

    /// <summary>眼位：眼矩阵原点的世界位置。</summary>
    static Vector3 GetEyePosition(Matrix4x4 eyeViewMatrix)
    {
        return eyeViewMatrix.inverse.MultiplyPoint3x4(Vector3.zero);
    }
}
