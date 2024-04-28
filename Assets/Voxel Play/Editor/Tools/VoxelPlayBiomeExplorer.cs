using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace VoxelPlay {

    public class VoxelPlayBiomeExplorer : UnityEditor.EditorWindow {

        enum MapLayer {
            Biomes,
            Altitude,
            Moisture
        }

        enum BiomeColors {
            ByConfig,
            Altitude,
            Moisture,
        }

        public static bool requestRefresh;

        MapLayer mapLayer = MapLayer.Biomes;
        BiomeColors biomeColors = BiomeColors.ByConfig;

        WorldDefinition world;
        Texture2D biomeTex;
        VoxelPlayEnvironment env;
        VoxelPlayTerrainGenerator tg;
        Material previewTextureMat;
        float minX = -1000;
        float maxX = 1000;
        float minZ = -1000;
        float maxZ = 1000;
        int mapResolution = 512;
        int gridStep = 256;
        GUIStyle titleLabelStyle;
        Color titleColor;
        Color waterColor;
        Dictionary<Color, Texture2D> miniColoredTex;
        List<BiomeDefinition> biomes;
        readonly StringBuilder sb = new StringBuilder();
        float proposedMinX = -1000, proposedMaxX = 1000, proposedMinZ = -1000, proposedMaxZ = 1000;
        float inputAltitude, inputMoisture;
        string biomeTestResult;
        Vector2 scrollPosition;
        float minHeight, maxHeight;
        float minMoisture, maxMoisture;

        [MenuItem("Assets/Create/Voxel Play/Biome Explorer", false, 1000)]
        public static void ShowWindow() {
            VoxelPlayBiomeExplorer window = GetWindow<VoxelPlayBiomeExplorer>("Biome Explorer", true);
            window.minSize = new Vector2(400, 400);
            window.Show();
        }

        void OnEnable() {
            requestRefresh = true;
            titleColor = EditorGUIUtility.isProSkin ? new Color(0.52f, 0.66f, 0.9f) : new Color(0.12f, 0.16f, 0.4f);
            waterColor = new Color(0, 0.1f, 1f, 0.8f);
            if (env == null) {
                env = VoxelPlayEnvironment.instance;
            }
            if (env != null) {
                env.NotifyTerrainGeneratorConfigurationChanged ();
            }
        }

        private void OnDestroy() {
            if (biomeTex = null) {
                DestroyImmediate(biomeTex);
            }
            foreach (Texture2D tex in miniColoredTex.Values) DestroyImmediate(tex);
        }

        void OnGUI() {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            if (env == null) {
                env = VoxelPlayEnvironment.instance;
                if (env == null) {
                    world = null;
                }
                EditorGUILayout.HelpBox("Biome Explorer cannot find a Voxel Play Environment instance in the current scene.", MessageType.Error);
                GUIUtility.ExitGUI();
            } else {
                world = env.world;
            }

            if (world == null) {
                EditorGUILayout.HelpBox("Assign a World Definition to the Voxel Play Environment instance.", MessageType.Warning);
                GUIUtility.ExitGUI();
            }

            if (biomes == null || biomeTex == null) {
                RefreshTextures();
                GUIUtility.ExitGUI();
            }

            if (titleLabelStyle == null) {
                titleLabelStyle = new GUIStyle(EditorStyles.label);
            }
            titleLabelStyle.normal.textColor = titleColor;
            titleLabelStyle.fontStyle = FontStyle.Bold;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox("Preview terrain generation and biome distribution based on current settings.", MessageType.Info);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min X", GUILayout.Width(100));
            proposedMinX = EditorGUILayout.FloatField(proposedMinX, GUILayout.MaxWidth(120));
            EditorGUILayout.LabelField("Max X", GUILayout.Width(100));
            proposedMaxX = EditorGUILayout.FloatField(proposedMaxX, GUILayout.MaxWidth(120));
            if (GUILayout.Button("<<", GUILayout.Width(40))) {
                float shift = (maxX - minX) * 0.5f;
                proposedMinX -= shift;
                proposedMaxX -= shift;
                requestRefresh = true;
            }
            if (GUILayout.Button("<", GUILayout.Width(40))) {
                float shift = (maxX - minX) * 0.1f;
                proposedMinX -= shift;
                proposedMaxX -= shift;
                requestRefresh = true;
            }
            if (GUILayout.Button(">", GUILayout.Width(40))) {
                float shift = (maxX - minX) * 0.1f;
                proposedMinX += shift;
                proposedMaxX += shift;
                requestRefresh = true;
            }
            if (GUILayout.Button(">>", GUILayout.Width(40))) {
                float shift = (maxX - minX) * 0.5f;
                proposedMinX += shift;
                proposedMaxX += shift;
                requestRefresh = true;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Min Z", GUILayout.Width(100));
            proposedMinZ = EditorGUILayout.FloatField(proposedMinZ, GUILayout.MaxWidth(120));
            EditorGUILayout.LabelField("Max Z", GUILayout.Width(100));
            proposedMaxZ = EditorGUILayout.FloatField(proposedMaxZ, GUILayout.MaxWidth(120));
            if (GUILayout.Button("<<", GUILayout.Width(40))) {
                float shift = (maxZ - minZ) * 0.5f;
                proposedMinZ -= shift;
                proposedMaxZ -= shift;
                requestRefresh = true;
            }
            if (GUILayout.Button("<", GUILayout.Width(40))) {
                float shift = (maxZ - minZ) * 0.1f;
                proposedMinZ -= shift;
                proposedMaxZ -= shift;
                requestRefresh = true;
            }
            if (GUILayout.Button(">", GUILayout.Width(40))) {
                float shift = (maxZ - minZ) * 0.1f;
                proposedMinZ += shift;
                proposedMaxZ += shift;
                requestRefresh = true;
            }
            if (GUILayout.Button(">>", GUILayout.Width(40))) {
                float shift = (maxZ - minZ) * 0.5f;
                proposedMinZ += shift;
                proposedMaxZ += shift;
                requestRefresh = true;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Refresh Window", "Refresh textures to reflect new filters."), GUILayout.Width(140))) {
                requestRefresh = true;
            }
            if (GUILayout.Button(new GUIContent("Center On Camera", "Moves the preview area so camera stays at the center."), GUILayout.Width(140))) {
                float dx = maxX - minX;
                float dz = maxZ - minZ;
                Camera cam = env.currentCamera;
                if (cam != null) {
                    Vector3 camPos = cam.transform.position;
                    proposedMinX = camPos.x - dx * 0.5f;
                    proposedMaxX = camPos.x + dx * 0.5f;
                    proposedMinZ = camPos.z - dz * 0.5f;
                    proposedMaxZ = camPos.z + dz * 0.5f;
                    requestRefresh = true;
                }
            }

            if (GUILayout.Button(new GUIContent("-> World Definition", "Show World Definition in the inspector."), GUILayout.Width(140))) {
                Selection.activeObject = world;
            }
            if (GUILayout.Button(new GUIContent("-> Terrain Generator", "Show Terrain Generator in the inspector."), GUILayout.Width(140))) {
                Selection.activeObject = tg;
            }
            if (GUILayout.Button(new GUIContent("-> Environment", "Show Voxel Play Environment in the inspector."), GUILayout.Width(140))) {
                Selection.activeGameObject = env.gameObject;
            }
            if (GUILayout.Button(new GUIContent("Reload Config", "Resets heightmaps and biome cache and initializes terrain generator."), GUILayout.Width(140))) {
                env.NotifyTerrainGeneratorConfigurationChanged();
                requestRefresh = true;
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();
            Rect space;

            if (previewTextureMat == null) {
                previewTextureMat = Resources.Load<Material>("VoxelPlay/PreviewTexture");
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            if (biomes != null && biomeTex != null) {

                // Draw heightmap texture
                EditorGUILayout.LabelField(new GUIContent("Biome Map Preview"), titleLabelStyle);

                EditorGUILayout.BeginHorizontal();

                // Biome legend
                EditorGUILayout.BeginVertical(GUILayout.MaxWidth(600));
                EditorGUILayout.Separator();
                EditorGUI.BeginChangeCheck();
                mapLayer = (MapLayer)EditorGUILayout.EnumPopup("Show Layer: ", mapLayer);
                if (EditorGUI.EndChangeCheck()) {
                    RefreshBiomeTexture();
                }

                if (mapLayer == MapLayer.Biomes) {
                    EditorGUI.BeginChangeCheck();
                    biomeColors = (BiomeColors)EditorGUILayout.EnumPopup("Biome Colors: ", biomeColors);

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Hide All", GUILayout.Width(80))) {
                        ToggleBiomes(false);
                        requestRefresh = true;
                    }
                    if (GUILayout.Button("Show All", GUILayout.Width(80))) {
                        ToggleBiomes(true);
                        requestRefresh = true;
                    }
                    EditorGUILayout.EndHorizontal();

                    DrawLegend(waterColor, "Water (water level at " + tg.waterLevel + ")", null);
                    for (int k = 0; k < biomes.Count; k++) {
                        BiomeDefinition biome = biomes[k];
                        if (biome == null)
                            continue;
                        float perc = (100f * biome.biomeMapOccurrences) / (biomeTex.width * biomeTex.height);
                        DrawLegend(biome.biomeMapColorTemp, biome.name + " (" + perc.ToString("F2") + "%)", biome);
                    }

                    if (EditorGUI.EndChangeCheck()) {
                        requestRefresh = true;
                    }
                }

                EditorGUILayout.Separator();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Grid Size", GUILayout.Width(100));
                gridStep = EditorGUILayout.IntField(gridStep, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Texture Size", GUILayout.Width(100));
                mapResolution = EditorGUILayout.IntField(mapResolution, GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                // Tester
                EditorGUILayout.LabelField(new GUIContent("Biome Tester"), titleLabelStyle);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Altitude?", GUILayout.Width(100));
                inputAltitude = EditorGUILayout.Slider(inputAltitude, 0, 1, GUILayout.Width(130));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Moisture?", GUILayout.Width(100));
                inputMoisture = EditorGUILayout.Slider(inputMoisture, 0, 1, GUILayout.Width(130));
                EditorGUILayout.EndHorizontal();
                if (EditorGUI.EndChangeCheck()) {
                    CalcBiome();
                }
                EditorGUILayout.LabelField(biomeTestResult);
                EditorGUILayout.EndVertical();

                // Biome map
                space = EditorGUILayout.BeginVertical();
                space.width = space.height;
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                EditorGUI.DrawPreviewTexture(space, biomeTex, previewTextureMat, ScaleMode.ScaleToFit);
                Event e = Event.current;
                Vector2 guiMousePos = e.mousePosition;
                if (space.Contains(guiMousePos)) {
                    ShowBiomeMapTooltip(guiMousePos, space);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Biome Map Preview (Data Range)"), titleLabelStyle);
                EditorGUILayout.LabelField("Altitude: " + minHeight + "m - " + maxHeight + "m");
                EditorGUILayout.LabelField("Moisture: " + minMoisture.ToString("F3") + " - " + maxMoisture.ToString("F3"));
                EditorGUILayout.Separator();
            }

            EditorGUILayout.EndScrollView();

            if (requestRefresh) {
                RefreshTextures();
            }
        }

        void ShowBiomeMapTooltip(Vector2 position, Rect rect) {
            float tx = (position.x - rect.xMin) / rect.width;
            float tz = (position.y - rect.yMin) / rect.height;
            Vector2 tooltipPosition = new Vector2(rect.xMin + rect.width * tx, rect.yMin + rect.height * tz);
            float x = minX + (maxX - minX) * tx;
            float z = minZ + (maxZ - minZ) * (1f - tz);
            HeightMapInfo info = env.GetHeightMapInfoFast(x, z);
            sb.Clear();
            sb.Append("Altitude: ");
            sb.Append(info.groundLevel);
            sb.Append(", Moisture: ");
            sb.Append(info.moisture.ToString("F3"));
            if (info.biome != null) {
                sb.Append("\nBiome: ");
                sb.Append(info.biome.name);
            }
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.normal.textColor = Color.black;
            Vector2 labelSize = new Vector2(600, 50);
            string labelText = sb.ToString();
            tooltipPosition.x += 10;
            tooltipPosition.y += 10;
            EditorGUI.LabelField(new Rect(tooltipPosition + new Vector2(1, 1), labelSize), labelText, style); ;
            style.normal.textColor = Color.yellow;
            EditorGUI.LabelField(new Rect(tooltipPosition, labelSize), labelText, style); ;

            Repaint();
        }


        void DrawLegend(Color color, string text, BiomeDefinition biome) {

            Rect space = EditorGUILayout.BeginHorizontal();
            space.position += new Vector2(5, 0);
            space.width = 16;
            space.height = 16;
            Color prevColor = GUI.color;
            EditorGUI.DrawPreviewTexture(space, GetColoredTexture(color));
            space.position += new Vector2(20, 0);
            space.width = 20;
            if (biome != null) {
                GUI.color = biome.showInBiomeMap ? prevColor : new Color(prevColor.r, prevColor.g, prevColor.b, 0.4f);
            } else {
                GUI.color = prevColor;
            }
            space.width = 20;
            if (biome != null) {
                biome.showInBiomeMap = EditorGUI.Toggle(space, biome.showInBiomeMap);
                space.width = 35;
                space.position += new Vector2(20, 0);
                if (GUI.Button(space, "->")) {
                    Selection.activeObject = biome;
                }
                space.position += new Vector2(35, 0);
            }
            space.width = 500;
            EditorGUI.LabelField(space, text);
            GUI.color = prevColor;
            EditorGUILayout.BeginVertical();
            GUILayout.Space(20);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        Texture2D GetColoredTexture(Color color) {
            if (miniColoredTex == null) {
                miniColoredTex = new Dictionary<Color, Texture2D>();
            }
            Texture2D tex;
            if (!miniColoredTex.TryGetValue(color, out tex) || tex == null) {
                Texture2D refTex = Texture2D.whiteTexture;
                tex = new Texture2D(refTex.width, refTex.height, TextureFormat.RGBA32, false);
                Color[] colors = new Color[refTex.width * refTex.height];
                for (int k = 0; k < colors.Length; k++) { colors[k] = color; }
                tex.SetPixels(colors);
                tex.Apply();
                miniColoredTex[color] = tex;
            }
            return tex;
        }

        void RefreshTextures() {

            requestRefresh = false;
            proposedMaxX = Mathf.Max(proposedMinX + 1, proposedMaxX);
            proposedMaxZ = Mathf.Max(proposedMinZ + 1, proposedMaxZ);

            minX = proposedMinX;
            maxX = proposedMaxX;
            minZ = proposedMinZ;
            maxZ = proposedMaxZ;

            if (tg == null || tg != world.terrainGenerator) {
                tg = world.terrainGenerator;
                if (tg == null)
                    return;
            }
            if (!tg.isInitialized) {
                tg.Initialize();
            }

            RefreshBiomeColors();
            RefreshBiomeTexture();

        }


        void RefreshBiomeColors() {
            if (biomes == null) {
                biomes = new List<BiomeDefinition>(world.biomes);
            } else {
                biomes.Clear();
                biomes.AddRange(world.biomes);
            }
            switch (biomeColors) {
                case BiomeColors.ByConfig:
                    LoadBiomeColors();
                    break;
                case BiomeColors.Altitude:
                    SetBiomeRampByAltitude();
                    break;
                case BiomeColors.Moisture:
                    SetBiomeRampByMoisture();
                    break;
            }
        }

        void SetBiomeRampByAltitude() {
            biomes.Sort((BiomeDefinition b1, BiomeDefinition b2) => { if (b1.zones != null && b2.zones != null && b1.zones.Length > 0 && b2.zones.Length > 0) return b1.zones[0].altitudeMin.CompareTo(b2.zones[0].altitudeMin); else return 0; });
            for (int k = 0; k < biomes.Count; k++) {
                float t = (float)k / biomes.Count;
                biomes[k].biomeMapColorTemp = new Color(t, t, t);
            }
        }
       

        void SetBiomeRampByMoisture() {
            biomes.Sort((BiomeDefinition b1, BiomeDefinition b2) => { if (b1.zones != null && b2.zones != null && b1.zones.Length > 0 && b2.zones.Length > 0) return b1.zones[0].moistureMin.CompareTo(b2.zones[0].moistureMin); else return 0; });
            for (int k = 0; k < biomes.Count; k++) {
                float t = (float)k / biomes.Count;
                biomes[k].biomeMapColorTemp = new Color(0, t, 0);
            }
        }

        void LoadBiomeColors() {
            foreach (BiomeDefinition biome in biomes) {
                biome.biomeMapColorTemp = biome.biomeMapColor;
            }
        }

        void RefreshBiomeTexture() {

            if (biomeTex == null || biomeTex.width != mapResolution) {
                biomeTex = new Texture2D(mapResolution, mapResolution, TextureFormat.ARGB32, false);
            }

            int width = biomeTex.width;
            int height = biomeTex.height;
            Color[] colors = new Color[width * height];

            minHeight = float.MaxValue;
            minMoisture = float.MaxValue;
            maxHeight = float.MinValue;
            maxMoisture = float.MinValue;
            if (env == null || tg == null) {
                colors.Fill<Color>(new Color(0, 0.5f, 0, 0.5f));
            } else {
                env.SetBiomeDefaultColors(false);
                colors.Fill<Color>(Misc.colorTransparent);

                // reset biome stats
                for (int k = 0; k < biomes.Count; k++) {
                    if (biomes[k] != null) {
                        biomes[k].biomeMapOccurrences = 0;
                    }
                }
                // draw biome colors
                Color chartColor;
                for (int j = 0; j < height; j++) {
                    float z = (maxZ - minZ) * (float)j / height + minZ;
                    int jj = j * width;
                    for (int k = 0; k < width; k++) {
                        float x = (maxX - minX) * (float)k / width + minX;
                        HeightMapInfo info = env.GetTerrainInfo(x, z);
                        if (info.groundLevel > maxHeight) maxHeight = info.groundLevel;
                        if (info.groundLevel < minHeight) minHeight = info.groundLevel;
                        if (info.moisture > maxMoisture) maxMoisture = info.moisture;
                        if (info.moisture < minMoisture) minMoisture = info.moisture;
                        BiomeDefinition biome = info.biome;
                        if ((object)biome != null) {
                            biome.biomeMapOccurrences++;
                        }
                        chartColor = Misc.colorGray;
                        switch (mapLayer) {
                            case MapLayer.Biomes:
                                if (info.groundLevel <= tg.waterLevel) {
                                    chartColor = waterColor;
                                } else
                                    if ((object)biome != null && biome.showInBiomeMap) {
                                    chartColor = biome.biomeMapColorTemp;
                                }
                                break;
                            case MapLayer.Altitude:
                                float alt = (info.groundLevel + tg.minHeight) / (tg.maxHeight - tg.minHeight);
                                chartColor.r = chartColor.g = chartColor.b = alt;
                                break;
                            case MapLayer.Moisture:
                                chartColor.r = chartColor.g = chartColor.b = info.moisture;
                                break;
                        }
                        colors[jj + k] = chartColor;
                    }
                }

                Color gridColor = new Color(64, 64, 64, 0.2f);
                // draw horizontal grid lines
                int gridCount = (int)((maxZ - minZ) / gridStep);
                if (gridCount > 0) {
                    for (int j = 0; j <= gridCount; j++) {
                        int y = (int)((height - 1f) * j / gridCount);
                        for (int k = 0; k < width; k++) {
                            colors[y * width + k] = gridColor;
                        }
                    }
                }
                // draw vertical grid lines
                gridCount = (int)((maxX - minX) / gridStep);
                if (gridCount > 0) {
                    for (int j = 0; j <= gridCount; j++) {
                        int x = (int)((width - 1f) * j / gridCount);
                        for (int k = 0; k < height; k++) {
                            colors[k * width + x] = gridColor;
                        }
                    }
                }
            }

            biomeTex.SetPixels(colors);
            biomeTex.Apply();
        }

        void ToggleBiomes(bool visible) {
            for (int k = 0; k < biomes.Count; k++) {
                BiomeDefinition biome = biomes[k];
                if (biome == null)
                    continue;
                biome.showInBiomeMap = visible;
            }
        }

        void CalcBiome() {
            BiomeDefinition biome = env.GetBiome(inputAltitude * tg.maxHeight, inputMoisture);
            if (biome == null) {
                biomeTestResult = "No matching biome.";
            } else {
                biomeTestResult = "Matching biome: " + biome.name + ".";
            }
        }

    }

}