using Microsoft.DirectX.Direct3D;
using Microsoft.DirectX.DirectInput;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using TGC.Core.Direct3D;
using TGC.Core.Example;
using TGC.Core.Geometry;
using TGC.Core.Input;
using TGC.Core.Mathematica;
using TGC.Core.SceneLoader;
using TGC.Core.Textures;
using Effect = Microsoft.DirectX.Direct3D.Effect;

namespace TGC.Group.Model
{

    // Vertex format posicion y color
    public struct VERTEX_POS_COLOR
    {
        public float x, y, z;		// Posicion
        public int color;		// Color
    };

    // Vertex format para dibujar en 2d 
    public struct VERTEX2D
    {
        public float x, y, z, rhw;		// Posicion
        public int color;		// Color
    };

    public class GameModel : TgcExample
    {

        public CScene ESCENA;
        public CShip SHIP;
        public float dist_cam = 50;
        private Effect effect;
        public float ftime; // frame time
        private Surface g_pDepthStencil; // Depth-stencil buffer
        private Surface g_pDepthStencilOld; // Depth-stencil buffer
        private Texture g_pRenderTarget, g_pRenderTarget2, g_pRenderTarget3, g_pRenderTarget4, g_pRenderTarget5;

        private VertexBuffer g_pVBV3D;
        public bool mouseCaptured;
        public Point mouseCenter;
        private string MyShaderDir;

        public bool paused;
        public int tipo_camara = 0;
        // camara fija
        public TGCVector3 camara_LA = new TGCVector3(1000, 0, 0);
        public TGCVector3 camara_LF = new TGCVector3(1000, 1000, 1000);
        // chasing camera
        public float chase_1 = 140;
        public float chase_2 = 35;
        public float chase_3 = 500;

        public float time;
        public bool camara_ready = false;

        public TGCVector3 cam_la_vel = new TGCVector3(0, 0, 0);
        public TGCVector3 cam_pos_vel = new TGCVector3(0, 0, 0);



        // interface 2d
        public Sprite sprite;
        public Microsoft.DirectX.Direct3D.Font font;
        public Microsoft.DirectX.Direct3D.Font s_font;
        public Microsoft.DirectX.Direct3D.Font c64_font;

        // mi render loop
        public float my_elapsed_time = 0;
        public float my_time = 0;
        public bool hay_render = false;
        public int cant_frames = 0;
        public float fps = 0;
        public float desf_frmrate = 0;

        public bool sound_ready = true;

        // sonido
        sndmng sound = new sndmng();
        sndmng sound_out = new sndmng();

        // maquina de estados
        int m_state = 0;
        int curr_file = 0;
        string[] wav_files;
        float timer_intro = 0;

        public bool motion_blur = true;


        /// <summary>
        ///     Constructor del juego.
        /// </summary>
        /// <param name="mediaDir">Ruta donde esta la carpeta con los assets</param>
        /// <param name="shadersDir">Ruta donde esta la carpeta con los shaders</param>
        public GameModel(string mediaDir, string shadersDir) : base(mediaDir, shadersDir)
        {
            Category = Game.Default.Category;
            Name = Game.Default.Name;
            Description = Game.Default.Description;

        }

