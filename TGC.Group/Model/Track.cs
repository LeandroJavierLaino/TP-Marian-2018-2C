using Microsoft.DirectX.Direct3D;
using System;
using System.Drawing;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using TGC.Core.Shaders;


namespace TGC.Group.Model
{

    public class CSoundBlock
    {
        public bool transparente = false;
        public int ds;          // separacion (en puntos de la ruta)
        public Microsoft.DirectX.Direct3D.Device device;
        public CScene T;
        public TGCVector3[] pt_ruta;
        public TGCVector3[] Binormal;
        public TGCVector3[] Normal;
        public float dr;
        public TGCBox box;
        public float dx = 20;
        public float dy = 10;
        public float dz = 30;


        public CSoundBlock(int sep, CScene pT)
        {
            T = pT;
            pt_ruta = T.pt_ruta;
            Binormal = T.Binormal;
            Normal = T.Normal;
            dr = T.ancho_ruta / 2;
            ds = sep;
            device = D3DDevice.Instance.Device;

            box = new TGCBox();
            box.Size = new TGCVector3(dx,dy,dz);
            box.Position = new TGCVector3(0, 0, 0); 
            box.Color = Color.BurlyWood;

            box.updateValues();

        }


        public virtual void Render(Effect effect)
        {
            int pr = T.player.pos_en_ruta - 8;
            if (pr < 0)
                return;

            TGCVector3 p0 = new TGCVector3(0, 0, 0);

            box.Effect = effect;
            box.Technique  = "EdgeCube";
            effect.Technique = "EdgeCube";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(new TGCVector3(0, 0.5f, 1)));



            float dr2 = dr - 27;
            int index = pr / T.pt_x_track;
            for (int t = 0; t < 20 && index < T.cant_sound_tracks; ++t)
            {
                int i = index * T.pt_x_track;
                TGCVector3 Eje_x = -Binormal[i];
                TGCVector3 Eje_y = Normal[i];
                TGCVector3 Eje_z = T.Tangent[i];
                int freq = T.sound_tracks[index];
                float X;
                if (freq <= 0)
                    X = -10000;
                else
                {
                    float s = (freq - 100.0f) / 340.0f;  // 0..1
                    X = (2 * dr2) * s - dr2;
                }
                p0 = pt_ruta[i] + Eje_x * (X - dx / 2) - Eje_z * (dz / 2);

                TGCMatrix matRot = new TGCMatrix();
                matRot.M11 = Eje_x.X;   matRot.M12 = Eje_y.X;   matRot.M13 = Eje_z.X;   matRot.M14 = 0;
                matRot.M21 = Eje_x.Y;   matRot.M22 = Eje_y.Y;   matRot.M23 = Eje_z.Y;   matRot.M24 = 0;
                matRot.M31 = Eje_x.Z;   matRot.M32 = Eje_y.Z;   matRot.M33 = Eje_z.Z;   matRot.M34 = 0;
                matRot.M41 = 0;         matRot.M42 = 0;         matRot.M43 = 0;         matRot.M44 = 1;
                TGCMatrix matEsc = TGCMatrix.Scaling(
                    T.player.colisiona && T.cur_block_index == index ? new TGCVector3(2, 2, 2) : new TGCVector3(1, 1, 1));


                TGCVector3 desf = new TGCVector3(0,(float)Math.Sin(T.time*30 + index),0) * 1.0f;
                TGCMatrix matWorld = TGCMatrix.Translation(new TGCVector3(dx/2, dy / 2, dz / 2) ) * 
                    matEsc * TGCMatrix.Translation(desf)  * TGCMatrix.TransposeMatrix(matRot) * TGCMatrix.Translation(p0);

                box.Transform = matWorld;
                box.Render();
                
                ++index;
            }

