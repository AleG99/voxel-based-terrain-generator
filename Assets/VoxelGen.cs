using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate float[,,] NoiseFunction(float[,,] noise, int x, int y, int z);

public class VoxelGen : MonoBehaviour {

	public enum NoiseFunctions { Identity, Inverse, Square, Root, HeightFalloff };
	public NoiseFunction[] myNoiseFunctions = { Identity, Inverse, Square, Root, HeightFalloff };
	public NoiseFunctions noiseFunction = NoiseFunctions.Identity;
	public int size = 200;
	[Range(0, 5)] public int octaves = 0;
	[Range(0, 1f)] public float threshold = 0.5f;
	public int resolution = 32;
	public float amplitude = 1f;
	public bool createAir = false;
    public int randomSeed = 0;
	public GameObject voxelRock;
	public GameObject voxelAir;

	void Start() {
		if (voxelRock != null && voxelAir != null) {
            if (randomSeed == 0) randomSeed = (int)System.DateTime.Now.Ticks;
		    Random.InitState(randomSeed);

            float[,,] noise = Amplify(Noise3D(size, size, size, resolution), size, size, size, amplitude);
            for (int h = 1; h <= octaves; h++) {
                float[,,] octave = Amplify(Noise3D(size, size, size, resolution / (int)System.Math.Pow(2, h)),
                                           size, size, size, amplitude / (int)System.Math.Pow(2, h));
                noise = Sum(noise, octave, size, size, size);
            }

            noise = Normalize(noise, size, size, size);

            noise = myNoiseFunctions[(int)noiseFunction](noise, size, size, size);

			for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    for (int k = 0; k < size; k++) {
                        if (noise[i, j, k] > threshold)
                            Instantiate(voxelRock, new Vector3(i + 0.5f, j + 0.5f, k + 0.5f), Quaternion.identity);
                        else if (createAir)
                            Instantiate(voxelAir, new Vector3(i + 0.5f, j + 0.5f, k + 0.5f), Quaternion.identity);
                    }
                }
            }
		}
	}

	private float[,,] Noise3D(int x, int y, int z, int resolution) {
        int xOut = 1 + resolution * (1 + (int)Mathf.Ceil(x / resolution));
		int yOut = 1 + resolution * (1 + (int)Mathf.Ceil(y / resolution));
		int zOut = 1 + resolution * (1 + (int)Mathf.Ceil(z / resolution));

		Vector3[,,] gradients = new Vector3[xOut, yOut, zOut];

		for (int i = 0; i < xOut; i += resolution) {
			for (int j = 0; j < yOut; j += resolution) {
				for (int k = 0; k < zOut; k += resolution) {
					gradients[i, j, k] = Random.insideUnitSphere;
				}
			}
		}

        float[,,] noise = new float[x, y, z];
        
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    int floorX = resolution * ((int)Mathf.Floor((float)i / resolution));
                    int floorY = resolution * ((int)Mathf.Floor((float)j / resolution));
                    int floorZ = resolution * ((int)Mathf.Floor((float)k / resolution));
                    int ceilX = resolution * ((int)Mathf.Ceil((float)i / resolution));
                    int ceilY = resolution * ((int)Mathf.Ceil((float)j / resolution));
                    int ceilZ = resolution * ((int)Mathf.Ceil((float)k / resolution));

                    float h1 = Vector3.Dot(gradients[floorX, floorY, floorZ], new Vector3(i, j, k) - new Vector3(floorX, floorY, floorZ));
                    float h2 = Vector3.Dot(gradients[floorX, floorY, ceilZ], new Vector3(i, j, k) - new Vector3(floorX, floorY, ceilZ));
                    float h3 = Vector3.Dot(gradients[floorX, ceilY, floorZ], new Vector3(i, j, k) - new Vector3(floorX, ceilY, floorZ));
                    float h4 = Vector3.Dot(gradients[floorX, ceilY, ceilZ], new Vector3(i, j, k) - new Vector3(floorX, ceilY, ceilZ));
                    float h5 = Vector3.Dot(gradients[ceilX, floorY, floorZ], new Vector3(i, j, k) - new Vector3(ceilX, floorY, floorZ));
                    float h6 = Vector3.Dot(gradients[ceilX, floorY, ceilZ], new Vector3(i, j, k) - new Vector3(ceilX, floorY, ceilZ));
                    float h7 = Vector3.Dot(gradients[ceilX, ceilY, floorZ], new Vector3(i, j, k) - new Vector3(ceilX, ceilY, floorZ));
                    float h8 = Vector3.Dot(gradients[ceilX, ceilY, ceilZ], new Vector3(i, j, k) - new Vector3(ceilX, ceilY, ceilZ));

                    float u = ((float)i - floorX) / resolution;
                    float v = ((float)j - floorY) / resolution;
                    float w = ((float)k - floorZ) / resolution;

                    float l1 = Mathf.Lerp(h1, h5, Slope(u));
                    float l2 = Mathf.Lerp(h2, h6, Slope(u));
                    float l3 = Mathf.Lerp(h3, h7, Slope(u));
                    float l4 = Mathf.Lerp(h4, h8, Slope(u));

                    float m1 = Mathf.Lerp(l1, l3, Slope(v));
                    float m2 = Mathf.Lerp(l2, l4, Slope(v));

                    noise[i, j, k] = Mathf.Lerp(m1, m2, Slope(w));
                }
            }
        }

        return noise;
	}

    private float[,,] Amplify(float[,,] m, int x, int y, int z, float amplitude) {
		for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    m[i, j, k] *= amplitude;
                }
            }
        }
		return m;
	}
    
    private float[,,] Normalize(float[,,] m, int x, int y, int z) {
		float max, min;
		max = float.MinValue;
		min = float.MaxValue;
        
		for (int i = 0; i < x; i++) {
			for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    if (m[i, j, k] < min) min = m[i, j, k];
                    if (m[i, j, k] > max) max = m[i, j, k];
                }
			}
		}
		for (int i = 0; i < x; i++) {
			for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
				    m[i, j, k] = (m[i, j, k] - min) / (max - min);
                }
			}
		}
		return m;
	}

    private float[,,] Sum(float[,,] m1, float[,,] m2, int x, int y, int z) {
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    m1[i, j, k] += m2[i, j, k];
                }
            }
        }
        return m1;
    }

	private static float Slope(float x) {
		return -2f * Mathf.Pow(x, 3) + 3f * Mathf.Pow(x, 2);
	}

	private static float[,,] Identity(float[,,] noise, int x, int y, int z) {
        return noise;
    }
    
    private static float[,,] Inverse(float[,,] noise, int x, int y, int z) {
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    noise[i, j, k] = 1f - noise[i, j, k];
                }
            }
        }
		return noise;
	}

	private static float[,,] Square(float[,,] noise, int x, int y, int z) {
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    noise[i, j, k] = Mathf.Pow(noise[i, j, k], 2f);
                }
            }
        }
        return noise;
    }
    
    private static float[,,] Root(float[,,] noise, int x, int y, int z) {
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    noise[i, j, k] = Mathf.Pow(noise[i, j, k], 0.5f);
                }
            }
        }
		return noise;
	}
    
    private static float[,,] HeightFalloff(float[,,] noise, int x, int y, int z) {
        for (int i = 0; i < x; i++) {
            for (int j = 0; j < y; j++) {
                for (int k = 0; k < z; k++) {
                    float falloff = 1f - ((float)j / y);
                    noise[i, j, k] *= falloff;
                }
            }
        }
		return noise;
    }
}
