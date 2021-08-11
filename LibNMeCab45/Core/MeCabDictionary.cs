//  MeCab -- Yet Another Part-of-Speech and Morphological Analyzer
//
//  Copyright(C) 2001-2006 Taku Kudo <taku@chasen.org>
//  Copyright(C) 2004-2006 Nippon Telegraph and Telephone Corporation
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NMeCab.Core
{
    public class MeCabDictionary : IDisposable
    {
        #region  Const/Field/Property

        private const uint DictionaryMagicID = 0xEF718F77u;
        private const uint DicVersion = 102u;

        private Token[] tokens;
        private byte[] features;

        private DoubleArray da = new DoubleArray();

        private Encoding encoding;

        /// <summary>
        /// 辞書の文字コード
        /// </summary>
        public string CharSet
        {
            get { return this.encoding.WebName; }
        }

        /// <summary>
        /// バージョン
        /// </summary>
        public uint Version { get; private set; }

        /// <summary>
        /// 辞書のタイプ
        /// </summary>
        public DictionaryType Type { get; private set; }

        public uint LexSize { get; private set; }

        /// <summary>
        /// 左文脈 ID のサイズ
        /// </summary>
        public uint LSize { get; private set; }

        /// <summary>
        /// 右文脈 ID のサイズ
        /// </summary>
        public uint RSize { get; private set; }

        /// <summary>
        /// 辞書のファイル名
        /// </summary>
        public string FileName { get; private set; }

        #endregion

        #region Open
        public void Open(string filePath)
        {
            this.FileName = filePath;

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (BinaryReader reader = new BinaryReader(fileStream))
            {
                this.Open(reader);
            }
        }
        public unsafe void Open(BinaryReader reader)
        {
            uint magic = reader.ReadUInt32();
            //CanSeekの時のみストリーム長のチェック
            if (reader.BaseStream.CanSeek && reader.BaseStream.Length != (magic ^ DictionaryMagicID))
                throw new MeCabInvalidFileException("dictionary file is broken", this.FileName);

            this.Version = reader.ReadUInt32();
            if (this.Version != DicVersion)
                throw new MeCabInvalidFileException("incompatible version", this.FileName);

            this.Type = (DictionaryType)reader.ReadUInt32();
            this.LexSize = reader.ReadUInt32();
            this.LSize = reader.ReadUInt32();
            this.RSize = reader.ReadUInt32();
            uint dSize = reader.ReadUInt32();
            uint tSize = reader.ReadUInt32();
            uint fSize = reader.ReadUInt32();
            reader.ReadUInt32(); //dummy

            string charSet = StrUtils.GetString(reader.ReadBytes(32), Encoding.ASCII);
            this.encoding = StrUtils.GetEncoding(charSet);

            this.da.Open(reader, dSize);

            this.tokens = new Token[tSize / sizeof(Token)];
            for (int i = 0; i < this.tokens.Length; i++)
                this.tokens[i] = Token.Create(reader);

            this.features = reader.ReadBytes((int)fSize);

            if (reader.BaseStream.ReadByte() != -1)
                throw new MeCabInvalidFileException("dictionary file is broken", this.FileName);
        }

        public void Open(byte[] contents)
        {
            int offset=0;
            uint magic = BitConverter.ToUInt32(contents, offset);
            offset += 4;
            this.Version = BitConverter.ToUInt32(contents, offset);
            if (this.Version != DicVersion)
            {
                throw new MeCabInvalidFileException("incompatible version", "");
            }

            offset += 4;

            this.Type = (DictionaryType)BitConverter.ToUInt32(contents, offset); offset += 4;
            this.LexSize = BitConverter.ToUInt32(contents, offset); offset += 4;
            this.LSize = BitConverter.ToUInt32(contents, offset); offset += 4;
            this.RSize = BitConverter.ToUInt32(contents, offset); offset += 4;
            uint dSize = BitConverter.ToUInt32(contents, offset); offset += 4;
            uint tSize = BitConverter.ToUInt32(contents, offset); offset += 4;
            uint fSize = BitConverter.ToUInt32(contents, offset); offset += 4;
            offset += 4;  //dummy

            byte[] b32 = new byte[33];
            Buffer.BlockCopy(contents, offset, b32, 0, 32);
            b32[32] = 0x00;
            offset += 32;
            string charSet = StrUtils.GetString(b32, Encoding.ASCII);
            this.encoding = StrUtils.GetEncoding(charSet);

            this.da.Open(contents,ref offset, dSize);

            this.tokens = new Token[tSize / 16];
            for (int i = 0; i < this.tokens.Length; i++)
            {
                this.tokens[i] = Token.Create(contents, offset);
                offset += 16;
            }
            this.features = new byte[(int)fSize];
            Buffer.BlockCopy(contents, offset, this.features, 0, (int)fSize);
            offset += (int)fSize;

            if( offset != contents.Length )
            {
                throw new MeCabInvalidFileException("dictionary file is broken", "");
            }
            //if (reader.BaseStream.ReadByte() != -1)
            //    throw new MeCabInvalidFileException("dictionary file is broken", this.FileName);
        }

        #endregion

        #region Search

        public unsafe DoubleArray.ResultPair ExactMatchSearch(string key)
        {
            fixed (char* pKey = key)
                return this.ExactMatchSearch(pKey, key.Length, 0);
        }

        public unsafe DoubleArray.ResultPair ExactMatchSearch(char* key, int len, int nodePos = 0)
        {
            //エンコード
            int maxByteCount = this.encoding.GetMaxByteCount(len);
            byte* bytes = stackalloc byte[maxByteCount];
            int bytesLen = this.encoding.GetBytes(key, len, bytes, maxByteCount);

            DoubleArray.ResultPair result = this.da.ExactMatchSearch(bytes, bytesLen, nodePos);

            //文字数をデコードしたものに変換
            result.Length = this.encoding.GetCharCount(bytes, result.Length);

            return result;
        }

        public unsafe int CommonPrefixSearch(char* key, int len, DoubleArray.ResultPair* result, int rLen)
        {
            //エンコード
            int maxByteLen = this.encoding.GetMaxByteCount(len);
            byte* bytes = stackalloc byte[maxByteLen];
            int bytesLen = this.encoding.GetBytes(key, len, bytes, maxByteLen);

            int n = this.da.CommonPrefixSearch(bytes, result, rLen, bytesLen);

            //文字数をデコードしたものに変換
            for (int i = 0; i < n; i++)
                result[i].Length = this.encoding.GetCharCount(bytes, result[i].Length);

            return n;
        }

        #endregion

        #region Get Infomation

        public unsafe Token[] GetToken(DoubleArray.ResultPair n)
        {
            Token[] dist = new Token[0xFF & n.Value];
            int tokenPos = n.Value >> 8;
            Array.Copy(this.tokens, tokenPos, dist, 0, dist.Length);
            return dist;
        }

        public string GetFeature(uint featurePos)
        {
            return StrUtils.GetString(this.features, (long)featurePos, this.encoding);
        }

        #endregion

        #region etc.

        public bool IsCompatible(MeCabDictionary d)
        {
            return (this.Version == d.Version &&
                    this.LSize == d.LSize &&
                    this.RSize == d.RSize &&
                    this.CharSet == d.CharSet);
        }

        #endregion

        #region Dispose

        private bool disposed;

        /// <summary>
        /// 使用されているリソースを開放する
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                if (this.da != null) this.da.Dispose();
            }

            this.disposed = true;
        }

        ~MeCabDictionary()
        {
            this.Dispose(false);
        }

        #endregion
    }
}
