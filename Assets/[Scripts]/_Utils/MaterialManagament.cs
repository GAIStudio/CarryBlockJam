using UnityEngine;

namespace GAITemplate.MaterialManagament
{
    public static class MaterialUtils
    {
        public static string GetCode(this Color color)
        {
            return ColorUtility.ToHtmlStringRGBA(color);
        }

        public static Color GetColor(this string code)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + code, out c);
            return c;
        }

        public static Material Duplicate(this Material mat)
        {
            Material m = MonoBehaviour.Instantiate(mat);
            m.name = mat.name + "-generated";
            return m;
        }

        public static Material CopyWithChangeColor(this Material mat, Color color, string shaderVariableName = null, float colorPower = 1f)
        {
            Material m = mat.Duplicate();

            if (shaderVariableName == null) m.color = color;
            else m.SetColor(shaderVariableName, color * colorPower);

            return m;
        }

        public static void SetColor(this Renderer rend, Color color, string shaderVariableName = null, float colorPower = 1f)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            rend.GetPropertyBlock(mpb);

            if (shaderVariableName == null) mpb.SetColor("_BaseColor", color * colorPower);
            else mpb.SetColor(shaderVariableName, color * colorPower);

            rend.SetPropertyBlock(mpb);
        }

        public static void SetIntensity(this MaterialPropertyBlock block, Color color, string shaderVariableName = null, float colorPower = 1f)
        {
            if (shaderVariableName == null) shaderVariableName = "_BaseColor";
            block.SetColor(shaderVariableName, color * colorPower);
        }

        public static void SetIntensity(this Renderer rend, float colorPower)
        {
            MaterialPropertyBlock mpb = new MaterialPropertyBlock();

            rend.GetPropertyBlock(mpb);
            mpb.SetIntensity(rend.sharedMaterial.GetColor("_EmissionColor"), "_EmissionColor", colorPower);

            rend.SetPropertyBlock(mpb);
        }

        public static void DuplicateMaterial(this Renderer rend)
        {
            rend.material = rend.material.Duplicate();
        }

        public static void DuplicateWithChangeColor(this Renderer rend, Color color, string shaderVariableName = null, float colorPower = 1f)
        {
            rend.material = rend.material.CopyWithChangeColor(color, shaderVariableName, colorPower);
        }
    }
}