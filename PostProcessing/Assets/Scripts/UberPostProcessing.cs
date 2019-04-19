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

    private void OnEnable()
    {
        InitShaders();
    }

    private void ChangeKeywords(string name, bool value)
    {
        if (value)
            Shader.EnableKeyword(name);
        else
            Shader.DisableKeyword(name);
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

    private void SetFogModeAndConstants()
    {
        Shader.SetGlobalColor("_FogColor", _fogColor);
        Shader.SetGlobalVector("_FogParams", new Vector4(_fogStart, _fogEnd, _fogIntensity, 0.0f));


        ChangeKeywords("POSTPROCESSING_FOG_LINEAR", false);
        ChangeKeywords("POSTPROCESSING_FOG_EXP", false);
        ChangeKeywords("POSTPROCESSING_FOG_EXP2", false);

        switch (_fogMode)
        {
            case FogMode.Fog_None:
                ChangeKeywords("POSTPROCESSING_FOG_LINEAR", false);
                ChangeKeywords("POSTPROCESSING_FOG_EXP", false);
                ChangeKeywords("POSTPROCESSING_FOG_EXP2", false);
                break;
            case FogMode.Fog_Linear:
                ChangeKeywords("POSTPROCESSING_FOG_LINEAR", true);
                ChangeKeywords("POSTPROCESSING_FOG_EXP", false);
                ChangeKeywords("POSTPROCESSING_FOG_EXP2", false);
                break;
            case FogMode.Fog_EXP2:
                ChangeKeywords("POSTPROCESSING_FOG_LINEAR", false);
                ChangeKeywords("POSTPROCESSING_FOG_EXP", false);
                ChangeKeywords("POSTPROCESSING_FOG_EXP2", true);
                break;
            case FogMode.Fog_EXP:
                ChangeKeywords("POSTPROCESSING_FOG_LINEAR", false);
                ChangeKeywords("POSTPROCESSING_FOG_EXP", true);
                ChangeKeywords("POSTPROCESSING_FOG_EXP2", false);
                break;
        }
    }

    private void PrepareMaterial()
    {
        if (_uberShader == null)
            return;

        _uberMaterial = CheckShaderAndCreateMaterial(_uberShader, _uberMaterial);
        _bloomMaterial = CheckShaderAndCreateMaterial(_bloomShader, _bloomMaterial);

        Shader.SetGlobalColor("_EdgeColor", _edgeColor);
        ChangeKeywords("EDGE_DECTECTION", _enableEdgeDectection);

        SetFogModeAndConstants();
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        PrepareMaterial();

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
        for(int index = 0; index < _iterations; ++index)
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

        var bloomTex = last;

        for (int i = 0; i < _iterations; i++)
        {
            if (_texturesDown[i] != null)
                RenderTexture.ReleaseTemporary(_texturesDown[i]);

            if (_texturesUp[i] != null && _texturesUp[i] != bloomTex)
                RenderTexture.ReleaseTemporary(_texturesUp[i]);

            _texturesUp[i] = null;
            _texturesDown[i] = null;
        }

        RenderTexture.ReleaseTemporary(prefiltered);

        _uberMaterial.SetTexture("_BloomTex", bloomTex);

        Graphics.Blit(source, destination, _uberMaterial, 0);

        RenderTexture.ReleaseTemporary(bloomTex);
    }
}
