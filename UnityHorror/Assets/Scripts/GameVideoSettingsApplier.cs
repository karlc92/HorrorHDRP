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
        float refreshRateValue = settings.RefreshRate > 0
            ? settings.RefreshRate
            : (float)currentResolution.refreshRateRatio.value;

        QualitySettings.vSyncCount = settings.UseVSync ? settings.VSyncCount : 0;
        Application.targetFrameRate = settings.UseVSync ? -1 : settings.TargetFrameRate;

        Screen.fullScreenMode = settings.FullScreenMode;
        Screen.SetResolution(width, height, settings.FullScreenMode, new RefreshRate { numerator = (uint)Mathf.RoundToInt(refreshRateValue), denominator = 1u });

        ScalableBufferManager.ResizeBuffers(settings.RenderScale, settings.RenderScale);
        QualitySettings.globalTextureMipmapLimit = (int)settings.TextureQuality;
        QualitySettings.shadows = settings.ShadowQuality;
        QualitySettings.anisotropicFiltering = settings.AnisotropicFiltering;
    }
}
