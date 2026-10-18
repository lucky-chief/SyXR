using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if SHIYANXR_URP_17_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

namespace com.unity.shiyantech.xr
{
    /// <summary>
    /// 眼视图预览的 SBS 呈现 Feature：把外面合成好的双宽贴图贴进相机颜色目标。
    ///
    /// 为什么必须是 RendererFeature：URP 下 Game View 分两段——先把相机渲进它自己的 RT，
    /// 再把那张 RT 画到视图上。在管线之外（CameraEvent / RenderPipelineManager 的相机事件）记的命令
    /// 属于第二段之前，会被写回的结果覆盖，画面看不出变化。放进管线队列并排在
    /// RenderPassEvent.AfterRendering，就早于 URP 自己的 FinalBlitPass（AfterRendering + 1），
    /// 内容会被一起带到屏幕。（这是 URP 14 的排法；URP 17 的顺序见 PresentPass.RecordRenderGraph）
    ///
    /// 两条前提缺一不可，装配见 ShiyanXRSbsFeatureInstaller：
    /// renderer 资产要引用本 Feature；同时要 SetDirty() 让 URP 重建 renderer
    /// （ScriptableRenderer 构造时会把 feature 列表拷一份，光写资产的话运行中的 renderer 不认）。
    ///
    /// URP 14 与 URP 17 的入口不同，两条都实现，由 URP 包版本定义的 SHIYANXR_URP_17_OR_NEWER 来选
    /// （见 Editor asmdef 的 versionDefines；判据取 URP 包版本而不是 Unity 版本 —— 决定 API 形状的是 URP 包）：
    /// URP 14（Unity 2022）重写 Execute；URP 17（Unity 6）起 Execute 已被删除，只剩 RecordRenderGraph，
    /// 相机颜色目标也不再是能直接取的裸句柄，外面那张贴图得先 import 进 Render Graph。
    ///
    /// 本类放在编辑器程序集（Editor/ 的 asmdef 只编编辑器）：这是开发期的预览工具，构建里不该有它。
    /// 构建里本类不存在，renderer 资产里这条引用会反序列化成 null，URP 构造 renderer 时直接跳过
    /// （ScriptableRenderer：<c>if (feature == null) continue;</c>）；真机立体由 OpenXR runtime 接管。
    /// </summary>
    public sealed class ShiyanXRSbsPresentFeature : ScriptableRendererFeature
    {
        /// <summary>待呈现的贴图；null 表示这一帧不呈现（播放器里恒为 null）。</summary>
        public static Texture Source;

        /// <summary>呈现到哪台相机。其余相机（Scene View / Preview 等）不受影响。</summary>
        public static Camera TargetCamera;

        /// <summary>眼图自身渲染期间置位：那两次渲染共用同一台相机，必须跳过呈现。</summary>
        public static bool Suppressed;

        sealed class PresentPass : ScriptableRenderPass
        {
            public PresentPass()
            {
                renderPassEvent = RenderPassEvent.AfterRendering;
            }

#if SHIYANXR_URP_17_OR_NEWER
            /// <summary>
            /// URP 17 只剩 Render Graph 一条路：待呈现的贴图不在这个 Graph 里，得先 import 才认得；
            /// 相机颜色目标也换成了这一帧的活动颜色目标。交给官方的 AddBlitPass，语义与下面的
            /// URP 14 分支一致（整屏贴到相机颜色目标）。
            ///
            /// 顺序与 URP 14 不同：Render Graph 下 URP 先记 FinalBlitPass（它顺带把活动颜色目标
            /// 切到 back buffer），之后才排 AfterRendering 上的自定义 pass —— 所以这里取到的活动
            /// 颜色目标已经是 back buffer，贴上去就是屏幕上的结果。
            /// </summary>
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var source = Source;
                if (source == null) return;

                var destination = frameData.Get<UniversalResourceData>().activeColorTexture;
                if (!destination.IsValid()) return;

                // RTHandles.Alloc 只给贴图套一层句柄，不接管所有权（贴图仍由眼视图预览那边自己释放）；
                // 每帧套一层，与 URP 14 分支每帧一个 CommandBuffer 同理，是小对象，不累积。
                renderGraph.AddBlitPass(renderGraph.ImportTexture(RTHandles.Alloc(source)), destination,
                    Vector2.one, Vector2.zero, passName: "ShiyanXR SBS Present");
            }
#else
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var source = Source;
                if (source == null) return;

                var cmd = new CommandBuffer { name = "ShiyanXR SBS Present" };
                // cameraColorTarget 在 URP 14 里被标了过时，但它是唯一既拿得到相机颜色目标、
                // 又不必把 SRP Core 拉进运行时程序集的入口；语义等同 cameraColorTargetHandle.nameID。
#pragma warning disable 618
                cmd.Blit(source, renderingData.cameraData.renderer.cameraColorTarget);
#pragma warning restore 618
                context.ExecuteCommandBuffer(cmd);
                cmd.Release();
            }
#endif
        }

        PresentPass m_Pass;

        public override void Create()
        {
            m_Pass = new PresentPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (Source == null || Suppressed) return;
            if (renderingData.cameraData.camera != TargetCamera) return;

            renderer.EnqueuePass(m_Pass);
        }
    }
}
