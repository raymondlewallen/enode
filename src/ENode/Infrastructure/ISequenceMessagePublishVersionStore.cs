﻿using System.Threading.Tasks;
using ECommon.IO;

namespace ENode.Infrastructure
{
    /// <summary>Represents a storage to store the sequence message published version of aggregate.
    /// </summary>
    public interface ISequenceMessagePublishedVersionStore
    {
        /// <summary>Update the published version for the given aggregate.
        /// </summary>
        Task<AsyncTaskResult> UpdatePublishedVersionAsync(string processorName, int aggregateRootTypeCode, string aggregateRootId, int publishedVersion);
        /// <summary>Get the current published version for the given aggregate.
        /// </summary>
        Task<AsyncTaskResult<int>> GetPublishedVersionAsync(string processorName, int aggregateRootTypeCode, string aggregateRootId);
    }
}
