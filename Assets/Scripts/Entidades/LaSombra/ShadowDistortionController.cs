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
    // MICRO-MINIMO
    // =========================================================

    [Header("MICRO-MIN")]

    [SerializeField] private float microMinNoiseDistortionStrength = 0.0003f;
    [SerializeField] private float microMinVerticalDistortionStrength = 0.00005f;
    [SerializeField] private float microMinChromaticAberrationStrength = 0.000025f;
    [SerializeField] private float microMinCRTScanlineStrength = 0.00015f;
    [SerializeField] private float microMinCRTJitterStrength = 0.00002f;
    [SerializeField] private float microMinCRTWobbleStrength = 0.00003f;
    [SerializeField] private float microMinCRTGlitchStrength = 0.0002f;
    [SerializeField] private float microMinCRTGlitchIntensity = 0.005f;
    [SerializeField] private float microMinCRTIntensityVariation = 0.001f;


    // =========================================================
    // BASE
    // =========================================================

    [Header("BASE")]

    [SerializeField] private float baseNoiseDistortionStrength = 0.03f;
    [SerializeField] private float baseVerticalDistortionStrength = 0.005f;
    [SerializeField] private float baseChromaticAberrationStrength = 0.0025f;
    [SerializeField] private float baseCRTScanlineStrength = 0.015f;
    [SerializeField] private float baseCRTJitterStrength = 0.002f;
    [SerializeField] private float baseCRTWobbleStrength = 0.003f;
    [SerializeField] private float baseCRTGlitchStrength = 0.02f;
    [SerializeField] private float baseCRTGlitchIntensity = 0.5f;
    [SerializeField] private float baseCRTIntensityVariation = 0.1f;


    // =========================================================
    // MEDIO
    // =========================================================

    [Header("MEDIO")]

    [SerializeField] private float mediumNoiseDistortionStrength = 0.035f;
    [SerializeField] private float mediumVerticalDistortionStrength = 0.0055f;
    [SerializeField] private float mediumChromaticAberrationStrength = 0.003f;
    [SerializeField] private float mediumCRTScanlineStrength = 0.017f;
    [SerializeField] private float mediumCRTJitterStrength = 0.0025f;
    [SerializeField] private float mediumCRTWobbleStrength = 0.004f;
    [SerializeField] private float mediumCRTGlitchStrength = 0.035f;
    [SerializeField] private float mediumCRTGlitchIntensity = 0.65f;
    [SerializeField] private float mediumCRTIntensityVariation = 0.18f;


    // =========================================================
    // MAXIMO
    // =========================================================

    [Header("MAXIMO")]

    [SerializeField] private float maxNoiseDistortionStrength = 0.05f;
    [SerializeField] private float maxVerticalDistortionStrength = 0.007f;
    [SerializeField] private float maxChromaticAberrationStrength = 0.004f;
    [SerializeField] private float maxCRTScanlineStrength = 0.020f;
    [SerializeField] private float maxCRTJitterStrength = 0.0035f;
    [SerializeField] private float maxCRTWobbleStrength = 0.005f;
    [SerializeField] private float maxCRTGlitchStrength = 0.05f;
    [SerializeField] private float maxCRTGlitchIntensity = 0.9f;
    [SerializeField] private float maxCRTIntensityVariation = 0.25f;


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


        // Empieza completamente apagado.
        distortionFeature.SetActive(false);

        // Imagen original.
        SetDistortionBlend(0f);
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

        // Dejamos el material limpio al salir de Play.
        SetDistortionBlend(0f);
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

        // Máximo de Blend = 0.5
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
        // MICRO-MIN -> BASE
        // 0 -> 0.20
        // =====================================================

        if (intensity <= 0.20f)
        {
            float t = intensity / 0.20f;


            noiseDistortion = Mathf.Lerp(
                microMinNoiseDistortionStrength,
                baseNoiseDistortionStrength,
                t
            );

            verticalDistortion = Mathf.Lerp(
                microMinVerticalDistortionStrength,
                baseVerticalDistortionStrength,
                t
            );

            chromaticAberration = Mathf.Lerp(
                microMinChromaticAberrationStrength,
                baseChromaticAberrationStrength,
                t
            );

            scanline = Mathf.Lerp(
                microMinCRTScanlineStrength,
                baseCRTScanlineStrength,
                t
            );

            jitter = Mathf.Lerp(
                microMinCRTJitterStrength,
                baseCRTJitterStrength,
                t
            );

            wobble = Mathf.Lerp(
                microMinCRTWobbleStrength,
                baseCRTWobbleStrength,
                t
            );

            glitchStrength = Mathf.Lerp(
                microMinCRTGlitchStrength,
                baseCRTGlitchStrength,
                t
            );

            glitchIntensity = Mathf.Lerp(
                microMinCRTGlitchIntensity,
                baseCRTGlitchIntensity,
                t
            );

            intensityVariation = Mathf.Lerp(
                microMinCRTIntensityVariation,
                baseCRTIntensityVariation,
                t
            );
        }


        // =====================================================
        // BASE -> MEDIO
        // 0.20 -> 0.60
        // =====================================================

        else if (intensity <= 0.60f)
        {
            float t = (intensity - 0.20f) / 0.40f;


            noiseDistortion = Mathf.Lerp(
                baseNoiseDistortionStrength,
                mediumNoiseDistortionStrength,
                t
            );

            verticalDistortion = Mathf.Lerp(
                baseVerticalDistortionStrength,
                mediumVerticalDistortionStrength,
                t
            );

            chromaticAberration = Mathf.Lerp(
                baseChromaticAberrationStrength,
                mediumChromaticAberrationStrength,
                t
            );

            scanline = Mathf.Lerp(
                baseCRTScanlineStrength,
                mediumCRTScanlineStrength,
                t
            );

            jitter = Mathf.Lerp(
                baseCRTJitterStrength,
                mediumCRTJitterStrength,
                t
            );

            wobble = Mathf.Lerp(
                baseCRTWobbleStrength,
                mediumCRTWobbleStrength,
                t
            );

            glitchStrength = Mathf.Lerp(
                baseCRTGlitchStrength,
                mediumCRTGlitchStrength,
                t
            );

            glitchIntensity = Mathf.Lerp(
                baseCRTGlitchIntensity,
                mediumCRTGlitchIntensity,
                t
            );

            intensityVariation = Mathf.Lerp(
                baseCRTIntensityVariation,
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
}