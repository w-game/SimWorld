using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct ParallelNoiseJob : IJobParallelFor
{
    [WriteOnly]
    public NativeArray<float> result;  // 输出高度
    public int mapSize;
    public float landNoiseFreq;
    public int seed;

    public void Execute(int index)
    {
        // index -> (x,y)
        int x = index % mapSize;
        int y = index / mapSize;

        // 这里计算 PerlinNoise
        float n = Mathf.PerlinNoise((x + seed) * landNoiseFreq,
                                    (y + seed) * landNoiseFreq);
        float mapped = n * 2f - 1f;

        float heightVal = mapped * 20f;  // 示例
        // 写回结果
        result[index] = heightVal;
    }
}