        public override void Init()
        {
            var d3dDevice = D3DDevice.Instance.Device;

            MyShaderDir = ShadersDir;

            wav_files = Directory.GetFiles(MediaDir + "sound");

            //sound.Create(MediaDir + "sound\\smokeonthewater.wav");
            //sound.Create(MediaDir + "sound\\rolemu_-_neogauge.wav");        // super dificl
            //sound.Create(MediaDir + "sound\\rolem_-_Neoishiki.wav");            // buenisimo (dificil)
            //sound.Create(MediaDir + "sound\\Azureflux_-_06_-_Kinetic_Sands.wav");
            //sound.Create(MediaDir + "sound\\Azureflux_-_01_-_BOMB.wav");
            //sound.Create(MediaDir + "sound\\Azureflux_-_02_-_Waves.wav");       // muy bueno, facil
            //sound.Create(MediaDir + "sound\\Monplaisir_-_03_-_Level_0.wav");       
            //sound.Create(MediaDir + "sound\\Monplaisir_-_04_-_Level_1.wav");           // monotono  
            //sound.Create(MediaDir + "sound\\Monplaisir_-_05_-_Level_2.wav");           // monotono  
            //sound.Create(MediaDir + "sound\\Monplaisir_-_06_-_Level_3.wav");  
            //sound.Create(MediaDir + "sound\\Monplaisir_-_07_-_Level_4.wav");            // bueno
            //sound.Create(MediaDir + "sound\\tecno1.wav");                         // dificil
            //sound.Create(MediaDir + "sound\\ScoobyDooPaPa.wav");                         // interesante
            //sound.Create(MediaDir + "sound\\highway intro.wav");                // se va de escala                 
            //sound.Create(MediaDir + "sound\\Shook_Me_All_Night.wav");           // bajo volumen
            //sound.Create(MediaDir + "sound\\ACDC.wav");           


            //Cargar Shader personalizado
            string compilationErrors;
            effect = Effect.FromFile(D3DDevice.Instance.Device, MyShaderDir + "shaders.fx",
                null, null, ShaderFlags.PreferFlowControl, null, out compilationErrors);
            if (effect == null)
            {
                throw new Exception("Error al cargar shader. Errores: " + compilationErrors);
            }
            //Configurar Technique dentro del shader
            effect.Technique = "DefaultTechnique";


            // para capturar el mouse
            var focusWindows = D3DDevice.Instance.Device.CreationParameters.FocusWindow;
            mouseCenter = focusWindows.PointToScreen(new Point(focusWindows.Width / 2, focusWindows.Height / 2));
            mouseCaptured = false;
            //Cursor.Hide();

            // stencil
            g_pDepthStencil = d3dDevice.CreateDepthStencilSurface(d3dDevice.PresentationParameters.BackBufferWidth,
                d3dDevice.PresentationParameters.BackBufferHeight,
                DepthFormat.D24S8, MultiSampleType.None, 0, true);
            g_pDepthStencilOld = d3dDevice.DepthStencilSurface;
            // inicializo el render target
            g_pRenderTarget = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget2 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget3 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget4 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);
            g_pRenderTarget5 = new Texture(d3dDevice, d3dDevice.PresentationParameters.BackBufferWidth
                , d3dDevice.PresentationParameters.BackBufferHeight, 1, Usage.RenderTarget,
                Format.X8R8G8B8, Pool.Default);

            // Resolucion de pantalla
            effect.SetValue("screen_dx", d3dDevice.PresentationParameters.BackBufferWidth);
            effect.SetValue("screen_dy", d3dDevice.PresentationParameters.BackBufferHeight);

            CustomVertex.PositionTextured[] vertices =
            {
                new CustomVertex.PositionTextured(-1, 1, 1, 0, 0),
                new CustomVertex.PositionTextured(1, 1, 1, 1, 0),
                new CustomVertex.PositionTextured(-1, -1, 1, 0, 1),
                new CustomVertex.PositionTextured(1, -1, 1, 1, 1)
            };
            //vertex buffer de los triangulos
            g_pVBV3D = new VertexBuffer(typeof(CustomVertex.PositionTextured),
                4, d3dDevice, Usage.Dynamic | Usage.WriteOnly,
                CustomVertex.PositionTextured.Format, Pool.Default);
            g_pVBV3D.SetData(vertices, 0, LockFlags.None);

            time = 0;

            sprite = new Sprite(d3dDevice);
            // ajusto las letras para que se vean mas o menos igual en todas las resoluciones. 
            float kx = (float)d3dDevice.PresentationParameters.BackBufferWidth / 1366.0f;
            float ky = (float)d3dDevice.PresentationParameters.BackBufferHeight / 768.0f;
            float k = Math.Min(kx, ky);

