using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class TerrainData : UpdatableData {
    
    public float heightMultiplier;
    public AnimationCurve heightCurve;
    public bool useFalloff;
    public bool useFlatShading;
    public float scale = 1f;
}
