namespace AgoraVai.WebAPI.Models
{
    public sealed class JobsConfig
    {
        public int ProcessingBatchSize { get; init; }
        public int ProcessingParalellism { get; init; }
        public int ProcessingWaitMs { get; init; }
        public int PersistenceBatchSize { get; init; }
        public int PersistenceWaitMs { get; init; }
    }
}
