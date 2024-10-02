using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Ica.SkinnedMesh;
using TMPro;
using Unity.Burst;
using UnityEngine.UI;


[BurstCompile]
public class PackAttribute : MonoBehaviour
{
    [Header("UI Elements")]
    public Slider NormalBitSlider;
    public Slider TangentBitSlider;
    public TMP_Text NormalText;
    public TMP_Text TangentText;
    public Toggle VisNormalToggle;
    public Toggle VisTangentToggle;
    public Toggle NormalizeColorsToggle;
    
    [Header("Render Settings")]
    public Material VisNormalMat;
    public Material VisTangentMat;
    public MeshRenderer MeshRenderer;
    public MeshRenderer RefMeshRenderer;
    public Mesh SourceMesh;

    public uint NormalBitCount = 8;
    public uint TangentAngleBitCount = 8;
    
    [Header("Debug Only")]
    public Mesh TargetMesh;

    private NativeArray<float3> Normals;
    private NativeArray<float4> Tangents;



    private void OnDestroy()
    {
        Normals.Dispose();
        Tangents.Dispose();
        Destroy(TargetMesh);
    }

    private void Start()
    {
        TargetMesh = new Mesh();
        TargetMesh.SetIndexBufferParams(TargetMesh.triangles.Length, SourceMesh.indexFormat);
        TargetMesh.vertices = SourceMesh.vertices;
        TargetMesh.triangles = SourceMesh.triangles;
        MeshRenderer.GetComponent<MeshFilter>().sharedMesh = TargetMesh;
        RefMeshRenderer.GetComponent<MeshFilter>().sharedMesh = SourceMesh;


        var mda = Mesh.AcquireReadOnlyMeshData(SourceMesh);
        var data = mda[0];
        data.AllocAndGetDataAsArray(VertexAttribute.Normal, out Normals, Allocator.Persistent);
        data.AllocAndGetDataAsArray(VertexAttribute.Tangent, out Tangents, Allocator.Persistent);
        mda.Dispose();

        NormalBitSlider.maxValue = 32;
        NormalBitSlider.minValue = 2;
        NormalBitSlider.wholeNumbers = true;
        NormalBitSlider.onValueChanged.AddListener((value =>
        {
            if ((uint)value % 2 != 0)
            {
                value += 0.1f;
                value = math.ceil(value);
                NormalBitSlider.value = value;
            }

            NormalBitCount = (uint)value;
            NormalText.text = "Normal: " + NormalBitCount.ToString() + " Bit";
            Calc();
        }));

        TangentBitSlider.maxValue = 32;
        TangentBitSlider.minValue = 2;
        TangentBitSlider.wholeNumbers = true;
        TangentBitSlider.onValueChanged.AddListener((value =>
        {
            TangentAngleBitCount = (uint)value;
            TangentText.text = "Tangent: " + TangentAngleBitCount + "+1 Bit";
            Calc();
        }));

        NormalBitSlider.value = NormalBitCount;
        TangentBitSlider.value = TangentAngleBitCount;

        VisNormalToggle.onValueChanged.AddListener(b =>
        {
            if (b)
            {
                MeshRenderer.sharedMaterial = VisNormalMat;
                RefMeshRenderer.sharedMaterial = VisNormalMat;
            }
        });

        VisTangentToggle.onValueChanged.AddListener(b =>
        {
            if (b)
            {
                MeshRenderer.sharedMaterial = VisTangentMat;
                RefMeshRenderer.sharedMaterial = VisTangentMat;
            }
        });

        NormalizeColorsToggle.onValueChanged.AddListener((b =>
        {
            VisNormalMat.SetFloat("_Normalize", b ? 1 : 0);
            VisTangentMat.SetFloat("_Normalize", b ? 1 : 0);
        }));
    }

    public void Calc()
    {
        var outNormals = new NativeArray<float3>(Normals.Length, Allocator.Temp);
        var outTangents = new NativeArray<float4>(Normals.Length, Allocator.Temp);
        Calculate(Normals, Tangents, ref outNormals, ref outTangents, NormalBitCount, TangentAngleBitCount);


        TargetMesh.SetNormals(outNormals);
        TargetMesh.SetTangents(outTangents);
    }

    [BurstCompile]
    private static void Calculate(
        [NoAlias] in NativeArray<float3> inNormals,
        [NoAlias] in NativeArray<float4> inTangents,
        [NoAlias] ref NativeArray<float3> outNormals,
        [NoAlias] ref NativeArray<float4> outTangents,
        uint NormalBitCount,
        uint TangentAngleBitCount)
    {
        for (int i = 0; i < inNormals.Length; i++)
        {
            //Encode
            float2 normalEncoded = VertexPacking.F32x3ToOct(inNormals[i]);

            //Pack
            uint2 normalPacked = new uint2(
                VertexPacking.F32toSNorm(normalEncoded.x, NormalBitCount / 2),
                VertexPacking.F32toSNorm(normalEncoded.y, NormalBitCount / 2));

            //Unpack
            float2 normalUnpacked = new float2(
                VertexPacking.SNormtoF32(normalPacked.x, NormalBitCount / 2),
                VertexPacking.SNormtoF32(normalPacked.y, NormalBitCount / 2));

            //Decode
            float3 normalDecoded = VertexPacking.OctToF32x3(normalUnpacked);

            
            float tangentEncoded = VertexPacking.EncodeTangent(normalDecoded, inTangents[i].xyz);
            var tangentPacked = VertexPacking.F32toUNorm(tangentEncoded, TangentAngleBitCount);
            var tangentUnpacked = VertexPacking.UNormtoF32(tangentPacked, TangentAngleBitCount);
            var tangentDecoded = VertexPacking.DecodeTangent(normalDecoded, tangentUnpacked);

            //SetData
            outNormals[i] = normalDecoded;
            outTangents[i] = new float4(tangentDecoded, inTangents[i].w);
        }
    }
}