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

    private Shader _uberShader;
    private Material _uberMaterial;

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

        Shader.SetGlobalColor("_EdgeColor", _edgeColor);
        ChangeKeywords("EDGE_DECTECTION", _enableEdgeDectection);

        SetFogModeAndConstants();
    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        PrepareMaterial();

        Graphics.Blit(source, destination, _uberMaterial, 0);
    }
}
