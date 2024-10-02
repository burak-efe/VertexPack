using System.Runtime.InteropServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

public static class VertexPacking
{
    
    public static uint F32toSNorm(float f32, uint bitCount)
    {
        uint maxVal = (uint)pow(2, bitCount) - 1;
        f32 += 1;
        f32 *= maxVal / 2;
        return (uint)clamp(f32, 0, maxVal);
    }

    public static float SNormtoF32(uint snorm, uint bitCount)
    {
        uint maxVal = (uint)pow(2, bitCount) - 1;
        float val = (float)snorm / (maxVal / 2);
        val -= 1.0f;
        val = clamp(val, -1, 1);
        return val;
    }


    public static uint F32toUNorm(float f32, uint bitCount)
    {
        uint maxVal = (uint)pow(2, bitCount) - 1;
        f32 *= maxVal;
        return (uint)clamp(f32, 0, maxVal);
    }

    public static float UNormtoF32(uint unorm, uint bitCount)
    {
        uint maxVal = (uint)pow(2, bitCount) - 1;
        float val = (float)unorm / maxVal;
        return clamp(val, 0, 1);
    }

    
    public static float2 OctSignNotZero(float2 v)
    {
        return new float2(
            (v.x >= 0.0f) ? +1.0f : -1.0f,
            (v.y >= 0.0f) ? +1.0f : -1.0f);
    }

    public static float2 F32x3ToOct(in float3 v)
    {
        // Project the sphere onto the octahedron, and then onto the xy plane
        float2 p = v.xy * (1.0f / (abs(v.x) + abs(v.y) + abs(v.z)));
        // Reflect the folds of the lower hemisphere over the diagonals
        return (v.z <= 0.0f) ? ((1.0f - abs(p.yx)) * OctSignNotZero(p)) : p;
    }

    public static float3 OctToF32x3(float2 e)
    {
        float3 v = new float3(e.xy, 1.0f - abs(e.x) - abs(e.y));
        if (v.z < 0) v.xy = (1.0f - abs(v.yx)) * OctSignNotZero(v.xy);
        return normalize(v);
    }

    public static float2 F32x3ToOctPrecise(float3 v, int n)
    {
        float2 s = F32x3ToOct(v); // Remap to the square
        // Each snorm’s max value interpreted as an integer,
        // e.g., 127.0 for snorm8
        //float M = float(1 << ((n / 2) - 1)) - 1.0;
        float M = (1 << ((n / 2) - 1)) - 1.0f;
        // Remap components to snorm(n/2) precision...with floor instead
        // of round (see equation 1)
        s = floor(clamp(s, -1.0f, +1.0f) * M) * (1.0f / M);
        float2 bestRepresentation = s;
        float highestCosine = dot(OctToF32x3(s), v);
        // Test all combinations of floor and ceil and keep the best.
        // Note that at +/- 1, this will exit the square... but that
        // will be a worse encoding and never win.
        for (int i = 0; i <= 1; ++i)
        for (int j = 0; j <= 1; ++j)
            // This branch will be evaluated at compile time
            if ((i != 0) || (j != 0))
            {
                // Offset the bit pattern (which is stored in floating
                // point!) to effectively change the rounding mode
                // (when i or j is 0: floor, when it is one: ceiling)
                float2 candidate = new float2(i, j) * (1 / M) + s;
                float cosine = dot(OctToF32x3(candidate), v);
                if (cosine > highestCosine)
                {
                    bestRepresentation = candidate;
                    highestCosine = cosine;
                }
            }

        return bestRepresentation;
    }


    public static float EncodeDiamond(float2 p)
    {
        // Project to the unit diamond, then to the x-axis.
        float x = p.x / (abs(p.x) + abs(p.y));

        // Contract the x coordinate by a factor of 4 to represent all 4 quadrants in
        // the unit range and remap
        float py_sign = sign(p.y);
        return -py_sign * 0.25f * x + 0.5f + py_sign * 0.25f;
    }

    public static float2 DecodeDiamond(float p)
    {
        float2 v;
        float p_sign = sign(p - 0.5f);
        v.x = -p_sign * 4.0f * p + 1.0f + p_sign * 2.0f;
        v.y = p_sign * (1.0f - abs(v.x));
        return normalize(v);
    }

    public static float EncodeTangent(float3 normal, float3 tangent)
    {
        // First, find a canonical direction in the tangent plane
        float3 t1;
        if (abs(normal.y) > abs(normal.z))
        {
            // Pick a canonical direction orthogonal to n with z = 0
            t1 = float3(normal.y, -normal.x, 0.0f);
        }
        else
        {
            // Pick a canonical direction orthogonal to n with y = 0
            t1 = float3(normal.z, 0.0f, -normal.x);
        }

        t1 = normalize(t1);
        // Construct t2 such that t1 and t2 span the plane
        float3 t2 = cross(t1, normal);

        

        // Decompose the tangent into two coordinates in the canonical basis
        float2 packed_tangent = new float2(dot(tangent, t1), dot(tangent, t2));

        // Apply our diamond encoding to our two coordinates
        return EncodeDiamond(packed_tangent);
    }

    public static float3 DecodeTangent(float3 normal, float diamond_tangent)
    {
        // First, find a canonical direction in the tangent plane
        float3 t1;
        if (abs(normal.y) > abs(normal.z))
        {
            // Pick a canonical direction orthogonal to n with z = 0
            t1 = float3(normal.y, -normal.x, 0.0f);
        }
        else
        {
            // Pick a canonical direction orthogonal to n with y = 0
            t1 = float3(normal.z, 0.0f, -normal.x);
        }

        t1 = normalize(t1);
        // Construct t2 such that t1 and t2 span the plane
        float3 t2 = cross(t1, normal);

        

        float2 packed_tangent = DecodeDiamond(diamond_tangent);
        return packed_tangent.x * t1 + packed_tangent.y * t2;
    }

}