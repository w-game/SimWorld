using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    public enum HouseType
    {
        None,
        House,
        Farm,
        Factory,
        Shop,
        Office,
        School,
        Hospital,
        Park
    }

    public class House
    {
        public List<Vector2Int> Blocks { get; private set; } // 房屋的所有块
        public Vector2Int Size { get; private set; } // 房屋大小，影响房屋半径
        public City City { get; private set; } // 房屋所在的城市
        public bool IsAvailable { get; set; } // 房屋是否可用
        public HouseType Type { get; private set; } // 房屋类型

        private System.Random _chunkRand;

        public House(List<Vector2Int> blocks, Vector2Int size, City city, System.Random chunkRand)
        {
            Blocks = blocks; // 转换为全局坐标
            Size = size;
            City = city;
            _chunkRand = chunkRand;
        }

        public void MergeWithBlocks(List<Vector2Int> newBlocks)
        {
            foreach (var block in newBlocks)
            {
                if (!Blocks.Contains(block))
                {
                    Blocks.Add(block);
                }
            }

            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (var pos in Blocks)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y > maxY) maxY = pos.y;
            }

            Size = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
        }

        public void CalcHouseType()
        {
            if (Type != HouseType.None)
            {
                return;
            }
            var prob = _chunkRand.Next(0, 100);
            if (prob < 20)
            {
                Type = HouseType.House;
            }
            else if (prob < 40)
            {
                Type = HouseType.Farm;
            }
            else if (prob < 80)
            {
                Type = HouseType.Shop;
            }
        }
    }

}