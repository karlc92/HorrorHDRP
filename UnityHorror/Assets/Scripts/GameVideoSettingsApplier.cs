using UnityEngine;

public static class GameVideoSettingsApplier
{
    public static void Apply(VideoSettings settings)
    {
        if (settings == null)
            return;

        Resolution currentResolution = Screen.currentResolution;
        int width = settings.ResolutionWidth > 0 ? settings.ResolutionWidth : currentResolution.width;
        int height = settings.ResolutionHeight > 0 ? settings.ResolutionHeight : currentResolution.height;
        int refreshRate = settings.RefreshRate > 0 ? settings.RefreshRate : currentResolution.refreshRate;

        QualitySettings.vSyncCount = settings.UseVSync ? settings.VSyncCount : 0;
        Application.targetFrameRate = settings.UseVSync ? -1 : settings.TargetFrameRate;

        Screen.fullScreenMode = settings.FullScreenMode;
        Screen.SetResolution(width, height, settings.FullScreenMode, refreshRate);

        ScalableBufferManager.ResizeBuffers(settings.RenderScale, settings.RenderScale);
        QualitySettings.globalTextureMipmapLimit = (int)settings.TextureQuality;
        QualitySettings.shadows = settings.ShadowQuality;
        QualitySettings.anisotropicFiltering = settings.AnisotropicFiltering;
    }
}
