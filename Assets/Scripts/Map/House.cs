using System;
using System.Collections.Generic;
using GameItem;
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
        public HouseType HouseType { get; private set; } // 房屋类型

        public List<GameItemBase> Furnitures { get; private set; } = new List<GameItemBase>(); // 房屋内的家具

        private System.Random _chunkRand;
        private Vector2Int _minPos; // 房屋最小坐标

        public House(List<Vector2Int> blocks, Vector2Int size, Vector2Int minPos, City city, System.Random chunkRand)
        {
            Blocks = blocks; // 转换为全局坐标
            Size = size;
            _minPos = minPos; // 房屋最小坐标
            City = city;
            _chunkRand = chunkRand;

            CalcHouseType();
            CalcFurnitures();
        }

        private void CalcFurnitures()
        {
            if (HouseType != HouseType.House) return;
            List<string> furnitures = new List<string>()
            {
                "BUILDING_TOILET",
                "BUILDING_TABLE",
                "BUILDING_STOVE"
                // "CHAIR_TABLE",
                // "CHAIR",
                // "TABLE",
                // "BED",
                // "SOFA",
                // "DESK",
                // "SHELF",
                // "CABINET"
            };

            foreach (var furniture in furnitures)
            {
                var pos = new Vector2Int(
                    _chunkRand.Next(_minPos.x + 1, _minPos.x + Size.x - 2),
                    _chunkRand.Next(_minPos.y + 1, _minPos.y + Size.y - 2));
                var config = GameManager.I.ConfigReader.GetConfig<BuildingConfig>(furniture);
                Debug.Log($"生成家具: {config.name}，位置: {pos}, 类型: {config.type}");
                var type = Type.GetType($"GameItem.{config.type}Item");
                var item = Activator.CreateInstance(type, new object[] { config, new Vector3(pos.x + 0.5f, pos.y + 0.5f, 0) }) as GameItemBase;
                Furnitures.Add(item);
            }
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

        private void CalcHouseType()
        {
            if (HouseType != HouseType.None)
            {
                return;
            }
            var prob = _chunkRand.Next(0, 100);
            if (prob < 20)
            {
                HouseType = HouseType.House;
            }
            else if (prob < 40)
            {
                HouseType = HouseType.Farm;
            }
            else if (prob < 80)
            {
                HouseType = HouseType.Shop;
            }
        }
    }

}