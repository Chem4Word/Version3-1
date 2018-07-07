// ---------------------------------------------------------------------------
//  Copyright (c) 2018, The .NET Foundation.
//  This software is released under the Apache License, Version 2.0.
//  The license and further copyright text can be found in the file LICENSE.md
//  at the root directory of the distribution.
// ---------------------------------------------------------------------------

using System.Collections.Generic;

namespace Chem4Word.Telemetry
{
    public class AzureServiceBusWriter
    {
        private readonly object _queueLock = new object();
        private Queue<MessageEntity> _buffer1 = new Queue<MessageEntity>();
        private bool _running = false;


    }
}