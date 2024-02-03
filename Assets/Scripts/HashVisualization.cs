using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
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
        public int seed;
        public SmallXXHash hash;
        public float3x4 domainTRS;

        public void Execute(int i)
        {
            // use uv coordinates to create an independent resolution
            // rather than just using index i
            // note that this is the same formula in getting uv coords in HLSL
            float vf = (int)floor(invResolution * i + 0.00001f);
            float uf = invResolution * (i - resolution * vf + 0.5f) - 0.5f;

            vf = invResolution * (vf + 0.5f) - 0.5f;

            float3 p = mul(domainTRS, float4(uf, 0f, vf, 1f));

            int u = (int)floor(p.x);
            int v = (int)floor(p.y);
            int w = (int)floor(p.z);

            // use Weyl's sequencing rather than a gradient
            // hashes[i] = (uint)(frac(u * v * 0.381f) * 256f);

            // use smallXXHash
            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }

    static int
        hashesId = Shader.PropertyToID("_Hashes"),
        configId = Shader.PropertyToID("_Config");

    [SerializeField] Mesh instanceMesh;
    [SerializeField] Material material;
    [SerializeField, Range(1, 512)] int resolution = 16;
    [SerializeField] int seed; // changes the seed for the xxhash method
    [SerializeField, Range(-2f, 2f)] float verticalOffset = 1f;
    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };

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
            invResolution = 1f / resolution,
            hash = SmallXXHash.Seed(seed),
            domainTRS = domain.Matrix,
            
        }.ScheduleParallel(hashes.Length, resolution, default).Complete();

        hashesBuffer.SetData(hashes);
        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetVector(configId, new Vector4(resolution, 1f / resolution, verticalOffset / resolution));
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
        /*
         * Testing purposes

        */
        domain.rotation.y += 10f * Time.deltaTime;
        Refresh();


        // draws the hash
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, 
            new Bounds(Vector3.zero, Vector3.one), hashes.Length, propertyBlock);
    }
}
