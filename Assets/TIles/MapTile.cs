using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class MapTile : RuleTile<MapTile.Neighbor> {
    public bool customField;

    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int Null = 3;
        public const int NotNull = 4;
    }

    public override bool RuleMatch(int neighbor, TileBase tile) {
        if (tile is MapTile)
        {
            switch (neighbor)
            {
                case TilingRuleOutput.Neighbor.This: return true;
                case TilingRuleOutput.Neighbor.NotThis: return false;
            }
        }
        return base.RuleMatch(neighbor, tile);
    }
}