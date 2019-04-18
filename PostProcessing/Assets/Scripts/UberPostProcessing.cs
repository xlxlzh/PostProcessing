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
    private RenderTexture[] _textures = new RenderTexture[16];

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
        RenderTexture currentDestination = _textures[0] = RenderTexture.GetTemporary(width, height, 0, fmt);
        Graphics.Blit(source, currentDestination, _bloomMaterial, 0);

        RenderTexture currentSource = currentDestination;
        int index = 1;
        for(; index < _iterations; ++index)
        {
            width /= 2;
            height /= 2;

            if (height < 2)
                break;

            currentDestination = _textures[index] = RenderTexture.GetTemporary(width, height, 0, fmt);
            Graphics.Blit(currentSource, currentDestination, _bloomMaterial, 1);
            currentSource = currentDestination;
        }

        //Up Samples
        for (index -=2; index >= 0; --index)
        {
            currentDestination = _textures[index];
            _textures[index] = null;
            Graphics.Blit(currentSource, currentDestination, _bloomMaterial, 2);
            RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }

        Shader.SetGlobalTexture("_SourceTex", source);

        Graphics.Blit(source, destination, _bloomMaterial, 3);
    }
}
