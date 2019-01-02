using System;
using Microsoft.DirectX.Direct3D;
using TGC.Core.Direct3D;
using System.Runtime.InteropServices;
using System.IO;
using TGC.Group.Model;
using TGC.Core.Mathematica;
using System.Drawing;

namespace TGC.Group
{

    // Esta catedra esta a favor del "lenguaje inclusivo"

    class sndmng : IDisposable
    {

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceCounter(
                   out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(
            out long lpFrequency);


        public const long TIME_LAP = 100;
        public const long MAX_SIZE = 5000 * 1024;
        public const int SAMPLE_RATE = 44100;
        public const long MAX_STREAMING = 100;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RIFF
        {
            public byte id0;
            public byte id1;
            public byte id2;
            public byte id3;
            public uint Size;
            public uint Format;
        };

        //The "WAVE" format consists of two subchunks: "fmt " and "data":
        //The "fmt " subchunk describes the sound data's format:
        [StructLayout(LayoutKind.Sequential)]
        public struct FormatChunk
        {
            public byte Subchunk1ID0;    //  Contains the letters "fmt "
            public byte Subchunk1ID1;
            public byte Subchunk1ID2;
            public byte Subchunk1ID3;
            public uint Subchunk1Size;    //	16 for PCM.  This is the size of the rest of the Subchunk which follows this number.
            public ushort AudioFormat;       //  PCM = 1 (i.e. Linear quantization) Values other than 1 indicate some form of compression.
            public ushort NumChannels;       //      Mono = 1, Stereo = 2, etc.
            public uint SampleRate;       //       8000, 44100, etc.
            public uint ByteRate;         // == SampleRate * NumChannels * BitsPerSample/8
            public ushort BlockAlign;        // == NumChannels * BitsPerSample/8 The number of bytes for one sample includingall channels. I wonder what happens when this number isn't an integer?
            public ushort BitsPerSample;     //    8 bits = 8, 16 bits = 16, etc.
        };

        // The "data" subchunk contains the size of the data and the actual sound:
        [StructLayout(LayoutKind.Sequential)]
        public struct DataChunk
        {
            public byte Subchunk2ID0;    //      Contains the letters "data"
            public byte Subchunk2ID1;
            public byte Subchunk2ID2;
            public byte Subchunk2ID3;
            public uint Subchunk2Size;    //	NumSamples * NumChannels * BitsPerSample/8 This is the number of bytes in the data.
                                          // You can also think of this as the size
                                          // of the read of the subchunk following this  number.
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct WAVEHDR
        {
            public IntPtr lpData; // pointer to locked data AudioBuffer
            public uint dwAudioBufferLength; // length of data AudioBuffer
            public uint dwBytesRecorded; // used for input only
            public IntPtr dwUser; // for client's use
            public uint dwFlags; // assorted flags (see defines)
            public uint dwLoops; // loop control counter
            public IntPtr lpNext; // PWaveHdr, reserved for driver
            public int reserved; // reserved for driver
        }

        [StructLayout(LayoutKind.Sequential)]
        public class WAVEFORMATEX
        {
            public short wFormatTag;
            public short nChannels;
            public int nSamplesPerSec;
            public int nAvgBytesPerSec;
            public short nBlockAlign;
            public short wBitsPerSample;
            public short cbSize;
        }


        // Voy a usar un AudioBuffer para almacenar los datos raw de audio, que van a estar compuestos de un WaveHeader + pcm data pp dicho
        // Pero necesito que eso este en memoria NO MANAGED para que el GC no me rompa las bolas y lo mueva de lugar

        // vamos a lo ANSI C !
        private IntPtr AudioBuffer = (IntPtr)0;              // PUNTERO al Buffer de audio
        private UInt32 AudioBufferIndex = 0;                 // indice dentro del buffer de audio
        private const int AudioBufferSize = 1024 * 1024;      // tamaño del buffer
        public IntPtr PCMBuffer = (IntPtr)0;               // PUNTERO al Buffer de una señal WAV de muestreo





        // cada vez que envio un paquete de datos, la cant. de bloques se incrementa y cuando me avisa
        // que se termina de procesar, esa cantidad se decrementa. 
        // Tambien la tengo que guardar en un area NO MANAGED 
        private IntPtr p_cant_bloques = (IntPtr)0;           // Puntero a un int32 que tiene la cantidad de bloques SIN PROCESAR

        // boludeces de los .h que preciso
        public const int WAVE_FORMAT_PCM = 0x0001;                                       /* PCM */
        public const int WAVE_MAPPER = -1;
        public const int CALLBACK_FUNCTION = 0x00030000;                                // flag used if we require a callback when audio frames are completed
        public const int CALLBACK_NULL = 0x00000000;                                    // flag used if no callback is required
        public const int MM_WOM_DONE = 0x3BD;                                           // flag used in callback to signal that the wave device has completed a AudioBuffer
        public const int MM_WOM_CLOSE = 0x3BC;                                          // flag used in callback to signal that wave device has closed
        public const int MMSYSERR_NOERROR = 0;
        public const int WHDR_BEGINLOOP = 4;
        public const int WHDR_DONE = 1;
        public const int WHDR_ENDLOOP = 8;

        // esta es la funcion CALLBACK que se llama cuando el circuito de audio termino de procesar un paquete
        public delegate void WaveDelegate(IntPtr dev, int uMsg, int dwUser, int dwParam1, int dwParam2);

        [DllImport("winmm.dll")]
        public static extern int waveOutOpen(out IntPtr hWaveOut, int uDeviceID, WAVEFORMATEX lpFormat, WaveDelegate dwCallback, IntPtr dwInstance, int dwFlags);
        [DllImport("winmm.dll")]
        public static extern int waveOutReset(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern int waveOutRestart(IntPtr hWaveOut);
        [DllImport("winmm.dll")]
        public static extern int waveOutPrepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutWrite(IntPtr hWaveOut, ref WAVEHDR lpWaveOutHdr, int uSize);
        [DllImport("winmm.dll")]
        public static extern int waveOutClose(IntPtr hWaveOut);

        public WaveDelegate woDone = new WaveDelegate(WaveOutDone);                    // delegate for wave out done callback
        public WaveDelegate woDonePlay = new WaveDelegate(WaveOutDonePlay);                    // delegate for wave out done callback
        public IntPtr hWaveOut = (IntPtr)0;

        public long F, T0;
        public int _cant_samples;


        int index;
        float lap;
        float volumen;
        double elapsed_time;

        public string wav_name;

        // analisis de fourier
        CFourier fft = new CFourier();

        // drawing
        VertexBuffer vb = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), 4000, D3DDevice.Instance.Device,
                Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Default);
        CustomVertex.PositionNormalTextured[] vb_data = new CustomVertex.PositionNormalTextured[4000];


        public unsafe void Create(string fname)
        {
            wav_name = Path.GetFileName(fname);
            volumen = 1.0f;
        
            // alloco memoria para el Buffer de audio y para el puntero a la cantidad de bloques 
            if (AudioBuffer == (IntPtr)0)
            {
                AudioBuffer = Marshal.AllocHGlobal(AudioBufferSize);
                AudioBufferIndex = 0;
                p_cant_bloques = Marshal.AllocHGlobal(4);
            }


            // Cargo el wav que voy a usar de muestreo
            bool flag = true;
            if (flag)
            {
                _cant_samples = generateWav(fname);
            }
            else
            {
                _cant_samples = SAMPLE_RATE * 4;
                    short* pcm = (short*)Marshal.AllocHGlobal(_cant_samples * sizeof(short));
                for (int i = 0; i < _cant_samples; ++i)
                    pcm[i] = 0;
                PCMBuffer = (IntPtr)pcm;
                int t = 0;
                t += generateTone(110, 1000, SAMPLE_RATE, pcm + t);
                t += generateTone(220, 1000, SAMPLE_RATE, pcm + t);
                t += generateTone(330, 1000, SAMPLE_RATE, pcm + t);
                t += generateTone(440, 1000, SAMPLE_RATE, pcm + t);
            }


            index = 0;
            WAVEFORMATEX Format = new WAVEFORMATEX();
            Format.cbSize = 0;
            Format.wFormatTag = WAVE_FORMAT_PCM;
            Format.nChannels = 1;
            Format.nSamplesPerSec = SAMPLE_RATE;
            Format.wBitsPerSample = 16;
            Format.nBlockAlign = 2;
            Format.nAvgBytesPerSec = Format.nSamplesPerSec * Format.nBlockAlign;
            waveOutOpen(out hWaveOut, WAVE_MAPPER, Format, woDone, p_cant_bloques, CALLBACK_FUNCTION);
            elapsed_time = 0;
            QueryPerformanceFrequency(out F);
            QueryPerformanceCounter(out T0);

            *((int*)p_cant_bloques) = 0;

            // Genero el timer 
            lap = .1f;

            // drawing
            vb = new VertexBuffer(typeof(CustomVertex.PositionNormalTextured), 4000, D3DDevice.Instance.Device,
                    Usage.Dynamic | Usage.WriteOnly, CustomVertex.PositionNormalTextured.Format, Pool.Default);
            vb_data = new CustomVertex.PositionNormalTextured[4000];


        }


        // esta funcion se llama continuamente, pero hay que sincronizar, de tal forma que el circuito 
        // de audio nunca se quede sin datos, pero sin llenar el buffer de salida 
        // entonces la idea es que cada vez que mando un paqueta sumo en uno la cantidad de bloques enviados
        // y cada vez que el circuito termina de tocarlos me avisa y decremento uno. 

        public unsafe void WaveOut()
        {

            int cant_bloques = *(int*)p_cant_bloques;
            if (cant_bloques >= 10)
                return;             // mejor espero

            // Avanzo el tiempo
            long T1;   // address of current frequency
            QueryPerformanceCounter(out T1);
            elapsed_time = (double)(T1 - T0) / (double)F;
            T0 = T1;

            // quiero generar una onda que dure el mismo tiempo que el lap del timer. (que es el dt del timer)
            // y un 10% mas, de tal forma de evitar el clack entre switchs
            // cant samples = SAMPLE_RATE * dt;		(dt ==1 )
            uint cant_samples = (uint)(SAMPLE_RATE * lap * 1.1f);
            uint pcm_size = cant_samples * sizeof(short);
            // el bloque esta compuesto del header (WAVEHDR) y el pcm data pp dicho
            uint block_size = (uint)sizeof(WAVEHDR) + pcm_size;
            byte* p_buff = ((byte*)AudioBuffer + AudioBufferIndex);
            WAVEHDR* header = (WAVEHDR*)p_buff;
            short* pcm = (short*)(p_buff + sizeof(WAVEHDR));
            short* wav = (short*)(PCMBuffer);

            float k_freq = 1.0f;

            for (int j = 0; j < cant_samples; j++)
            {
                float s = j / cant_samples;
                int ndx = (int)(index * k_freq) % _cant_samples;
                pcm[j] = (short)((wav[ndx]) * volumen);
                ++index;
            }
            header->lpData = (IntPtr)pcm;
            header->dwAudioBufferLength = pcm_size;
            header->dwFlags = WHDR_BEGINLOOP | WHDR_ENDLOOP;
            header->dwLoops = 1;


            // analisis de fourier
            fft.ComplexFFT(pcm, (int)cant_samples, 16384, 1);
            

            if (waveOutPrepareHeader(hWaveOut, ref *header, sizeof(WAVEHDR)) == MMSYSERR_NOERROR)
                if (waveOutWrite(hWaveOut, ref *header, sizeof(WAVEHDR)) == MMSYSERR_NOERROR)
                {
                    // incremento el indice dentro el buffer de audio, que se comporta como una lista circular
                    // cuando me quedo sin lugar arranca de nuevo donde seguro que esos paquetes ya fueron procesados
                    AudioBufferIndex += block_size;
                    if (AudioBufferIndex >= AudioBufferSize)
                        AudioBufferIndex = 0;
                    (*(int*)p_cant_bloques)++;      // un bloque mas
                }
        }

        // este funcion callback se llama cuando el circuito de audio termino de procesar el paquete. 
        unsafe static void WaveOutDone(IntPtr dev, int uMsg, int dwUser, int dwParam1, int dwParam2)
        {
            // me avisan que termino de procesar un bloque, entonces decremento la cantidad de bloques
            if ((uMsg == MM_WOM_DONE))
            {
                //WAVEHDR *header = (WAVEHDR*)dwParam1;
                // int cant_bloques = *(int*)dwUser;
                // sincronzacion del streaming : marco que hay unbloque menos
                (*(int*)dwUser)--;
            }
        }


        public unsafe void Play(float freq, float duration, float vol)
        {
            volumen = vol;
            WAVEFORMATEX Format = new WAVEFORMATEX();
            Format.cbSize = 0;
            Format.wFormatTag = WAVE_FORMAT_PCM;
            Format.nChannels = 1;
            Format.nSamplesPerSec = SAMPLE_RATE;
            Format.wBitsPerSample = 16;
            Format.nBlockAlign = 2;
            Format.nAvgBytesPerSec = Format.nSamplesPerSec * Format.nBlockAlign;
            waveOutOpen(out hWaveOut, WAVE_MAPPER, Format, woDonePlay, (IntPtr)0, CALLBACK_FUNCTION);

            // sintetizo una onda con la frecuencia y duracion dadas
            float kv;
            int cant_samples = (int)(SAMPLE_RATE * duration / 1000.0);

            // aloco memoria para el header y para los datos pcm pp dichos, todo junto
            PCMBuffer = Marshal.AllocHGlobal(sizeof(WAVEHDR) + sizeof(short) * cant_samples);
            WAVEHDR* header = (WAVEHDR*)PCMBuffer;
            short* pcm = (short*)(PCMBuffer + sizeof(WAVEHDR));
            for (int j = 0; j < cant_samples; j++)
            {
                // dice que la conversion es redundante.....las pelotas es redundante... si no lo convertis a doble da siempre cero!!! 
                double t = (double)j / (double)SAMPLE_RATE;     // tiempo transcurrido
                float s0 = 0.1f;
                float s1 = 0.95f;
                float s = (float)j / (float)cant_samples;
                if (s < s0)
                    kv = s / s0;
                else
                if (s > s1)
                    kv = (1 - s) / (1 - s1);
                else
                    kv = 1;
                double value = Math.Sin(2 * Math.PI * freq * t) + 0.3 * Math.Sin(4 * Math.PI * freq * t);
                value = value * volumen * kv;
                if (value < -1)
                    value = -1;
                else
                if (value > 1)
                    value = 1;
                pcm[j] = (short)(value * 32500.0);
            }


            header->lpData = (IntPtr)pcm;
            header->dwAudioBufferLength = (uint)(sizeof(short) * cant_samples);
            header->dwFlags = WHDR_BEGINLOOP | WHDR_ENDLOOP;
            header->dwLoops = 1;

            if (waveOutPrepareHeader(hWaveOut, ref *header, sizeof(WAVEHDR)) == MMSYSERR_NOERROR)
                if (waveOutWrite(hWaveOut, ref *header, sizeof(WAVEHDR)) == MMSYSERR_NOERROR)
                {
                }
        }


        // este funcion callback se llama cuando el circuito de audio termino de procesar el paquete en el contexto de la funcion Play
        unsafe static void WaveOutDonePlay(IntPtr dev, int uMsg, int dwUser, int dwParam1, int dwParam2)
        {
            if ((uMsg == MM_WOM_DONE))
            {
                IntPtr PCMBuffer = (IntPtr)dwParam1;
                WAVEHDR* header = (WAVEHDR*)PCMBuffer;
                // tengo que llamar a esta funcion para liberar el header
                waveOutUnprepareHeader(dev, ref *header, sizeof(WAVEHDR));
                // libero la memoria
                Marshal.FreeHGlobal(PCMBuffer);
            }
        }


        // Genera un tono wave sin tipo para afinar 
        public unsafe int generateTone(int freq, double lengthMS, int sampleRate,short *pcm)
        {
            int numSamples = (int)(((double)sampleRate) * lengthMS / 1000.0f);

            int attack = (int)(numSamples * 0.1);
            int sustain = (int)(attack + numSamples * 0.4);
            int decay = (int)(sustain + numSamples * 0.1);



            for (int i = 0; i < numSamples; ++i)
            {
                double value = Math.Sin(2 * Math.PI * freq * i / sampleRate);

                if (i < attack)
                    value *= (double)i / (double)attack;
                else
                if (i < sustain)
                    value *= 1;
                else
                if (i < decay)
                    value *= 0.01f + (double)(i - sustain) / (double)(decay - sustain);
                else
                    value *= 0.01f;
                    

                pcm[i] = (short)(value * 32500.0);
            }

            return numSamples;
        }



        public unsafe int generateWav(string fname)
        {
            byte[] bytes = File.ReadAllBytes(fname);
            

            RIFF* m_pRiff = (RIFF*)Marshal.AllocHGlobal(sizeof(RIFF));
            FormatChunk* m_pFmt = (FormatChunk*)Marshal.AllocHGlobal(sizeof(FormatChunk));
            DataChunk* m_pData = (DataChunk*)Marshal.AllocHGlobal(sizeof(DataChunk));

            uint pos = 0;
            // memcpy(&m_pRiff, bytes, sizeof(struct RIFF));
            byte* p = (byte*)m_pRiff;
            for (int i = 0; i < sizeof(RIFF); ++i)
                *p++ = bytes[pos++];

            //memcpy(&m_pFmt, bytes + pos, sizeof(struct FormatChunk));
            p = (byte*)m_pFmt;
            for (int i = 0; i < sizeof(FormatChunk); ++i)
                *p++ = bytes[pos++];

            int extra_bytes = (int)m_pFmt->Subchunk1Size - 16;
            if (extra_bytes > 0)
                pos += (uint)extra_bytes;

            //memcpy(&m_pData, bytes + pos, sizeof(struct DataChunk));
            p = (byte*)m_pData;
            uint pos_aux = pos;
            for (int i = 0; i < sizeof(DataChunk); ++i)
                *p++ = bytes[pos_aux++];

            // busco el chunk de datos pp dichos
            //while (strncmp(m_pData.Subchunk2ID, "data", 4) != 0)
            while(m_pData->Subchunk2ID0 != 'd' || m_pData->Subchunk2ID1 != 'a' || m_pData->Subchunk2ID2 != 't' || m_pData->Subchunk2ID3 != 'a')
            {
                pos += 8 + m_pData->Subchunk2Size;
                // memcpy(&m_pData, bytes + pos, sizeof(struct DataChunk));
                p = (byte*)m_pData;
                for (int i = 0; i < sizeof(DataChunk); ++i)
                    *p++ = (byte)bytes[pos++];


            }
            pos += (uint)sizeof(DataChunk);

            // Ahora viene la data pp dicha
            int rta = (int)(m_pData->Subchunk2Size / 2);
            // la transformo a lo que necesito:
            int data_size = (int)m_pData->Subchunk2Size;
            if (m_pFmt->BitsPerSample == 8)
                data_size *= 2;
            if (m_pFmt->NumChannels == 1)
                data_size *= 2;
            if (m_pFmt->SampleRate != SAMPLE_RATE)
                data_size *= (int)(SAMPLE_RATE / m_pFmt->SampleRate);

            // agrego 5 segundos de silencio inicial 
            data_size += SAMPLE_RATE * 5 * sizeof(short);
            short* data = (short*)Marshal.AllocHGlobal(data_size);
            int index = 0;
            for(int i=0;i< SAMPLE_RATE * 5;++i)
                data[index++] = 0;

            if (m_pFmt->BitsPerSample == 16)
            {
                // me quedo con un solo canal , el otro esta al pedo
                //short* pb = (short*)(bytes + pos);
                int cant_samples = (int)(m_pData->Subchunk2Size / m_pFmt->NumChannels / 2);
                int F = (int)(SAMPLE_RATE / m_pFmt->SampleRate);
                for (int i = 0; i < cant_samples; ++i)
                    // corrijo la dif. de muestreo repitiendo los samples
                    for (int t = 0; t < F; ++t)
                    {
                        int j = m_pFmt->NumChannels * i;
                        uint ndx = (uint)(pos + 2 * j);
                        if (ndx > bytes.Length - 2)
                            ndx = (uint)(bytes.Length - 2);

                        short b0 = bytes[ndx];
                        short b1 = bytes[ndx+1];
                        short sample = (short)(b0 + b1 * 256);
                        data[index++] = sample;
                    }
                rta = index;
            }
            else
            // viene de culo:
            if (m_pFmt->BitsPerSample == 8)
            {
                int cant_samples = (int)(m_pData->Subchunk2Size / m_pFmt->NumChannels);
                int F = (int)(SAMPLE_RATE / m_pFmt->SampleRate);
                for (int i = 0; i < cant_samples; ++i)
                {
                    // tomo el sample y lo paso a 16 bits
                    short sample = (short)(bytes[pos + m_pFmt->NumChannels * i] << 7);
                    // corrijo la dif. de muestreo repitiendo los samples
                    for (int t = 0; t < F; ++t)
                        data[index++] = sample;
                }
                rta = index;
            }

            // guardo el puntero a la señal WAV
            PCMBuffer = (IntPtr)data;

            Marshal.FreeHGlobal((IntPtr)m_pRiff);
            Marshal.FreeHGlobal((IntPtr)m_pFmt);
            Marshal.FreeHGlobal((IntPtr)m_pData);

            // Retorna la cantidad de samples
            return rta;

        }


        public void render(Effect effect , CScene T , CShip S)
        {
            var device = D3DDevice.Instance.Device;
            int pos_en_ruta = S.pos_en_ruta;
            float dr = T.ancho_ruta;
            TGCVector3 Normal = T.Normal[pos_en_ruta];
            TGCVector3 Binormal = T.Binormal[pos_en_ruta];
            TGCVector3 Tangent = T.Tangent[pos_en_ruta];
            int cant_tri = 0;
            int dataIdx = 0;
            int cant_bandas = 200;
            float M = 1000000.0f;
       
            for (int j = 0; j < cant_bandas; j++)
            {

                float ei = fft.coef(2 * j) / M;
                float y = 40 + ei * 1230;
                float x0 = 2 * dr * (float)j / (float)cant_bandas - dr - 5 ;
                float x1 = 2 * dr * (float)(j+1) / (float)cant_bandas- dr + 5;

                TGCVector3 p0, p1, p2, p3;
                float dt = -20;
                p0 = S.posC - Binormal * x0 - Normal * 5 + Tangent * dt;
                p1 = S.posC - Binormal * x1 - Normal * 5 + Tangent * dt;
                p2 = p0 + Tangent * y;
                p3 = p1 + Tangent * y;

                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, Normal, 0, 0);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, Normal, 1, 0);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, Normal, 0, 1);

                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, Normal, 1, 0);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, Normal, 0, 1);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, Normal, 1, 1);

                cant_tri += 2;


                x0 = 2 * dr * (float)j / (float)cant_bandas - dr;
                x1 = 2 * dr * (float)(j + 1) / (float)cant_bandas - dr;

                p0 = S.posC - Binormal * x0 - Normal * 5 + Tangent * dt;
                p1 = S.posC - Binormal * x1 - Normal * 5 + Tangent * dt;
                p2 = p0 + Tangent * y;
                p3 = p1 + Tangent * y;

                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p0, Normal, 0, 0);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, Normal, 1, 0);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, Normal, 0, 1);

                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p1, Normal, 1, 0);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p2, Normal, 0, 1);
                vb_data[dataIdx++] = new CustomVertex.PositionNormalTextured(p3, Normal, 1, 1);

                cant_tri += 2;


            }



            vb.SetData(vb_data, 0, LockFlags.None);

            device.SetStreamSource(0, vb, 0);
            device.VertexFormat = CustomVertex.PositionNormalTextured.Format;
            effect.Technique = "GlowBar";
            effect.SetValue("cube_color", TGCVector3.Vector3ToFloat4Array(new TGCVector3(1,0.3f, 0.3f)));
            device.RenderState.AlphaBlendEnable = true;
            device.RenderState.ZBufferEnable = false;


            int numPasses = effect.Begin(0);
            for (var n = 0; n < numPasses; n++)
            {
                effect.BeginPass(n);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, cant_tri);
                effect.EndPass();
            }
            effect.End();
            device.RenderState.ZBufferEnable = true;

        }


        public void Dispose()
        {
            // libero la memoria
            if (AudioBuffer == (IntPtr)0)
            {
                Marshal.FreeHGlobal(AudioBuffer);
                Marshal.FreeHGlobal(p_cant_bloques);
            }

            if (PCMBuffer == (IntPtr)0)
            {
                Marshal.FreeHGlobal(PCMBuffer);
            }
        }
    }




    class CFourier : IDisposable
    {
        public int ff;      // frecuencia fundamental
        public float Eff;  // Energia de la ff
        public float Ef;  // Energia de la ff normalizada segun M
        public IntPtr mem = (IntPtr)0;
        public int sample_rate = 0;
        public float M;            // factor de energia media
        /// filtro pasabanda
        public int freq_inf, freq_sup;
        public int i_inf , i_sup;


        public CFourier()
        {
            mem = (IntPtr)0;
            M = 100000.0f;
            freq_inf = 90;
            freq_sup = 500;
            i_inf = 2 * (int)((float)freq_inf * 16384.0f / 44110.0f);
            i_sup = 2 * (int)((float)freq_sup * 16384.0f / 44110.0f);

        }

        public void Dispose()
        {
            // libero la memoria
            if (mem == (IntPtr)0)
            {
                Marshal.FreeHGlobal(mem);
            }
        }

        public unsafe void SWAP(int i,int j)
        {
            float* vector = (float*)mem;
            float aux = vector[i];
            vector[i] = vector[j];
            vector[j] = aux;
        }

        public unsafe float coef(int k)
        {
            if (mem == (IntPtr)0)
                return 0;
            float* vector = (float*)mem;
            return vector[k];
        }

        // devuelve la frecuencia real
        public int que_frecuencia()
        {
            return (int)(ff * 44100.0f / (float)sample_rate);
        }


        // FFT 1D
        public unsafe void ComplexFFT(short *data, int cant_samples, int p_sample_rate, int sign)
        {

            sample_rate = p_sample_rate;

            // algoritmo standard de transformada rapida de fourier
            int n, mmax, m, j, istep, i;
            double wtemp, wr, wpr, wpi, wi, theta, tempr, tempi;
            //the complex array is real+complex so the array 
            //as a size n = 2* number of complex samples
            //real part is the data[index] and 
            //the complex part is the data[index+1]
            //new complex array of size n=2*sample_rate

            //nota: por el teorema de muestreo de niquist, es que se precisa 2 veces el sample_rate. 

            if (mem == (IntPtr)0)
            {
                mem = Marshal.AllocHGlobal(2 * sample_rate * sizeof(float));
            }

            float* vector = (float*)mem;
            
            //put the real array in a complex array
            //the complex part is filled with 0's
            //the remaining vector with no data is filled with 0's
            for (n = 0; n < sample_rate; n++)
            {
                if (n < cant_samples)
                    vector[2 * n] = data[n] / 32768.0f;     // va entre -1 y 1
                else
                    vector[2 * n] = 0;
                vector[2 * n + 1] = 0;

            }

            //binary inversion (note that the indexes 
            //start from 0 witch means that the
            //real part of the complex is on the even-indexes 
            //and the complex part is on the odd-indexes)
            n = sample_rate << 1;
            j = 0;
            for (i = 0; i < n / 2; i += 2)
            {
                if (j > i)
                {
                    SWAP(j, i);
                    SWAP(j + 1, i + 1);
                    if ((j / 2) < (n / 4))
                    {
                        SWAP(n - (i + 2), n - (j + 2));
                        SWAP((n - (i + 2)) + 1, (n - (j + 2)) + 1);
                    }
                }
                m = n >> 1;
                while (m >= 2 && j >= m)
                {
                    j -= m;
                    m >>= 1;
                }
                j += m;
            }
            //end of the bit-reversed order algorithm

            //Danielson-Lanzcos routine
            mmax = 2;
            while (n > mmax)
            {
                istep = mmax << 1;
                theta = sign * (2 * Math.PI / mmax);
                wtemp = Math.Sin(0.5 * theta);
                wpr = -2.0 * wtemp * wtemp;
                wpi = Math.Sin(theta);
                wr = 1.0;
                wi = 0.0;
                for (m = 1; m < mmax; m += 2)
                {
                    for (i = m; i <= n; i += istep)
                    {
                        j = i + mmax;
                        tempr = wr * vector[j - 1] - wi * vector[j];
                        tempi = wr * vector[j] + wi * vector[j - 1];
                        vector[j - 1] = (float)(vector[i - 1] - tempr);
                        vector[j] = (float)(vector[i] - tempi);
                        vector[i - 1] += (float)tempr;
                        vector[i] += (float)tempi;
                    }
                    wr = (wtemp = wr) * wpr - wi * wpi + wr;
                    wi = wi * wpr + wtemp * wpi + wi;
                }
                mmax = istep;
            }
            //end of the algorithm


            // reemplazo el vector por el modulo
            // freq_real = (indice * 44100.0f / (float)sample_rate);
            i_inf = 2 * (int)((float)freq_inf * (float)sample_rate / 44100.0f);
            i_sup = 2 * (int)((float)freq_sup * (float)sample_rate / 44100.0f);
            for (i = 0; i <= sample_rate; i += 2)
            {
                if (i >= i_inf && i <= i_sup)
                    vector[i] = vector[i] * vector[i] + vector[i + 1] * vector[i + 1];
                else
                vector[i] = 0;
            }


            //voy a buscar en que frecuencia se concentra la mayor parte de la energia de onda
            ff = 0;
            for (i = 2; i <= sample_rate; i += 2)
            {
                if (vector[i] > vector[ff])
                    ff= i;
            }

            ff = (int)Math.Floor((float)ff/ 2);

            // energia de la freq. fundamental
            Eff = vector[2 * ff];
            Ef = vector[2 * ff] / M;

    }
}

}
