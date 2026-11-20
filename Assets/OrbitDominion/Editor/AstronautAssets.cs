using System.IO;
using UnityEditor;
using UnityEngine;

namespace OrbitDominion.Editor
{
    // Generated once by Setup. Existing authored materials and textures are never overwritten.
    public static class AstronautAssets
    {
        const string Root="Assets/OrbitDominion/Resources/";
        public static void Ensure()
        {
            string albedo=Root+"Textures/AstronautWeave.png",normal=Root+"Textures/AstronautWeaveNormal.png";
            if(!File.Exists(albedo)||!File.Exists(normal))
            {
                const int size=512;
                var color=new Texture2D(size,size,TextureFormat.RGBA32,false);
                var bump=new Texture2D(size,size,TextureFormat.RGBA32,false,true);
                float Height(int x,int y)
                {
                    float u=(x+size)%size,v=(y+size)%size;
                    float warp=Mathf.Cos(u*Mathf.PI/4),weft=Mathf.Cos(v*Mathf.PI/4);
                    return .5f+(warp+weft)*.12f+warp*weft*.05f;
                }
                for(int y=0;y<size;y++)for(int x=0;x<size;x++)
                {
                    float h=Height(x,y),noise=((x*73+y*157+x*y*13)%113)/113f;
                    float value=.87f+h*.09f+noise*.018f;
                    color.SetPixel(x,y,new Color(value,value,value, .77f));
                    Vector3 n=new Vector3((Height(x-1,y)-Height(x+1,y))*.45f,(Height(x,y-1)-Height(x,y+1))*.45f,1).normalized;
                    bump.SetPixel(x,y,new Color(n.x*.5f+.5f,n.y*.5f+.5f,n.z*.5f+.5f,1));
                }
                color.Apply();bump.Apply();
                if(!File.Exists(albedo))File.WriteAllBytes(albedo,color.EncodeToPNG());
                if(!File.Exists(normal))File.WriteAllBytes(normal,bump.EncodeToPNG());
                Object.DestroyImmediate(color);Object.DestroyImmediate(bump);
                AssetDatabase.ImportAsset(albedo);AssetDatabase.ImportAsset(normal);
            }
            Configure(albedo,false);Configure(normal,true);
            var cloth=Create("AstronautCloth",Color.white,0,.22f);
            if(cloth!=null)
            {
                cloth.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));
                cloth.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                cloth.SetTextureScale("_BaseMap",Vector2.one*12);cloth.SetFloat("_BumpScale",.6f);
                cloth.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(cloth);
            }
            Create("AstronautShell",new Color(.73f,.75f,.71f),.17f,.28f);
            Create("AstronautHardware",new Color(.30f,.34f,.35f),.7f,.35f);
            AssetDatabase.SaveAssets();
        }
        static Material Create(string name,Color color,float metal,float gloss)
        {
            string path=Root+name+".mat";
            if(AssetDatabase.LoadAssetAtPath<Material>(path)!=null)return null;
            var material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
            material.SetFloat("_Metallic",metal);material.SetFloat("_Smoothness",gloss);
            AssetDatabase.CreateAsset(material,path);return material;
        }
        static void Configure(string path,bool normal)
        {
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            const string marker="OrbitDominion.AstronautWeave.v1";
            if(importer.userData==marker)return;
            importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
            importer.sRGBTexture=!normal;importer.wrapMode=TextureWrapMode.Repeat;
            importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=8;
            importer.mipmapEnabled=true;importer.textureCompression=TextureImporterCompression.CompressedHQ;
            importer.userData=marker;importer.SaveAndReimport();
        }
    }
}
