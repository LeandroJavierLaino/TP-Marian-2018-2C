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
using Microsoft.DirectX.DirectInput;

namespace TGC.Group.Model
{

    public class CShip
    {
        public TgcMesh mesh;
        public TGCVector3 car_Scale = new TGCVector3(1,1,1) * 0.25f;
        public CScene S;
        public TGCBox box ,bb;
        public int pos_en_ruta = 0;
        public float pos_t;     // 0 si esta justo en pos_en_ruta, hasta 1 si esta en pos_en_ruta + 1
        public float yaw = 0.0f;
        public float pitch = 0.0f;
        public float roll = 0.0f;
        public TGCVector3 pos, dir, normal,binormal;
        public TGCVector3 posC;     // posicion proyectada al centro del track
        public float X = 0.0f;
        public float desfH = 10;            // altura de la nave con respecto al piso
        public float rotationSpeed = 0.0f;
        public float acel_angular = 0.0f;
        public float time = 0.0f;
        public float dr = 0;            // ancho util del track/2
        public bool colisiona = false;
        public int score = 0;
        public int score_total = 0;

        public static float vel_lineal = 400.0f;



        public CShip(string MediaDir, CScene pscene , String fname)
        {
            S = pscene;
            var loader = new TgcSceneLoader();
            mesh = loader.loadSceneFromFile(MediaDir + fname).Meshes[0];
            mesh.AutoTransform = false;
            mesh.Technique = "DefaultTechnique";

            box = TGCBox.fromExtremes(new TGCVector3(0, 0, 0), new TGCVector3(100, 40, 40),
                TgcTexture.createTexture(MediaDir + "texturas\\plasma.png"));
            box.AutoTransform = false;
            box.Technique = "Fire";

            bb = TGCBox.fromExtremes(new TGCVector3(0, 0, 0), new TGCVector3(1, 1,1),
                TgcTexture.createTexture(MediaDir + "texturas\\plasma.png"));
            bb.AutoTransform = false;
            bb.Technique = "DefaultTechnique";

            dr = S.ancho_ruta / 2 - 27;


        }


        // simple physics
        // computa todas las fuerzas que invervienen
        // necesito el Input, ya que las teclas generan fuerzas 
        public void computeForces(TgcD3dInput Input, float ElapsedTime)
        {
            // limpio cualquier otra fuerza 
            acel_angular = 0.0f;

            float dx = 3.0f;
            if ((Input.keyDown(Key.Left)) && X < dr)
            {
                // MODO ARCADE: muevo directo de carril
                if (Input.keyDown(Key.LeftShift))
                    X = dr - 20;
                else
                    X += dx;
                acel_angular = 2.5f;
            }
            else
            if ((Input.keyDown(Key.Right)) && X > -dr)
            {
                if (Input.keyDown(Key.LeftShift))
                    X = -dr + 20;
                else
                    X -= dx;
                acel_angular = -2.5f;
            }

            // Modo piano
            if ((Input.keyDown(Key.NumPad6)))
            {
                X = -dr + 30;
                acel_angular = -2.5f;
            }
            else
            if ((Input.keyDown(Key.NumPad5)))
            {
                X = 0;
                acel_angular = 0f;
            }
            else
            if ((Input.keyDown(Key.NumPad4)))
            {
                X = dr - 30;
                acel_angular = 2.5f;
            }



            if (X < -dr)
                X = -dr;
            else
            if (X > dr)
                X = dr;
        }

        // integra (simple euler) las fuerzas
        public void integrateForces(float ElapsedTime)
        {
            // velocidad angular
            rotationSpeed += acel_angular * ElapsedTime;
            float rozamiento = rotationSpeed * 2.3f;
            rotationSpeed -= rozamiento * ElapsedTime;
            // aprovecho para girar la nave
            roll = rotationSpeed * 0.7f;

        }

        public int que_frecuencia()
        {
            float s = 0.5f - X / (2 * dr);      // s = 0..1
            return (int)(s * 340 + 100);
        }

        public void Update(TgcD3dInput Input, CScene S, float ElapsedTime)
        {
            // tiempo total 
            time += ElapsedTime;

            // computo e integro las fuerza
            computeForces(Input, ElapsedTime);
            integrateForces(ElapsedTime);


            pos_en_ruta = S.que_pos(time, vel_lineal, out pos_t);

            // interpolo segun pos_t 
            posC = TGCVector3.Lerp(S.pt_ruta[pos_en_ruta], S.pt_ruta[pos_en_ruta + 1], pos_t);
            dir = TGCVector3.Lerp( S.Tangent[pos_en_ruta], S.Tangent[pos_en_ruta+1] , pos_t);
            normal = TGCVector3.Lerp(S.Normal[pos_en_ruta], S.Normal[pos_en_ruta + 1], pos_t);
            binormal = TGCVector3.Lerp(S.Binormal[pos_en_ruta], S.Binormal[pos_en_ruta + 1], pos_t);
            // elevo un poco la nave 
            posC += normal * desfH;
            pos = posC + binormal * X;

            // actualizo la posicion de la nave
            mesh.Transform = CalcularMatriz(pos, car_Scale, -dir, normal);

            // colisiones con los sound tracks de la escena
            colisiona = false;
            if (pos_en_ruta%S.pt_x_track==0)
            {
                int index = pos_en_ruta / S.pt_x_track;
                int freq_wav = S.sound_tracks[index];
                int freq_ship = que_frecuencia();
                if (Math.Abs(freq_ship - freq_wav) < 60)
                {
                    // colision
                    colisiona = true;
                    score++;
                }
                score_total++;
            }

        }

