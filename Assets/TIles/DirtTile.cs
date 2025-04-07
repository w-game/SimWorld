using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class DirtTile : RuleTile<DirtTile.Neighbor>
{
    public bool customField;

    public class Neighbor : RuleTile.TilingRule.Neighbor
    {

    }

    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        if (tile != null && tile is RuleTile)
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