            // Fonts
            font = new Microsoft.DirectX.Direct3D.Font(d3dDevice, (int)(24 *k), 0, FontWeight.Light, 0, false, CharacterSet.Default,
                    Precision.Default, FontQuality.Default, PitchAndFamily.DefaultPitch, "Lucida Console");
            font.PreloadGlyphs('0', '9');
            font.PreloadGlyphs('a', 'z');
            font.PreloadGlyphs('A', 'Z');

            s_font = new Microsoft.DirectX.Direct3D.Font(d3dDevice, (int)(18 * k), 0, FontWeight.Light, 0, false, CharacterSet.Default,
                    Precision.Default, FontQuality.Default, PitchAndFamily.DefaultPitch, "Lucida Console");
            s_font.PreloadGlyphs('0', '9');
            s_font.PreloadGlyphs('a', 'z');
            s_font.PreloadGlyphs('A', 'Z');

            c64_font = new Microsoft.DirectX.Direct3D.Font(d3dDevice, (int)(40*k),(int)(30*k), FontWeight.Light, 0, false, CharacterSet.Default,
                    Precision.Default, FontQuality.Default, PitchAndFamily.DefaultPitch, "System");
            c64_font.PreloadGlyphs('0', '9');
            c64_font.PreloadGlyphs('a', 'z');
            c64_font.PreloadGlyphs('A', 'Z');

        }


        // inicializa el modo juego ppdicho (una vez que el jugador selecciono el wav
        public void InitGame()
        {
            // esta en un thread aparte , aviso cuando termina
            // cargo el WAV 
            sound.Create(wav_files[curr_file]);
            // genero la escena a partir del WAV
            ESCENA = new CScene(MediaDir, sound.PCMBuffer, sound._cant_samples);
            // el resto de la carga
            ESCENA.player = SHIP = new CShip(MediaDir, ESCENA, "nave\\Swoop+Bike-TgcScene.xml");
            SHIP.pos_en_ruta = 10;
            // termino pongo el status en 2
            m_state = 2;
            timer_intro = 5;        // 5 segundos de intro antes de que arranque 

        }


        public void ProcessKeyboard()
        {

            switch (m_state)
            {
                case 0:
                    // selecciona el wav
                    if (Input.keyPressed(Key.DownArrow))
                        curr_file++;
                    else
                    if (Input.keyPressed(Key.UpArrow))
                        curr_file--;
                    else
                    if (Input.keyPressed(Key.Return))
                    {
                        // PASO AL MODO GAME
                        m_state = 1;
                        Thread newThread = new Thread(this.InitGame);
                        newThread.Start();
                    }
                    break;
                case 1:
                    // loading....
                    break;

                case 2:
                    // modo juego
                    if (Input.keyPressed(Key.P))
                        paused = !paused;
                    if (Input.keyPressed(Key.M))
                        motion_blur = !motion_blur;
                    if (Input.keyPressed(Key.Escape))
                        // vuelvo al modo seleccion
                        m_state = 0;
                    break;

            }

        }


