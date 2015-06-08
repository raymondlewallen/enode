﻿using System.Threading.Tasks;
using ECommon.Logging;
using ECommon.IO;

namespace ENode.Infrastructure.Impl
{
    public abstract class AbstractSequenceProcessingMessageHandler<X, Y, Z> : IProcessingMessageHandler<X, Y, Z>
        where X : class, IProcessingMessage<X, Y, Z>, ISequenceProcessingMessage
        where Y : ISequenceMessage
    {
        #region Private Variables

        private readonly ISequenceMessagePublishedVersionStore _publishedVersionStore;
        private readonly IOHelper _ioHelper;
        private readonly ILogger _logger;

        #endregion

        public abstract string Name { get; }

        #region Constructors

        public AbstractSequenceProcessingMessageHandler(ISequenceMessagePublishedVersionStore publishedVersionStore, IOHelper ioHelper, ILoggerFactory loggerFactory)
        {
            _publishedVersionStore = publishedVersionStore;
            _ioHelper = ioHelper;
            _logger = loggerFactory.Create(GetType().FullName);
        }

        #endregion

        protected abstract Task<AsyncTaskResult> DispatchProcessingMessageAsync(X processingMessage);

        public void HandleAsync(X processingMessage)
        {
            HandleMessageAsync(processingMessage, 0);
        }

        private void HandleMessageAsync(X processingMessage, int retryTimes)
        {
            var message = processingMessage.Message;

            _ioHelper.TryAsyncActionRecursively<AsyncTaskResult<int>>("GetPublishedVersionAsync",
            () => _publishedVersionStore.GetPublishedVersionAsync(Name, message.AggregateRootTypeCode, message.AggregateRootId),
            currentRetryTimes => HandleMessageAsync(processingMessage, currentRetryTimes),
            result =>
            {
                var publishedVersion = result.Data;
                if (publishedVersion + 1 == message.Version)
                {
                    DispatchProcessingMessageAsync(processingMessage, 0);
                }
                else if (publishedVersion + 1 < message.Version)
                {
                    _logger.DebugFormat("The sequence message cannot be process now as the version is not the next version, it will be handle later. contextInfo [aggregateRootId={0},lastPublishedVersion={1},messageVersion={2}]", message.AggregateRootId, publishedVersion, message.Version);
                    processingMessage.AddToWaitingList();
                }
                else
                {
                    processingMessage.Complete(default(Z));
                }
            },
            () => string.Format("sequence message [messageId:{0}, messageType:{1}, aggregateRootId:{2}, aggregateRootVersion:{3}]", message.Id, message.GetType().Name, message.AggregateRootId, message.Version),
            null,
            retryTimes,
            true);
        }
        private void DispatchProcessingMessageAsync(X processingMessage, int retryTimes)
        {
            _ioHelper.TryAsyncActionRecursively<AsyncTaskResult>("DispatchProcessingMessageAsync",
            () => DispatchProcessingMessageAsync(processingMessage),
            currentRetryTimes => DispatchProcessingMessageAsync(processingMessage, currentRetryTimes),
            result =>
            {
                UpdatePublishedVersionAsync(processingMessage, 0);
            },
            () => string.Format("sequence message [messageId:{0}, messageType:{1}, aggregateRootId:{2}, aggregateRootVersion:{3}]", processingMessage.Message.Id, processingMessage.Message.GetType().Name, processingMessage.Message.AggregateRootId, processingMessage.Message.Version),
            null,
            retryTimes,
            true);
        }
        private void UpdatePublishedVersionAsync(X processingMessage, int retryTimes)
        {
            _ioHelper.TryAsyncActionRecursively<AsyncTaskResult>("UpdatePublishedVersionAsync",
            () => _publishedVersionStore.UpdatePublishedVersionAsync(Name, processingMessage.Message.AggregateRootTypeCode, processingMessage.Message.AggregateRootId, processingMessage.Message.Version),
            currentRetryTimes => UpdatePublishedVersionAsync(processingMessage, currentRetryTimes),
            result =>
            {
                processingMessage.Complete(default(Z));
            },
            () => string.Format("sequence message [messageId:{0}, messageType:{1}, aggregateRootId:{2}, aggregateRootVersion:{3}]", processingMessage.Message.Id, processingMessage.Message.GetType().Name, processingMessage.Message.AggregateRootId, processingMessage.Message.Version),
            null,
            retryTimes,
            true);
        }
    }
}
