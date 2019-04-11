﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2019, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Resources;

namespace Chem4Word.ACME.Utils
{
    public static class CursorUtils
    { // ---------------------------------------------------------------------------
      //  Copyright (c) 2019, The .NET Foundation.
      //  This software is released under the Apache License, Version 2.0.
      //  The license and further copyright text can be found in the file LICENSE.md
      //  at the root directory of the distribution.


        //see https://stackoverflow.com/questions/38377743/how-to-customize-and-add-cursor-files-to-a-project


        public static Cursor Eraser { get; }

        static CursorUtils()
        {
            Eraser = FromByteArray(Properties.Resources.Eraser);
        }
        public static Cursor FromByteArray(byte[] array)
        {
          using (MemoryStream memoryStream = new MemoryStream(array))
          {
              return new Cursor(memoryStream);
          }
        
        }
    }
}