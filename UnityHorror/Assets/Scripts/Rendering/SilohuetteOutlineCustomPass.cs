using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// HDRP Custom Pass that renders an inverted-hull silhouette outline for the currently selected renderers
/// provided by <see cref="OutlineManager"/>.
///
/// Setup: Add this pass to a Custom Pass Volume (Global) and assign an outline material
/// created from the "Hidden/HDRP/SilhouetteOutline" shader.
/// </summary>
[System.Serializable]
public sealed class SilhouetteOutlineCustomPass : CustomPass
{
    [Header("Outline Material (asset)")]
    [SerializeField] Material outlineMaterial;

    // We create a runtime instance so we don't mutate the shared asset material.
    Material runtimeMaterial;

    protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
    {
        if (outlineMaterial != null)
        {
            runtimeMaterial = new Material(outlineMaterial)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }

    protected override void Execute(CustomPassContext ctx)
    {
        if (runtimeMaterial == null)
            return;

        OutlineManager om = OutlineManager.Instance;
        if (om == null)
            return;

        Renderer[] renderers = om.TargetRenderers;
        if (renderers == null || renderers.Length == 0)
            return;

        // Update the runtime material's params for this frame.
        runtimeMaterial.SetColor("_OutlineColor", om.OutlineColor);
        runtimeMaterial.SetFloat("_OutlineThickness", om.OutlineThickness);

        // Ensure we're drawing into the camera buffers for this injection point.
        CoreUtils.SetRenderTarget(ctx.cmd, ctx.cameraColorBuffer, ctx.cameraDepthBuffer);

        // Draw each renderer with the outline material.
        // NOTE: Unity 6.3 CommandBuffer.DrawRenderer has no MaterialPropertyBlock overload. :contentReference[oaicite:1]{index=1}
        for (int r = 0; r < renderers.Length; r++)
        {
            Renderer renderer = renderers[r];
            if (renderer == null || !renderer.enabled)
                continue;

            // Try to draw each submesh so the silhouette hull matches the full object.
            Mesh mesh = null;
            if (renderer is SkinnedMeshRenderer smr)
                mesh = smr.sharedMesh;
            else if (renderer is MeshRenderer)
                mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;

            int subMeshCount = mesh != null ? mesh.subMeshCount : 1;

            for (int s = 0; s < subMeshCount; s++)
            {
                // DrawRenderer(Renderer, Material, submeshIndex, shaderPass) :contentReference[oaicite:2]{index=2}
                ctx.cmd.DrawRenderer(renderer, runtimeMaterial, s, 0);
            }
        }
    }

    protected override void Cleanup()
    {
        if (runtimeMaterial != null)
        {
            CoreUtils.Destroy(runtimeMaterial);
            runtimeMaterial = null;
        }
    }
}