        public void Render(Microsoft.DirectX.Direct3D.Effect effect)
        {
            var device = D3DDevice.Instance.Device;

            mesh.Effect = effect;
            device.RenderState.AlphaBlendEnable = false;
            mesh.Render();


            box.Effect = effect;

            // los mesh importados de skp tienen 100 de tamaño normalizado, primero trabajo en el espacio normalizado
            // del mesh y luego paso al worldspace con la misma matriz de la nave

            float e = 2.0f;
            float dl = 50.0f + 50.0f* e;
            TGCVector3[] T = new TGCVector3[11];
            T[0] = new TGCVector3(dl, -5.0f, 0.0f);
            T[1] = new TGCVector3(dl, -5.0f, -1.0f);
            T[2] = new TGCVector3(dl, -5.0f, 1.0f);
            T[3] = new TGCVector3(dl, -6.0f, -1.0f);
            T[4] = new TGCVector3(dl, -4.0f, 1.0f);

            T[5] = new TGCVector3(dl, -5.0f, 16.0f);
            T[6] = new TGCVector3(dl, -5.0f, 16.0f - 0.3f);
            T[7] = new TGCVector3(dl, -5.0f, 16.0f + 0.3f);

            T[8] = new TGCVector3(dl, -5.0f, -16.0f);
            T[9] = new TGCVector3(dl, -5.0f, -16.0f - 0.3f);
            T[10] = new TGCVector3(dl, -5.0f, -16.0f + 0.3f);


            for (int i = 0; i < 11; ++i)
            {
                box.Transform = TGCMatrix.Scaling(new TGCVector3(e, 0.1f, 0.1f)) *
                            TGCMatrix.Translation(T[i]) * mesh.Transform;
                device.RenderState.AlphaBlendEnable = true;
                box.Render();
            }

        }

        // helper
        public TGCMatrix CalcularMatriz(TGCVector3 Pos, TGCVector3 Scale, TGCVector3 Dir, TGCVector3 VUP)
        {
            var matWorld = TGCMatrix.Scaling(Scale);

            // xxx
            matWorld = matWorld * TGCMatrix.RotationY(-(float)Math.PI / 2.0f) 
                    * TGCMatrix.RotationYawPitchRoll(yaw, pitch, roll);


            // determino la orientacion
            var U = TGCVector3.Cross(VUP, Dir);
            U.Normalize();
            var V = TGCVector3.Cross(Dir, U);
            V.Normalize();
            TGCMatrix Orientacion = new TGCMatrix();
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * TGCMatrix.Translation(Pos);
            return matWorld;
        }


        public TGCMatrix CalcularMatriz2(TGCVector3 Pos, TGCVector3 Scale, TGCVector3 Dir, TGCVector3 VUP)
        {
            var matWorld = TGCMatrix.Scaling(Scale);
            matWorld = matWorld * TGCMatrix.RotationY(-(float)Math.PI / 2.0f)
                    * TGCMatrix.RotationYawPitchRoll(yaw, pitch, 0);

            // determino la orientacion
            var U = TGCVector3.Cross(VUP, Dir);
            U.Normalize();
            var V = TGCVector3.Cross(Dir, U);
            V.Normalize();
            TGCMatrix Orientacion = new TGCMatrix();
            Orientacion.M11 = U.X;
            Orientacion.M12 = U.Y;
            Orientacion.M13 = U.Z;
            Orientacion.M14 = 0;

            Orientacion.M21 = V.X;
            Orientacion.M22 = V.Y;
            Orientacion.M23 = V.Z;
            Orientacion.M24 = 0;

            Orientacion.M31 = Dir.X;
            Orientacion.M32 = Dir.Y;
            Orientacion.M33 = Dir.Z;
            Orientacion.M34 = 0;

            Orientacion.M41 = 0;
            Orientacion.M42 = 0;
            Orientacion.M43 = 0;
            Orientacion.M44 = 1;
            matWorld = matWorld * Orientacion;

            // traslado
            matWorld = matWorld * TGCMatrix.Translation(Pos);
            return matWorld;
        }

        public TGCVector3 rotar_xz(TGCVector3 v, float an)
        {
            return new TGCVector3((float)(v.X * Math.Cos(an) - v.Z * Math.Sin(an)), v.Y,
                (float)(v.X * Math.Sin(an) + v.Z * Math.Cos(an)));
        }


        public void Dispose()
        {
            mesh.Dispose();
        }


    }


}