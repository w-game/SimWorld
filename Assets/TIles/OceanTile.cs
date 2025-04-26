using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class OceanTile : RuleTile<WaterTile.Neighbor> {
    public bool customField;

    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int Null = 3;
        public const int NotNull = 4;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        switch (neighbor) {
            case TilingRuleOutput.Neighbor.This: return true;
            case TilingRuleOutput.Neighbor.NotThis: return false;
        }
        return base.RuleMatch(neighbor, tile);
    }
}