using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

using static Unity.Mathematics.math;

public class HashVisualization : MonoBehaviour
{
    // compile sync to immediately compile the burst version when needed
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct HashJob : IJobFor
    {
        [WriteOnly]
        public NativeArray<uint> hashes;

        public int resolution;

        // inverse resolution/reciprocal
        public float invResolution;

        public void Execute(int i)
        {
            // use uv coordinates to create an independent resolution
            // rather than just using index i
            // note that this is the same formula in getting uv coords in HLSL
            float v = floor(invResolution * i + 0.00001f);
            float u = i - resolution * v;

            // use Weyl's sequencing rather than a gradient
            hashes[i] = (uint)(frac(u * v * 0.381f) * 256f);
        }
    }

    /**
     * A variant of the xxHash constants by Yann Collet.
     * Skips the algorithms steps 2, 3, and 4.
     */
    public struct SmallXXHash
    {
        const uint primeA = 0b10011110001101110111100110110001;
        const uint primeB = 0b10000101111010111100101001110111;
        const uint primeC = 0b11000010101100101010111000111101;
        const uint primeD = 0b00100111110101001110101100101111;
        const uint primeE = 0b00010110010101100110011110110001;
    }

    static int
        hashesId = Shader.PropertyToID("_Hashes"),
        configId = Shader.PropertyToID("_Config");

    [SerializeField] Mesh instanceMesh;
    [SerializeField] Material material;
    [SerializeField, Range(1, 512)] int resolution = 16;

    NativeArray<uint> hashes;

    ComputeBuffer hashesBuffer;
    MaterialPropertyBlock propertyBlock;

    private void OnEnable()
    {
        int length = resolution * resolution;
        hashes = new NativeArray<uint>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length, 4);

        new HashJob
        {
            hashes = hashes,
            resolution = resolution,
            invResolution = 1f / resolution
        }.ScheduleParallel(hashes.Length, resolution, default).Complete();

        hashesBuffer.SetData(hashes);

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetVector(configId, new Vector4(resolution, 1f / resolution));
    }

    private void OnDisable()
    {
        hashes.Dispose();
        hashesBuffer.Release();
        hashesBuffer = null;
    }

    private void OnValidate()
    {
        // reset to refresh the grid
        if (hashesBuffer != null && enabled)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        OnDisable();
        OnEnable();
    }

    private void Update()
    {
        // draws the hash
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, 
            new Bounds(Vector3.zero, Vector3.one), hashes.Length, propertyBlock);
    }
}