            // el box.render algo caga con las transformaciones.... tengo que hacer esto para recuperarlo
            box.Transform = TGCMatrix.Identity;
            box.Render();

        }

    }



    public class CSkyBox
    {
        Microsoft.DirectX.Direct3D.Device device;
        Texture textura;
        public string MyMediaDir;
        CScene T;
        public VertexBuffer vb = null;


        public CSkyBox(String tx_skybox, CScene pT)
        {
            T = pT;
            MyMediaDir = T.path_media + "Texturas\\";
            device = D3DDevice.Instance.Device;
            textura = Texture.FromBitmap(device, (Bitmap)Image.FromFile(MyMediaDir + tx_skybox),
                    Usage.None, Pool.Managed);

            //Cargar vertices
            int dataIdx = 0;
            int totalVertices = 36*3;

            vb = new VertexBuffer(typeof(CustomVertex.PositionTextured), totalVertices, D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionTextured.Format, Pool.Default);

            float ep = 0.001f;
            CustomVertex.PositionTextured[] data = new CustomVertex.PositionTextured[totalVertices];
            float[,] v = {
                {-1, -1, -1, 0.25f+ep, 1.0f / 3.0f +ep},      //v0
                {1, -1, -1, 0.5f-ep, 1.0f/3.0f+ep},           //v1
                {-1, 1, -1, 0.25f+ep, 2.0f / 3.0f -ep},       //v2
                { 1, 1, -1, 0.5f-ep, 2.0f / 3.0f -ep},        //v3
                { -1, -1, 1, 0.0f+ep, 1.0f / 3.0f +ep},       //v4
                { -1, 1, 1, 0.0f+ep, 2.0f / 3.0f -ep},        //v5
                { -1, -1, 1, 1.0f-ep, 1.0f / 3.0f +ep},        //v6
                { -1, 1, 1, 1.0f-ep, 2.0f / 3.0f-ep },        //v7
                { -1, -1, 1, 0.25f+ep, 0.0f +ep},             //v8
                { 1, -1, 1, 0.5f-ep, 0.0f +ep},               //v9
                { -1, 1, 1, 0.25f+ep, 1.0f-ep},              //v10
                { 1, 1, 1, 0.5f-ep, 1.0f -ep},                //v11
                { 1, -1, 1, 0.75f-ep, 1.0f / 3.0f +ep},       //v12
                { 1, 1, 1, 0.75f-ep, 2.0f / 3.0f -ep},        //v13
                };

         
            int[] index = {
                0, 1, 3,        // face1 = v0 - v1 - v3
                0, 3, 2,        // face2 = v0 - v3 - v2 
                0, 2, 4,        // face3 = v0 - v2 - v4 
                2, 4, 5,        // face4 = v2 - v4 - v5 
                1, 3, 12,       // face5 = v1 - v3 - v12 
                12, 13,3,       // face6 = v12 - v13 - v3 
                6, 7, 12,       // face7 = v6 - v7 - v12 
                7, 12, 13,      // face8 = v7 - v12 - v13
                2, 3, 10,       // face9 = v2 - v3 - v10 
                3, 10, 11,      // face10 = v3 - v10 - v11
                0, 1, 8,        // face11 = v0 - v1 - v8 
                1, 8, 9         // face12 = v1 - v8 - v9
            };

            for(int face = 0;face<12;++face)
            {
                for(int i=0;i<3;++i)
                {
                    int j = index[face * 3 + i];
                    TGCVector3 pos = new TGCVector3(v[j, 0], v[j, 1], v[j, 2]);
                    data[dataIdx++] = new CustomVertex.PositionTextured(pos, v[j, 3] , v[j, 4]);
                }
            }

            vb.SetData(data, 0, LockFlags.None);


        }

        public void Render(Effect effect)
        {
            effect.Technique = "SkyBox";
            effect.SetValue("texDiffuseMap", textura);
            device.SetStreamSource(0, vb, 0);
            device.VertexFormat = CustomVertex.PositionTextured.Format;
            int numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, 12);
                effect.EndPass();
            }
            effect.End();

        }


        public void Dispose()
        {
            if (vb != null)
            {
                vb.Dispose();
                vb = null;
            }

            if (textura != null)
            {
                textura.Dispose();
                textura = null;
            }
        }


    }


    public class CBaseTrack
    {
        public bool transparente = false;
        public int ds;          // separacion (en puntos de la ruta)
        public int cant_v = 12;      // cantidad de vertices x item
        public int cant_items;
        public int totalVertices;
        public int inicio = 0;      // punto inicial de la ruta
        public int fin = 0;      // punto final de la ruta
        public VertexBuffer vb = null;
        public string tx_fname;
        public Texture textura;
        public Microsoft.DirectX.Direct3D.Device device;

        public CScene T;
        public string MyMediaDir;
        public TGCVector3[] pt_ruta;
        public TGCVector3[] Binormal;
        public TGCVector3[] Normal;
        public float dr;
        public int dataIdx;

        public static CBaseTrack Create(int cant, int pinicio, int sep, CScene pT)
        {
            CBaseTrack obj = new CBaseTrack();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public virtual void Init(int cant, int pinicio, int sep, CScene pT)
        {
            T = pT;
            MyMediaDir = T.path_media + "Texturas\\";
            pt_ruta = T.pt_ruta;
            Binormal = T.Binormal;
            Normal = T.Normal;
            dr = T.ancho_ruta/2;
            cant_items = cant;
            ds = sep;
            inicio = pinicio;
            fin = inicio + cant * ds;

            //Crear vertexBuffer
            totalVertices = cant * cant_v;
            vb = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), totalVertices, D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Default);

            //Cargar vertices
            dataIdx = 0;
            CustomVertex.PositionNormalTextured[] data = new CustomVertex.PositionNormalTextured[totalVertices];
            for (var t = 0; t < cant; t++)
            {
                FillVertexBuffer(t, data);
            }
            vb.SetData(data, 0, LockFlags.None);

            device = D3DDevice.Instance.Device;
            if (tx_fname.Length > 1)
                textura = Texture.FromBitmap(device, (Bitmap)Image.FromFile(MyMediaDir + tx_fname),
                    Usage.None, Pool.Managed);
            else
                textura = null;
        
        }

        public virtual void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
        }

        public virtual void Render(Effect effect)
        {
            int pr = T.player.pos_en_ruta-8;

            // verifico si el objeto esta dentro del conjunto ptencialmente visible 
            if (pr > fin)     // el objeto quedo atras en la ruta
                return;
            if (pr + T.fov < inicio)       // esta muy adelante, todavia no entro en campo visual
                return;

            var p = pr - inicio;            // posicion relativa
            if (p < 0)
                p = 0;

            int start = cant_v * (int)(p / ds);
            int end = cant_v * (int)( (p+T.fov) / ds);
            if (end >= start + totalVertices)
                end = totalVertices + start;
            int cant_vertices = end - start;
            int cant_primitivas = cant_vertices / 3;
			
			SetTextures(effect);
            device.SetStreamSource(0, vb, 0);
            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
			
            int numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                device.DrawPrimitives(PrimitiveType.TriangleList, start, cant_primitivas);
                effect.EndPass();
            }
            effect.End();

        }
		
		public virtual void SetTextures(Effect effect)
		{
            effect.Technique = "DefaultTechnique";
            effect.SetValue("texDiffuseMap", textura);
		}
			

        public void Dispose()
        {
            if (vb != null)
            {
                vb.Dispose();
                vb = null;
            }

            if(textura!=null)
            {
                textura.Dispose();
                textura = null;
            }
        }



    }

    
    public class CGlowRing : CBaseTrack
    {
        public CGlowRing()
        {
            cant_v = 6;      // cantidad de vertices x item
            tx_fname = "";
            transparente = true;
        }

        public static new CGlowRing Create(int cant, int pinicio, int sep, CScene pT)
        {
            CGlowRing obj = new CGlowRing();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t * ds;
            TGCVector3 p0, p1, p2, p3;
            p0 = pt_ruta[i] - Binormal[i] * (dr + 50) - Normal[i] * 30;
            p1 = pt_ruta[i] - Binormal[i] * (dr + 50) + Normal[i] * 120;
            p2 = pt_ruta[i] + Binormal[i] * (dr + 50) - Normal[i] * 30;
            p3 = pt_ruta[i] + Binormal[i] * (dr + 50) + Normal[i] * 120;
            TGCVector3 N = new TGCVector3(0, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 0, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 1, 1);
        }

        public override void SetTextures(Effect effect)
        {
            effect.Technique = "GlowRing";
            effect.SetValue("texDiffuseMap", textura);
        }

    }


    public class CGuardRail : CBaseTrack
    {
        public float ancho_guarray = 5;
        public float alto_guarray = 50;
        public float Kr = 0.05f;
        public float KBN = 1;
        public float KP = 1;
        public TGCVector3 color;
        public TGCVector3 shader_param;


        public CGuardRail()
        {
            cant_v = 6;      // cantidad de vertices x item
            tx_fname = "";
            transparente = true;
        }

        public static CGuardRail Create(int cant, int pinicio, int sep, CScene pT, bool izq, TGCVector3 color, TGCVector3 shader_param )
        {
            CGuardRail obj = new CGuardRail();
            obj.Init(cant, pinicio, sep, pT,izq);
            obj.color = color;
            obj.shader_param = shader_param;
            return obj;
        }

        public void Init(int cant, int pinicio, int sep, CScene pT, bool izq)
        {
            KBN = izq ? -1 : 1;
            KP = izq ? 0 : 1;
            Init(cant, pinicio, sep, pT);
        }


        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t;
            var p0 = pt_ruta[i] - Binormal[i]* KBN * dr;
            var p1 = pt_ruta[i] - Binormal[i] * KBN * (dr + ancho_guarray) + Normal[i] * alto_guarray;
            var p2 = pt_ruta[i+1] - Binormal[i+1] * KBN * dr;
            var p3 = pt_ruta[i+1] - Binormal[i] * KBN * (dr + ancho_guarray) + Normal[i+1] * alto_guarray;
            TGCVector3 N = new TGCVector3(0, 1, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, i * Kr, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, i * Kr, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, (i+1) * Kr, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, (i + 1) * Kr, 0);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, i * Kr, 1);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, (i +1) * Kr, 1);

        }

        public override void SetTextures(Effect effect)
        {
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(color));
            effect.SetValue("gl_k0", shader_param.X);
            effect.SetValue("gl_k1", shader_param.Y);
            effect.SetValue("gl_k2", shader_param.Z);
            effect.Technique = "GlowLines";
        }

    }


    public class CPiso : CBaseTrack
    {

        public CPiso()
        {
            cant_v = 6;      // cantidad de vertices x item
            tx_fname = "";
            transparente = true;
        }

        public static new CPiso Create(int cant, int pinicio, int sep, CScene pT)
        {
            CPiso obj = new CPiso();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t;
            var Kr = 0.05f;

            var p0 = pt_ruta[i] - Binormal[i] * dr;
            var p1 = pt_ruta[i] + Binormal[i] * dr;
            var p2 = pt_ruta[i+1] - Binormal[i + 1] * dr;
            var p3 = pt_ruta[i + 1] + Binormal[i + 1] * dr;

            TGCVector3 N = new TGCVector3(0, 1, 0);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0 , i * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1, i * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0 , (i + 1) * Kr);

            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0 , (i + 1) * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1 , i * Kr);
            data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1 , (i + 1) * Kr);

        }

        public override void SetTextures(Effect effect)
        {
            effect.Technique = "Piso";
        }


    }


    public class CBaseCubo : CBaseTrack
    {
        public int cubosxpt = 2;
        public TGCVector3 color;
        public TGCVector3 size;
        public Random rnd;
        public CBaseCubo()
        {
            tx_fname = "";
            rnd = new Random();
            transparente = false;
        }

        public void Init(int cant_cubos, int cant, int pinicio, int sep, CScene pT, TGCVector3 pcolor , TGCVector3 psize)
        {
            cubosxpt = cant_cubos;          // cantidad de cubos por punto 
            cant_v = 36 * cubosxpt;      // cantidad de vertices x item
            color = pcolor;
            size = psize;
            Init(cant, pinicio, sep, pT);
        }

        public static CBaseCubo Create(int cant_cubos, int cant, int pinicio, int sep, CScene pT,TGCVector3 pcolor, TGCVector3 psize)
        {
            CBaseCubo obj = new CBaseCubo();
            obj.Init(cant_cubos,cant, pinicio, sep, pT,pcolor,psize);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            var i = inicio + t * ds;
            for (var j = 0; j < cubosxpt; ++j)
            {
                TGCVector3[] Pt = new TGCVector3[8];
                que_puntos(i,j,Pt);
                float ex = 1;
                float ey = 1;

                // abajo
                TGCVector3 N = new TGCVector3(0, -1, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 0, 1*ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1*ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[3], N, 1 * ex, 0);

                // arriba
                N = new TGCVector3(0, 1, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[5], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 1 * ex, 0);

                // cara atras
                N = new TGCVector3(0, 0, -1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[5], N, 1 * ex, 1 * ey);

                // cara adelante
                N = new TGCVector3(0, 0, 1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[3], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 0, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);

                // cara derecha
                N = new TGCVector3(1, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[2], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[1], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[6], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[5], N, 0, 1 * ey);

                // cara izquierda
                N = new TGCVector3(-1, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[3], N, 1 * ex, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[0], N, 0, 0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[7], N, 1 * ex, 1 * ey);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(Pt[4], N, 0, 1 * ey);
            }
        }

      


        public virtual void que_puntos(int i,int j,TGCVector3 []pt) 
        {
            var dx = size.X;
            var dy = size.Y;
            var dz = size.Z;
            TGCVector3 p0 = new TGCVector3(0, 0, 0);

            switch ((i + j) % 2)
            {
                default:
                case 0:
                    p0 = pt_ruta[i] + Binormal[i] * (dr + 20);
                    break;
                case 1:
                    p0 = pt_ruta[i] - Binormal[i] * (dr + 20);
                    break;
            }

            TGCVector3[] Tangent = T.Tangent;

            pt[0] = p0;
            pt[1] = p0 + Binormal[i] * dx;
            pt[2] = p0 + Binormal[i] * dx + Tangent[i] * dz;
            pt[3] = p0 + Tangent[i] * dz;
            pt[4] = p0 + Normal[i] * dy;
            pt[5] = pt[1] + Normal[i] * dy;
            pt[6] = pt[2] + Normal[i] * dy;
            pt[7] = pt[3] + Normal[i] * dy;

           
        }

        public override void SetTextures(Effect effect)
        {
            effect.Technique = "EdgeCube";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(color));
        }

    }


    public class CLineas : CBaseCubo
    {
        public static new CLineas Create(int cant_cubos, int cant, int pinicio, int sep, CScene pT, TGCVector3 pcolor, TGCVector3 psize)
        {
            CLineas obj = new CLineas();
            obj.Init(cant_cubos, cant, pinicio, sep, pT, pcolor, psize);
            obj.transparente = true;
            return obj;
        }

        public override void que_puntos(int i, int j, TGCVector3[] pt)
        {
            var dx = size.X;
            var dy = size.Y;
            var dz = size.Z;
            TGCVector3 p0 = new TGCVector3(0, 0, 0);

            float desf_x;
            switch ((i + j) % 2)
            {
                default:
                case 0:
                    desf_x = dr +  30;
                    break;
                case 1:
                    desf_x = -dr - 30;
                    break;
            }

            TGCVector3[] Tangent = T.Tangent;

            p0 = pt_ruta[i] - Normal[i] * dy + Binormal[i] * desf_x;

            pt[0] = p0 ;
            pt[1] = p0 + Binormal[i] * dx;
            pt[2] = p0 + Binormal[i] * dx + Tangent[i] * dz;
            pt[3] = p0 + Tangent[i] * dz;
            pt[4] = p0 + Normal[i] * 2*dy + Binormal[i] * 20 * desf_x;
            pt[5] = pt[1] + Normal[i] * 2 * dy + Binormal[i] * 20 * desf_x;
            pt[6] = pt[2] + Normal[i] * 2 * dy + Binormal[i] * 20 * desf_x;
            pt[7] = pt[3] + Normal[i] * 2 * dy + Binormal[i] * 20 * desf_x;


        }

        public override void SetTextures(Effect effect)
        {
            effect.Technique = "GlowBar";
            //effect.Technique = "Buildings";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(color));
        }

    }


    public class CBuildings : CBaseCubo
    {
        public IntPtr PCMBuffer = (IntPtr)0;
        int cant_samples = 0;
        public CBuildings(IntPtr wav_data, int p_cant_samples)
        {
            PCMBuffer = wav_data;
            cant_samples = p_cant_samples;
            tx_fname = "";
            rnd = new Random();
            transparente = true;
        }

        public static CBuildings Create(IntPtr wav_data, int p_cant_samples, int cant, int pinicio, int sep, CScene pT, TGCVector3 pcolor)
        {
            CBuildings obj = new CBuildings(wav_data, p_cant_samples);
            obj.Init(10,cant, pinicio, sep, pT, pcolor,new TGCVector3(0,0,0));
            return obj;
        }


        public unsafe override void que_puntos(int i, int j, TGCVector3[] pt)
        {
            int SAMPLE_RATE = 44100;
            float s = (float)j / (float)cubosxpt;
            float dist = T.Distance[i] * (1 - s) + T.Distance[i + 1] * s;
            float time = dist / CShip.vel_lineal;
            int index = (int)(time * SAMPLE_RATE) % cant_samples;
            index -= (int)(0.6f*SAMPLE_RATE);
            if (index < 0)
                index = 0;

            short* wav = (short*)(PCMBuffer);
            var dx = 30;
            var dy = wav[index] / 32768.0f * 400;
            var dz = 15;
            TGCVector3[] Tangent = T.Tangent;
            TGCVector3 p0 = new TGCVector3(0, 0, 0);

             switch ((i + j) % 2)
             {
                 default:
                 case 0:
                     p0 = pt_ruta[i] + Binormal[i] * (dr + 110 );
                     break;
                 case 1:
                     p0 = pt_ruta[i] - Binormal[i] * (dr + 110 );
                     break;
             }


            p0 = p0 + Tangent[i]* (j*10);
            pt[0] = p0;
            pt[1] = p0 + Binormal[i] * dx;
            pt[2] = p0 + Binormal[i] * dx + Tangent[i] * dz;
            pt[3] = p0 + Tangent[i] * dz;
            pt[4] = p0 + Normal[i] * dy;
            pt[5] = pt[1] + Normal[i] * dy;
            pt[6] = pt[2] + Normal[i] * dy;
            pt[7] = pt[3] + Normal[i] * dy;
        }


        public override void Render(Effect effect)
        {
            int pr = T.player.pos_en_ruta - 8;
            int fov = 20;
            // verifico si el objeto esta dentro del conjunto ptencialmente visible 
            if (pr > fin)     // el objeto quedo atras en la ruta
                return;
            if (pr + fov < inicio)       // esta muy adelante, todavia no entro en campo visual
                return;

            var p = pr - inicio;            // posicion relativa
            if (p < 0)
                p = 0;

            int start = cant_v * (int)(p / ds);
            int end = cant_v * (int)((p + fov) / ds);
            if (end >= start + totalVertices)
                end = totalVertices + start;
            int cant_vertices = end - start;
            int cant_primitivas = cant_vertices / 3;

            SetTextures(effect);
            device.SetStreamSource(0, vb, 0);
            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;

            int numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                device.DrawPrimitives(PrimitiveType.TriangleList, start, cant_primitivas);
                effect.EndPass();
            }
            effect.End();

        }



        public override void SetTextures(Effect effect)
        {
            effect.Technique = "Buildings";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(color));
        }


    }

    public class CPatchSideTrack : CBaseTrack
    {
        public int quadsxpt = 30;
        public int cant_patches;

        public CPatchSideTrack()
        {
            cant_v = 12 * quadsxpt;
            tx_fname = "";
            transparente = true;
        }

        public static new CPatchSideTrack Create(int cant, int pinicio, int sep, CScene pT)
        {
            CPatchSideTrack obj = new CPatchSideTrack();
            obj.Init(cant, pinicio, sep, pT);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            float r = 1.5F * dr;
            float dp = 50;
            float dh = 5;
			var i = inicio + t;
			for (var j = 0; j < 2*quadsxpt; ++j)
			{
				float dj = dp * (j- quadsxpt);
				float dj1 = dj + dp;

				var p0 = pt_ruta[i] - Binormal[i] * dj - Normal[i] * 300;
				var p1 = pt_ruta[i] - Binormal[i] * dj1 - Normal[i] * 300;
				var p2 = pt_ruta[i + 1] - Binormal[i + 1] * dj - Normal[i] * 300;
				var p3 = pt_ruta[i + 1] - Binormal[i + 1] * dj1 - Normal[i] * 300;

				p0 = p0 + Normal[i] * (noise(p0 * 0.01f) * dh * j - 20);
				p1 = p1 + Normal[i] * (noise(p1 * 0.01f) * dh * (j + 1) - 20);
				p2 = p2 + Normal[i] * (noise(p2 * 0.01f) * dh * j - 20);
				p3 = p3 + Normal[i] * (noise(p3 * 0.01f) * dh * (j + 1) - 20);

				var N = TGCVector3.Cross(p2 - p0, p3 - p0) * (-1);
				N.Normalize();
				data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
				data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, 0, 1);
				data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);

				N = TGCVector3.Cross(p3 - p0, p1 - p0) * (-1);
				N.Normalize();
				data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, 0, 0);
				data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, 1, 1);
				data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, 1, 0);
			}
        }


		public override void SetTextures(Effect effect)
		{
            effect.SetValue("texDiffuseMap", textura);
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(new TGCVector3(1, 0.5f, 0)));
            if (textura != null)
            {
                effect.SetValue("texDiffuseMap", textura);
                effect.Technique = "DefaultTechnique";
            }
            else
                effect.Technique = "Terrain";
            device.RenderState.AlphaBlendEnable = true;
		}
		
        // helpers
        public float lerp(float x, float y, float s)
        {
            if (s < 0)
                s = 0;
            else
            if (s > 1)
                s = 1;
            return x * (1 - s) + y * s;
        }

        public float noise(TGCVector3 x)
        {
            TGCVector3 p = new TGCVector3((float)Math.Floor(x.X), (float)Math.Floor(x.Y), (float)Math.Floor(x.Z));
            TGCVector3 f = p - x;
            f.X = f.X * f.X * (3.0f - 2.0f * f.X);
            f.Y = f.Y * f.Y * (3.0f - 2.0f * f.Y);
            f.Z = f.Z * f.Z * (3.0f - 2.0f * f.Z);

            float n = p.X + p.Y * 57.0f + 113.0f * p.Z;
            float rta = lerp(lerp(lerp(hash(n + 0.0f), hash(n + 1.0f), f.X),
                lerp(hash(n + 57.0f), hash(n + 58.0f), f.X), f.Y),
                lerp(lerp(hash(n + 113.0f), hash(n + 114.0f), f.X),
                    lerp(hash(n + 170.0f), hash(n + 171.0f), f.X), f.Y), f.Z);
            return rta;
        }

        float hash(float n)
        {
            float s = (float)Math.Sin(n) * 43758.5453f;
            float rta = s - (float)Math.Floor(s);
            return rta;
        }

    }

    public class CTunelSideTrack : CPatchSideTrack
    {
        public bool perturbar = true;

        public void Init(int cant, int pinicio, int sep, CScene pT, string p_tx_fname, bool p_perturbar)
        {
            perturbar = p_perturbar;
            tx_fname = p_tx_fname;
            Init(cant, pinicio, sep, pT);
        }

        public static CTunelSideTrack Create(int cant, int pinicio, int sep, CScene pT, string p_tx_fname, bool p_perturbar)
        {
            CTunelSideTrack obj = new CTunelSideTrack();
            obj.Init(cant, pinicio, sep, pT,p_tx_fname,p_perturbar);
            return obj;
        }

        public override void FillVertexBuffer(int t, CustomVertex.PositionNormalTextured[] data)
        {
            float r = 1.5F * dr;
            float dp = 35;
            float dh = 30;
            var i = inicio + t;
            for (var j = 0; j < 2 * quadsxpt; ++j)
            {
                float s0 = (float)j / (2.0f * (float)quadsxpt);
                float an = lerp(-0.5f, (float)Math.PI + .5f, s0);
                float X = (float)Math.Cos(an) * r;
                float Y = (float)Math.Sin(an) * r;

                float s1 = (float)(j + 1) / (2.0f * (float)quadsxpt);
                float an1 = lerp(-0.5f, (float)Math.PI + .5f, s1);
                float X1 = (float)Math.Cos(an1) * r;
                float Y1 = (float)Math.Sin(an1) * r;

                var n0 = Binormal[i] * X + Normal[i] * Y;
                var n1 = Binormal[i] * X1 + Normal[i] * Y1;
                var n2 = Binormal[i + 1] * X + Normal[i + 1] * Y;
                var n3 = Binormal[i + 1] * X1 + Normal[i + 1] * Y1;

                var p0 = pt_ruta[i] + n0;
                var p1 = pt_ruta[i] + n1;
                var p2 = pt_ruta[i + 1] + n2;
                var p3 = pt_ruta[i + 1] + n3;

                // perturbacion
                bool perturbacion = true;
                if (perturbacion)
                {
                    n0.Normalize();
                    n1.Normalize();
                    n2.Normalize();
                    n3.Normalize();
                    p0 = p0 - n0 * (noise(p0 * 0.01f) * dh);
                    p1 = p1 - n1 * (noise(p1 * 0.01f) * dh);
                    p2 = p2 - n2 * (noise(p2 * 0.01f) * dh);
                    p3 = p3 - n3 * (noise(p3 * 0.01f) * dh);
                }

                float t0 = (float)i / (2.0f * (float)quadsxpt);
                float t1 = (float)(i + 1) / (2.0f * (float)quadsxpt);


                var N = TGCVector3.Cross(p2 - p0, p3 - p0) * (-1);
                N.Normalize();
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, s0, t0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, N, s0, t1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, s1, t1);

                N = TGCVector3.Cross(p3 - p0, p1 - p0) * (-1);
                N.Normalize();
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, N, s0, t0);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, N, s1, t1);
                data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, N, s1, t0);
            }

        }




    }

    public class CScene
    {
        const int MAX_POINTS = 5000;
        const int MAX_TRACKS = 100;
        const int max_pt = 250;
        public float ancho_ruta = 120;
        public int fov = 250;   // cuantos puntos de la ruta puedo ver hacia adelante

        public int cant_ptos_ruta;
        public float dh = 3; // alto de la pared
        public float M_PI = 3.14151f;
        public TGCVector3[] pt_ruta = new TGCVector3[MAX_POINTS];
        public TGCVector3[] Normal = new TGCVector3[MAX_POINTS];
        public TGCVector3[] Tangent = new TGCVector3[MAX_POINTS];
        public TGCVector3[] Binormal = new TGCVector3[MAX_POINTS];
        public float[] Distance = new float[MAX_POINTS];
        public float scaleXZ = 20;
        public float scaleY = 15;

        public CBaseTrack[] tracks = new CBaseTrack[MAX_TRACKS];
        int cant_tracks;
        public string path_media;

        public int[] sound_tracks;
        public int cant_sound_tracks = 0;
        public int pt_x_track = 5;        // cada cuantos pt_ruta hay un sound_track

        public CSkyBox skybox;
        public CShip player;
        public CSoundBlock bloques;

        public Sprite sprite;
        public Texture exploTexture;
        public int cur_block_index;
        public float timer_colision = 0;
        public TGCVector3 curr_block_p0;
        public TGCVector3 curr_block_p1;
        public TGCVector3 curr_block_p2;
        public TGCVector3 curr_block_p3;
        public Line line;
        public float time = 0;


        public CScene(string mediaDir, IntPtr wav_data, int cant_samples)
        {
            path_media = mediaDir;
            CrearRuta(mediaDir,wav_data, cant_samples);
            //skybox = new CSkyBox("skybox1.jpg", this);        // alta calidad
            skybox = new CSkyBox("skybox.jpg", this);           // baja calidad
            sprite = new Sprite(D3DDevice.Instance.Device);
            line = new Line(D3DDevice.Instance.Device);
            exploTexture = TextureLoader.FromFile(D3DDevice.Instance.Device, path_media + "Texturas\\explosion.png");
            bloques = new CSoundBlock(pt_x_track , this);

        }


        public int rt_Spline(TGCVector3[] pt ,  TGCVector3  []P, int cant_p, float alfa)
        {
            int cant = 0;
            float fi = alfa != 0 ? (1 / alfa) : 1;

            for (int i = 1; i < cant_p - 3; ++i)
            {
                // Segmento Pi - Pi+1
                // Calculo los 4 puntos de control de la curva de Bezier
                TGCVector3 B0 = P[i];
                TGCVector3 B3 = P[i + 1];
                // Calculo las derivadas en los extremos
                TGCVector3 B0p = (P[i + 1] - P[i - 1]) * fi;
                TGCVector3 B1p = (P[i + 2] - P[i]) * fi;

                // los otros 2 ptos de control usando las derivadas
                TGCVector3 B1 = B0p * (1.0f / 3.0f) + B0;
                TGCVector3 B2 = B3 - B1p * (1.0f / 3.0f);

                // Ahora dibujo la curva F(t), con 0<=t<=1
                // Interpolo con rectas
                float dt = 0.05f;
                for (float t = 0; t < 1-dt; t += dt)
                {
                    // calculo los coeficientes del polinomio de Bezier
                    float k0 = (1f - t) * (1f - t) * (1f - t);
                    float k1 = 3f * (1f - t) * (1f - t) * t;
                    float k2 = 3f * (1f - t) * t * t;
                    float k3 = t * t * t;
                    // calculo el valor del polinimio pp dicho 
                    TGCVector3 Bt = B0 * k0 + B1 * k1 + B2 * k2 + B3 * k3;
                    pt[cant++] = Bt;
                }

                //pt[cant++] = P[i];
                // paso al siguiente segmento
            }
            pt[cant++] = P[cant_p - 1];
            return cant;
        }


        // Carga los ptos de la ruta
        public void load_pt_ruta()
        {
            // Genero el path de la ruta
            cant_ptos_ruta = 0;

            TGCVector3[] p = new TGCVector3[max_pt];
            TGCVector3[] n = new TGCVector3[max_pt];
            var rnd = new Random();
            TGCVector3 dir = new TGCVector3(5, 1, 0);
            dir.Normalize();
            TGCVector3 Q = new TGCVector3(0, 0, 0);
            TGCVector3 N = new TGCVector3(0, 1, 0);
            for (int i = 0; i < max_pt; ++i)
            {


                p[i] = Q;
                n[i] = Q + N;

                TGCVector3 B = TGCVector3.Cross(dir, N);
                B.Normalize();

                Q = Q + dir * 500;

                if (i % 5 == 0)
                {
                    //dir.TransformNormal(TGCMatrix.RotationAxis(N, (float)Math.PI * rnd.Next(-20, 20) / 180.0f));
                    //dir.TransformNormal(TGCMatrix.RotationAxis(B, (float)Math.PI * rnd.Next(-15, 15) / 180.0f));
                    dir.TransformNormal(TGCMatrix.RotationAxis(N, (float)Math.PI * rnd.Next(-30, 30) / 180.0f));
                    dir.TransformNormal(TGCMatrix.RotationAxis(B, (float)Math.PI * rnd.Next(-30, 30) / 180.0f));
                }
                /*                else
                                if (i % 3 == 0)
                                {
                                    dir.TransformNormal(TGCMatrix.RotationAxis(N, (float)Math.PI * rnd.Next(-35, 35) / 180.0f));
                                }*/

                // computo la siguiente normal
                TGCVector3 BN = TGCVector3.Cross(dir, N);
                BN.Normalize();
                N = TGCVector3.Cross(BN, dir);
                N.Normalize();
            }

            // puntos de la ruta (sobre el piso)
            cant_ptos_ruta = rt_Spline(pt_ruta, p, max_pt, 2.0f);
            // puntos de la ruta elevados 
            rt_Spline(Normal, n, max_pt, 2.0f);
            // computo las normales , tangente y binormal
            for(int i=0;i < cant_ptos_ruta;++i)
            {
                Tangent[i] = pt_ruta[i + 1] - pt_ruta[i];
                Tangent[i].Normalize();

                TGCVector3 Up = Normal[i] - pt_ruta[i];
                Up.Normalize();

                Binormal[i] = TGCVector3.Cross(Tangent[i] , Up);
                Binormal[i].Normalize();

                Normal[i] = TGCVector3.Cross(Binormal[i] , Tangent[i]);
                Normal[i].Normalize();
            }

            // computo la distancia total recorrida
            Distance[0] = 0;
            for (int i = 1; i < cant_ptos_ruta; ++i)
            {
                Distance[i] = Distance[i-1] + (pt_ruta[i]-pt_ruta[i-1]).Length();
            }

            --cant_ptos_ruta; // me aseguro que siempre exista el i+1
        }

        public unsafe void CrearRuta(string mediaDir , IntPtr wav_data , int cant_samples)
        {
            // Cargo la ruta
            load_pt_ruta();

            // creo los sound tracks 

            int SAMPLE_RATE = 44100;
            sound_tracks = new int[cant_samples / pt_x_track + 1];

            short* wav = (short*)wav_data;
            CFourier fft = new CFourier();
            cant_sound_tracks = 0;

            for (int i = 0; i < cant_ptos_ruta- pt_x_track; i += pt_x_track)
            {

                float time = Distance[i] / CShip.vel_lineal;
                int index = (int)(time * SAMPLE_RATE) % cant_samples;
                int samples_left = cant_samples - index - 1;
                fft.ComplexFFT(wav + index, samples_left, 16384, 1);
                int freq = fft.que_frecuencia();
                sound_tracks[cant_sound_tracks] = fft.Ef > 0.1 ? freq : 0;
                ++cant_sound_tracks;

            }

            // si en 2 o mas tracks seguidos esta aproximadamente el mismo pulso, los agrupo en uno solo
            // con mayor duracion. Es una aproximacion muy basica a obtener el "ritmo". (peor es nada.)
            int Q = sound_tracks[0];
            for (int i = 1; i < cant_tracks; ++i)
            {
                if (Math.Abs(sound_tracks[i] - Q) <= 5)
                    // agrupo este track con el pivote 
                    sound_tracks[i] = 0;
                else
                    // reseteo el pivote
                    Q = sound_tracks[i];

            }


            // Creo los tracks pp dichos
            // palos largos al costado tipo mojones
            tracks[cant_tracks++] = CLineas.Create(2, cant_ptos_ruta / 10, 0, 10, this, new TGCVector3(1, 0.7f, 0.7f), new TGCVector3(40, 2000, 40));

            tracks[cant_tracks++] = CGlowRing.Create(cant_ptos_ruta / 20, 0, 20, this);
            // bloques que representan el WAV
            tracks[cant_tracks++] = CBuildings.Create(wav_data, cant_samples, cant_ptos_ruta, 0, 1, this, new TGCVector3(0, 0.5f, 1));

            tracks[cant_tracks++] = CPiso.Create(cant_ptos_ruta, 0, 1, this);

            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, true, 
                    new TGCVector3(0, 0.5f, 1), new TGCVector3(0.1f, 4.0f, 5.0f));
            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, false, 
                    new TGCVector3(0, 0.5f, 1), new TGCVector3(0.1f, 4.0f, 5.0f));

            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, true,
                    new TGCVector3(1, 0.5f, 0), new TGCVector3(0.2f, 2.0f, 3.0f));
            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, false,
                    new TGCVector3(1, 0.5f, 0), new TGCVector3(0.2f, 2.0f, 3.0f));

            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, true,
                    new TGCVector3(1, 0 , 0.5f), new TGCVector3(0.4f, 7.5f, 3.0f));
            tracks[cant_tracks++] = CGuardRail.Create(cant_ptos_ruta, 0, 1, this, false,
                    new TGCVector3(1, 0, 0.5f ), new TGCVector3(0.4f, 7.5f, 3.0f));



        }

        public void Update(float ElapsedTime)
        {
            time += ElapsedTime;
            var device = D3DDevice.Instance.Device;
            if (timer_colision == 0)
            {
                if (player.colisiona)
                {
                    // arranca la colision: computo la pos X 
                    int p = player.pos_en_ruta;
                    int index = p / pt_x_track;
                    int freq = sound_tracks[index];
                    TGCVector3 Eje_x = -Binormal[p];
                    float dr2 = ancho_ruta / 2 - 27;
                    float s = (freq - 100.0f) / 340.0f;  // 0..1
                    float X = (2 * dr2) * s - dr2;
                    float dx = 20;
                    curr_block_p0 = pt_ruta[p] + Eje_x * (X - dx / 2);
                    curr_block_p1 = curr_block_p0 + Eje_x * (dx);
                    curr_block_p2 = curr_block_p1 + Normal[p] * 10;
                    curr_block_p3 = curr_block_p0 + Normal[p] * 10;
                    timer_colision += ElapsedTime;
                    cur_block_index = index;
                }
            }
            else
            {
                // explosion en curso
                timer_colision += ElapsedTime;
                if (timer_colision > 0.25f)
                    timer_colision = 0;
            }
        }


        public void render(Effect effect)
        {
            var device = D3DDevice.Instance.Device;

            effect.Technique = "DefaultTechnique";
            TgcShaders.Instance.setShaderMatrixIdentity(effect);
            device.RenderState.AlphaBlendEnable = false;

            if(!player.colisiona)
                skybox.Render(effect);
            

            // objetos transparentes 
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.ZBufferEnable= false;
            for (int i = 0; i < cant_tracks; ++i)
                if (tracks[i].transparente)
                    tracks[i].Render(effect);

            // objetos opacos
            device.RenderState.AlphaBlendEnable = false;
            device.RenderState.ZBufferEnable = true;
            for (int i = 0; i < cant_tracks; ++i)
                if (!tracks[i].transparente)
                    tracks[i].Render(effect);

            // bloques
            bloques.Render(effect);

            // sprites
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.ZBufferEnable = false;
            if (timer_colision > 0)
            {
                float W = D3DDevice.Instance.Width ;
                float H = D3DDevice.Instance.Height;

                TGCVector3 p0 = curr_block_p0 * (1);
                TGCVector3 p1 = curr_block_p1 * (1);
                TGCVector3 p2 = curr_block_p2 * (1);
                TGCVector3 p3 = curr_block_p3 * (1);
                TGCVector3 cg = (p0 + p1 + p2 + p3)*0.25f;
                p0.Project(device.Viewport, TGCMatrix.FromMatrix(device.Transform.Projection),
                    TGCMatrix.FromMatrix(device.Transform.View), TGCMatrix.FromMatrix(device.Transform.World));
                p1.Project(device.Viewport, TGCMatrix.FromMatrix(device.Transform.Projection),
                    TGCMatrix.FromMatrix(device.Transform.View), TGCMatrix.FromMatrix(device.Transform.World));
                p2.Project(device.Viewport, TGCMatrix.FromMatrix(device.Transform.Projection),
                    TGCMatrix.FromMatrix(device.Transform.View), TGCMatrix.FromMatrix(device.Transform.World));
                p3.Project(device.Viewport, TGCMatrix.FromMatrix(device.Transform.Projection),
                    TGCMatrix.FromMatrix(device.Transform.View), TGCMatrix.FromMatrix(device.Transform.World));
                cg.Project(device.Viewport, TGCMatrix.FromMatrix(device.Transform.Projection),
                    TGCMatrix.FromMatrix(device.Transform.View), TGCMatrix.FromMatrix(device.Transform.World));


                if (cg.Z > 0)
                {
                    sprite.Begin(SpriteFlags.AlphaBlend);
                    float escala = timer_colision * 6.0f;
                    float px = 250 * escala;
                    float py = 250 * escala;

                    float s = 1 - timer_colision / 2.0f;
                    sprite.Transform = TGCMatrix.Transformation2D(
                        new TGCVector2(0, 0), 0, new TGCVector2(1, 1) * escala, new TGCVector2(px, py), timer_colision * 3.0f, new TGCVector2(cg.X - px, cg.Y - py));
                    sprite.Draw(exploTexture, Rectangle.Empty, TGCVector3.Empty, new TGCVector3(0, 0, 0), Color.FromArgb((int)(255 * s), 255, 255, 255));

                    TGCVector2[] pt = new TGCVector2[5];
                    pt[0].X = p0.X;
                    pt[0].Y = p0.Y;
                    pt[1].X = p1.X;
                    pt[1].Y = p1.Y;
                    pt[2].X = p2.X;
                    pt[2].Y = p2.Y;
                    pt[3].X = p3.X;
                    pt[3].Y = p3.Y;
                    pt[4] = pt[0];
                    line.Draw(TGCVector2.ToVector2Array(pt), Color.FromArgb(255, 255, 0, 255));
                    sprite.End();
                }

            }
        }


        public int que_pos(float time,float vel_lineal , out float t)
        {
            // primero determino cuanto va a recorrer linealmente
            float ds = vel_lineal * time;
            // busco en segmento estoy
            int i=0;
            while (i < cant_ptos_ruta && Distance[i] < ds)
                ++i;

            // retrocedo uno
            i--;
            // ahora estoy entre el punto i y el i+1, es decir se cumple que 
            // Di <= ds <= Di+1
            t = (ds - Distance[i]) / (Distance[i+1] - Distance[i]);

            return i;

        }

        public void dispose()
        {
            for (var i = 0; i < cant_tracks; ++i)
                tracks[i].Dispose();

            sprite.Dispose();
            line.Dispose();
            exploTexture.Dispose();

        }

    }
}