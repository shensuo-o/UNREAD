using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ShadowDistortionController : MonoBehaviour
{
    [Header("Renderer")]
    [SerializeField] private UniversalRendererData rendererData;

    [Header("Distortion Material")]
    [SerializeField] private Material distortionMaterial;

    private FullScreenPassRendererFeature distortionFeature;


    // =========================================================
    // INTENSIDADES
    // =========================================================

    private float distanceIntensity;
    private float lookIntensity;


    // =========================================================
    // OSCURECIMIENTO POR MIRAR A LA SOMBRA
    // =========================================================

    [Header("Shadow Death Fade")]

    // Qué tan concentrado está el oscurecimiento hacia el final.
    // Más alto = tarda más en ponerse oscuro.
    [SerializeField] private float shadowDeathFadePower = 6f;

    // Qué tan suave es la transición del oscurecimiento.
    // Más alto = más lento y suave.
    [SerializeField] private float shadowDeathFadeSmoothTime = 1f;

    // Valor actual que está usando el shader.
    private float currentShadowDeathFade;

    // Velocidad interna utilizada por SmoothDamp.
    private float shadowDeathFadeVelocity;


    // =========================================================
    // MICRO-MINIMO
    // =========================================================

    [Header("MICRO-MIN")]

    [SerializeField] private float microMinNoiseDistortionStrength = 0.00020f;
    [SerializeField] private float microMinVerticalDistortionStrength = 0.00004f;
    [SerializeField] private float microMinChromaticAberrationStrength = 0.000008f;
    [SerializeField] private float microMinCRTScanlineStrength = 0.00009f;
    [SerializeField] private float microMinCRTJitterStrength = 0.000009f;
    [SerializeField] private float microMinCRTWobbleStrength = 0.00002f;
    [SerializeField] private float microMinCRTGlitchStrength = 0.00009f;
    [SerializeField] private float microMinCRTGlitchIntensity = 0.002f;
    [SerializeField] private float microMinCRTIntensityVariation = 0.0004f;


    // =========================================================
    // MICRO
    // =========================================================

    [Header("MICRO")]

    [SerializeField] private float microNoiseDistortionStrength = 0.00030f;
    [SerializeField] private float microVerticalDistortionStrength = 0.00005f;
    [SerializeField] private float microChromaticAberrationStrength = 0.000012f;
    [SerializeField] private float microCRTScanlineStrength = 0.00015f;
    [SerializeField] private float microCRTJitterStrength = 0.000018f;
    [SerializeField] private float microCRTWobbleStrength = 0.00003f;
    [SerializeField] private float microCRTGlitchStrength = 0.00020f;
    [SerializeField] private float microCRTGlitchIntensity = 0.005f;
    [SerializeField] private float microCRTIntensityVariation = 0.001f;


    // =========================================================
    // MEDIO
    // =========================================================

    [Header("MEDIO")]

    [SerializeField] private float mediumNoiseDistortionStrength = 0.00040f;
    [SerializeField] private float mediumVerticalDistortionStrength = 0.00007f;
    [SerializeField] private float mediumChromaticAberrationStrength = 0.000015f;
    [SerializeField] private float mediumCRTScanlineStrength = 0.00020f;
    [SerializeField] private float mediumCRTJitterStrength = 0.000025f;
    [SerializeField] private float mediumCRTWobbleStrength = 0.00004f;
    [SerializeField] private float mediumCRTGlitchStrength = 0.00025f;
    [SerializeField] private float mediumCRTGlitchIntensity = 0.007f;
    [SerializeField] private float mediumCRTIntensityVariation = 0.0015f;


    // =========================================================
    // MAXIMO
    // =========================================================

    [Header("MAXIMO")]

    [SerializeField] private float maxNoiseDistortionStrength = 0.035f;
    [SerializeField] private float maxVerticalDistortionStrength = 0.0055f;
    [SerializeField] private float maxChromaticAberrationStrength = 0.0006f;
    [SerializeField] private float maxCRTScanlineStrength = 0.017f;
    [SerializeField] private float maxCRTJitterStrength = 0.0022f;
    [SerializeField] private float maxCRTWobbleStrength = 0.0035f;
    [SerializeField] private float maxCRTGlitchStrength = 0.023f;
    [SerializeField] private float maxCRTGlitchIntensity = 0.55f;
    [SerializeField] private float maxCRTIntensityVariation = 0.12f;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (rendererData == null)
        {
            Debug.LogError(
                "No se asignó el Universal Renderer Data."
            );

            return;
        }


        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is FullScreenPassRendererFeature fullScreenFeature)
            {
                distortionFeature = fullScreenFeature;
                break;
            }
        }


        if (distortionFeature == null)
        {
            Debug.LogError(
                "No se encontró el Full Screen Pass Renderer Feature."
            );

            return;
        }


        distortionFeature.SetActive(false);

        SetDistortionBlend(0f);

        // NUEVO
        currentShadowDeathFade = 0f;
        shadowDeathFadeVelocity = 0f;

        SetShadowDeathFade(0f);
    }


    // =========================================================
    // ACTIVAR / DESACTIVAR
    // =========================================================

    public void SetDistortionEnabled(bool enabled)
    {
        if (distortionFeature == null)
            return;

        distortionFeature.SetActive(enabled);
    }


    // =========================================================
    // INTENSIDAD POR DISTANCIA
    // =========================================================

    public void SetDistanceIntensity(float intensity)
    {
        distanceIntensity = Mathf.Clamp01(intensity);

        UpdateFinalIntensity();
    }


    // =========================================================
    // INTENSIDAD POR MIRADA
    // =========================================================

    public void SetLookIntensity(float intensity)
    {
        lookIntensity = Mathf.Clamp01(intensity);


        // =====================================================
        // OSCURECIMIENTO POR MIRAR
        // =====================================================

        // Primero hacemos que el oscurecimiento aparezca
        // principalmente hacia el final.

        float targetDeathFade = Mathf.Pow(
            lookIntensity,
            shadowDeathFadePower
        );


        // =====================================================
        // TRANSICIÓN SUAVE
        // =====================================================

        // En lugar de mandar el valor directamente al shader,
        // lo hacemos llegar suavemente.

        currentShadowDeathFade = Mathf.SmoothDamp(
            currentShadowDeathFade,
            targetDeathFade,
            ref shadowDeathFadeVelocity,
            shadowDeathFadeSmoothTime
        );


        SetShadowDeathFade(
            currentShadowDeathFade
        );


        // =====================================================
        // CRT ORIGINAL
        // =====================================================

        UpdateFinalIntensity();
    }


    // =========================================================
    // APLICAR OSCURECIMIENTO AL SHADER
    // =========================================================

    private void SetShadowDeathFade(float fade)
    {
        if (distortionMaterial == null)
            return;


        fade = Mathf.Clamp01(fade);


        distortionMaterial.SetFloat(
            "_ShadowDeathFade",
            fade
        );
    }


    // =========================================================
    // INTENSIDAD FINAL
    // =========================================================

    private void UpdateFinalIntensity()
    {
        float finalIntensity = Mathf.Max(
            distanceIntensity,
            lookIntensity
        );


        SetDistortionEnabled(
            finalIntensity > 0f
        );


        SetDistortionIntensity(
            finalIntensity
        );
    }


    // =========================================================
    // DISTORTION BLEND
    // =========================================================

    public void SetDistortionBlend(float blend)
    {
        if (distortionMaterial == null)
            return;


        blend = Mathf.Clamp01(blend);


        distortionMaterial.SetFloat(
            "_DistortionBlend",
            blend
        );
    }


    // =========================================================
    // PROGRESIÓN GENERAL
    // =========================================================

    public void SetDistortionIntensity(float intensity)
    {
        if (distortionMaterial == null)
            return;


        intensity = Mathf.Clamp01(intensity);


        // =====================================================
        // BLEND
        // =====================================================

        SetDistortionBlend(
            intensity * 0.5f
        );


        float noiseDistortion;
        float verticalDistortion;
        float chromaticAberration;
        float scanline;
        float jitter;
        float wobble;
        float glitchStrength;
        float glitchIntensity;
        float intensityVariation;


        // =====================================================
        // MICRO-MIN -> MICRO
        // 0 -> 0.20
        // =====================================================

        if (intensity <= 0.20f)
        {
            float t = intensity / 0.20f;


            noiseDistortion = Mathf.Lerp(
                microMinNoiseDistortionStrength,
                microNoiseDistortionStrength,
                t
            );

            verticalDistortion = Mathf.Lerp(
                microMinVerticalDistortionStrength,
                microVerticalDistortionStrength,
                t
            );

            chromaticAberration = Mathf.Lerp(
                microMinChromaticAberrationStrength,
                microChromaticAberrationStrength,
                t
            );

            scanline = Mathf.Lerp(
                microMinCRTScanlineStrength,
                microCRTScanlineStrength,
                t
            );

            jitter = Mathf.Lerp(
                microMinCRTJitterStrength,
                microCRTJitterStrength,
                t
            );

            wobble = Mathf.Lerp(
                microMinCRTWobbleStrength,
                microCRTWobbleStrength,
                t
            );

            glitchStrength = Mathf.Lerp(
                microMinCRTGlitchStrength,
                microCRTGlitchStrength,
                t
            );

            glitchIntensity = Mathf.Lerp(
                microMinCRTGlitchIntensity,
                microCRTGlitchIntensity,
                t
            );

            intensityVariation = Mathf.Lerp(
                microMinCRTIntensityVariation,
                microCRTIntensityVariation,
                t
            );
        }


        // =====================================================
        // MICRO -> MEDIO
        // 0.20 -> 0.60
        // =====================================================

        else if (intensity <= 0.60f)
        {
            float t = (intensity - 0.20f) / 0.40f;


            noiseDistortion = Mathf.Lerp(
                microNoiseDistortionStrength,
                mediumNoiseDistortionStrength,
                t
            );

            verticalDistortion = Mathf.Lerp(
                microVerticalDistortionStrength,
                mediumVerticalDistortionStrength,
                t
            );

            chromaticAberration = Mathf.Lerp(
                microChromaticAberrationStrength,
                mediumChromaticAberrationStrength,
                t
            );

            scanline = Mathf.Lerp(
                microCRTScanlineStrength,
                mediumCRTScanlineStrength,
                t
            );

            jitter = Mathf.Lerp(
                microCRTJitterStrength,
                mediumCRTJitterStrength,
                t
            );

            wobble = Mathf.Lerp(
                microCRTWobbleStrength,
                mediumCRTWobbleStrength,
                t
            );

            glitchStrength = Mathf.Lerp(
                microCRTGlitchStrength,
                mediumCRTGlitchStrength,
                t
            );

            glitchIntensity = Mathf.Lerp(
                microCRTGlitchIntensity,
                mediumCRTGlitchIntensity,
                t
            );

            intensityVariation = Mathf.Lerp(
                microCRTIntensityVariation,
                mediumCRTIntensityVariation,
                t
            );
        }


        // =====================================================
        // MEDIO -> MAXIMO
        // 0.60 -> 1
        // =====================================================

        else
        {
            float t = (intensity - 0.60f) / 0.40f;


            noiseDistortion = Mathf.Lerp(
                mediumNoiseDistortionStrength,
                maxNoiseDistortionStrength,
                t
            );

            verticalDistortion = Mathf.Lerp(
                mediumVerticalDistortionStrength,
                maxVerticalDistortionStrength,
                t
            );

            chromaticAberration = Mathf.Lerp(
                mediumChromaticAberrationStrength,
                maxChromaticAberrationStrength,
                t
            );

            scanline = Mathf.Lerp(
                mediumCRTScanlineStrength,
                maxCRTScanlineStrength,
                t
            );

            jitter = Mathf.Lerp(
                mediumCRTJitterStrength,
                maxCRTJitterStrength,
                t
            );

            wobble = Mathf.Lerp(
                mediumCRTWobbleStrength,
                maxCRTWobbleStrength,
                t
            );

            glitchStrength = Mathf.Lerp(
                mediumCRTGlitchStrength,
                maxCRTGlitchStrength,
                t
            );

            glitchIntensity = Mathf.Lerp(
                mediumCRTGlitchIntensity,
                maxCRTGlitchIntensity,
                t
            );

            intensityVariation = Mathf.Lerp(
                mediumCRTIntensityVariation,
                maxCRTIntensityVariation,
                t
            );
        }


        // =====================================================
        // APLICAR AL MATERIAL
        // =====================================================

        distortionMaterial.SetFloat(
            "_NoiseDistortionStrength",
            noiseDistortion
        );

        distortionMaterial.SetFloat(
            "_VerticalDistortionStrength",
            verticalDistortion
        );

        distortionMaterial.SetFloat(
            "_ChromaticAberrationStrength",
            chromaticAberration
        );

        distortionMaterial.SetFloat(
            "_CRTScanlineStrength",
            scanline
        );

        distortionMaterial.SetFloat(
            "_CRTJitterStrength",
            jitter
        );

        distortionMaterial.SetFloat(
            "_CRTWobbleStrength",
            wobble
        );

        distortionMaterial.SetFloat(
            "_CRTGlitchStrength",
            glitchStrength
        );

        distortionMaterial.SetFloat(
            "_CRTGlitchIntensity",
            glitchIntensity
        );

        distortionMaterial.SetFloat(
            "_CRTIntensityVariation",
            intensityVariation
        );
    }


    // =========================================================
    // AL SALIR DE PLAY
    // =========================================================

    private void OnDisable()
    {
        if (distortionFeature != null)
        {
            distortionFeature.SetActive(false);
        }


        SetDistortionBlend(0f);


        // NUEVO
        currentShadowDeathFade = 0f;
        shadowDeathFadeVelocity = 0f;

        SetShadowDeathFade(0f);
    }
}