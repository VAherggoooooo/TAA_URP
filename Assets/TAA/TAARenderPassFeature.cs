using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TAARenderPassFeature : ScriptableRendererFeature
{
    #region member
    TAARenderPass pass;
    public Setting setting = new Setting();
    #endregion

    public override void Create()
    {
        pass = new TAARenderPass(this);
        pass.renderPassEvent = setting.evt;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        pass.src = renderer.cameraColorTarget;
        renderingData.cameraData.camera.ResetProjectionMatrix();
        renderer.EnqueuePass(pass);
    }

    public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
    {
        //Debug.Log("before cull");
    }

    #region class
    class TAARenderPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier src;
        /// <summary>
        /// 长度为9的Halton数列: https://baike.baidu.com/item/Halton%20sequence/16697800
        /// </summary>
        /// <value>长度为9的Halton数列</value>
        private Vector2[] HaltonSequence9 = new Vector2[]
        {
            new Vector2(0.5f, 1.0f / 3f),
            new Vector2(0.25f, 2.0f / 3f),
            new Vector2(0.75f, 1.0f / 9f),
            new Vector2(0.125f, 4.0f / 9f),
            new Vector2(0.625f, 7.0f / 9f),
            new Vector2(0.375f, 2.0f / 9f),
            new Vector2(0.875f, 5.0f / 9f),
            new Vector2(0.0625f, 8.0f / 9f),
            new Vector2(0.5625f, 1.0f / 27f),
        };
        private int index = 0;//当前halton序号
        private TAARenderPassFeature ft;
        private const string shaderName = "Hidden/TAA";
        private Material mat;
        /// <summary>
        /// 上一帧图像
        /// </summary>
        private RenderTexture preRT;
        private Camera curCam;

        public TAARenderPass(TAARenderPassFeature f)
        {
            ft = f;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (mat == null)
            {
                Shader shader = Shader.Find(shaderName);
                if (shader == null) return;
                mat = CoreUtils.CreateEngineMaterial(shader);
            }

            //camera shake
            curCam = renderingData.cameraData.camera;
            curCam.ResetProjectionMatrix();
            Matrix4x4 pm = curCam.projectionMatrix;
            Vector2 jitter = new Vector2((HaltonSequence9[index].x - 0.5f) / curCam.pixelWidth, (HaltonSequence9[index].y - 0.5f) / curCam.pixelHeight);
            jitter *= ft.setting.jitter;
            pm.m02 -= jitter.x * 2;
            pm.m12 -= jitter.y * 2;
            curCam.projectionMatrix = pm;
            index = (index + 1) % 9;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            curCam = renderingData.cameraData.camera;
            if (!renderingData.cameraData.postProcessEnabled) return;
            CommandBuffer cmd = CommandBufferPool.Get(shaderName);

            int w = curCam.pixelWidth;
            int h = curCam.pixelHeight;

            mat.SetFloat("_Blend", ft.setting.blend);

            if (preRT == null || preRT.width != curCam.pixelWidth || preRT.height != curCam.pixelHeight)
            {
                preRT = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.DefaultHDR);
                cmd.Blit(src, preRT);
                mat.SetTexture("_PreTex", preRT);
            }

            int des = Shader.PropertyToID("_Temp1");
            cmd.GetTemporaryRT(des, w, h, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            cmd.Blit(src, des);
            cmd.Blit(des, src, mat, 0);
            cmd.Blit(src, preRT);
            mat.SetTexture("_PreTex", preRT);

            cmd.ReleaseTemporaryRT(des);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            //Debug.Log("clean");
        }
    }
    [System.Serializable]
    public class Setting
    {
        public RenderPassEvent evt = RenderPassEvent.BeforeRenderingPostProcessing;
        [Header("Data")]
        [Range(0f, 5f)] public float jitter = 1f;//intensity
        [Range(0f, 1f)] public float blend = 0.05f;//blend
    }
    #endregion
}


