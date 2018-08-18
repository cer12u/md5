using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace md5
{
    class Program
    {

        static void Main(string[] args)
        {
            string mes = "message ";
            string mes2 = "digest";

            MD5Data m = MD5Init();


            MD5Add(m, System.Text.Encoding.ASCII.GetBytes(mes));

            byte[] sb = System.Text.Encoding.ASCII.GetBytes(mes2);
            MemoryStream st = new MemoryStream(sb);

            MD5Add(m, st);

            string md5 = MD5Final(m);

            Console.WriteLine(md5);

        }


        /// <summary>
        /// MD5計算の初期値を代入する関数
        /// </summary>
        public static MD5Data MD5Init()
        {
            MD5Data initData = new MD5Data();

            return initData;
        }

        /// <summary>
        /// MD5の計算を追記する関数(byte[]タイプ)
        /// 64bytesごとに計算して保持しておく
        /// </summary>
        /// <param name="md5">MD5クラスを指定する</param>
        /// <param name="input">入力するバイト列</param>
        public static void MD5Add(MD5Data md5, byte[] input)
        {
            byte[] vs;

            //前回の余りデータがあれば結合する
            if (md5.BytesPtr != 0)
            {
                byte[] bt = new byte[md5.BytesPtr];
                Buffer.BlockCopy(md5.Bytes, 0, bt, 0, md5.BytesPtr);
                vs = (bt).Concat(input).ToArray();
            }
            //なければそのまま
            else
            {
                vs = input;
            }

            //処理長さを保持
            //int tempLen = input.Length;
            int tempLen = vs.Length;


            //64Bytesごとに処理
            for (int i = 0; i < (tempLen / 64); i++)
            {

                //処理本体
                byte[] b = new byte[64];
                //Buffer.BlockCopy(input, i * 64, b, 0, 64);
                Buffer.BlockCopy(vs, i * 64, b, 0, 64);

                md5.words = CalcPart(md5.words, b);

                //長さの反映
                md5.DataLen += 64;

            }

            //余りがあったら処理
            md5.BytesPtr = tempLen % 64;
            if (md5.BytesPtr != 0)
            {
                //Buffer.BlockCopy(input, (tempLen / 64) * 64, md5.Bytes, 0, md5.BytesPtr);
                Buffer.BlockCopy(vs, (tempLen / 64) * 64, md5.Bytes, 0, md5.BytesPtr);
            }
        }


        /// <summary>
        /// MD5の計算を追記する関数(Streamタイプ)
        /// 64bytesごとに計算して保持しておく
        /// </summary>
        /// <param name="md5">MD5クラスを指定する</param>
        /// <param name="input">入力するストリーム</param>
        public static void MD5Add(MD5Data md5, Stream input)
        {

            int blockSize = 64;
            int buffSize = blockSize;
            int startPoint = 0;
            int readLen;

            byte[] vs = new byte[blockSize];

            if (md5.BytesPtr != 0)
            {

                //                byte[] bt = new byte[md5.BytesPtr];
                Buffer.BlockCopy(md5.Bytes, 0, vs, 0, md5.BytesPtr);
                buffSize = blockSize - md5.BytesPtr;
                startPoint = md5.BytesPtr;
            }


            while (true)
            {
                readLen = input.Read(vs, startPoint, buffSize);

                if (readLen > 0)
                {
                    if (readLen + startPoint == blockSize)
                    {
                        //処理本体
                        md5.words = CalcPart(md5.words, vs);

                        md5.DataLen += 64;

                    }
                    else if (readLen + startPoint < blockSize)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }

                buffSize = blockSize;
                startPoint = 0;

            }

            if (readLen + startPoint > 0)
            {
                md5.BytesPtr = readLen + startPoint % 64;
                if (md5.BytesPtr != 0)
                {
                    Buffer.BlockCopy(vs, 0, md5.Bytes, 0, md5.BytesPtr);
                }
            }

            
        }

        /// <summary>
        /// MD5の計算を終了し、結果を返す関数
        /// パディング処理、結果の変換
        /// </summary>
        /// <returns>MD5計算値</returns>
        public static string MD5Final(MD5Data md5)
        {
            byte[] bs = new byte[64];
            int ptr = md5.BytesPtr;

            //md5.Bytes.CopyTo(bs, 0);
            //long ptr = md5.BytesPtr;
            Buffer.BlockCopy(md5.Bytes, 0, bs, 0, ptr);

            //int padLen = (int)(md5.DataLen % 64);
            int padLen = ptr;

            if (padLen < 56)
            {
                padLen = 56 - padLen;
            }
            else
            {
                padLen = 56 + padLen;
            }

            if (padLen > 0)
            {
                bs[ptr++] = 0x80;
                for (int i = 1; i < padLen; i++)
                {
                    bs[ptr++] = 0x00;
                }
            }


            //Byteからbitに変換
            long originLen = (md5.DataLen + md5.BytesPtr) * 8;

            //長さ情報をbyte配列に入れる
            UInt32 lenLSB = (UInt32)(originLen & 0xFFFFFFFF);
            UInt32 lenMSB = (UInt32)(originLen >> 32);

            byte[] len = BitConverter.GetBytes(lenLSB);
            len.CopyTo(bs, ptr);
            ptr += 4;

            len = BitConverter.GetBytes(lenMSB);
            len.CopyTo(bs, ptr);
            ptr += 4;

            md5.words = CalcPart(md5.words, bs);

            //UInt32 -> Byte[]
            byte[] bArrayA = BitConverter.GetBytes(md5.words[0]);
            byte[] bArrayB = BitConverter.GetBytes(md5.words[1]);
            byte[] bArrayC = BitConverter.GetBytes(md5.words[2]);
            byte[] bArrayD = BitConverter.GetBytes(md5.words[3]);

            //Byte配列に格納
            byte[] bArray = new byte[16];


            bArrayA.CopyTo(bArray, 0);
            bArrayB.CopyTo(bArray, 4);
            bArrayC.CopyTo(bArray, 8);
            bArrayD.CopyTo(bArray, 12);
            return BitConverter.ToString(bArray).Replace("-","");




        }

        static private UInt32[] CalcPart(UInt32[] word, byte[] ba)
        {


            UInt32 A = word[0];
            UInt32 B = word[1];
            UInt32 C = word[2];
            UInt32 D = word[3];

            // Byte配列からwordの取り出し
            UInt32[] X = new UInt32[16];
            for (int j = 0; j < 0x10; j++)
            {
                X[j] = (UInt32)(ba[j * 4 + 0] << 0);
                X[j] += (UInt32)(ba[j * 4 + 1] << 8);
                X[j] += (UInt32)(ba[j * 4 + 2] << 16);
                X[j] += (UInt32)(ba[j * 4 + 3] << 24);
            }
            

            // Round1
            A = Round1(A, B, C, D, X[0], 7, 0xd76aa478);
            D = Round1(D, A, B, C, X[1], 12, 0xe8c7b756);
            C = Round1(C, D, A, B, X[2], 17, 0x242070db);
            B = Round1(B, C, D, A, X[3], 22, 0xc1bdceee);

            A = Round1(A, B, C, D, X[4], 7, 0xf57c0faf);
            D = Round1(D, A, B, C, X[5], 12, 0x4787c62a);
            C = Round1(C, D, A, B, X[6], 17, 0xa8304613);
            B = Round1(B, C, D, A, X[7], 22, 0xfd469501);

            A = Round1(A, B, C, D, X[8], 7, 0x698098d8);
            D = Round1(D, A, B, C, X[9], 12, 0x8b44f7af);
            C = Round1(C, D, A, B, X[10], 17, 0xffff5bb1);
            B = Round1(B, C, D, A, X[11], 22, 0x895cd7be);

            A = Round1(A, B, C, D, X[12], 7, 0x6b901122);
            D = Round1(D, A, B, C, X[13], 12, 0xfd987193);
            C = Round1(C, D, A, B, X[14], 17, 0xa679438e);
            B = Round1(B, C, D, A, X[15], 22, 0x49b40821);


            //Round2
            A = Round2(A, B, C, D, X[1], 5, 0xf61e2562);
            D = Round2(D, A, B, C, X[6], 9, 0xc040b340);
            C = Round2(C, D, A, B, X[11], 14, 0x265e5a51);
            B = Round2(B, C, D, A, X[0], 20, 0xe9b6c7aa);

            A = Round2(A, B, C, D, X[5], 5, 0xd62f105d);
            D = Round2(D, A, B, C, X[10], 9, 0x02441453);
            C = Round2(C, D, A, B, X[15], 14, 0xd8a1e681);
            B = Round2(B, C, D, A, X[4], 20, 0xe7d3fbc8);

            A = Round2(A, B, C, D, X[9], 5, 0x21e1cde6);
            D = Round2(D, A, B, C, X[14], 9, 0xc33707d6);
            C = Round2(C, D, A, B, X[3], 14, 0xf4d50d87);
            B = Round2(B, C, D, A, X[8], 20, 0x455a14ed);

            A = Round2(A, B, C, D, X[13], 5, 0xa9e3e905);
            D = Round2(D, A, B, C, X[2], 9, 0xfcefa3f8);
            C = Round2(C, D, A, B, X[7], 14, 0x676f02d9);
            B = Round2(B, C, D, A, X[12], 20, 0x8d2a4c8a);


            //Round3
            A = Round3(A, B, C, D, X[5], 4, 0xfffa3942);
            D = Round3(D, A, B, C, X[8], 11, 0x8771f681);
            C = Round3(C, D, A, B, X[11], 16, 0x6d9d6122);
            B = Round3(B, C, D, A, X[14], 23, 0xfde5380c);

            A = Round3(A, B, C, D, X[1], 4, 0xa4beea44);
            D = Round3(D, A, B, C, X[4], 11, 0x4bdecfa9);
            C = Round3(C, D, A, B, X[7], 16, 0xf6bb4b60);
            B = Round3(B, C, D, A, X[10], 23, 0xbebfbc70);

            A = Round3(A, B, C, D, X[13], 4, 0x289b7ec6);
            D = Round3(D, A, B, C, X[0], 11, 0xeaa127fa);
            C = Round3(C, D, A, B, X[3], 16, 0xd4ef3085);
            B = Round3(B, C, D, A, X[6], 23, 0x04881d05);

            A = Round3(A, B, C, D, X[9], 4, 0xd9d4d039);
            D = Round3(D, A, B, C, X[12], 11, 0xe6db99e5);
            C = Round3(C, D, A, B, X[15], 16, 0x1fa27cf8);
            B = Round3(B, C, D, A, X[2], 23, 0xc4ac5665);


            //Round4
            A = Round4(A, B, C, D, X[0], 6, 0xf4292244);
            D = Round4(D, A, B, C, X[7], 10, 0x432aff97);
            C = Round4(C, D, A, B, X[14], 15, 0xab9423a7);
            B = Round4(B, C, D, A, X[5], 21, 0xfc93a039);

            A = Round4(A, B, C, D, X[12], 6, 0x655b59c3);
            D = Round4(D, A, B, C, X[3], 10, 0x8f0ccc92);
            C = Round4(C, D, A, B, X[10], 15, 0xffeff47d);
            B = Round4(B, C, D, A, X[1], 21, 0x85845dd1);

            A = Round4(A, B, C, D, X[8], 6, 0x6fa87e4f);
            D = Round4(D, A, B, C, X[15], 10, 0xfe2ce6e0);
            C = Round4(C, D, A, B, X[6], 15, 0xa3014314);
            B = Round4(B, C, D, A, X[13], 21, 0x4e0811a1);

            A = Round4(A, B, C, D, X[4], 6, 0xf7537e82);
            D = Round4(D, A, B, C, X[11], 10, 0xbd3af235);
            C = Round4(C, D, A, B, X[2], 15, 0x2ad7d2bb);
            B = Round4(B, C, D, A, X[9], 21, 0xeb86d391);


            // 保持値を反映
            word[0] += A;
            word[1] += B;
            word[2] += C;
            word[3] += D;

            return word;

        }

        public class MD5Data
        {
            //合計の長さ保持用
            public long DataLen
            {
                get; set;
            }
            //余りのByteの長さ管理
            public int BytesPtr
            {
                get; set;
            }
            // 余り部分の管理用
            public byte[] Bytes
            {
                get; set;
            }

            public UInt32[] words
            {
                get; set;
            }

            public MD5Data()
            {

                DataLen = 0;
                BytesPtr = 0;
                Bytes = new byte[64];

                words = new UInt32[4];

                //MD5計算前イニシャライズ
                words[0] = 0x67452301;
                words[1] = 0xefcdab89;
                words[2] = 0x98badcfe;
                words[3] = 0x10325476;
                
            }



        }



        public static UInt32 Round1(UInt32 a, UInt32 b, UInt32 c, UInt32 d, UInt32 Xk, Int32 s, UInt32 Ti)
        {
            UInt32 ba = ((a + F(b, c, d) + Xk + Ti));
            return (b + ((ba << s) | ((ba >> (32 - s)))));
        }

        public static UInt32 Round2(UInt32 a, UInt32 b, UInt32 c, UInt32 d, UInt32 Xk, Int32 s, UInt32 Ti)
        {
            UInt32 ba = ((a + G(b, c, d) + Xk + Ti));
            return (b + ((ba << s) | ((ba >> (32 - s)))));

        }
        public static UInt32 Round3(UInt32 a, UInt32 b, UInt32 c, UInt32 d, UInt32 Xk, Int32 s, UInt32 Ti)
        {
            UInt32 ba = ((a + H(b, c, d) + Xk + Ti));
            return (b + ((ba << s) | ((ba >> (32 - s)))));

        }
        public static UInt32 Round4(UInt32 a, UInt32 b, UInt32 c, UInt32 d, UInt32 Xk, Int32 s, UInt32 Ti)
        {
            UInt32 ba = ((a + I(b, c, d) + Xk + Ti));
            return (b + ((ba << s) | ((ba >> (32 - s)))));

        }

        public static UInt32 F(UInt32 x, UInt32 y, UInt32 z) => (((x) & (y)) | ((~x) & (z)));

        public static UInt32 G(UInt32 x, UInt32 y, UInt32 z) => (((x) & (z)) | ((y) & (~z)));

        public static UInt32 H(UInt32 x, UInt32 y, UInt32 z) => ((x) ^ (y) ^ (z));

        public static UInt32 I(UInt32 x, UInt32 y, UInt32 z) => ((y) ^ ((x) | (~z)));

    }
}