        public override void Update()
        {
            PreUpdate();
            if (ElapsedTime <= 0 || ElapsedTime > 1000)
                return;
            ProcessKeyboard();

            if (paused)
                return;


            // renderloop personalizado
            hay_render = false;
            float frame_rate = 1.0f / 60.0f;

            my_elapsed_time += ElapsedTime;
            if (my_elapsed_time < frame_rate - desf_frmrate)
                return;
            desf_frmrate += 0.1f*(my_elapsed_time - frame_rate);
            ++cant_frames;
            ElapsedTime = my_elapsed_time;
            my_elapsed_time = 0;
            hay_render = true;

            my_time += ElapsedTime;
            if (my_time>1)
            {
                // recomputo los fps
                fps = cant_frames / my_time;
                my_time = 0;
                cant_frames = 0;
            }

            time += ElapsedTime;

            switch(m_state)
            {
                case 0:
                    // selecciona el wav
                    break;
                case 1:
                    // presentacion
                    break;

                case 2:
                    // modo game
                    {
                           // intro? 
                        if(timer_intro>0)
                            timer_intro -= ElapsedTime;

                        // sonido
                        sound.WaveOut();

                        // actualizo la nave
                        SHIP.Update(Input, ESCENA, ElapsedTime);
                        // actualizo la camara
                        Camara.SetCamera(SHIP.posC - SHIP.dir * 120.0f + SHIP.normal * 30.0f, SHIP.pos + SHIP.dir * 70.0f, SHIP.normal);
                        Camara.UpdateCamera(ElapsedTime);

                        // actualizo la escena
                        ESCENA.Update(ElapsedTime);

                        if (SHIP.colisiona)
                        {

                            if (sound_ready)
                            {
                                int index = SHIP.pos_en_ruta / ESCENA.pt_x_track;
                                int freq_wav = ESCENA.sound_tracks[index];
                                sound_out.Play(freq_wav, 500, 0.5f);
                                sound_ready = false;
                            }
                        }
                        else
                        {
                            sound_ready = true;
                        }
                    }
                    break;
            }


            PostUpdate();
        }


        public override void Render()
        {
            switch (m_state)
            {
                case 0:
                    // selecciona el wav
                    RenderGUI();
                    break;

                case 1:
                    RenderLoading();
                    break;

                case 2:
                    // modo game
                    RenderScene();
                    break;
            }

        }


        public void RenderGUI()
        {
            ClearTextures();

            var device = D3DDevice.Instance.Device;
            var screen_dx = device.PresentationParameters.BackBufferWidth;
            var screen_dy = device.PresentationParameters.BackBufferHeight;
            effect.Technique = "DefaultTechnique";
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            device.BeginScene();
            device.RenderState.ZBufferEnable = false;
            device.RenderState.AlphaBlendEnable = true;
            FillRect(50, 50, screen_dx-100, screen_dy-100, Color.FromArgb(128, 0, 0, 0));
            FillTextS(100, 80, MediaDir + "sound", Color.White);
            DrawLine(100, 100, screen_dx-100, 100, 2, Color.LightBlue);
            curr_file = Math.Abs(curr_file % wav_files.Length);
            for (int i=0;i< wav_files.Length; ++i)
                if(i==curr_file)
                    FillTextS(100, i * 20 + 110, "[]       "+Path.GetFileName(wav_files[i]), Color.Turquoise);
                else
                    FillTextS(100, i * 20 + 110, "[] "+ Path.GetFileName(wav_files[i]), Color.White);

            FillTextS(screen_dx-100, screen_dy-20, "Use (Up) (Down) Key to Select the file - Enter to confirm", Color.Yellow , 2);

            device.EndScene();
            device.Present();

        }


        public void RenderLoading()
        {
            ClearTextures();

            var device = D3DDevice.Instance.Device;
            effect.Technique = "DefaultTechnique";
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Yellow , 1.0f, 0);
            var screen_dx = device.PresentationParameters.BackBufferWidth;
            var screen_dy = device.PresentationParameters.BackBufferHeight;
            device.RenderState.ZBufferEnable = false;
            device.RenderState.AlphaBlendEnable = true;
            byte r = (byte)(time * 1000000000);
            byte g = (byte)(time * 1000000);
            byte b = (byte)(time * 1000);

