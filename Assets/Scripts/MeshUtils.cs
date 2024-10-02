#region

using System;
using System.ComponentModel;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

#endregion

namespace Ica.SkinnedMesh
{
    public static class MeshUtils
    {
        public static void AllocAndGetDataAsArray<T>(this Mesh.MeshData data, VertexAttribute attribute,
            out NativeArray<T> outData, Allocator allocator) where T : unmanaged
        {
            switch (attribute)
            {
                case VertexAttribute.Position:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetVertices(outData.Reinterpret<Vector3>());
                    break;
                case VertexAttribute.Normal:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetNormals(outData.Reinterpret<Vector3>());
                    break;
                case VertexAttribute.Tangent:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetTangents(outData.Reinterpret<Vector4>());
                    break;
                case VertexAttribute.Color:
                    if (typeof(T) != typeof(Color))
                    {
                        throw new ArrayTypeMismatchException("T must be Color, not " + typeof(T).Name);
                    }

                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetColors(outData.Reinterpret<Color>());
                    break;
                case VertexAttribute.TexCoord0:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(0, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord1:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(1, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord2:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(2, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord3:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(3, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord4:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(4, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord5:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(5, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord6:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(6, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.TexCoord7:
                    outData = new NativeArray<T>(data.vertexCount, allocator, NativeArrayOptions.UninitializedMemory);
                    data.GetUVs(7, outData.Reinterpret<Vector2>());
                    break;
                case VertexAttribute.BlendWeight:
                case VertexAttribute.BlendIndices:
                default:
                    throw new InvalidEnumArgumentException("Unsupported Vertex Attribute: " + attribute);
            }
        }

    }
}