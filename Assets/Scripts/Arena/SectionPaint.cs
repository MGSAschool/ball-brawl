using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class SectionPaint : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private MeshRenderer rend;
    private MaterialPropertyBlock block;

    public Color BaseColor { get; private set; }

    public void SetBaseColor(Color color)
    {
        BaseColor = color;
        Show(color);
    }

    public void ShowBase()
    {
        Show(BaseColor);
    }

    public void Show(Color color)
    {
        if (rend == null) rend = GetComponent<MeshRenderer>();
        if (block == null) block = new MaterialPropertyBlock();

        rend.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        rend.SetPropertyBlock(block);
    }
}
