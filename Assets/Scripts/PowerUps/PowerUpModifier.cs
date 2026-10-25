using UnityEngine;
public abstract class PowerUpModifier : ScriptableObject
{
    [SerializeField] private float duration = 5f;
    [SerializeField] private float weight = 50f;
    [SerializeField] private Sprite sprite;
    public float Weight => weight;
    public float Duration => duration;
    public Sprite Sprite => sprite;
    public abstract void ApplyModifier(GameObject target);
    public virtual float ModifyMultiplier(float value)
    {
        return value;
    } 
    public virtual void RemoveModifier(GameObject target){}
}
