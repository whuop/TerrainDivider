using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Landfall.Editor
{
    public static class TerrainDivider
    {
        private static float SubtractFromAndReturn(ref float subtractee, float subtractAmount)
        {
            float retVal = 0.0f;
            if (subtractAmount >= subtractee)
            {
                retVal = subtractee;
                subtractee = 0.0f;
                return retVal;
            }

            subtractee -= subtractAmount;
            retVal = subtractAmount;
            return retVal;
        }

        public static List<Terrain> SplitIntoChunks(int chunkSizeX, int chunkSizeZ, Terrain origTerrain, string terrainSavePath)
        {
            //  Create folder structure
            Landfall.Editor.PathUtils.EnsurePathExists(terrainSavePath);

            if (origTerrain == null)
            {
                Debug.LogWarning("No terrain found on transform");
                return null;
            }

            List<Terrain> terrains = new List<Terrain>();

            float totalAllotedSizeX = origTerrain.terrainData.size.x;
            float totalAllotedSizeZ = origTerrain.terrainData.size.z;

            float totalTakenX = 0.0f;
            float totalTakenZ = 0.0f;

            float xMin = 0.0f;
            float xMax = 0.0f;
            float zMin = 0.0f;
            float zMax = 0.0f;

            int x = 0;
            int z = 0;
            while (totalAllotedSizeX > 0)
            {
                float takenX = SubtractFromAndReturn(ref totalAllotedSizeX, (float)chunkSizeX);


                xMin = totalTakenX;
                totalTakenX += takenX;
                xMax = totalTakenX;

                while (totalAllotedSizeZ > 0)
                {
                    //EditorUtility.DisplayProgressBar("Splitting Terrain", "Copying heightmap, detail, splat, and trees", (float)((x * numSplitsZ) + z) / (numSplitsX * numSplitsZ));
                    float takenZ = SubtractFromAndReturn(ref totalAllotedSizeZ, (float)chunkSizeZ);

                    zMin = totalTakenZ;
                    totalTakenZ += takenZ;
                    zMax = totalTakenZ;

                    float cSizeX = (xMax - xMin);
                    float cSizeZ = (zMax - zMin);

                    float chunkOffsetX = xMin / origTerrain.terrainData.size.x;
                    float chunkOffsetZ = zMin / origTerrain.terrainData.size.z;

                    float chunkWidthRatio = cSizeX / origTerrain.terrainData.size.x;
                    float chunkDepthRatio = cSizeZ / origTerrain.terrainData.size.z;
                    
                    float chunkSizeRatio = (chunkWidthRatio > chunkDepthRatio) ? chunkWidthRatio : chunkDepthRatio;

                    Debug.LogError("Largest Percent: " + chunkSizeRatio);
                    Debug.LogError("ChunkSizePercent X/Z: " + chunkWidthRatio + "/" + chunkDepthRatio);

                    int heightmapResolution = Mathf.RoundToInt((float)origTerrain.terrainData.heightmapResolution * chunkSizeRatio);
                    heightmapResolution = NearestPoT(heightmapResolution) + 1;

                    int splatResolution = Mathf.RoundToInt((float)origTerrain.terrainData.alphamapResolution * chunkSizeRatio);
                    splatResolution = NearestPoT(splatResolution);

                    int detailResolution = Mathf.RoundToInt((float)origTerrain.terrainData.detailResolution * chunkSizeRatio);
                    detailResolution = NearestPoT(detailResolution);

                    int basemapResolution = Mathf.RoundToInt((float)origTerrain.terrainData.baseMapResolution * chunkSizeRatio);
                    basemapResolution = NearestPoT(basemapResolution);

                    

                    //  Switched X for Z in CopyTerrain. Not really sure why that has to be done currently, but if i dont everything is mirrored all weird, so i think it's just that the 
                    //  x and z index of the loop does not correspond to the order they are being looped.
                    var terrainChunk = CopyTerrain(origTerrain, string.Format("{0}{1}_{2}", origTerrain.name, x, z), terrainSavePath, xMin, xMax, zMin, zMax, heightmapResolution, detailResolution, splatResolution, basemapResolution, chunkOffsetX, chunkOffsetZ, chunkWidthRatio, chunkDepthRatio, x, z);
                    terrains.Add(terrainChunk);

                    Debug.LogError("Heightmap Resolution: " + heightmapResolution);
                    Debug.LogError("Splat Resolution: " + splatResolution);
                    Debug.LogError("Detail Resolution: " + detailResolution);
                    Debug.LogError("Basemap Resolution: " + basemapResolution);



                    /*float[,,] srcAlphamap = origTerrain.terrainData.GetAlphamaps(0, 0, origTerrain.terrainData.alphamapWidth, origTerrain.terrainData.alphamapHeight);

                    for(int i = 0; i < srcAlphamap.GetLength(2); i++)
                    {
                        float[,] resizeMap = new float[srcAlphamap.GetLength(0), srcAlphamap.GetLength(1)];
                        for(int xx = 0; xx < srcAlphamap.GetLength(0); xx++)
                        {
                            for(int yy = 0; yy < srcAlphamap.GetLength(1); yy++)
                            {
                                resizeMap[xx, yy] = srcAlphamap[xx, yy, i];
                            }
                        }

                        float[,] dstAlphamap = new float[splatResolution, splatResolution];
                        FloatArrayRescaler.RescaleArray(resizeMap, dstAlphamap);
                    }*/
                    z++;
                }

                totalAllotedSizeZ = origTerrain.terrainData.size.z;
                totalTakenZ = 0.0f;
                zMin = 0.0f;
                zMax = 0.0f;
                z = 0;
                x++;
            }


            /*for (int x = 0; x < numSplitsX; x++)
            {
                for (int z = 0; z < numSplitsZ; z++)
                {
                    EditorUtility.DisplayProgressBar("Splitting Terrain", "Copying heightmap, detail, splat, and trees", (float)((x * numSplitsZ) + z) / (numSplitsX * numSplitsZ));
                    float xMin = origTerrain.terrainData.size.x / numSplitsX * x;
                    float xMax = origTerrain.terrainData.size.x / numSplitsX * (x + 1);
                    float zMin = origTerrain.terrainData.size.z / numSplitsZ * z;
                    float zMax = origTerrain.terrainData.size.z / numSplitsZ * (z + 1);
                    CopyTerrain(origTerrain, terrains, string.Format("{0}{1}_{2}", origTerrain.name, x, z), terrainSavePath, xMin, xMax, zMin, zMax, heightResolution, detailResolution, splatResolution, x, z);
                }
            }*/
            //EditorUtility.ClearProgressBar();


            return terrains;
        }

        static Terrain CopyTerrain(Terrain origTerrain, string newName, string savePath, float xMin, float xMax, float zMin, float zMax, int heightmapResolution, int detailResolution, int alphamapResolution, int basemapResolution, float chunkOffsetX, float chunkOffsetZ, float chunkWidthRatio, float chunkDepthRatio, int chunkX, int chunkZ)
        {
            if (heightmapResolution < 33 || heightmapResolution > 4097)
            {
                Debug.Log("Invalid heightmap resolution " + heightmapResolution);
                return null;
            }
            if (detailResolution < 17 || detailResolution > 4048)
            {
                Debug.LogError("Invalid detailResolution " + detailResolution);
                return null;
            }
            if (alphamapResolution < 17 || alphamapResolution > 2048)
            {
                Debug.LogError("Invalid alphamapResolution " + alphamapResolution);
                return null;
            }

            if (xMin < 0 || xMin > xMax || xMax > origTerrain.terrainData.size.x)
            {
                Debug.LogError("Invalid xMin or xMax");
                return null;
            }
            if (zMin < 0 || zMin > zMax || zMax > origTerrain.terrainData.size.z)
            {
                Debug.LogError("Invalid zMin or zMax");
                return null;
            }

            //  Remove old terrain asset if it exists.
            string assetPath = savePath + newName + ".asset";
            if (AssetDatabase.FindAssets(newName).Length != 0)
            {
                Debug.Log("Asset with name " + newName + " already exists, deleting old one to make room for new.");
                AssetDatabase.DeleteAsset(assetPath);
            }

            //  Remove old terrain game object if it exists.
            GameObject oldT = GameObject.Find(newName);
            if (oldT != null)
            {
                Debug.Log("Terrain Game object with name " + newName + " already exists. Deleting old one to make room for new.");
                GameObject.DestroyImmediate(oldT);
            }

            TerrainData td = new TerrainData();
            GameObject gameObject = Terrain.CreateTerrainGameObject(td);
            Terrain newTerrain = gameObject.GetComponent<Terrain>();


            // Must do this before Splat
            //  Create the actual asset
            AssetDatabase.CreateAsset(td, assetPath);

            //  Lighting
            newTerrain.lightmapIndex = origTerrain.lightmapIndex;
            newTerrain.lightmapScaleOffset = origTerrain.lightmapScaleOffset;
            newTerrain.realtimeLightmapIndex = origTerrain.realtimeLightmapIndex;
            newTerrain.realtimeLightmapScaleOffset = origTerrain.realtimeLightmapScaleOffset;
            newTerrain.reflectionProbeUsage = origTerrain.reflectionProbeUsage;
            newTerrain.castShadows = origTerrain.castShadows;

            //  Material
            newTerrain.materialTemplate = origTerrain.materialTemplate;
            newTerrain.materialType = origTerrain.materialType;

            //  Legacy
            newTerrain.legacyShininess = origTerrain.legacyShininess;
            newTerrain.legacySpecular = origTerrain.legacySpecular;

            //  Heightmap
            newTerrain.drawHeightmap = origTerrain.drawHeightmap;
            newTerrain.heightmapMaximumLOD = origTerrain.heightmapMaximumLOD;
            newTerrain.heightmapPixelError = origTerrain.heightmapPixelError;

            //  Detail
            newTerrain.detailObjectDensity = origTerrain.detailObjectDensity;
            newTerrain.detailObjectDistance = origTerrain.detailObjectDistance;

            newTerrain.collectDetailPatches = origTerrain.collectDetailPatches;
            newTerrain.patchBoundsMultiplier = origTerrain.patchBoundsMultiplier;

            //  Tree
            td.treePrototypes = origTerrain.terrainData.treePrototypes;
            newTerrain.drawTreesAndFoliage = origTerrain.drawTreesAndFoliage;
            newTerrain.treeBillboardDistance = origTerrain.treeBillboardDistance;
            newTerrain.treeCrossFadeLength = origTerrain.treeCrossFadeLength;
            newTerrain.treeDistance = origTerrain.treeDistance;
            newTerrain.treeMaximumFullLODCount = origTerrain.treeMaximumFullLODCount;
            newTerrain.bakeLightProbesForTrees = origTerrain.bakeLightProbesForTrees;

            //  Misc
            newTerrain.editorRenderFlags = origTerrain.editorRenderFlags;
            newTerrain.basemapDistance = origTerrain.basemapDistance;

            //  TerrainData
            td.detailPrototypes = origTerrain.terrainData.detailPrototypes;

            //  Adjust splatmap tile position to chunk position
            var splats = origTerrain.terrainData.splatPrototypes;
            foreach (var splat in splats)
            {
                splat.tileOffset = new Vector2((heightmapResolution - 1) * chunkX, (heightmapResolution - 1) * chunkZ);
            }
            td.splatPrototypes = splats;

            td.alphamapResolution = alphamapResolution;
            td.SetDetailResolution(detailResolution, 8);
            td.baseMapResolution = basemapResolution;

            //  Grass
            td.wavingGrassAmount = origTerrain.terrainData.wavingGrassAmount;
            td.wavingGrassSpeed = origTerrain.terrainData.wavingGrassSpeed;
            td.wavingGrassStrength = origTerrain.terrainData.wavingGrassStrength;
            td.wavingGrassTint = origTerrain.terrainData.wavingGrassTint;

            // Get percent of original
            float xMinNorm = xMin / (float)origTerrain.terrainData.heightmapWidth;
            float xMaxNorm = xMax / (float)origTerrain.terrainData.heightmapWidth;
            float zMinNorm = zMin / (float)origTerrain.terrainData.heightmapHeight;
            float zMaxNorm = zMax / (float)origTerrain.terrainData.heightmapHeight;

            Debug.LogError("XminNorm: " + xMinNorm);
            Debug.LogError("XMaxNorm: " + xMaxNorm);

            Vector2 startSamples = new Vector2(
                xMinNorm * (float)origTerrain.terrainData.heightmapWidth,
                zMinNorm * (float)origTerrain.terrainData.heightmapHeight
                );

            Vector2 endSamples = new Vector2(
                xMaxNorm * (float)origTerrain.terrainData.heightmapWidth,
                zMaxNorm * (float)origTerrain.terrainData.heightmapHeight
                );

            Debug.LogError("Start/End sample X: " + startSamples.x + "/" + endSamples.x);
            Debug.LogError("Start/End sample Y: " + startSamples.y + "/" + endSamples.y);

            // Height
            //Vector2 newTerrainSize = new Vector2(xMax - xMin, zMax - zMin);
            CalculateSubHeightmap(td, heightmapResolution, origTerrain, chunkOffsetX, chunkOffsetZ, chunkWidthRatio, chunkDepthRatio, chunkX, chunkZ);

            // Must happen after setting heightmapResolution
            td.size = new Vector3(xMax - xMin, origTerrain.terrainData.size.y, zMax - zMin);

            // Calculate sub splat map
            CalculateSubSplatmaps(td, origTerrain, alphamapResolution, startSamples, endSamples, chunkWidthRatio, chunkDepthRatio, chunkOffsetX, chunkOffsetZ);

            //  Calculate sub detail map

            // Detail
            /*td.SetDetailResolution(detailResolution, 8); // Default? Haven't messed with resolutionPerPatch
            for (int layer = 0; layer < origTerrain.terrainData.detailPrototypes.Length; layer++)
            {
                int[,] detailLayer = origTerrain.terrainData.GetDetailLayer(0, 0, origTerrain.terrainData.detailWidth, origTerrain.terrainData.detailHeight, layer);
                int[,] newDetailLayer = new int[detailResolution, detailResolution];
                for (int x = 0; x < newDetailLayer.GetLength(0); x++)
                {
                    for (int z = 0; z < newDetailLayer.GetLength(1); z++)
                    {
                        newDetailLayer[z, x] = detailLayer[chunkZ * (detailResolution) + z, chunkX * (detailResolution) + x];
                    }
                }
                td.SetDetailLayer(0, 0, layer, newDetailLayer);
            }*/




            // Tree
            /*for (int i = 0; i < origTerrain.terrainData.treeInstanceCount; i++)
            {
                TreeInstance ti = origTerrain.terrainData.treeInstances[i];
                if (ti.position.x < xMinNorm || ti.position.x >= xMaxNorm)
                    continue;
                if (ti.position.z < zMinNorm || ti.position.z >= zMaxNorm)
                    continue;
                ti.position = new Vector3(((ti.position.x * origTerrain.terrainData.size.x) - xMin) / (xMax - xMin), ti.position.y, ((ti.position.z * origTerrain.terrainData.size.z) - zMin) / (zMax - zMin));
                newTerrain.AddTreeInstance(ti);
            }*/

            gameObject.transform.position = new Vector3(origTerrain.transform.position.x + xMin, origTerrain.transform.position.y, origTerrain.transform.position.z + zMin);
            gameObject.name = newName;

            newTerrain.ApplyDelayedHeightmapModification();
            td.SetBaseMapDirty();

            AssetDatabase.SaveAssets();

            AssetDatabase.Refresh();
            return newTerrain;
        }

        private static void CalculateSubHeightmap(TerrainData newTerrainData, int heightmapResolution, Terrain origTerrain, float chunkOffsetX, float chunkOffsetZ, float chunkWidthRatio, float chunkDepthRatio, int chunkX, int chunkZ)
        {
            newTerrainData.heightmapResolution = heightmapResolution;
            float[,] newHeights = new float[heightmapResolution, heightmapResolution];

            float sampleSizeNormalizedX = chunkWidthRatio / ((float)heightmapResolution - 1.0f);
            float sampleSizeNormalizedZ = chunkDepthRatio / ((float)heightmapResolution - 1.0f);

            float xOffset = chunkOffsetX;
            float zOffset = chunkOffsetZ;

            //Color col = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
            for (int z = 0; z < heightmapResolution; z++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    float posX = xOffset + (x * sampleSizeNormalizedX);
                    float posZ = zOffset + (z * sampleSizeNormalizedZ);

                    float height = origTerrain.terrainData.GetInterpolatedHeight(posX, posZ);

                    newHeights[z, x] = height / origTerrain.terrainData.size.y;

                    //Debug.DrawLine(new Vector3(posX * origTerrain.terrainData.size.x, height, posZ * origTerrain.terrainData.size.z), new Vector3(posX * origTerrain.terrainData.size.x, height + 1.0f, posZ * origTerrain.terrainData.size.z), col, 20.0f);
                }
            }
            newTerrainData.SetHeightsDelayLOD(0, 0, newHeights);
        }

        private static void CalculateSubSplatmaps(TerrainData newTerrainData, Terrain origTerrain, int splatmapResolution, Vector2 startSamples, Vector2 endSamples, float chunkWidthRatio, float chunkDepthRatio, float chunkOffsetX, float chunkOffsetZ)
        {
            //  Get the splat map from the larger source terrain.
            var sourceSplats = origTerrain.terrainData.GetAlphamaps(0, 0, origTerrain.terrainData.alphamapWidth, origTerrain.terrainData.alphamapHeight);

            //  Initialize a new splat map for this chunk.
            var destSplats = new float[splatmapResolution, splatmapResolution, sourceSplats.GetLength(2)];

            //  Calculate a ratio of the new sample rate and previous sample rate.
            float srcSamplesX = endSamples.x - startSamples.x;
            float srcSAmplesZ = endSamples.y - startSamples.y;

            float sampleSizeNormalizedX = chunkWidthRatio / ((float)splatmapResolution);
            float sampleSizeNormalizedZ = chunkDepthRatio / ((float)splatmapResolution);

            Debug.Log("Check: " + (32 * sampleSizeNormalizedX) * origTerrain.terrainData.alphamapWidth);

            /*Debug.LogError("Splat Res: " + splatmapResolution);
            Debug.LogError("ChunkWidthR: " + chunkWidthRatio);
            Debug.LogError("ChunkDepthR: " + chunkDepthRatio);

            Debug.LogError("SrcSampples X/Z: " + srcSamplesX + "/" + srcSAmplesZ);
            Debug.LogError("Dst Samples: " + splatmapResolution);*/

            Vector2 dstSampleToSrcSampleRatio = new Vector2(
                sampleSizeNormalizedX,
                sampleSizeNormalizedZ
                );

            //Debug.LogError("Sample Scale Ratio: " + dstSampleToSrcSampleRatio.x + "/" + dstSampleToSrcSampleRatio.y);

            
            for (int i = 0; i < sourceSplats.GetLength(2); i++)
            {
                for (int x = 0; x < splatmapResolution; x++)
                {
                    for (int z = 0; z < splatmapResolution; z++)
                    {
                        float srcPositionX = chunkOffsetX + (x * dstSampleToSrcSampleRatio.x);
                        float srcPositionZ = chunkOffsetZ + (z * dstSampleToSrcSampleRatio.y);

                        srcPositionX *= origTerrain.terrainData.alphamapWidth;
                        srcPositionZ *= origTerrain.terrainData.alphamapHeight;

                        int posZ = Mathf.FloorToInt(srcPositionX);
                        int posX = Mathf.FloorToInt(srcPositionZ);

                        float valAtPos = sourceSplats[posX, posZ, i];
                        float valAtNextX = sourceSplats[posX, posZ, i];
                        if ((posX + 1) < sourceSplats.GetLength(0))
                            valAtNextX = sourceSplats[posX + 1, posZ, i];

                        float valatNextZ = sourceSplats[posX, posZ, i];
                        if ((posZ + 1) < sourceSplats.GetLength(1))
                            valatNextZ = sourceSplats[posX, posZ + 1, i];

                        float valAtNextXZ = sourceSplats[posX, posZ, i];
                        if ((posX + 1) < sourceSplats.GetLength(0) && (posZ + 1) < sourceSplats.GetLength(1))
                            valAtNextXZ = sourceSplats[posX + 1, posZ + 1, i];

                        float remainderX = srcPositionX - (float)posX;
                        float remainderZ = srcPositionZ - (float)posZ;

                        float lengthToX = remainderX;
                        float lengthToZ = remainderZ;    
                        float lengthToXZ = new Vector2(lengthToX, lengthToZ).magnitude;

                        //  Try a new interpolation down the line where it starts at the center of the grid instead and interpolates 4 times against the corner,
                        //  instead of the current one which interpolates from the bottom left corner towards the other 4 corners. 

                        /*float newValueX = Mathf.Lerp(valAtNextXZ, valatNextZ, remainderZ);
                        float newValueZ = Mathf.Lerp(newValueX, valAtNextX, remainderX);
                        float newValueXZ = Mathf.Lerp(newValueZ, valAtPos, lengthToXZ);*/

                        float newValueX = Mathf.Lerp(valAtPos, valAtNextX, lengthToX);
                        float newValueZ = Mathf.Lerp(newValueX, valatNextZ, lengthToZ);
                        float newValueXZ = Mathf.Lerp(newValueZ, valAtNextXZ, lengthToXZ);
                        //float finalVal = Mathf.Lerp(newValueXZ, valAtPos, (1.0f - lengthToXZ));

                        float finalVal = newValueXZ;
                        //float finalVal = valAtPos;
                        destSplats[z, x, i] = finalVal;
                    }
                }
            }

            newTerrainData.SetAlphamaps(0, 0, destSplats);
        }

        private static void CalculateDetailMap(TerrainData newTerrain, Terrain origTerrain, int detailResolution)
        {
            //  Get the detail map from the larger source terrain
            //var sourceDetail = origTerrain.terrainData.get
            float aVal = 10;
        }

        public static int NearestPoT(int num)
        {
            return (int)Mathf.Pow(2, Mathf.Round(Mathf.Log(num) / Mathf.Log(2)));
        }
    }
}