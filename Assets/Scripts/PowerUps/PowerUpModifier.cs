using UnityEngine;
public abstract class PowerUpModifier : ScriptableObject
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float weight = 50f;
    public float Weight => weight;
    public float Duration => duration;
    public abstract void ApplyModifier(GameObject target);
    public virtual float ModifyKnockback(float value)
    {
        return value;
    } 
}
