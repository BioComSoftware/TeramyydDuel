using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Teramyyd/Ships/Ship Definition", fileName = "ShipDefinition")]
public class ShipDefinition : ScriptableObject
{
    [Header("Identity")] 
    public string id;
    public string displayName;
    [TextArea] public string description;

    [Header("Economy")] 
    public int cost = 0;

    [Header("Base Stats")] 
    public float maxHull = 100f;
    public float baseSpeed = 10f;
    public float turnRate = 45f;

    [Header("Mount Layout (logical)")]
    public List<ShipMountConfig> mounts = new List<ShipMountConfig>();
}

[Serializable]
public class ShipMountConfig
{
    public string mountId;          // logical name; match a scene mount
    public string acceptedType = "cannon";
    public float yawLimitDeg = 45f;     // total arc; +/- half each side
    public float pitchUpDeg = 15f;
    public float pitchDownDeg = 15f;
}

