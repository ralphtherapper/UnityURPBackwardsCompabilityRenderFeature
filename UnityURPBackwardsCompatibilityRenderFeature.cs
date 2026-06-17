using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace URPMigration
{
    public class UnityURPBackwardsCompatibilityRenderFeature : ScriptableRendererFeature
    {
        private RTHandle _globalRenderTexture;
        
        public static string FrameTextureName { get; set; } = "_FrameTextureHolder";

        // Custom pass data
        private class PassData
        {
            public TextureHandle Source;
            public TextureHandle Destination;
        }
        
        public static void TidyAndImportTexture(RenderGraph renderGraph, UniversalResourceData resourceData, ref RTHandle globalRenderTexture, out TextureHandle outTex)
        {
            var descriptor = resourceData.activeColorTexture.GetDescriptor(renderGraph);
            
            RenderTextureDescriptor rtd =
                new RenderTextureDescriptor(descriptor.width, descriptor.height, descriptor.colorFormat, 0);
            rtd.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref globalRenderTexture, rtd, name: FrameTextureName);

            outTex = renderGraph.ImportTexture(globalRenderTexture);
        }
        
        public class FrameBufferToTexturePass : ScriptableRenderPass
        {
            private RTHandle _globalRenderTexture;
            
            public FrameBufferToTexturePass(RTHandle globalRenderTexture)
            {
                _globalRenderTexture = globalRenderTexture;
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                
                
                //renderGraph.AddCopyPass(resourceData.activeColorTexture, destinationTextureHandle, passName: "Frame to texture copy");
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Texture to frame buffer", out var passData))
                {
                    TidyAndImportTexture(renderGraph, resourceData, ref _globalRenderTexture,
                        out TextureHandle destinationTextureHandle);
                    passData.Source = resourceData.activeColorTexture;
                    passData.Destination = destinationTextureHandle;
                    builder.UseTexture(passData.Source, AccessFlags.Read);
                    //builder.UseTexture(passData.Destination, AccessFlags.Write);
                    builder.SetRenderAttachment(passData.Destination, 0);
                    builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1,1,0,0), 0, false);
                    });
                }
            }
        }
        
        public class TextureToFrameBufferPass : ScriptableRenderPass
        {
            private RTHandle _globalRenderTexture;
            
            public TextureToFrameBufferPass(RTHandle globalRenderTexture)
            {
                _globalRenderTexture = globalRenderTexture;
            }
            
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                
                //renderGraph.AddCopyPass(sourceTextureHandle, resourceData.activeColorTexture, passName: "Texture to frame pass");
                using (var builder = renderGraph.AddRasterRenderPass<PassData>("Texture to frame buffer", out var passData))
                {
                    TidyAndImportTexture(renderGraph, resourceData, ref _globalRenderTexture,
                        out TextureHandle sourceTextureHandle);
                    passData.Source = sourceTextureHandle;
                    passData.Destination = resourceData.activeColorTexture;
                    builder.UseTexture(passData.Source, AccessFlags.Read);
                    //builder.UseTexture(passData.Destination, AccessFlags.Write);
                    builder.SetRenderAttachment(passData.Destination, 0);
                    builder.SetRenderFunc(static (PassData passData, RasterGraphContext context) =>
                    {
                        Blitter.BlitTexture(context.cmd, passData.Source, new Vector4(1,1,0,0), 0, false);
                    });
                }
            }
        }
        
        #region runtime 
        private TextureHandle _renderTextureHandle;
        private TextureDesc _renderTextureDescriptor;
        private FrameBufferToTexturePass _frameBufferToTexturePass;
        private TextureToFrameBufferPass _textureToFrameBufferPass;
        #endregion
        
        public override void Create()
        {
            _textureToFrameBufferPass = new TextureToFrameBufferPass(_globalRenderTexture);
            _textureToFrameBufferPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;
            _textureToFrameBufferPass.ConfigureInput(ScriptableRenderPassInput.Color);
            _frameBufferToTexturePass = new FrameBufferToTexturePass(_globalRenderTexture);
            _frameBufferToTexturePass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            _frameBufferToTexturePass.ConfigureInput(ScriptableRenderPassInput.Color);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_textureToFrameBufferPass); 
            renderer.EnqueuePass(_frameBufferToTexturePass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _globalRenderTexture?.Release();
        }
    }
}
