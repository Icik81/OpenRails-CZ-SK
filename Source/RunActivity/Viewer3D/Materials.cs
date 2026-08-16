// COPYRIGHT 2009, 2010, 2011, 2012, 2013, 2014 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

// This file is the responsibility of the 3D & Environment Team. 

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Viewer3D.Common;
using Orts.Viewer3D.Popups;
using ORTS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Orts.Viewer3D
{
    public struct MaterialKey : IEquatable<MaterialKey>
    {
        public readonly string MaterialName;
        public readonly string TextureName;
        public readonly int Options;
        public readonly float MipMapBias;
        public readonly int CabShaderKey;

        public MaterialKey(string materialName, string textureName, int options, float mipMapBias, int cabShaderKey)
        {
            MaterialName = materialName;
            TextureName = textureName;
            Options = options;
            MipMapBias = mipMapBias;
            CabShaderKey = cabShaderKey;
        }

        public bool Equals(MaterialKey other)
        {
            return Options == other.Options
                && CabShaderKey == other.CabShaderKey
                && MipMapBias.Equals(other.MipMapBias)
                && string.Equals(MaterialName, other.MaterialName, StringComparison.Ordinal)
                && string.Equals(TextureName, other.TextureName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is MaterialKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (MaterialName != null ? StringComparer.Ordinal.GetHashCode(MaterialName) : 0);
                hash = (hash * 397) ^ (TextureName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(TextureName) : 0);
                hash = (hash * 397) ^ Options;
                hash = (hash * 397) ^ MipMapBias.GetHashCode();
                hash = (hash * 397) ^ CabShaderKey;
                return hash;
            }
        }
    }

    [CallOnThread("Loader")]
    public class SharedTextureManager
    {
        readonly Viewer Viewer;
        readonly GraphicsDevice GraphicsDevice;
        readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, bool> TextureMarks = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, bool> FileExistenceCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        [CallOnThread("Render")]
        internal SharedTextureManager(Viewer viewer, GraphicsDevice graphicsDevice)
        {
            Viewer = viewer;
            GraphicsDevice = graphicsDevice;
        }

        public Texture2D Get(string path, bool required = false)
        {
            return (Get(path, SharedMaterialManager.MissingTexture, required));
        }

        private bool CheckFileExistsCached(string filePath)
        {
            if (!FileExistenceCache.TryGetValue(filePath, out bool exists))
            {
                exists = File.Exists(filePath);
                FileExistenceCache[filePath] = exists;
            }
            return exists;
        }

        public Texture2D Get(string path, Texture2D defaultTexture, bool required = false)
        {
            if (Thread.CurrentThread.Name != "Loader Process")
                Trace.TraceError("SharedTextureManager.Get incorrectly called by {0}; must be Loader Process or crashes will occur.", Thread.CurrentThread.Name);

            if (string.IsNullOrEmpty(path))
                return defaultTexture;

            if (Textures.TryGetValue(path, out var cachedTexture))
                return cachedTexture;

            Texture2D texture;

            // Icik - optimalizovaná kontrola rychlostníků s mezipamětí existence souborů
            string fileName = Path.GetFileName(path);
            if (fileName.IndexOf("speed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                fileName.IndexOf("post", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string speedZoneDir = Path.Combine(Viewer.ContentPath, "RestrictedSpeedPost");
                string pathRestrictedSpeedZone = Path.Combine(speedZoneDir, fileName);

                if (CheckFileExistsCached(pathRestrictedSpeedZone))
                {
                    texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, pathRestrictedSpeedZone);
                    Textures[path] = texture;
                    return texture;
                }

                string nameNoExt = Path.GetFileNameWithoutExtension(path);
                string ext = Path.GetExtension(path);

                string pathCz = Path.Combine(speedZoneDir, nameNoExt + "_cz" + ext);
                if (CheckFileExistsCached(pathCz))
                {
                    texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, pathCz);
                    Textures[path] = texture;
                    return texture;
                }

                string pathSk = Path.Combine(speedZoneDir, nameNoExt + "_sk" + ext);
                if (CheckFileExistsCached(pathSk))
                {
                    texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, pathSk);
                    Textures[path] = texture;
                    return texture;
                }
            }

            try
            {
                string extension = Path.GetExtension(path);

                if (string.Equals(extension, ".dds", StringComparison.OrdinalIgnoreCase))
                {
                    if (CheckFileExistsCached(path))
                    {
                        DDSLib.DDSFromFile(path, GraphicsDevice, true, out texture);
                    }
                    else
                    {
                        var aceTexture = Path.ChangeExtension(path, ".ace");
                        if (CheckFileExistsCached(aceTexture))
                        {
                            texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, aceTexture);
                            Trace.TraceWarning("Required texture {0} not existing; using existing texture {1}", path, aceTexture);
                        }
                        else
                        {
                            texture = defaultTexture;
                        }
                    }
                }
                else if (string.Equals(extension, ".ace", StringComparison.OrdinalIgnoreCase))
                {
                    var alternativeTexture = Path.ChangeExtension(path, ".dds");

                    if (Viewer.Settings.PreferDDSTexture && CheckFileExistsCached(alternativeTexture))
                    {
                        DDSLib.DDSFromFile(alternativeTexture, GraphicsDevice, true, out texture);
                    }
                    else if (CheckFileExistsCached(path))
                    {
                        texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, path);
                    }
                    else
                    {
                        Texture2D missing()
                        {
                            if (required)
                                Trace.TraceWarning("Missing texture {0} replaced with default texture", path);
                            return defaultTexture;
                        }
                        Texture2D invalid()
                        {
                            if (required)
                                Trace.TraceWarning("Invalid texture {0} replaced with default texture", path);
                            return defaultTexture;
                        }

                        try
                        {
                            var currentDir = Directory.GetParent(path);
                            if (currentDir != null && currentDir.Parent != null)
                            {
                                string searchPath = Path.Combine(currentDir.Parent.FullName, fileName);
                                if (searchPath.IndexOf("texture", StringComparison.OrdinalIgnoreCase) >= 0 && CheckFileExistsCached(searchPath))
                                {
                                    try
                                    {
                                        texture = Orts.Formats.Msts.AceFile.Texture2DFromFile(GraphicsDevice, searchPath);
                                    }
                                    catch
                                    {
                                        return invalid();
                                    }
                                }
                                else
                                {
                                    return missing();
                                }
                            }
                            else
                            {
                                return missing();
                            }
                        }
                        catch
                        {
                            return missing();
                        }
                    }
                }
                else
                {
                    return defaultTexture;
                }

                Textures[path] = texture;
                return texture;
            }
            catch (InvalidDataException error)
            {
                Trace.TraceWarning("Skipped texture with error: {1} in {0}", path, error.Message);
                return defaultTexture;
            }
            catch (Exception error)
            {
                if (CheckFileExistsCached(path))
                    Trace.WriteLine(new FileLoadException(path, error));
                else
                    Trace.TraceWarning("Ignored missing texture file {0}", path);
                return defaultTexture;
            }
        }

        public static Texture2D Get(GraphicsDevice graphicsDevice, string path)
        {
            if (string.IsNullOrEmpty(path))
                return SharedMaterialManager.MissingTexture;

            var ext = Path.GetExtension(path);

            if (string.Equals(ext, ".ace", StringComparison.OrdinalIgnoreCase))
                return Orts.Formats.Msts.AceFile.Texture2DFromFile(graphicsDevice, path);

            if (!File.Exists(path))
                return SharedMaterialManager.MissingTexture;

            using (var stream = File.OpenRead(path))
            {
                if (string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(ext, ".png", StringComparison.OrdinalIgnoreCase))
                {
                    return Texture2D.FromStream(graphicsDevice, stream);
                }
                else if (string.Equals(ext, ".bmp", StringComparison.OrdinalIgnoreCase))
                {
                    using (var image = System.Drawing.Image.FromStream(stream))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            return Texture2D.FromStream(graphicsDevice, memoryStream);
                        }
                    }
                }
                else
                {
                    Trace.TraceWarning("Unsupported texture format: {0}", path);
                }
                return SharedMaterialManager.MissingTexture;
            }
        }

        public void Mark()
        {
            TextureMarks.Clear();
            foreach (var path in Textures.Keys)
                TextureMarks[path] = false;
        }

        public void Mark(Texture2D texture)
        {
            foreach (var kvp in Textures)
            {
                if (kvp.Value == texture)
                {
                    TextureMarks[kvp.Key] = true;
                    break;
                }
            }
        }

        public void Sweep()
        {
            var keysToRemove = new List<string>();
            foreach (var kvp in TextureMarks)
            {
                if (!kvp.Value)
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var path in keysToRemove)
            {
                var texture = Textures[path];
                Textures.Remove(path);
                TextureMarks.Remove(path);

                if (Viewer.Settings.ReduceMemory && texture != null)
                {
                    texture.Dispose();
                }
            }
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return Viewer.Catalog.GetPluralStringFmt("{0:F0} texture", "{0:F0} textures", Textures.Keys.Count);
        }
    }

    [CallOnThread("Loader")]
    public class SharedMaterialManager
    {
        readonly Viewer Viewer;
        readonly Dictionary<MaterialKey, Material> Materials = new Dictionary<MaterialKey, Material>();
        readonly Dictionary<MaterialKey, bool> MaterialMarks = new Dictionary<MaterialKey, bool>();

        public readonly LightConeShader LightConeShader;
        public readonly LightGlowShader LightGlowShader;
        public readonly ParticleEmitterShader ParticleEmitterShader;
        public readonly PopupWindowShader PopupWindowShader;
        public readonly PrecipitationShader PrecipitationShader;
        public readonly SceneryShader SceneryShader;
        public readonly ShadowMapShader ShadowMapShader;
        public readonly SkyShader SkyShader;
        public readonly DebugShader DebugShader;

        public static Texture2D MissingTexture;
        public static Texture2D DefaultSnowTexture;
        public static Texture2D DefaultDMSnowTexture;

        [CallOnThread("Render")]
        public SharedMaterialManager(Viewer viewer)
        {
            Viewer = viewer;
            LightConeShader = new LightConeShader(viewer.RenderProcess.GraphicsDevice);
            LightGlowShader = new LightGlowShader(viewer.RenderProcess.GraphicsDevice);
            ParticleEmitterShader = new ParticleEmitterShader(viewer.RenderProcess.GraphicsDevice);
            PopupWindowShader = new PopupWindowShader(viewer, viewer.RenderProcess.GraphicsDevice);
            PrecipitationShader = new PrecipitationShader(viewer.RenderProcess.GraphicsDevice);
            SceneryShader = new SceneryShader(viewer.RenderProcess.GraphicsDevice);
            var microtexPath = viewer.Simulator.RoutePath + @"\OpenRails\microtex.ace";
            if (File.Exists(microtexPath))
            {
                try
                {
                    SceneryShader.OverlayTexture = Orts.Formats.Msts.AceFile.Texture2DFromFile(viewer.GraphicsDevice, microtexPath);
                }
                catch (InvalidDataException error)
                {
                    Trace.TraceWarning("Skipped texture with error: {1} in {0}", microtexPath, error.Message);
                }
                catch (Exception error)
                {
                    Trace.WriteLine(new FileLoadException(microtexPath, error));
                }
            }
            else
            {
                microtexPath = viewer.Simulator.RoutePath + @"\TERRTEX\microtex.ace";
                if (File.Exists(microtexPath))
                {
                    try
                    {
                        SceneryShader.OverlayTexture = Orts.Formats.Msts.AceFile.Texture2DFromFile(viewer.GraphicsDevice, microtexPath);
                    }
                    catch (InvalidDataException error)
                    {
                        Trace.TraceWarning("Skipped texture with error: {1} in {0}", microtexPath, error.Message);
                    }
                    catch (Exception error)
                    {
                        Trace.WriteLine(new FileLoadException(microtexPath, error));
                    }
                }
            }

            ShadowMapShader = new ShadowMapShader(viewer.RenderProcess.GraphicsDevice);
            SkyShader = new SkyShader(viewer.RenderProcess.GraphicsDevice);
            DebugShader = new DebugShader(viewer.RenderProcess.GraphicsDevice);

            MissingTexture = SharedTextureManager.Get(viewer.RenderProcess.GraphicsDevice, Path.Combine(viewer.ContentPath, "blank.bmp"));

            var defaultSnowTexturePath = viewer.Simulator.RoutePath + @"\TERRTEX\SNOW\ORTSDefaultSnow.ace";
            DefaultSnowTexture = Viewer.TextureManager.Get(defaultSnowTexturePath);
            var defaultDMSnowTexturePath = viewer.Simulator.RoutePath + @"\TERRTEX\SNOW\ORTSDefaultDMSnow.ace";
            DefaultDMSnowTexture = Viewer.TextureManager.Get(defaultDMSnowTexturePath);
        }

        public Material Load(string materialName)
        {
            return Load(materialName, null, 0, 0, 0, null, false);
        }

        public Material Load(string materialName, string textureName)
        {
            return Load(materialName, textureName, 0, 0, 0, null, false);
        }

        public Material Load(string materialName, string textureName, int options)
        {
            return Load(materialName, textureName, options, 0, 0, null, false);
        }

        public Material Load(string materialName, string textureName, int options, float mipMapBias)
        {
            return Load(materialName, textureName, options, mipMapBias, 0, null, false);
        }

        public Material Load(string materialName, string textureName, int options, float mipMapBias, int cabShaderKey, CabShader cabShader, bool lightItem)
        {
            var materialKey = new MaterialKey(materialName, textureName, options, mipMapBias, cabShaderKey);

            if (!Materials.TryGetValue(materialKey, out var material))
            {
                switch (materialName)
                {
                    case "Debug":
                        material = new HUDGraphMaterial(Viewer);
                        break;
                    case "DebugNormals":
                        material = new DebugNormalMaterial(Viewer);
                        break;
                    case "Forest":
                        material = new ForestMaterial(Viewer, textureName);
                        break;
                    case "Label3D":
                        material = new Label3DMaterial(Viewer);
                        break;
                    case "LightCone":
                        material = new LightConeMaterial(Viewer);
                        break;
                    case "LightGlow":
                        material = new LightGlowMaterial(Viewer, textureName);
                        break;
                    case "PopupWindow":
                        material = new PopupWindowMaterial(Viewer);
                        break;
                    case "ParticleEmitter":
                        material = new ParticleEmitterMaterial(Viewer, textureName);
                        break;
                    case "Precipitation":
                        material = new PrecipitationMaterial(Viewer);
                        break;
                    case "Scenery":
                        material = new SceneryMaterial(Viewer, textureName, (SceneryMaterialOptions)options, mipMapBias);
                        break;
                    case "ShadowMap":
                        material = new ShadowMapMaterial(Viewer);
                        break;
                    case "SignalLight":
                        material = new SignalLightMaterial(Viewer, textureName);
                        break;
                    case "SignalLightGlow":
                        material = new SignalLightGlowMaterial(Viewer);
                        break;
                    case "Sky":
                        material = new SkyMaterial(Viewer);
                        break;
                    case "MSTSSky":
                        material = new MSTSSkyMaterial(Viewer);
                        break;
                    case "SpriteBatch":
                        material = new SpriteBatchMaterial(Viewer);
                        break;
                    case "CabSpriteBatch":
                        if (string.IsNullOrEmpty(textureName) || textureName.IndexOf("LIGHT", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            material = new CabSpriteBatchMaterial(Viewer, cabShader, true, textureName);
                        }
                        else
                        {
                            material = new CabSpriteBatchMaterial(Viewer, cabShader, false, textureName);
                        }
                        break;
                    case "Terrain":
                        material = new TerrainMaterial(Viewer, textureName, SharedMaterialManager.MissingTexture);
                        break;
                    case "TerrainShared":
                        material = new TerrainSharedMaterial(Viewer, textureName);
                        break;
                    case "TerrainSharedDistantMountain":
                        material = new TerrainSharedDistantMountain(Viewer, textureName);
                        break;
                    case "Transfer":
                        material = new TransferMaterial(Viewer, textureName);
                        break;
                    case "Water":
                        material = new WaterMaterial(Viewer, textureName);
                        break;
                    default:
                        Trace.TraceInformation("Skipped unknown material type {0}", materialName);
                        material = new YellowMaterial(Viewer);
                        break;
                }
                Materials[materialKey] = material;
            }
            return material;
        }

        public bool LoadNightTextures()
        {
            int count = 0;
            foreach (var materialPair in Materials)
            {
                if (materialPair.Value is SceneryMaterial material)
                {
                    if (material.LoadNightTexture()) count++;
                    if (count >= 20)
                    {
                        count = 0;
                        var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.HUDWindow.GetWorkingSetSize();
                        if (remainingMemorySpace < 0)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public bool LoadDayTextures()
        {
            int count = 0;
            foreach (var materialPair in Materials)
            {
                if (materialPair.Value is SceneryMaterial material)
                {
                    if (material.LoadDayTexture()) count++;
                    if (count >= 20)
                    {
                        count = 0;
                        var remainingMemorySpace = Viewer.LoadMemoryThreshold - Viewer.HUDWindow.GetWorkingSetSize();
                        if (remainingMemorySpace < 0)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        public void Mark()
        {
            MaterialMarks.Clear();
            foreach (var key in Materials.Keys)
                MaterialMarks[key] = false;
        }

        public void Mark(Material material)
        {
            foreach (var kvp in Materials)
            {
                if (kvp.Value == material)
                {
                    MaterialMarks[kvp.Key] = true;
                    break;
                }
            }
        }

        public void Sweep()
        {
            var keysToRemove = new List<MaterialKey>();
            foreach (var kvp in MaterialMarks)
            {
                if (!kvp.Value)
                    keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                Materials.Remove(key);
                MaterialMarks.Remove(key);
            }
        }

        public void LoadPrep()
        {
            if (Viewer.Settings.UseMSTSEnv == false)
            {
                Viewer.World.Sky.LoadPrep();
                sunDirection = Viewer.World.Sky.solarDirection;
            }
            else
            {
                Viewer.World.MSTSSky.LoadPrep();
                sunDirection = Viewer.World.MSTSSky.mstsskysolarDirection;
            }
        }

        [CallOnThread("Updater")]
        public string GetStatus()
        {
            return Viewer.Catalog.GetPluralStringFmt("{0:F0} material", "{0:F0} materials", Materials.Keys.Count);
        }

        public static Color FogColor = new Color(110, 110, 110, 255);

        internal Vector3 sunDirection;
        internal void UpdateShaders()
        {
            if (Viewer.Settings.UseMSTSEnv == false)
                sunDirection = Viewer.World.Sky.solarDirection;
            else
                sunDirection = Viewer.World.MSTSSky.mstsskysolarDirection;

            SceneryShader.SetLightVector_ZFar(sunDirection, Viewer.Settings.ViewingDistance);

            SceneryShader.SetMultiHeadlights(
                Viewer.HeadlightPositions,
                Viewer.HeadlightDirections,
                Viewer.HeadlightColors,
                Viewer.HeadlightRcpDistances,
                Viewer.ActiveHeadlightCount
            );

            if (Viewer.Settings.UseMSTSEnv == false)
            {
                SceneryShader.Overcast = Viewer.Simulator.Weather.OvercastFactor;
                SceneryShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
                ParticleEmitterShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
                SceneryShader.ViewerPos = Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation);

                LightGlowShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
                LightGlowShader.ViewerPos = Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation);
            }
            else
            {
                SceneryShader.Overcast = Viewer.World.MSTSSky.mstsskyovercastFactor;
                SceneryShader.SetFog(Viewer.World.MSTSSky.mstsskyfogDistance, ref SharedMaterialManager.FogColor);
                ParticleEmitterShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
                SceneryShader.ViewerPos = Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation);

                LightGlowShader.SetFog(Viewer.Simulator.Weather.FogDistance, ref SharedMaterialManager.FogColor);
                LightGlowShader.ViewerPos = Viewer.Camera.XnaLocation(Viewer.Camera.CameraWorldLocation);
            }
        }
    }

    public abstract class Material
    {
        public readonly Viewer Viewer;
        readonly string Key;

        protected Material(Viewer viewer, string key)
        {
            Viewer = viewer;
            Key = key;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Key))
                return GetType().Name;
            return string.Format("{0}({1})", GetType().Name, Key);
        }

        public virtual void SetState(GraphicsDevice graphicsDevice, Material previousMaterial) { }
        public virtual void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix) { }
        public virtual void ResetState(GraphicsDevice graphicsDevice) { }

        public virtual bool GetBlending() { return false; }
        public virtual Texture2D GetShadowTexture() { return null; }
        public virtual SamplerState GetShadowTextureAddressMode() { return SamplerState.LinearWrap; }
        public int KeyLengthRemainder()
        {
            if (string.IsNullOrEmpty(Key))
                return 0;
            return Key.Length % 10;
        }

        [CallOnThread("Loader")]
        public virtual void Mark()
        {
            Viewer.MaterialManager.Mark(this);
        }
    }

    public class EmptyMaterial : Material
    {
        public EmptyMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }
    }

    public class BasicMaterial : Material
    {
        public BasicMaterial(Viewer viewer, string key)
            : base(viewer, key)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            foreach (var item in renderItems)
                item.RenderPrimitive.Draw(graphicsDevice);
        }
    }

    public class BasicBlendedMaterial : BasicMaterial
    {
        public BasicBlendedMaterial(Viewer viewer, string key)
            : base(viewer, key)
        {
        }

        public override bool GetBlending()
        {
            return true;
        }
    }

    public class SpriteBatchMaterial : BasicBlendedMaterial
    {
        public readonly SpriteBatch SpriteBatch;

        public SpriteBatchMaterial(Viewer viewer)
            : base(viewer, null)
        {
            SpriteBatch = new SpriteBatch(Viewer.RenderProcess.GraphicsDevice);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            SpriteBatch.End();

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }

    public class CabSpriteBatchMaterial : BasicBlendedMaterial
    {
        public readonly SpriteBatch SpriteBatch;
        private CabShader CabShader;
        bool LightItem;
        string TextureName;

        public CabSpriteBatchMaterial(Viewer viewer, CabShader cabShader, bool lightItem, string textureName)
            : base(viewer, null)
        {
            SpriteBatch = new SpriteBatch(Viewer.RenderProcess.GraphicsDevice);
            CabShader = cabShader;
            LightItem = lightItem;
            TextureName = textureName;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            if (CabShader != null)
            {
                CabShader.SetData(Viewer.MaterialManager.sunDirection, false, false, Viewer.Simulator.Weather.OvercastFactor, LightItem, TextureName);
                SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, null, DepthStencilState.Default, null, CabShader);
            }
            else
                SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            SpriteBatch.End();

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }
    }

    [Flags]
    public enum SceneryMaterialOptions
    {
        None = 0,
        Diffuse = 0x1,
        AlphaTest = 0x2,
        AlphaBlendingNone = 0x0,
        AlphaBlendingBlend = 0x4,
        AlphaBlendingAdd = 0x8,
        AlphaBlendingMask = 0xC,
        ShaderImage = 0x00,
        ShaderDarkShade = 0x10,
        ShaderHalfBright = 0x20,
        ShaderFullBright = 0x30,
        ShaderVegetation = 0x40,
        ShaderMask = 0x70,
        Specular0 = 0x000,
        Specular25 = 0x080,
        Specular750 = 0x100,
        SpecularMask = 0x180,
        TextureAddressModeWrap = 0x000,
        TextureAddressModeMirror = 0x200,
        TextureAddressModeClamp = 0x400,
        TextureAddressModeBorder = 0x600,
        TextureAddressModeMask = 0x600,
        NightTexture = 0x800,
        UndergroundTexture = 0x40000000,
    }

    public class SceneryMaterial : Material
    {
        readonly SceneryMaterialOptions Options;
        readonly float MipMapBias;
        protected Texture2D Texture;
        private readonly string TexturePath;
        protected Texture2D NightTexture;
        byte AceAlphaBits;
        IEnumerator<EffectPass> ShaderPassesDarkShade;
        IEnumerator<EffectPass> ShaderPassesFullBright;
        IEnumerator<EffectPass> ShaderPassesHalfBright;
        IEnumerator<EffectPass> ShaderPassesImage;
        IEnumerator<EffectPass> ShaderPassesVegetation;
        IEnumerator<EffectPass> ShaderPasses;
        public static readonly DepthStencilState DepthReadCompareLess = new DepthStencilState
        {
            DepthBufferWriteEnable = false,
            DepthBufferFunction = CompareFunction.Less,
        };
        private static readonly Dictionary<TextureAddressMode, Dictionary<float, SamplerState>> SamplerStates = new Dictionary<TextureAddressMode, Dictionary<float, SamplerState>>();

        public SceneryMaterial(Viewer viewer, string texturePath, SceneryMaterialOptions options, float mipMapBias)
            : base(viewer, texturePath)
        {
            Options = options;
            MipMapBias = mipMapBias;
            TexturePath = texturePath;
            Texture = SharedMaterialManager.MissingTexture;
            NightTexture = SharedMaterialManager.MissingTexture;

            if (!string.IsNullOrEmpty(texturePath) && (Options & SceneryMaterialOptions.NightTexture) != 0 && ((!viewer.DontLoadNightTextures && !viewer.DontLoadDayTextures)
                || TexturePath.IndexOf(@"\trainset\", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, texturePath);
                if (!string.IsNullOrEmpty(nightTexturePath))
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath);
                Texture = Viewer.TextureManager.Get(texturePath, true);
            }
            else if ((Options & SceneryMaterialOptions.NightTexture) != 0 && viewer.DontLoadNightTextures)
            {
                viewer.NightTexturesNotLoaded = true;
                Texture = Viewer.TextureManager.Get(texturePath, true);
            }
            else if ((Options & SceneryMaterialOptions.NightTexture) != 0 && viewer.DontLoadDayTextures)
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, texturePath);
                if (!string.IsNullOrEmpty(nightTexturePath))
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath);
                if (NightTexture != SharedMaterialManager.MissingTexture)
                {
                    viewer.DayTexturesNotLoaded = true;
                }
            }
            else
            {
                Texture = Viewer.TextureManager.Get(texturePath, true);
            }

            var texture = SharedMaterialManager.MissingTexture;
            if (Texture != SharedMaterialManager.MissingTexture && Texture != null) texture = Texture;
            else if (NightTexture != SharedMaterialManager.MissingTexture && NightTexture != null) texture = NightTexture;
            if (texture.Tag != null && texture.Tag.GetType() == typeof(Orts.Formats.Msts.AceInfo))
                AceAlphaBits = ((Orts.Formats.Msts.AceInfo)texture.Tag).AlphaBits;
            else
                AceAlphaBits = 0;
        }

        public bool LoadNightTexture()
        {
            bool oneMore = false;
            if (((Options & SceneryMaterialOptions.NightTexture) != 0) && (NightTexture == SharedMaterialManager.MissingTexture))
            {
                var nightTexturePath = Helpers.GetNightTextureFile(Viewer.Simulator, TexturePath);
                if (!string.IsNullOrEmpty(nightTexturePath))
                {
                    NightTexture = Viewer.TextureManager.Get(nightTexturePath);
                    oneMore = true;
                }
            }
            return oneMore;
        }

        public bool LoadDayTexture()
        {
            bool oneMore = false;
            if (Texture == SharedMaterialManager.MissingTexture && !string.IsNullOrEmpty(TexturePath))
            {
                Texture = Viewer.TextureManager.Get(TexturePath);
                oneMore = true;
            }
            return oneMore;
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            var shader = Viewer.MaterialManager.SceneryShader;
            if (ShaderPassesDarkShade == null) ShaderPassesDarkShade = shader.Techniques["DarkShadePS"].Passes.GetEnumerator();
            if (ShaderPassesFullBright == null) ShaderPassesFullBright = shader.Techniques["FullBrightPS"].Passes.GetEnumerator();
            if (ShaderPassesHalfBright == null) ShaderPassesHalfBright = shader.Techniques["HalfBrightPS"].Passes.GetEnumerator();
            if (ShaderPassesImage == null) ShaderPassesImage = shader.Techniques["ImagePS"].Passes.GetEnumerator();
            if (ShaderPassesVegetation == null) ShaderPassesVegetation = shader.Techniques["VegetationPS"].Passes.GetEnumerator();

            shader.LightingDiffuse = (Options & SceneryMaterialOptions.Diffuse) != 0 ? 1 : 0;

            if (GetBlending())
            {
                if (previousMaterial == null
                    && (Options & SceneryMaterialOptions.AlphaBlendingMask) != SceneryMaterialOptions.AlphaBlendingAdd)
                {
                    graphicsDevice.BlendState = BlendState.NonPremultiplied;
                    graphicsDevice.DepthStencilState = DepthStencilState.Default;
                    shader.ReferenceAlpha = 250;
                }
                else
                {
                    shader.ReferenceAlpha = 10;

                    if ((Options & SceneryMaterialOptions.AlphaBlendingMask) == SceneryMaterialOptions.AlphaBlendingBlend)
                    {
                        graphicsDevice.BlendState = BlendState.NonPremultiplied;
                        graphicsDevice.DepthStencilState = DepthReadCompareLess;
                    }
                    else
                    {
                        graphicsDevice.BlendState = BlendState.Additive;
                        graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                    }
                }
            }
            else
            {
                graphicsDevice.BlendState = BlendState.Opaque;
                if ((Options & SceneryMaterialOptions.AlphaTest) != 0)
                {
                    shader.ReferenceAlpha = 200;
                }
                else
                {
                    shader.ReferenceAlpha = -1;
                }
            }

            switch (Options & SceneryMaterialOptions.ShaderMask)
            {
                case SceneryMaterialOptions.ShaderImage:
                    shader.CurrentTechnique = shader.Techniques["ImagePS"];
                    ShaderPasses = ShaderPassesImage;
                    break;
                case SceneryMaterialOptions.ShaderDarkShade:
                    shader.CurrentTechnique = shader.Techniques["DarkShadePS"];
                    ShaderPasses = ShaderPassesDarkShade;
                    break;
                case SceneryMaterialOptions.ShaderHalfBright:
                    shader.CurrentTechnique = shader.Techniques["HalfBrightPS"];
                    ShaderPasses = ShaderPassesHalfBright;
                    break;
                case SceneryMaterialOptions.ShaderFullBright:
                    shader.CurrentTechnique = shader.Techniques["FullBrightPS"];
                    ShaderPasses = ShaderPassesFullBright;
                    break;
                case SceneryMaterialOptions.ShaderVegetation:
                case SceneryMaterialOptions.ShaderVegetation | SceneryMaterialOptions.ShaderFullBright:
                    shader.CurrentTechnique = shader.Techniques["VegetationPS"];
                    ShaderPasses = ShaderPassesVegetation;
                    break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.ShaderMask value.");
            }

            switch (Options & SceneryMaterialOptions.SpecularMask)
            {
                case SceneryMaterialOptions.Specular0:
                    shader.LightingSpecular = 0;
                    break;
                case SceneryMaterialOptions.Specular25:
                    shader.LightingSpecular = 25;
                    break;
                case SceneryMaterialOptions.Specular750:
                    shader.LightingSpecular = 750;
                    break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.SpecularMask value.");
            }

            if (TexturePath != null && TexturePath.IndexOf("acleantrack", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                shader.LightingSpecular = 0;
            }

            graphicsDevice.SamplerStates[0] = GetShadowTextureAddressMode();

            if (NightTexture != null && NightTexture != SharedMaterialManager.MissingTexture && (((Options & SceneryMaterialOptions.UndergroundTexture) != 0 &&
                (Viewer.MaterialManager.sunDirection.Y <= -0.085f || Viewer.Simulator.CabInDarkTunnel || Viewer.Simulator.CarInDarkTunnel)) || Viewer.MaterialManager.sunDirection.Y < 0.0f - ((float)KeyLengthRemainder()) / 5000f))
            {
                shader.ImageTexture = NightTexture;
                shader.ImageTextureIsNight = true;
            }
            else
            {
                shader.ImageTexture = Texture;
                shader.ImageTextureIsNight = false;
            }
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.SceneryShader;

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref XNAViewMatrix, ref XNAProjectionMatrix);
                    shader.ZBias = item.RenderPrimitive.ZBias;
                    ShaderPasses.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            var shader = Viewer.MaterialManager.SceneryShader;
            shader.ImageTextureIsNight = false;
            shader.LightingDiffuse = 1;
            shader.LightingSpecular = 0;
            shader.ReferenceAlpha = 0;

            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            bool alphaTestRequested = (Options & SceneryMaterialOptions.AlphaTest) != 0;
            bool alphaBlendRequested = (Options & SceneryMaterialOptions.AlphaBlendingMask) != 0;

            return alphaBlendRequested
                    && (AceAlphaBits > 1
                          || (AceAlphaBits == 1 && !alphaTestRequested));
        }

        public override Texture2D GetShadowTexture()
        {
            if (NightTexture != null && NightTexture != SharedMaterialManager.MissingTexture && (((Options & SceneryMaterialOptions.UndergroundTexture) != 0 &&
                (Viewer.MaterialManager.sunDirection.Y <= -0.085f || Viewer.Simulator.CabInDarkTunnel || Viewer.Simulator.CarInDarkTunnel)) || Viewer.MaterialManager.sunDirection.Y < 0.0f - ((float)KeyLengthRemainder()) / 5000f))
                return NightTexture;

            return Texture;
        }

        public override SamplerState GetShadowTextureAddressMode()
        {
            var mipMapBias = MipMapBias < -1 ? -1 : MipMapBias;
            TextureAddressMode textureAddressMode;
            switch (Options & SceneryMaterialOptions.TextureAddressModeMask)
            {
                case SceneryMaterialOptions.TextureAddressModeWrap:
                    textureAddressMode = TextureAddressMode.Wrap; break;
                case SceneryMaterialOptions.TextureAddressModeMirror:
                    textureAddressMode = TextureAddressMode.Mirror; break;
                case SceneryMaterialOptions.TextureAddressModeClamp:
                    textureAddressMode = TextureAddressMode.Clamp; break;
                case SceneryMaterialOptions.TextureAddressModeBorder:
                    textureAddressMode = TextureAddressMode.Border; break;
                default:
                    throw new InvalidDataException("Options has unexpected SceneryMaterialOptions.TextureAddressModeMask value.");
            }

            if (!SamplerStates.ContainsKey(textureAddressMode))
                SamplerStates.Add(textureAddressMode, new Dictionary<float, SamplerState>());

            if (!SamplerStates[textureAddressMode].ContainsKey(mipMapBias))
                SamplerStates[textureAddressMode].Add(mipMapBias, new SamplerState
                {
                    AddressU = textureAddressMode,
                    AddressV = textureAddressMode,
                    Filter = TextureFilter.Anisotropic,
                    MaxAnisotropy = 16,
                    MipMapLevelOfDetailBias = mipMapBias
                });

            return SamplerStates[textureAddressMode][mipMapBias];
        }

        public override void Mark()
        {
            Viewer.TextureManager.Mark(Texture);
            Viewer.TextureManager.Mark(NightTexture);
            base.Mark();
        }
    }

    public class ShadowMapMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesShadowMap;
        IEnumerator<EffectPass> ShaderPassesShadowMapForest;
        IEnumerator<EffectPass> ShaderPassesShadowMapBlocker;
        IEnumerator<EffectPass> ShaderPasses;
        IEnumerator<EffectPass> ShaderPassesBlur;
        VertexBuffer BlurVertexBuffer;

        public enum Mode
        {
            Normal,
            Forest,
            Blocker,
        }

        public ShadowMapMaterial(Viewer viewer)
            : base(viewer, null)
        {
            var shadowMapResolution = Viewer.Settings.ShadowMapResolution;
            BlurVertexBuffer = new VertexBuffer(Viewer.RenderProcess.GraphicsDevice, typeof(VertexPositionTexture), 4, BufferUsage.WriteOnly);
            BlurVertexBuffer.SetData(new[] {
                new VertexPositionTexture(new Vector3(-1, +1, 0), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-1, -1, 0), new Vector2(0, shadowMapResolution)),
                new VertexPositionTexture(new Vector3(+1, +1, 0), new Vector2(shadowMapResolution, 0)),
                new VertexPositionTexture(new Vector3(+1, -1, 0), new Vector2(shadowMapResolution, shadowMapResolution)),
            });
        }

        public void SetState(GraphicsDevice graphicsDevice, Mode mode)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques[mode == Mode.Forest ? "ShadowMapForest" : mode == Mode.Blocker ? "ShadowMapBlocker" : "ShadowMap"];
            if (ShaderPassesShadowMap == null) ShaderPassesShadowMap = shader.Techniques["ShadowMap"].Passes.GetEnumerator();
            if (ShaderPassesShadowMapForest == null) ShaderPassesShadowMapForest = shader.Techniques["ShadowMapForest"].Passes.GetEnumerator();
            if (ShaderPassesShadowMapBlocker == null) ShaderPassesShadowMapBlocker = shader.Techniques["ShadowMapBlocker"].Passes.GetEnumerator();
            ShaderPasses = mode == Mode.Forest ? ShaderPassesShadowMapForest : mode == Mode.Blocker ? ShaderPassesShadowMapBlocker : ShaderPassesShadowMap;

            graphicsDevice.RasterizerState = mode == Mode.Blocker ? RasterizerState.CullClockwise : RasterizerState.CullCounterClockwise;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.ShadowMapShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            shader.SetData(ref XNAViewMatrix);
            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    var wvp = item.XNAMatrix * viewproj;
                    shader.SetData(ref wvp, item.Material.GetShadowTexture());
                    graphicsDevice.SamplerStates[0] = item.Material.GetShadowTextureAddressMode();
                    ShaderPasses.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }

        public RenderTarget2D ApplyBlur(GraphicsDevice graphicsDevice, RenderTarget2D shadowMap, RenderTarget2D renderTarget)
        {
            var wvp = Matrix.Identity;

            var shader = Viewer.MaterialManager.ShadowMapShader;
            shader.CurrentTechnique = shader.Techniques["ShadowMapBlur"];
            shader.SetBlurData(ref wvp);
            if (ShaderPassesBlur == null) ShaderPassesBlur = shader.CurrentTechnique.Passes.GetEnumerator();

            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
            graphicsDevice.SetVertexBuffer(BlurVertexBuffer);

            ShaderPassesBlur.Reset();
            while (ShaderPassesBlur.MoveNext())
            {
                shader.SetBlurData(renderTarget);
                ShaderPassesBlur.Current.Apply();
                graphicsDevice.SetRenderTarget(shadowMap);
                graphicsDevice.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);

                graphicsDevice.SetRenderTarget(null);
            }

            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;

            return shadowMap;
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
        }
    }

    public class PopupWindowMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesPopupWindow;
        IEnumerator<EffectPass> ShaderPassesPopupWindowGlass;
        IEnumerator<EffectPass> ShaderPasses;

        public PopupWindowMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }

        public void SetState(GraphicsDevice graphicsDevice, Texture2D screen)
        {
            var shader = Viewer.MaterialManager.PopupWindowShader;
            shader.CurrentTechnique = screen == null ? shader.Techniques["PopupWindow"] : shader.Techniques["PopupWindowGlass"];
            if (ShaderPassesPopupWindow == null) ShaderPassesPopupWindow = shader.Techniques["PopupWindow"].Passes.GetEnumerator();
            if (ShaderPassesPopupWindowGlass == null) ShaderPassesPopupWindowGlass = shader.Techniques["PopupWindowGlass"].Passes.GetEnumerator();
            ShaderPasses = screen == null ? ShaderPassesPopupWindow : ShaderPassesPopupWindowGlass;
            shader.GlassColor = Color.Black;

            graphicsDevice.BlendState = BlendState.NonPremultiplied;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            graphicsDevice.DepthStencilState = DepthStencilState.None;
        }

        public void Render(GraphicsDevice graphicsDevice, RenderPrimitive renderPrimitive, ref Matrix XNAWorldMatrix, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.PopupWindowShader;

            Matrix wvp = XNAWorldMatrix * XNAViewMatrix * XNAProjectionMatrix;
            shader.SetMatrix(XNAWorldMatrix, ref wvp);

            ShaderPasses.Reset();
            while (ShaderPasses.MoveNext())
            {
                ShaderPasses.Current.Apply();
                renderPrimitive.Draw(graphicsDevice);
            }
        }

        public override void ResetState(GraphicsDevice graphicsDevice)
        {
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override bool GetBlending()
        {
            return true;
        }
    }

    public class YellowMaterial : Material
    {
        static BasicEffect basicEffect;

        public YellowMaterial(Viewer viewer)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.RenderProcess.GraphicsDevice);
                basicEffect.Alpha = 1.0f;
                basicEffect.DiffuseColor = new Vector3(197.0f / 255.0f, 203.0f / 255.0f, 37.0f / 255.0f);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }

    public class SolidColorMaterial : Material
    {
        static BasicEffect basicEffect;

        public SolidColorMaterial(Viewer viewer, float a, float r, float g, float b)
            : base(viewer, null)
        {
            if (basicEffect == null)
            {
                basicEffect = new BasicEffect(Viewer.RenderProcess.GraphicsDevice);
                basicEffect.Alpha = a;
                basicEffect.DiffuseColor = new Vector3(r, g, b);
                basicEffect.SpecularColor = new Vector3(0.25f, 0.25f, 0.25f);
                basicEffect.SpecularPower = 5.0f;
                basicEffect.AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight0.DiffuseColor = Vector3.One * 0.8f;
                basicEffect.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(1.0f, -1.0f, -1.0f));
                basicEffect.DirectionalLight0.SpecularColor = Vector3.One;

                basicEffect.DirectionalLight1.Enabled = true;
                basicEffect.DirectionalLight1.DiffuseColor = new Vector3(0.5f, 0.5f, 0.5f);
                basicEffect.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(-1.0f, -1.0f, 1.0f));
                basicEffect.DirectionalLight1.SpecularColor = new Vector3(0.5f, 0.5f, 0.5f);

                basicEffect.LightingEnabled = true;
            }
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            basicEffect.View = XNAViewMatrix;
            basicEffect.Projection = XNAProjectionMatrix;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                foreach (var item in renderItems)
                {
                    basicEffect.World = item.XNAMatrix;
                    pass.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }

    public class Label3DMaterial : SpriteBatchMaterial
    {
        public readonly Texture2D Texture;
        public readonly WindowTextFont Font;

        readonly List<Rectangle> TextBoxes = new List<Rectangle>();

        public Label3DMaterial(Viewer viewer)
            : base(viewer)
        {
            Texture = new Texture2D(SpriteBatch.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Texture.SetData(new[] { Color.White });
            Font = Viewer.WindowManager.TextManager.GetScaled("Arial", 12, System.Drawing.FontStyle.Bold, 1);
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var scaling = (float)graphicsDevice.PresentationParameters.BackBufferHeight / Viewer.RenderProcess.GraphicsDeviceManager.PreferredBackBufferHeight;
            SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, null, null, null, null, Matrix.CreateScale(scaling));
            SpriteBatch.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            TextBoxes.Clear();
            base.Render(graphicsDevice, renderItems, ref XNAViewMatrix, ref XNAProjectionMatrix);
        }

        public override bool GetBlending()
        {
            return true;
        }

        public Point GetTextLocation(int x, int y, string text)
        {
            var textBox = new Rectangle(x, y, Font.MeasureString(text), Font.Height);
            textBox.X -= textBox.Width / 2;
            textBox.Inflate(5, 2);
            var boxes = TextBoxes.Where(box => box.Top <= textBox.Bottom && box.Right >= textBox.Left && box.Left <= textBox.Right).OrderBy(box => -box.Top);
            foreach (var box in boxes)
                if (box.Top <= textBox.Bottom && box.Bottom >= textBox.Top)
                    textBox.Y = box.Top - textBox.Height;
            TextBoxes.Add(textBox);
            return new Point(textBox.X + 5, textBox.Y + 2);
        }
    }

    public class DebugNormalMaterial : Material
    {
        IEnumerator<EffectPass> ShaderPassesGraph;

        public DebugNormalMaterial(Viewer viewer)
            : base(viewer, null)
        {
        }

        public override void SetState(GraphicsDevice graphicsDevice, Material previousMaterial)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            shader.CurrentTechnique = shader.Techniques["Normal"];
            if (ShaderPassesGraph == null) ShaderPassesGraph = shader.Techniques["Normal"].Passes.GetEnumerator();
        }

        public override void Render(GraphicsDevice graphicsDevice, IEnumerable<RenderItem> renderItems, ref Matrix XNAViewMatrix, ref Matrix XNAProjectionMatrix)
        {
            var shader = Viewer.MaterialManager.DebugShader;
            var viewproj = XNAViewMatrix * XNAProjectionMatrix;

            ShaderPassesGraph.Reset();
            while (ShaderPassesGraph.MoveNext())
            {
                foreach (var item in renderItems)
                {
                    shader.SetMatrix(item.XNAMatrix, ref viewproj);
                    ShaderPassesGraph.Current.Apply();
                    item.RenderPrimitive.Draw(graphicsDevice);
                }
            }
        }
    }
}