using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class InversColorRenderPassFeature : ScriptableRendererFeature
{
    #region pass
    class InversColorRenderPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier src;
        private const string shaderName = "Hidden/InversColor";//测试shader
        private Material mat;

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if(mat == null)
            {
                Shader shader = Shader.Find(shaderName);
                if(shader == null) return;
                mat = CoreUtils.CreateEngineMaterial(shader);
            }
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(mat == null || !renderingData.cameraData.postProcessEnabled) return;
            CommandBuffer cmd = CommandBufferPool.Get(shaderName);//获得一个cmd, 这里就以shadername为名了

            Camera curCam = renderingData.cameraData.camera;//当前相机
            int w = curCam.pixelWidth;
            int h = curCam.pixelHeight;
            int des = Shader.PropertyToID("_Temp1");
            cmd.GetTemporaryRT(des, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

            cmd.Blit(src, des);
            cmd.Blit(des, src, mat, 0);//使用材质渲染

            cmd.ReleaseTemporaryRT(des);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
        public override void OnCameraCleanup(CommandBuffer cmd)
        {

        }
    }
    #endregion

    private InversColorRenderPass pass;
    public RenderPassEvent evt = RenderPassEvent.BeforeRenderingPostProcessing;

    public override void Create()
    {
        pass = new InversColorRenderPass();
        pass.renderPassEvent = evt;
    }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.src = renderer.cameraColorTarget;//设置图像源
        renderer.EnqueuePass(pass);
    }
}


