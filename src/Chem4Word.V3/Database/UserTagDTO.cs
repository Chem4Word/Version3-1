﻿// ---------------------------------------------------------------------------
//  Copyright (c) 2023, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

namespace Chem4Word.Database
{
    public class UserTagDTO
    {
        public long Id { get; set; }
        public string Text { get; set; }
        public long Lock { get; set; }
    }
}