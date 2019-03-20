using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UberPostProcessing : PostProcessingBase
{
    [Header("Edge Dectection")]
    public bool _enableEdgeDectection = false;
    public Color _edgeColor = Color.black;

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

    private void PrepareMaterial()
    {
        if (_uberShader == null)
            return;

        _uberMaterial = CheckShaderAndCreateMaterial(_uberShader, _uberMaterial);

        Shader.SetGlobalColor("_EdgeColor", _edgeColor);
        ChangeKeywords("EDGE_DECTECTION", _enableEdgeDectection);

    }

    public void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        PrepareMaterial();

        Graphics.Blit(source, destination, _uberMaterial, 0);
    }
}