            FillRect(0, 0, screen_dx, screen_dy, Color.FromArgb(255,r,g,b));
            Color clr = Color.FromArgb(255, 165, 165, 255);
            FillRect(50, 50, screen_dx-50, screen_dy - 50, Color.FromArgb(255, 66 ,66 ,231));
            FillTextWithFont(c64_font,screen_dx/2, 100, "**** COMMODORE 64 BASIC V2 ****", clr, 1);
            FillTextWithFont(c64_font, screen_dx / 2, 150, "64 K RAM SYSTEM 38991 BASIC BYTES FREE", clr, 1);
            FillTextWithFont(c64_font, 50, 200, "READY.", clr, 0);
            FillRect(50, 232, 50+33,232+38, Math.Floor(time*3) % 2 == 0 ? Color.FromArgb(255, 66, 66, 231):clr);
            device.BeginScene();
            device.EndScene();
            device.Present();

        }



        // renderiza la scena en el modo GAME
        public void RenderScene()
        {
            ClearTextures();

            var device = D3DDevice.Instance.Device;
            effect.Technique = "DefaultTechnique";
            effect.SetValue("time", time);

            // guardo el Render target anterior y seteo la textura como render target
            var pOldRT = device.GetRenderTarget(0);
            var pSurf = g_pRenderTarget.GetSurfaceLevel(0);
            device.SetRenderTarget(0, pSurf);
            // hago lo mismo con el depthbuffer, necesito el que no tiene multisampling
            var pOldDS = device.DepthStencilSurface;
            device.DepthStencilSurface = g_pDepthStencil;


            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, SHIP.colisiona ? Color.Yellow : Color.Black, 1.0f, 0);
            device.BeginScene();
            effect.SetValue("eyePosition", TGCVector3.Vector3ToFloat4Array(Camara.Position));

            ESCENA.render(effect);
            sound.render(effect, ESCENA, SHIP);
            SHIP.Render(effect);

            // -------------------------------------
            device.EndScene();
            pSurf.Dispose();

            // Ultima pasada vertical va sobre la pantalla pp dicha
            device.SetRenderTarget(0, pOldRT);
            device.DepthStencilSurface = g_pDepthStencilOld;
            device.BeginScene();
            effect.Technique = motion_blur ? "FrameMotionBlur" : "FrameCopy";

            device.VertexFormat = CustomVertex.PositionTextured.Format;
            device.SetStreamSource(0, g_pVBV3D, 0);
            effect.SetValue("g_RenderTarget", g_pRenderTarget);
            effect.SetValue("g_RenderTarget2", g_pRenderTarget2);
            effect.SetValue("g_RenderTarget3", g_pRenderTarget3);
            effect.SetValue("g_RenderTarget4", g_pRenderTarget4);
            effect.SetValue("g_RenderTarget5", g_pRenderTarget5);
            device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.Black, 1.0f, 0);
            effect.Begin(FX.None);
            effect.BeginPass(0);
            device.DrawPrimitives(PrimitiveType.TriangleStrip, 0, 2);
            effect.EndPass();
            effect.End();
            
            // dibujo el scoreboard, tiempo, vidas, etc (y fps)
            RenderHUD();

            device.EndScene();
            device.Present();

            ftime += ElapsedTime;
            if (ftime > 0.02f)
            {
                ftime = 0;
                var aux = g_pRenderTarget5;
                g_pRenderTarget5 = g_pRenderTarget4;
                g_pRenderTarget4 = g_pRenderTarget3;
                g_pRenderTarget3 = g_pRenderTarget2;
                g_pRenderTarget2 = g_pRenderTarget;
                g_pRenderTarget = aux;
            }
        }

        public void RenderHUD()
        {
            var device = D3DDevice.Instance.Device;
            var screen_dx = device.PresentationParameters.BackBufferWidth;
            var screen_dy = device.PresentationParameters.BackBufferHeight;

            bool ant_zenable = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;
            device.RenderState.AlphaBlendEnable= true;

            FillRect(0, 0, 2000, 50, Color.FromArgb(128, 0, 0, 0));

            FillText(30, 10, sound.wav_name, Color.White);
            if (SHIP.score_total!=0)
            {
                int ptje = (int)(100.0f * (float)SHIP.score / (float)SHIP.score_total);
                FillText(screen_dx - 10, 10, "Score: " + SHIP.score + " / " + SHIP.score_total+ 
                    " ("+ptje+"%)", Color.White, 2);
            }

            if(timer_intro>1)
            {
                FillRect(100, screen_dy / 2-100, screen_dx-100, screen_dy/2+100, Color.FromArgb(128, 0, 0, 0));
                FillText(screen_dx / 2, screen_dy / 2 - 50, "Teclas <- ->  mover la nave", Color.White, 1);
                FillText(screen_dx / 2, screen_dy / 2, "ESC -  Volver al menu    M-> Toogle Motion blur", Color.White, 1);
                FillText(screen_dx / 2, screen_dy / 2 + 50, "Arranca en " + Math.Floor(timer_intro-1) + "s", Color.White, 1);
            }

            FillText(50, screen_dy - 30, "FPS:" + Math.Round(fps), Color.Yellow);
            device.RenderState.ZBufferEnable = ant_zenable;

        }

        public void DrawLine(TGCVector3 p0, TGCVector3 p1, TGCVector3 up, float dw, Color color)
        {

            TGCVector3 v = p1 - p0;
            v.Normalize();
            TGCVector3 n = TGCVector3.Cross(v, up);
            TGCVector3 w = TGCVector3.Cross(n, v);

            TGCVector3[] p = new TGCVector3[8];

            dw *= 0.5f;
            p[0] = p0 - n * dw;
            p[1] = p1 - n * dw;
            p[2] = p1 + n * dw;
            p[3] = p0 + n * dw;
            for (int i = 0; i < 4; ++i)
            {
                p[4 + i] = p[i] + w * dw;
                p[i] -= w * dw;
            }

            int[] index_buffer = { 0, 1, 2, 0, 2, 3,
                                       4, 5, 6, 4, 6, 7,
                                       0, 1, 5, 0, 5, 4,
                                       3, 2, 6, 3, 6, 7 };

            VERTEX_POS_COLOR[] pt = new VERTEX_POS_COLOR[index_buffer.Length];
            for (int i = 0; i < index_buffer.Length; ++i)
            {
                int index = index_buffer[i];
                pt[i].x = p[index].X;
                pt[i].y = p[index].Y;
                pt[i].z = p[index].Z;
                pt[i].color = color.ToArgb();
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Position | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, index_buffer.Length / 3, pt);
        }


        // line 2d
        public void DrawLine(float x0, float y0, float x1, float y1, int dw, Color color)
        {
            TGCVector2[] V = new TGCVector2[4];
            V[0].X = x0;
            V[0].Y = y0;
            V[1].X = x1;
            V[1].Y = y1;

            if (dw < 1)
                dw = 1;

            // direccion normnal
            TGCVector2 v = V[1] - V[0];
            v.Normalize();
            TGCVector2 n = new TGCVector2(-v.Y, v.X);

            V[2] = V[1] + n * dw;
            V[3] = V[0] + n * dw;

            VERTEX2D[] pt = new VERTEX2D[16];
            // 1er triangulo
            pt[0].x = V[0].X;
            pt[0].y = V[0].Y;
            pt[1].x = V[1].X;
            pt[1].y = V[1].Y;
            pt[2].x = V[2].X;
            pt[2].y = V[2].Y;

            // segundo triangulo
            pt[3].x = V[0].X;
            pt[3].y = V[0].Y;
            pt[4].x = V[2].X;
            pt[4].y = V[2].Y;
            pt[5].x = V[3].X;
            pt[5].y = V[3].Y;

            for (int t = 0; t < 6; ++t)
            {
                pt[t].z = 0.5f;
                pt[t].rhw = 1;
                pt[t].color = color.ToArgb();
                ++t;
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Transformed | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 2, pt);
        }

        public void DrawRect(float x0, float y0, float x1, float y1, int dw, Color color)
        {
            DrawLine(x0, y0, x1, y0, dw, color);
            DrawLine(x0, y1, x1, y1, dw, color);
            DrawLine(x0, y0, x0, y1, dw, color);
            DrawLine(x1, y0, x1, y1, dw, color);
        }

        public void FillRect(float x0, float y0, float x1, float y1, Color color)
        {
            TGCVector2[] V = new TGCVector2[4];
            V[0].X = x0;
            V[0].Y = y0;
            V[1].X = x0;
            V[1].Y = y1;
            V[2].X = x1;
            V[2].Y = y1;
            V[3].X = x1;
            V[3].Y = y0;

            VERTEX2D[] pt = new VERTEX2D[16];
            // 1er triangulo
            pt[0].x = V[0].X;
            pt[0].y = V[0].Y;
            pt[1].x = V[1].X;
            pt[1].y = V[1].Y;
            pt[2].x = V[2].X;
            pt[2].y = V[2].Y;

            // segundo triangulo
            pt[3].x = V[0].X;
            pt[3].y = V[0].Y;
            pt[4].x = V[2].X;
            pt[4].y = V[2].Y;
            pt[5].x = V[3].X;
            pt[5].y = V[3].Y;

            for (int t = 0; t < 6; ++t)
            {
                pt[t].z = 0.5f;
                pt[t].rhw = 1;
                pt[t].color = color.ToArgb();
                ++t;
            }

            // dibujo como lista de triangulos
            var device = D3DDevice.Instance.Device;
            device.VertexFormat = VertexFormats.Transformed | VertexFormats.Diffuse;
            device.DrawUserPrimitives(PrimitiveType.TriangleList, 2, pt);
        }

        public void FillText(int x, int y, string text, Color color, int center = 0)
        {
            FillTextWithFont(font, x, y, text, color, center);
        }
        public void FillTextS(int x, int y, string text, Color color, int center = 0)
        {
            FillTextWithFont(s_font, x, y, text, color, center);
        }

        public void FillTextWithFont(Microsoft.DirectX.Direct3D.Font p_font , int x, int y, string text, Color color, int center )
        {
            var device = D3DDevice.Instance.Device;
            var screen_dx = device.PresentationParameters.BackBufferWidth;
            var screen_dy = device.PresentationParameters.BackBufferHeight;
            // elimino cualquier textura que me cague el modulate del vertex color
            device.SetTexture(0, null);
            // Desactivo el zbuffer
            bool ant_zenable = device.RenderState.ZBufferEnable;
            device.RenderState.ZBufferEnable = false;
            // pongo la matriz identidad
            Microsoft.DirectX.Matrix matAnt = sprite.Transform * Microsoft.DirectX.Matrix.Identity;
            sprite.Transform = Microsoft.DirectX.Matrix.Identity;                
            sprite.Begin(SpriteFlags.AlphaBlend);
            switch (center)
            {
                case 1:
                    {
                        Rectangle rc = new Rectangle(0, y, screen_dx, y + 100);
                        p_font.DrawText(sprite, text, rc, DrawTextFormat.Center, color);
                    }
                    break;
                case 2:
                    {
                        Rectangle rc = new Rectangle(x- screen_dx, y, x, y + 100);
                        p_font.DrawText(sprite, text, rc, DrawTextFormat.NoClip | DrawTextFormat.Top | DrawTextFormat.Right, color);
                    }
                    break;
                default:
                    {
                        Rectangle rc = new Rectangle(x, y, x + 600, y + 100);
                        p_font.DrawText(sprite, text, rc, DrawTextFormat.NoClip | DrawTextFormat.Top | DrawTextFormat.Left, color);
                    }
                    break;
            }
            sprite.End();

            // Restauro el zbuffer
            device.RenderState.ZBufferEnable = ant_zenable;
            // Restauro la transformacion del sprite
            sprite.Transform = matAnt;
        }




        public override void Dispose()
        {
            ESCENA.dispose();
            SHIP.Dispose();
            effect.Dispose();
            g_pRenderTarget.Dispose();
            g_pRenderTarget2.Dispose();
            g_pRenderTarget3.Dispose();
            g_pRenderTarget4.Dispose();
            g_pRenderTarget5.Dispose();
            g_pDepthStencil.Dispose();
            g_pVBV3D.Dispose();
        }

    }

}