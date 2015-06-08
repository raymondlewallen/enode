﻿using System;
using System.Collections.Generic;

namespace ENode.EQueue
{
    [Serializable]
    public class PublishableExceptionMessage
    {
        public string UniqueId { get; set; }
        public string AggregateRootId { get; set; }
        public int AggregateRootTypeCode { get; set; }
        public DateTime Timestamp { get; set; }
        public int ExceptionTypeCode { get; set; }
        public IDictionary<string, string> SerializableInfo { get; set; }
    }
}
