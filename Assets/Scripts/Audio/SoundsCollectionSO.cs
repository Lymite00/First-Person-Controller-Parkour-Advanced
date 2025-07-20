using UnityEngine;

[CreateAssetMenu()]
public class SoundsCollectionSO : ScriptableObject
{
    [Header("SFX")]
    public SoundSO[] Shoot;
    public SoundSO[] Jump;

    [Header("Music")] public SoundSO[] Music;
}
