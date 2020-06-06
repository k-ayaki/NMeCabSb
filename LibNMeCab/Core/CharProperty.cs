//  MeCab -- Yet Another Part-of-Speech and Morphological Analyzer
//
//  Copyright(C) 2001-2006 Taku Kudo <taku@chasen.org>
//  Copyright(C) 2004-2006 Nippon Telegraph and Telephone Corporation
//  
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace NMeCab.Core
{
    public class CharProperty
    {
        #region Const/Field/Property

        private string[] cList;

        private readonly CharInfo[] charInfoList = new CharInfo[0xFFFF];

        public int Size
        {
            get { return this.cList.Length; }
        }

        #endregion

        #region Open
        // リソース埋め込み用 2019/10/20
        public void Open()
        {
            byte[] contents = Properties.Resources._char;
            int offset = 0;
            uint cSize = BitConverter.ToUInt32(contents, offset);
            offset += 4;

            this.cList = new string[cSize];
            for (int i = 0; i < this.cList.Length; i++)
            {
                byte[] b32 = new byte[33];
                Buffer.BlockCopy(contents, offset, b32, 0, 32);
                b32[32] = 0x00;
                offset += 32;
                this.cList[i] = StrUtils.GetString(b32, Encoding.ASCII);
            }

            for (int i = 0; i < this.charInfoList.Length; i++)
            {
                charInfoList[i] = new CharInfo(BitConverter.ToUInt32(contents, offset));
                offset += 4;
            }
        }

        #endregion

        #region Get Infometion

        public string Name(int i)
        {
            return this.cList[i];
        }

        public unsafe char* SeekToOtherType(char* begin, char* end, CharInfo c, CharInfo* fail, int* cLen)
        {
            char* p = begin;
            *cLen = 0;

            *fail = this.GetCharInfo(*p);

            while (p != end && c.IsKindOf(*fail))
            {
                p++;
                (*cLen)++;
                c = *fail;

                *fail = this.GetCharInfo(*p);
            }

            return p;
        }

        public CharInfo GetCharInfo(char c)
        {
            return this.charInfoList[c];
        }

        #endregion
    }
}
