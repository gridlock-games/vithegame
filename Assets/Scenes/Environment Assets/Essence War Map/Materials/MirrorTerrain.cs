using UnityEngine;

public class MirrorTerrain : MonoBehaviour
{
    public Terrain terrain;

    void Start()
    {
        if (terrain == null)
        {
            terrain = GetComponent<Terrain>();
        }

        MirrorTerrainHeightmap();
    }

    void MirrorTerrainHeightmap()
    {
        TerrainData terrainData = terrain.terrainData;
        int heightmapWidth = terrainData.heightmapResolution;
        int heightmapHeight = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, heightmapWidth, heightmapHeight);
        float[,] mirroredHeights = new float[heightmapWidth, heightmapHeight];

        for (int y = 0; y < heightmapHeight; y++)
        {
            for (int x = 0; x < heightmapWidth; x++)
            {
                mirroredHeights[y, x] = heights[y, heightmapWidth - 1 - x];
            }
        }

        terrainData.SetHeights(0, 0, mirroredHeights);
    }
}
