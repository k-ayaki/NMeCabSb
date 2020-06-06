//  MeCab -- Yet Another Part-of-Speech and Morphological Analyzer
//
//  Copyright(C) 2001-2006 Taku Kudo <taku@chasen.org>
//  Copyright(C) 2004-2006 Nippon Telegraph and Telephone Corporation
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace NMeCab.Core
{
    public class Connector : IDisposable
    {
        #region Const/Field/Property

        private short[] matrix;

        public ushort LSize { get; private set; }

        public ushort RSize { get; private set; }

        #endregion

        #region Open
        // リソース埋め込み用
        public void Open()
        {
            byte[] contents = Properties.Resources.matrix;
            int idx = 0;
            this.LSize = BitConverter.ToUInt16(contents, idx);
            idx += 2;
            this.RSize = BitConverter.ToUInt16(contents, idx);
            idx += 2;
            this.matrix = new short[this.LSize * this.RSize];
            for (int i = 0; i < this.matrix.Length; i++)
            {
                matrix[i] = BitConverter.ToInt16(contents, idx);
                idx += 2;
            }
        }

        #endregion

        #region Cost

        public int Cost(MeCabNode lNode, MeCabNode rNode)
        {
            int pos = lNode.RCAttr + this.LSize * rNode.LCAttr;

            return this.matrix[pos] + rNode.WCost;
        }

        #endregion

        #region Dispose

        private bool disposed;

        /// <summary>
        /// 使用中のリソースを開放する
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
            }

            this.disposed = true;
        }

        ~Connector()
        {
            Dispose(false);
        }

        #endregion
    }
}
