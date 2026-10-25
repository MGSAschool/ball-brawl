using UnityEngine;
public abstract class PowerUpModifier : ScriptableObject
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float weight = 50f;
    [SerializeField] private Sprite sprite;
    [Tooltip("Strength tier shown in the arena (pickup size, glow, sparks) and on the HUD.")]
    [SerializeField, Range(1, 3)] private int level = 1;
    public float Weight => weight;
    public int Level => level;
    public float Duration => duration;
    public Sprite Sprite => sprite;
    public abstract void ApplyModifier(GameObject target);
    public virtual float ModifyMultiplier(float value)
    {
        return value;
    } 
    public virtual void RemoveModifier(GameObject target){}
}
