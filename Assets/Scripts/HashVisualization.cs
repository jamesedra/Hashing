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
        [ReadOnly] public NativeArray<float3> positions;
        
        [WriteOnly]
        public NativeArray<uint> hashes;
        // public int resolution;
        // inverse resolution/reciprocal
        // public float invResolution;
        public int seed;
        public SmallXXHash hash;
        public float3x4 domainTRS;

        public void Execute(int i)
        {
            // use uv coordinates to create an independent resolution
            // rather than just using index i
            // note that this is the same formula in getting uv coords in HLSL
            // float vf = (int)floor(invResolution * i + 0.00001f);
            // float uf = invResolution * (i - resolution * vf + 0.5f) - 0.5f;

            // vf = invResolution * (vf + 0.5f) - 0.5f;

            float3 p = mul(domainTRS, float4(positions[i], 1f));

            int u = (int)floor(p.x);
            int v = (int)floor(p.y);
            int w = (int)floor(p.z);

            // use Weyl's sequencing rather than a gradient
            // hashes[i] = (uint)(frac(u * v * 0.381f) * 256f);

            // use smallXXHash
            hashes[i] = hash.Eat(u).Eat(v).Eat(w);
        }
    }

    // shader identifiers
    static int
        hashesId = Shader.PropertyToID("_Hashes"),
        positionsId = Shader.PropertyToID("_Positions"),
        normalsId = Shader.PropertyToID("_Normals"),
        configId = Shader.PropertyToID("_Config");

    [SerializeField] Mesh instanceMesh;
    [SerializeField] Material material;
    [SerializeField, Range(1, 512)] int resolution = 16;
    [SerializeField] int seed; // changes the seed for the xxhash method
    // displacement used to have each point have its own direction
    [SerializeField, Range(-0.5f, 0.5f)] float displacement = 0.1f;
    [SerializeField]
    SpaceTRS domain = new SpaceTRS
    {
        scale = 8f
    };

    NativeArray<uint> hashes;

    NativeArray<float3> positions, normals;

    ComputeBuffer hashesBuffer, positionsBuffer, normalsBuffer;
    MaterialPropertyBlock propertyBlock;

    // check if there is a need to refresh
    bool isUpdated;

    private void OnEnable()
    {
        isUpdated = true;
        int length = resolution * resolution;
        hashes = new NativeArray<uint>(length, Allocator.Persistent);
        positions = new NativeArray<float3>(length, Allocator.Persistent);
        normals = new NativeArray<float3>(length, Allocator.Persistent);
        hashesBuffer = new ComputeBuffer(length, 4);
        positionsBuffer = new ComputeBuffer(length, 3 * 4);
        normalsBuffer = new ComputeBuffer(length, 3 * 4);

        propertyBlock ??= new MaterialPropertyBlock();
        propertyBlock.SetBuffer(hashesId, hashesBuffer);
        propertyBlock.SetBuffer(positionsId, positionsBuffer);
        propertyBlock.SetBuffer(normalsId, normalsBuffer);
        propertyBlock.SetVector(configId, new Vector4(resolution, 1f / resolution, displacement));
    }

    private void OnDisable()
    {
        hashes.Dispose();
        positions.Dispose();
        normals.Dispose();
        hashesBuffer.Release();
        positionsBuffer.Release();
        normalsBuffer.Release();
        hashesBuffer = null;
        positionsBuffer = null;
        normalsBuffer = null;
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
         *         domain.rotation.y += 10f * Time.deltaTime;
        Refresh();

        */

        // if there are changes in the visualization, set it to false and make a job handle with
        // changed data
        if (isUpdated || transform.hasChanged)
        {
            isUpdated = false;
            transform.hasChanged = false;

            JobHandle handle = Shapes.Job.ScheduleParallel(
                positions, normals, resolution, transform.localToWorldMatrix, default
                );

            new HashJob
            {
                positions = positions,
                hashes = hashes,
                hash = SmallXXHash.Seed(seed),
                domainTRS = domain.Matrix
            }.ScheduleParallel(hashes.Length, resolution, handle).Complete();

            hashesBuffer.SetData(hashes);
            positionsBuffer.SetData(positions);
            normalsBuffer.SetData(normals);
        }


        // draws the hash
        Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, 
            new Bounds(Vector3.zero, Vector3.one), hashes.Length, propertyBlock);
    }
}
