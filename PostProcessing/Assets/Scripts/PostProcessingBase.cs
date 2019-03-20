using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class PostProcessingBase : MonoBehaviour
{
    void Start()
    {
        CheckResources();
    }

    protected Material CheckShaderAndCreateMaterial(Shader shader, Material mat)
    {
        if (shader == null)
            return null;

        if (shader.isSupported && mat && mat.shader == shader)
            return mat;

        if (!shader.isSupported)
            return null;
        else
        {
            mat = new Material(shader);
            mat.hideFlags = HideFlags.DontSave;
            return mat;
        }
    }

    protected void NotSupport()
    {
        enabled = false;
    }

    protected void CheckResources()
    {
        bool isSupported = CheckSupport();
        if (!isSupported)
        {
            NotSupport();
        }
    }

    protected bool CheckSupport()
    {
        return SystemInfo.supportsImageEffects;
    }
}
