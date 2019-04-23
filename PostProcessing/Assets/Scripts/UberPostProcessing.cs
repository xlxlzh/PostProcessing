using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UberPostProcessing : PostProcessingBase
{
    public enum FogMode
    {
        Fog_None,
        Fog_Linear,
        Fog_EXP,
        Fog_EXP2
    }

    public enum VignetteMode
    {
        Vignette_None,
        Vignette_Classic,
        Vignette_Mask
    }


    [Header("Edge Dectection")]
    public bool _enableEdgeDectection = false;
    public Color _edgeColor = Color.black;

    [Header("Fog")]
    public FogMode _fogMode = FogMode.Fog_None;
    public Color _fogColor = Color.white;
    public float _fogStart = 0.0f;
    public float _fogEnd = 10.0f;
    public float _fogIntensity = 1.0f;

    [Header("Bloom")]
    public bool _enableBloomEffect = false;

    [Range(0, 10)]
    public float _intensity = 1;

    [Range(0, 10)]
    public float _threshold = 1;

    [Range(0, 1)]
    public float _softThreshold = 0.5f;

    [Range(1, 16)]
    public int _iterations = 4;

    private Shader _uberShader;
    private Shader _bloomShader;
    private Material _bloomMaterial;
    private Material _uberMaterial;

    const int MAX_BLUR = 16;
    private RenderTexture[] _texturesDown = new RenderTexture[MAX_BLUR];
    private RenderTexture[] _texturesUp = new RenderTexture[MAX_BLUR];
    private RenderTexture _bloomTex = null;

    [Header("Vignette")]
    public VignetteMode _vignetteMode = VignetteMode.Vignette_None;
    public Color _vignetteColor = Color.black;
    public Vector2 _vignetteCenter = new Vector2(0.5f, 0.5f);

    [Range(0.0f, 1.0f)]
    public float _vignetteIntensity = 1.0f;
    [Range(0.01f, 1.0f)]
    public float _vignetteSmoothness = 1.0f;
    [Range(0.0f, 1.0f)]
    public float _vignetteRoundness = 0.0f;
    public bool _vignetteRounded = false;

    public Texture _vignetteMask;
    [Range(0.0f, 1.0f)]
    public float _vignetteOpacity;

    private void OnEnable()
    {
        InitShaders();
    }

    private void ChangeKeywords(Material mat, string name, bool value)
    {
        if (value)
            mat.EnableKeyword(name);
        else
            mat.DisableKeyword(name);
    }

    private void InitShaders()
    {
        _uberShader = Shader.Find("XlXlZh/Uber");
        _bloomShader = Shader.Find("XlXlZh/Bloom");
    }

    private bool NeedDepthTexture()
    {
        return false;
    }

    private void PrepareFog()
    {
        ChangeKeywords(_uberMaterial, "POSTPROCESSING_FOG_LINEAR", _fogMode == FogMode.Fog_Linear);
        ChangeKeywords(_uberMaterial, "POSTPROCESSING_FOG_EXP", _fogMode == FogMode.Fog_EXP);
        ChangeKeywords(_uberMaterial, "POSTPROCESSING_FOG_EXP2", _fogMode == FogMode.Fog_EXP2);

        if (_fogMode == FogMode.Fog_None)
            return;

        Shader.SetGlobalColor("_FogColor", _fogColor);
        Shader.SetGlobalVector("_FogParams", new Vector4(_fogStart, _fogEnd, _fogIntensity, 0.0f));

    }

    private void PrepareMaterial()
    {
        if (_uberShader == null)
            return;

        _uberMaterial = CheckShaderAndCreateMaterial(_uberShader, _uberMaterial);
        _bloomMaterial = CheckShaderAndCreateMaterial(_bloomShader, _bloomMaterial);

        _uberMaterial.SetColor("_EdgeColor", _edgeColor);
        ChangeKeywords(_uberMaterial, "EDGE_DECTECTION", _enableEdgeDectection);
        ChangeKeywords(_uberMaterial, "POSTPROCESSING_BLOOM", _enableBloomEffect);

        PrepareVignette();
        PrepareFog();
    }

    private void PrepareVignette()
    {
        ChangeKeywords(_uberMaterial, "POSTPROCESSING_VIGNETTE_CLASSIC", _vignetteMode == VignetteMode.Vignette_Classic);

        bool maskEnable = _vignetteMode == VignetteMode.Vignette_Mask && _vignetteMask != null && _vignetteOpacity > 0;
        ChangeKeywords(_uberMaterial, "POSTPROCESSING_VIGNETTE_MASK", maskEnable);

        if (_vignetteMode == VignetteMode.Vignette_None)
            return;

        _uberMaterial.SetColor("_Vignette_Color", _vignetteColor);

        if (_vignetteMode == VignetteMode.Vignette_Classic)
        {
            _uberMaterial.SetVector("_Vignette_Center", _vignetteCenter);
            float roundness = (1.0f - _vignetteRoundness) * 6.0f + _vignetteRoundness;
            _uberMaterial.SetVector("_Vignette_Settings", new Vector4(_vignetteIntensity * 3.0f, _vignetteSmoothness * 5.0f, roundness, _vignetteRounded ? 1.0f : 0.0f));
        }
        else if (maskEnable)
        {
            _uberMaterial.SetTexture("_Vignette_Mask", _vignetteMask);
            _uberMaterial.SetFloat("_Vignette_Opacity", _vignetteOpacity);
        }
    }

    void RenderBloomTex(RenderTexture source, RenderTexture destination)
    {
        float knee = _threshold * _softThreshold;
        Vector4 filter;
        filter.x = _threshold;
        filter.y = filter.x - knee;
        filter.z = 2f * knee;
        filter.w = 0.25f / (knee + 0.00001f);
        _bloomMaterial.SetVector("_Filter", filter);
        _bloomMaterial.SetFloat("_Intensity", Mathf.GammaToLinearSpace(_intensity));

        int width = source.width / 2;
        int height = source.height / 2;
        RenderTextureFormat fmt = source.format;

        //Down Samples
        var prefiltered = RenderTexture.GetTemporary(width, height, 0, fmt);
        Graphics.Blit(source, prefiltered, _bloomMaterial, 0);

        var last = prefiltered;
        for (int index = 0; index < _iterations; ++index)
        {
            width /= 2;
            height /= 2;

            if (height < 2)
                break;

            _texturesDown[index] = RenderTexture.GetTemporary(width, height, 0, fmt);

            Graphics.Blit(last, _texturesDown[index], _bloomMaterial, 1);
            last = _texturesDown[index];
        }

        //Up Samples
        for (int index = _iterations - 2; index >= 0; --index)
        {
            var baseTex = _texturesDown[index];

            _bloomMaterial.SetTexture("_SourceTex", baseTex);
            _texturesUp[index] = RenderTexture.GetTemporary(baseTex.width, baseTex.height, 0, fmt);
            Graphics.Blit(last, _texturesUp[index], _bloomMaterial, 2);
            last = _texturesUp[index];
        }

        _bloomTex = last;

        for (int i = 0; i < _iterations; i++)
        {
            if (_texturesDown[i] != null)
                RenderTexture.ReleaseTemporary(_texturesDown[i]);

            if (_texturesUp[i] != null && _texturesUp[i] != _bloomTex)
                RenderTexture.ReleaseTemporary(_texturesUp[i]);

            _texturesUp[i] = null;
            _texturesDown[i] = null;
        }

        RenderTexture.ReleaseTemporary(prefiltered);

        _uberMaterial.SetTexture("_BloomTex", _bloomTex);
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        PrepareMaterial();

        if (_enableBloomEffect)
            RenderBloomTex(source, destination);

        Graphics.Blit(source, destination, _uberMaterial, 0);

        RenderTexture.ReleaseTemporary(_bloomTex);
    }
}
