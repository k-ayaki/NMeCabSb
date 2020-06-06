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
            this.matrix = new short[contents.Length / 2];
            for (int i=0; i*2< contents.Length; i++)
            {
                matrix[i] = BitConverter.ToInt16(contents, i*2);
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
